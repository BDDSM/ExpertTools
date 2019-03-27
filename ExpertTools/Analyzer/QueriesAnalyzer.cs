using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;
using System.Linq;
using System.IO;

namespace ExpertTools
{
    /// <summary>
    /// Represents an analyzer which collects data of the 1C:Enterprise technology log and the SQL trace,
    /// processes it and uploads into the database
    /// </summary>
    public class QueriesAnalyzer
    {
        QueriesAnalyzerSettings _settings;
        Logcfg _logcfg;
        XESession _session;

        private string CONTEXT_TEMP_FILEPATH => Path.Combine(_settings.TempFolder, "temp_tl_context.csv");
        private const string CONTEXT_TABLENAME = "QueriesAnalyzeTlContexts";

        private string DBMSSQL_TEMP_FILEPATH => Path.Combine(_settings.TempFolder, "temp_tl_queries.csv");
        private const string DBMSSQL_TABLENAME = "QueriesAnalyzeTlQueries";

        private string SQL_QUERIES_TEMP_FILEPATH => Path.Combine(_settings.TempFolder, "temp_sql_queries.csv");
        private const string SQL_QUERIES_TABLENAME = "QueriesAnalyzeSqlQueries";

        /// <summary>
        /// Initialize a new QueriesAnalyzer class instance
        /// </summary>
        /// <param name="tlConfFolder">Parent folder of the "logcfg" file</param>
        /// <param name="tlFolder">Technology log folder</param>
        /// <param name="sqlFolder">SQL trace data folder</param>
        /// <param name="collectPeriod">Collection period (in minutes)</param>
        public QueriesAnalyzer(QueriesAnalyzerSettings settings)
        {
            _settings = settings;

            InitializeLogCfg();
            InitializeXESession();
        }

        /// <summary>
        /// Initializes the _logcfg variable
        /// </summary>
        private void InitializeLogCfg()
        {
            _logcfg = new Logcfg(_settings);

            _logcfg.AddLog(TlHelper.GetCollectPeriod(_settings.CollectPeriod).ToString(), _settings.TlFolder);
            _logcfg.AddEvent(Logcfg.DBMSSQL_EV);
            _logcfg.AddProperty(Logcfg.USER_PR);
            _logcfg.AddProperty(Logcfg.CONTEXT_PR);
            _logcfg.AddProperty(Logcfg.CONNECT_ID_PR);
            _logcfg.AddProperty(Logcfg.SQL_PR);
            _logcfg.AddProperty(Logcfg.CLIENT_ID_PR);

            if (_settings.FilterByDatabase)
            {
                _logcfg.AddFilter(Logcfg.EQ_CT, Logcfg.PROCESS_NAME_PR, _settings.Database1C);
            }

            if (_settings.FilterByDuration)
            {
                _logcfg.AddFilter(Logcfg.GE_CT, Logcfg.DURATION_PR, _settings.Duration.ToString());
            }
        }

        /// <summary>
        /// Initializes the _session variable
        /// </summary>
        private void InitializeXESession()
        {
            _session = new XESession(_settings);

            _session.AddEvent(XESession.RPC_COMPLETED_EV);
            _session.AddEvent(XESession.SQL_BATCH_COMPLETED_EV);

            if (_settings.FilterByDatabase)
            {
                _session.AddFilter(XESession.DATABASE_NAME_F, XESession.EQ_CT, _settings.DatabaseSQL);
            }

            if (_settings.FilterByDuration)
            {
                _session.AddFilter(XESession.DURATION_F, XESession.GE_CT, _settings.Duration.ToString());
            }

            _session.SetTargetFile(_settings.SqlTraceFolder);
        }

