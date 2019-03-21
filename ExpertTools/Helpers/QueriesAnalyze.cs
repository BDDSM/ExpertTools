using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpertTools;
using ExpertTools.Model;
using System.Threading.Tasks.Dataflow;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace ExpertTools.Helpers
{
    /// <summary>
    /// Класс, содержащий методы для анализа запросов и контекста 1С
    /// </summary>
    public class QueriesAnalyze : ITlAnalyzer, ISqlAnalyzer
    {
        private const string NAME = "QueriesAnalyze";

        private const string CONTEXT_TEMP_FILENAME = "temp_tl_context.csv";
        private const string CONTEXT_TABLENAME = "QueriesAnalyzeTlContexts";

        private const string DBMSSQL_TEMP_FILENAME = "temp_tl_queries.csv";
        private const string DBMSSQL_TABLENAME = "QueriesAnalyzeTlQueries";

        private const string SQL_QUERIES_TEMP_FILENAME = "temp_sql_queries.csv";
        private const string SQL_QUERIES_TABLENAME = "QueriesAnalyzeSqlQueries";

        public async Task StartCollectTlData()
        {
            await TL.CreateLogCfg(NAME);

            await TL.WaitStartCollectData();
        }

        public void StopCollectTlData()
        {
            TL.DeleteLogCfg();
        }

        public async Task HandleTlData()
        {
            // Пути к выходным файлам
            var contextFilePath = Path.Combine(Config.TempFolder, CONTEXT_TEMP_FILENAME);
            var dbmssqlFilePath = Path.Combine(Config.TempFolder, DBMSSQL_TEMP_FILENAME);

            // Настройки параллельности для блока конвейера
            var parallelBlockOptions = new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount, BoundedCapacity = 1000 };

            using (var contextWriter = Common.GetOutputStream(contextFilePath))
            using (var dbmssqlWriter = Common.GetOutputStream(dbmssqlFilePath))
            {
                HashSet<EventDescription> events = new HashSet<EventDescription>();

                // Блоки с выходными потоками для обработанных данных
                var contextOutputBlock = new ActionBlock<string>(async (text) => await Common.WriteToOutputStream(text, contextWriter));
                var dbmssqlOutputBlock = new ActionBlock<string>(async (text) => await Common.WriteToOutputStream(text, dbmssqlWriter));

                // Блоки обработки данных
                var contextBlock = new ActionBlock<string>((text) => HandleContextEvent(text, contextOutputBlock), parallelBlockOptions);
                events.Add(new EventDescription("Context", contextBlock));

                var dbmssqlBlock = new ActionBlock<string>((text) => HandleDbmssqlEvent(text, dbmssqlOutputBlock), parallelBlockOptions);
                events.Add(new EventDescription("DBMSSQL", dbmssqlBlock));

                // Блок, читающий файл технологического лога
                var readBlock = new ActionBlock<string>((filePath) => TL.ReadFile(events, filePath), parallelBlockOptions);

                foreach (var file in TL.GetLogFilesPaths())
                {
                    await readBlock.SendAsync(file);
                }

                // Отметим блок как законченный
                readBlock.Complete();

                // Создадим связи для передачи готовности блоков по цепочке
                await readBlock.Completion.ContinueWith(c => dbmssqlBlock.Complete());
                await dbmssqlBlock.Completion.ContinueWith(c => dbmssqlOutputBlock.Complete());

                await readBlock.Completion.ContinueWith(c => contextBlock.Complete());
                await contextBlock.Completion.ContinueWith(c => contextOutputBlock.Complete());

                // Ожидания записи обработанных данных
                await contextOutputBlock.Completion;
                await dbmssqlOutputBlock.Completion;
            }
        }

        public async Task LoadTlDataIntoDatabase()
        {
            var contextFilePath = Path.Combine(Config.TempFolder, CONTEXT_TEMP_FILENAME);
            var dbmssqlFilePath = Path.Combine(Config.TempFolder, DBMSSQL_TEMP_FILENAME);

            await SQL.InsertFileIntoTable(contextFilePath, CONTEXT_TABLENAME);
            await SQL.InsertFileIntoTable(dbmssqlFilePath, DBMSSQL_TABLENAME);
        }

        public async Task StartCollectSqlData()
        {
            await SQL.StartCollectSqlData(NAME);
        }

        public async Task StopCollectSqlData()
        {
            await SQL.StopSession(NAME);
        }

        public async Task HandleSqlData()
        {
            // Путь к выходному файлу
            var queryFilePath = Path.Combine(Config.TempFolder, SQL_QUERIES_TEMP_FILENAME);

            // Настройки параллельности для блока конвейера
            var parallelBlockOptions = new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount, BoundedCapacity = 5 };

            using (var queryWriter = Common.GetOutputStream(queryFilePath))
            {
                // Блок с выходным потоком для обработанных данных
                var queryOutputBlock = new ActionBlock<string>(async (text) => await Common.WriteToOutputStream(text, queryWriter));

                // Блок, обрабатывающий события сессии XE
                var queryBlock = new ActionBlock<string>((xeEvent) => HandleQueryEvent(xeEvent, queryOutputBlock), parallelBlockOptions);

                // Путь к файлам трассировки
                var traceFilePath = Path.Combine(Config.SqlTraceFolder, $"{NAME}*.xel");

                using (var connection = await SQL.GetSqlConnection())
                using (var data = await SQL.GetEventsReader(connection, NAME))
                {
                    while(await data.ReadAsync())
                    {
                        await queryBlock.SendAsync(data.GetString(0));
                    }
                }

                // Отметим блок как законченный
                queryBlock.Complete();

                // Создадим связи для передачи готовности блоков по цепочке
                await queryBlock.Completion.ContinueWith(c => queryOutputBlock.Complete());

                // Ожидания записи обработанных данных
                await queryOutputBlock.Completion;
            }
        }

        public async Task LoadSqlDataIntoDatabase()
        {
            var contextFilePath = Path.Combine(Config.TempFolder, SQL_QUERIES_TEMP_FILENAME);

            await SQL.InsertFileIntoTable(contextFilePath, SQL_QUERIES_TABLENAME);

            // Группируем собранные данные
            using (var connection = await SQL.GetSqlConnection())
            {
                var command = connection.CreateCommand();
                var cmdText = SQL.ReplaceCommonQueryVariables(Properties.Resources.group_queries_avg);
                command.CommandText = cmdText;
                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Обарабатывает событие DBMSSQL и передает результат следующему блоку
        /// </summary>
        /// <param name="text">Текст события</param>
        /// <param name="targetBlock">Принимающий блок</param>
        /// <returns></returns>
        private async Task HandleDbmssqlEvent(string text, ITargetBlock<string> targetBlock)
        {
            var sql = await TL.ClearSql(text);

            // Бывают попадаются странные записи без свойств
            if (sql == string.Empty) return;

            var clientId = await TL.GetClientId(text);

            // Если clientId пустой, значит это системный вызов, такие события пропускаем
            if (clientId == string.Empty) return;

            var dateTime = TL.GetEventDateTime(text);
            var normalizedSql = await Common.GetNormalizedSql(sql);
            var user = await TL.GetUser(text);
            var context = await TL.GetContext(text);
            var firstLine = await TL.GetFirstLineContext(context);
            var lastLine = await TL.GetLastLineContext(context);
            var connectId = await TL.GetConnectId(text);
            var contextExists = firstLine.Length == 0 ? 0 : 1;
            var hash = await Common.GetMD5Hash(normalizedSql);

            var data = 
                Common.FS + 
                dateTime + 
                Common.FS + 
                user + 
                Common.FS + 
                connectId +
                Common.FS +
                clientId +
                Common.FS +
                sql +
                Common.FS + 
                normalizedSql + 
                Common.FS + 
                firstLine + 
                Common.FS + 
                lastLine + 
                Common.FS + 
                contextExists + 
                Common.FS + 
                hash + 
                Common.LS;

            await targetBlock.SendAsync(data);
        }

        /// <summary>
        /// Обарабатывает событие Context и передает результат следующему блоку
        /// </summary>
        /// <param name="text">Текст события</param>
        /// <param name="targetBlock">Принимающий блок</param>
        /// <returns></returns>
        private async Task HandleContextEvent(string text, ITargetBlock<string> targetBlock)
        {
            var clientId = await TL.GetClientId(text);

            // Если clientId пустой, значит это системный вызов, такие события пропускаем
            if (clientId == string.Empty) return;

            var dateTime = TL.GetEventDateTime(text);
            var context = await TL.GetContext(text);
            var firstLineContext = await TL.GetFirstLineContext(context);
            var lastLineContext = await TL.GetLastLineContext(context);
            var user = await TL.GetUser(text);
            var connectId = await TL.GetConnectId(text);

            var data = 
                Common.FS + 
                dateTime + 
                Common.FS + 
                user + 
                Common.FS + 
                connectId +
                Common.FS +
                clientId +
                Common.FS + 
                firstLineContext + 
                Common.FS + 
                lastLineContext + 
                Common.LS;

            await targetBlock.SendAsync(data);
        }

        /// <summary>
        /// Обрабатывает событие сессии XE
        /// </summary>
        /// <param name="xeEvent">Событие</param>
        /// <param name="targetBlock">Принимающий блок</param>
        /// <returns></returns>
        private async Task HandleQueryEvent(string xeEvent, ITargetBlock<string> targetBlock)
        {
            var doc = XDocument.Parse(xeEvent);

            var sql = await SQL.ClearSql((doc.Element("event").Attribute("name").Value == "rpc_completed" ? SQL.GetDataValue(doc, "statement") : SQL.GetDataValue(doc, "batch_text")));
            var normalized_sql = await Common.GetNormalizedSql(sql);
            var duration = SQL.GetDataValue(doc, "duration");
            var physical_reads = SQL.GetDataValue(doc, "physical_reads");
            var logical_reads = SQL.GetDataValue(doc, "logical_reads");
            var writes = SQL.GetDataValue(doc, "writes");
            var cpu_time = SQL.GetDataValue(doc, "cpu_time");
            var hash = await Common.GetMD5Hash(normalized_sql);

            await targetBlock.SendAsync(
                Common.FS +
                sql +
                Common.FS +  
                normalized_sql + 
                Common.FS + 
                duration + 
                Common.FS + 
                physical_reads + 
                Common.FS + 
                logical_reads + 
                Common.FS + 
                writes + 
                Common.FS +
                cpu_time +
                Common.FS +
                hash +
                Common.LS);
        }
    }
}
