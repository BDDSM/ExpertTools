using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;
using System.Linq;
using System.IO;

namespace ExpertTools.Core
{
    /// <summary>
    /// Represents an analyzer which collects data of the 1C:Enterprise technology log and the SQL trace,
    /// processes it and uploads into the database
    /// </summary>
    public class QueriesAnalyzer
    {
        private int _collectPeriod;
        private string _tempFolder;

        private const string CONTEXT_TEMP_FILENAME = "temp_tl_context.csv";
        private const string CONTEXT_TABLENAME = "QueriesAnalyzeTlContexts";

        private const string DBMSSQL_TEMP_FILENAME = "temp_tl_queries.csv";
        private const string DBMSSQL_TABLENAME = "QueriesAnalyzeTlQueries";

        public Logcfg LogCfg { get; private set; }

        /// <summary>
        /// Initialize a new QueriesAnalyzer class instance
        /// </summary>
        /// <param name="tlConfFolder">Parent folder of the "logcfg" file</param>
        /// <param name="tlFolder">Technology log folder</param>
        /// <param name="sqlFolder">SQL trace data folder</param>
        /// <param name="collectPeriod">Collection period (in minutes)</param>
        public QueriesAnalyzer(string tlConfFolder, string tlFolder, string sqlFolder, string tempFolder, int collectPeriod)
        {
            _collectPeriod = collectPeriod;
            _tempFolder = tempFolder;

            InitializeLogCfg(tlConfFolder, tlFolder, TLHelper.GetCollectPeriod(collectPeriod));
        }

        /// <summary>
        /// Initialize the LogCfg property
        /// </summary>
        /// <param name="tlConfFolder">Parent folder of the "logcfg" file</param>
        /// <param name="tlFolder">Technology log folder</param>
        /// <param name="history">Collection period (in hours)</param>
        private void InitializeLogCfg(string tlConfFolder, string tlFolder, int history)
        {
            LogCfg = new Logcfg(tlConfFolder);

            LogCfg.AddLog(history.ToString(), tlFolder);
            LogCfg.AddEvent(Logcfg.DBMSSQL_EV);
            LogCfg.AddProperty(Logcfg.USER_PR);
            LogCfg.AddProperty(Logcfg.CONTEXT_PR);
            LogCfg.AddProperty(Logcfg.CONNECT_ID_PR);
            LogCfg.AddProperty(Logcfg.SQL_PR);
            LogCfg.AddProperty(Logcfg.CLIENT_ID_PR);
        }

        /// <summary>
        /// Set filter by database
        /// </summary>
        /// <param name="database">Database name</param>
        public void SetDatabaseFilter(string database)
        {
            LogCfg.AddFilter(Logcfg.EQ_CT, Logcfg.PROCESS_NAME_PR, database);
        }

        /// <summary>
        /// Set filter by duration
        /// </summary>
        /// <param name="duration">Duration</param>
        public void SetDurationFilter(string duration)
        {
            LogCfg.AddFilter(Logcfg.EQ_CT, Logcfg.DURATION_PR, duration);
        }

        /// <summary>
        /// Run this analyzer
        /// </summary>
        /// <returns></returns>
        public async Task Run()
        {
            //await StartCollectTlData();
            //await Task.Delay(_collectPeriod * 60 * 1000);
            //StopCollectTlData();
            await HandleTlData();
        }

        /// <summary>
        /// Starts collect data of the technology log
        /// </summary>
        /// <returns></returns>
        private async Task StartCollectTlData()
        {
            await LogCfg.Write();
            await TLHelper.WaitStartCollectData(LogCfg);
        }

        /// <summary>
        /// Stops collect data of the technology log
        /// </summary>
        private void StopCollectTlData()
        {
            LogCfg.Delete();
        }

        /// <summary>
        /// Starts handling technology log data
        /// </summary>
        /// <returns></returns>
        public async Task HandleTlData()
        {
            // Paths to the output 
            var contextFilePath = Path.Combine(_tempFolder, CONTEXT_TEMP_FILENAME);
            var dbmssqlFilePath = Path.Combine(_tempFolder, DBMSSQL_TEMP_FILENAME);

            // Настройки параллельности для блока конвейера
            var parallelBlockOptions = new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount, BoundedCapacity = 1000 };