        /// <summary>
        /// Run this analyzer
        /// </summary>
        /// <returns></returns>
        public async Task Run()
        {
            try
            {
                Logger.Log("Start the queries analyze");

                Logger.Log("Check settings");

                await _settings.Check();

                Logger.Log("Create the database");

                await SqlHelper.CreateDatabase(_settings);

                Logger.Log("Clean the folders");

                Common.CleanFolder(_settings.TlFolder);
                Common.CleanFolder(_settings.SqlTraceFolder);
                Common.CleanFolder(_settings.TempFolder);

                Logger.Log("Start the data collection");

                await _session.Create();
                await _logcfg.Write();

                Logger.Log("Wait while the data collection will be started...");

                await TlHelper.WaitStartCollectData(_logcfg);
                await _session.Start();

                Logger.Log("Wait while the data collection is going...");

                // Wait while the data collection is going
                await Task.Delay(_settings.CollectPeriod * 60 * 1000);

                Logger.Log("Stop the data collection");

                _logcfg.Delete();
                await _session.Stop();

                Logger.Log("Process the tech log data");
                await ProcessTlData();
                Logger.Log("Process the extended events data");
                await ProcessXESessionData();
                await _session.Delete();

                Logger.Log("Insert data into the database");

                await SqlHelper.InsertFileIntoTable(_settings, SQL_QUERIES_TABLENAME, SQL_QUERIES_TEMP_FILEPATH);
                await SqlHelper.InsertFileIntoTable(_settings, CONTEXT_TABLENAME, CONTEXT_TEMP_FILEPATH);
                await SqlHelper.InsertFileIntoTable(_settings, DBMSSQL_TABLENAME, DBMSSQL_TEMP_FILEPATH);

                await FillContext();
                await FillQueriesAvgTable();

                Logger.Log("Analyze was successfully completed");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                Logger.Log("Analyze was completed with errors");
                throw new Exception();
            }
        }

        /// <summary>
        /// Starts the processing data of the technology log
        /// </summary>
        /// <returns></returns>
        private async Task ProcessTlData()
        {
            // Options of the many-threads blocks
            // Limit a max bounded capacity to improve a consumption of memory
            var parallelBlockOptions = new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount, BoundedCapacity = 1000 };

            using (var contextWriter = Common.GetOutputStream(CONTEXT_TEMP_FILEPATH))
            using (var dbmssqlWriter = Common.GetOutputStream(DBMSSQL_TEMP_FILEPATH))
            {
                HashSet<(string eventName, ITargetBlock<string> nextBlock)> events = new HashSet<(string, ITargetBlock<string>)>();

                // Blocks with the output streams for processed data
                var contextOutputBlock = new ActionBlock<string>(async (text) => await Common.WriteToOutputStream(text, contextWriter));
                var dbmssqlOutputBlock = new ActionBlock<string>(async (text) => await Common.WriteToOutputStream(text, dbmssqlWriter));

                // Blocks of the processing data
                var contextBlock = new ActionBlock<string>((text) => ProcessContextEvent(text, contextOutputBlock), parallelBlockOptions);
                events.Add(("Context", contextBlock));

                var dbmssqlBlock = new ActionBlock<string>((text) => ProcessDbmssqlEvent(text, dbmssqlOutputBlock), parallelBlockOptions);
                events.Add(("DBMSSQL", dbmssqlBlock));

                // Reading tech log block
                var readBlock = new ActionBlock<string>((filePath) => TlHelper.ReadFile(events, filePath), parallelBlockOptions);

                foreach (var file in TlHelper.GetLogFiles(_logcfg))
                {
                    await readBlock.SendAsync(file);
                }

                // Mark block as completed
                readBlock.Complete();

                // Create relations between blocks (signals "complete" to the next blocks)
                await readBlock.Completion.ContinueWith(c => dbmssqlBlock.Complete());
                await dbmssqlBlock.Completion.ContinueWith(c => dbmssqlOutputBlock.Complete());

                await readBlock.Completion.ContinueWith(c => contextBlock.Complete());
                await contextBlock.Completion.ContinueWith(c => contextOutputBlock.Complete());

                // Wait writing processed data to the temp files
                await contextOutputBlock.Completion;
                await dbmssqlOutputBlock.Completion;
            }
        }

        private async Task ProcessXESessionData()
        {
            // Настройки параллельности для блока конвейера
            var parallelBlockOptions = new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount, BoundedCapacity = 5 };

            using (var queryWriter = Common.GetOutputStream(SQL_QUERIES_TEMP_FILEPATH))
            {
                // Блок с выходным потоком для обработанных данных
                var queryOutputBlock = new ActionBlock<string>(async (text) => await Common.WriteToOutputStream(text, queryWriter));

                // Блок, обрабатывающий события сессии XE
                var queryBlock = new ActionBlock<string>((text) => ProcessQueryEvent(text, queryOutputBlock), parallelBlockOptions);

                using (var data = await _session.GetEventsReader())
                {
                    while (await data.ReadAsync())
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

        /// <summary>
        /// Processes a DBMSSQL event and send the result to the next block
        /// </summary>
        /// <param name="text">Event data</param>
        /// <param name="targetBlock">Next block</param>
        /// <returns></returns>
        private async Task ProcessDbmssqlEvent(string text, ITargetBlock<string> targetBlock)
        {
            var user = await TlHelper.GetPropertyValue(text, Logcfg.USER_PR);

            // If "usr" property is empty then skip this event
            if (user == string.Empty) return;

            var sql = await TlHelper.GetPropertyValue(text, Logcfg.SQL_PR);

            // If "sql" property is empty then skip this event
            if (sql == string.Empty) return;

            var clientId = await TlHelper.GetPropertyValue(text, Logcfg.CLIENT_ID_PR);

            // If "client id" property is empty then it`s a system call, such event skipping
            if (clientId == string.Empty) return;

            var clearedSql = await TlHelper.CleanSql(sql);
            var normalizedSql = await Common.GetNormalizedSql(clearedSql);
            var hash = await Common.GetMD5Hash(normalizedSql);
            var dateTime = await TlHelper.GetPropertyValue(text, Logcfg.DATETIME_PR);
            var context = await TlHelper.GetPropertyValue(text, Logcfg.CONTEXT_PR);
            var firstLine = await TlHelper.GetFirstLineContext(context);
            var lastLine = await TlHelper.GetLastLineContext(context);
            var connectId = await TlHelper.GetPropertyValue(text, Logcfg.CONNECT_ID_PR);
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
                Common.RS;

            await targetBlock.SendAsync(data);
        }

        /// <summary>
        /// Processes a Context event and send the result to the next block
        /// </summary>
        /// <param name="text">Event data</param>
        /// <param name="targetBlock">Next block</param>
        /// <returns></returns>
        private async Task ProcessContextEvent(string text, ITargetBlock<string> targetBlock)
        {
            var clientId = await TlHelper.GetPropertyValue(text, Logcfg.CLIENT_ID_PR);

            // If "client id" property is empty then it`s a system call, such event skipping
            if (clientId == string.Empty) return;

            var dateTime = await TlHelper.GetPropertyValue(text, Logcfg.DATETIME_PR);
            var context = await TlHelper.GetPropertyValue(text, Logcfg.CONTEXT_PR);
            var firstLineContext = await TlHelper.GetFirstLineContext(context);
            var lastLineContext = await TlHelper.GetLastLineContext(context);
            var user = await TlHelper.GetPropertyValue(text, Logcfg.USER_PR);
            var connectId = await TlHelper.GetPropertyValue(text, Logcfg.CONNECT_ID_PR);

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
                Common.RS;

            await targetBlock.SendAsync(data);
        }

        /// <summary>
        /// Processes the rpc_completed and the sql_batch_completed events and send the result to the next block
        /// </summary>
        /// <param name="text">Event data</param>
        /// <param name="targetBlock">Next block</param>
        /// <returns></returns>
        private async Task ProcessQueryEvent(string text, ITargetBlock<string> targetBlock)
        {
            var doc = XDocument.Parse(text);

            var sql = doc.Element("event").Attribute("name").Value == "rpc_completed" ? SqlHelper.GetDataValue(doc, "statement") : SqlHelper.GetDataValue(doc, "batch_text");
            var clearedSql = await SqlHelper.CleanSql(sql);
            var normalizedSql = await Common.GetNormalizedSql(clearedSql);
            var duration = SqlHelper.GetDataValue(doc, "duration");
            var physical_reads = SqlHelper.GetDataValue(doc, "physical_reads");
            var logical_reads = SqlHelper.GetDataValue(doc, "logical_reads");
            var writes = SqlHelper.GetDataValue(doc, "writes");
            var cpu_time = SqlHelper.GetDataValue(doc, "cpu_time");
            var hash = await Common.GetMD5Hash(normalizedSql);

            await targetBlock.SendAsync(
                Common.FS +
                sql +
                Common.FS +
                normalizedSql +
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
                Common.RS);
        }

        private async Task FillQueriesAvgTable()
        {
            using (var connection = await SqlHelper.GetSqlConnection(_settings))
            {
                var command = connection.CreateCommand();

                command.CommandText = Properties.Resources.FillQueriesAvg;

                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task FillContext()
        {
            using (var connection = await SqlHelper.GetSqlConnection(_settings))
            {
                var command = connection.CreateCommand();

                command.CommandText = Properties.Resources.FillContext;

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