            using (var contextWriter = Common.GetOutputStream(contextFilePath))
            using (var dbmssqlWriter = Common.GetOutputStream(dbmssqlFilePath))
            {
                HashSet<(string eventName, ITargetBlock<string> nextBlock)> events = new HashSet<(string, ITargetBlock<string>)>();

                // Блоки с выходными потоками для обработанных данных
                var contextOutputBlock = new ActionBlock<string>(async (text) => await Common.WriteToOutputStream(text, contextWriter));
                var dbmssqlOutputBlock = new ActionBlock<string>(async (text) => await Common.WriteToOutputStream(text, dbmssqlWriter));

                // Блоки обработки данных
                var contextBlock = new ActionBlock<string>((text) => HandleContextEvent(text, contextOutputBlock), parallelBlockOptions);
                events.Add(("Context", contextBlock));

                var dbmssqlBlock = new ActionBlock<string>((text) => HandleDbmssqlEvent(text, dbmssqlOutputBlock), parallelBlockOptions);
                events.Add(("DBMSSQL", dbmssqlBlock));

                // Блок, читающий файл технологического лога
                var readBlock = new ActionBlock<string>((filePath) => TLHelper.ReadFile(events, filePath), parallelBlockOptions);

                foreach (var file in TLHelper.GetLogFiles(LogCfg))
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

        /// <summary>
        /// Processes a DBMSSQL event and send the result to the next block
        /// </summary>
        /// <param name="text">Event data</param>
        /// <param name="targetBlock">Next block</param>
        /// <returns></returns>
        private async Task HandleDbmssqlEvent(string text, ITargetBlock<string> targetBlock)
        {
            var sql = await TLHelper.GetPropertyValue(text, Logcfg.SQL_PR);

            // Бывают попадаются странные записи без свойств
            if (sql == string.Empty) return;

            var clientId = await TLHelper.GetPropertyValue(text, Logcfg.CLIENT_ID_PR);

            // Если clientId пустой, значит это системный вызов, такие события пропускаем
            if (clientId == string.Empty) return;

            var clearedSql = await TLHelper.ClearSql(sql);
            var normalizedSql = await Common.GetNormalizedSql(clearedSql);
            var hash = await Common.GetMD5Hash(normalizedSql);
            var dateTime = await TLHelper.GetPropertyValue(text, Logcfg.DATETIME_PR);
            var user = await TLHelper.GetPropertyValue(text, Logcfg.USER_PR);
            var context = await TLHelper.GetPropertyValue(text, Logcfg.CONTEXT_PR);
            var firstLine = await TLHelper.GetFirstLineContext(context);
            var lastLine = await TLHelper.GetLastLineContext(context);
            var connectId = await TLHelper.GetPropertyValue(text, Logcfg.CONNECT_ID_PR);
            var contextExists = firstLine.Length == 0 ? 0 : 1;

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
        /// Processes a Context event and send the result to the next block
        /// </summary>
        /// <param name="text">Event data</param>
        /// <param name="targetBlock">Next block</param>
        /// <returns></returns>
        private async Task HandleContextEvent(string text, ITargetBlock<string> targetBlock)
        {
            var clientId = await TLHelper.GetPropertyValue(text, Logcfg.CLIENT_ID_PR);

            // Если clientId пустой, значит это системный вызов, такие события пропускаем
            if (clientId == string.Empty) return;

            var dateTime = TLHelper.GetEventDateTime(text);
            var context = await TLHelper.GetPropertyValue(text, Logcfg.CONTEXT_PR);
            var firstLineContext = await TLHelper.GetFirstLineContext(context);
            var lastLineContext = await TLHelper.GetLastLineContext(context);
            var user = await TLHelper.GetPropertyValue(text, Logcfg.USER_PR);
            var connectId = await TLHelper.GetPropertyValue(text, Logcfg.CONNECT_ID_PR);

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
    }
}
