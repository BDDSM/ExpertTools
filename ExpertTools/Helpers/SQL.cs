using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Xml.Linq;
using System.Threading.Tasks.Dataflow;
using System.Text.RegularExpressions;
using System.Text;

namespace ExpertTools.Helpers
{
    public static class SQL
    {
        public const string SESSION_FORJOIN_NAME = "FORJOIN";

        #region Common

        /// <summary>
        /// Возвращает соединение с windows аутентификацией
        /// </summary>
        /// <param name="server">Адрес сервера</param>
        /// <returns></returns>
        public static SqlConnection GetSqlConnection()
        {
            var server = Config.Get<string>("SqlServer");
            var windowsAuth = Config.Get<bool>("WindowsAuthentication");

            var connectionStringBuilder = new SqlConnectionStringBuilder
            {
                DataSource = server
            };

            if (windowsAuth)
            {
                connectionStringBuilder.IntegratedSecurity = true;
            }
            else
            {
                connectionStringBuilder.UserID = Config.Get<string>("SqlUser");
                connectionStringBuilder.Password = Config.Get<string>("SqlPassword");
            }

            return new SqlConnection(connectionStringBuilder.ToString());
        }

        /// <summary>
        /// Создает базу данных приложения, если она отсутствует
        /// </summary>
        /// <param name="connection">SQL соединение</param>
        /// <returns></returns>
        public static async Task CreateDatabaseIfNotExists(SqlConnection connection)
        {
            var command = connection.CreateCommand();

            var databaseExistsText = ReplaceCommonQueryVariables(Properties.Resources.database_exists);
            command.CommandText = databaseExistsText;
            var exists = await command.ExecuteScalarAsync() != null;

            // Если база уже существует, тогда не создаем структуру
            if (exists)
            {
                return;
            }

            var createDbText = ReplaceCommonQueryVariables(Properties.Resources.create_database);
            command.CommandText = createDbText;
            await command.ExecuteNonQueryAsync();

            var createDbStructureText = ReplaceCommonQueryVariables(Properties.Resources.create_database_structure);
            command.CommandText = createDbStructureText;
            await command.ExecuteNonQueryAsync();

        }

        /// <summary>
        /// Удаляет базу данных приложения
        /// </summary>
        /// <param name="connection">SQL соединение</param>
        /// <returns></returns>
        public static async Task DropDatabase(SqlConnection connection)
        {
            var command = connection.CreateCommand();

            var commandText = ReplaceCommonQueryVariables(Properties.Resources.drop_database);
            command.CommandText = commandText;

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Запускает сессию Extended Events
        /// </summary>
        /// <param name="connection">SQL соединение</param>
        /// <param name="sessionName">Имя сеанса</param>
        /// <returns></returns>
        public static async Task StartSession(SqlConnection connection, string sessionName)
        {
            var command = connection.CreateCommand();

            var commandText = ReplaceCommonQueryVariables(Properties.Resources.start_session);
            commandText = ReplaceQueryVariable(commandText, "SESSION_NAME", sessionName);

            command.CommandText = commandText;

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Загружает CSV файл в таблицу
        /// </summary>
        /// <param name="connection">SQL соединение</param>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns></returns>
        public static async Task InsertFileIntoTable(SqlConnection connection, string filePath, string tableName)
        {
            var command = connection.CreateCommand();

            var commandText = ReplaceCommonQueryVariables(Properties.Resources.bulk_insert);
            commandText = ReplaceQueryVariable(commandText, "BULK_FILE_PATH", filePath);
            commandText = ReplaceQueryVariable(commandText, "TABLE_NAME", tableName);

            command.CommandText = commandText;

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Останавливает сессию Extended Events
        /// </summary>
        /// <param name="connection">SQL соединение</param>
        /// <param name="sessionName">Имя сеанса</param>
        /// <returns></returns>
        public static async Task StopSession(SqlConnection connection, string sessionName)
        {
            var command = connection.CreateCommand();

            var commandText = ReplaceCommonQueryVariables(Properties.Resources.stop_session);
            commandText = ReplaceQueryVariable(commandText, "SESSION_NAME", sessionName);

            command.CommandText = commandText;

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Предоставляет поток для чтения событи Extended Events
        /// </summary>
        /// <param name="connection">SQL соединение</param>
        /// <param name="logDirectoryPath">Каталог логов Extended Events</param>
        /// <returns></returns>
        private static async Task<SqlDataReader> GetEventsReader(SqlConnection connection)
        {
            var sqlTraceFolder = Config.Get<string>("SqlTraceFolder");
            var filePath = Path.Combine(sqlTraceFolder, "*.xel");

            var command = connection.CreateCommand();

            var commandText = ReplaceCommonQueryVariables(Properties.Resources.get_session_events);
            commandText = ReplaceQueryVariable(commandText, "SESSION_FILES_PATH", filePath);

            command.CommandText = commandText;

            return await command.ExecuteReaderAsync();
        }

        /// <summary>
        /// Возвращает значение элемента Data события Extended Events
        /// </summary>
        /// <param name="document">XDocument объект с данными события</param>
        /// <param name="dataName">Имя элемента Data</param>
        /// <returns>Значение элемента Data</returns>
        private static string GetDataValue(XDocument document, string dataName)
        {
            var value = document.Element("event").Elements("data").FirstOrDefault(c => c.Attribute("name").Value == dataName).Element("value").Value;

            return value ?? "";
        }

        /// <summary>
        /// Возвращает значение элемента Action события Extended Events
        /// </summary>
        /// <param name="document">XDocument объект с данными события</param>
        /// <param name="dataName">Имя элемента Action</param>
        /// <returns>Значение элемента Action</returns>
        private static string GetActionValue(XDocument document, string actionName)
        {
            var value = document.Element("event").Elements("action").FirstOrDefault(c => c.Attribute("name").Value == actionName).Element("value").Value;

            return value ?? "";
        }

        /// <summary>
        /// Заменяет переменную формата [NAME] на переданное значение
        /// </summary>
        /// <param name="query">Обрабатываемый текст</param>
        /// <param name="variable">Название переменной</param>
        /// <param name="value">Подсавляемое значение переменной</param>
        /// <returns></returns>
        private static string ReplaceQueryVariable(string query, string variable, string value)
        {
            return query.Replace($"[{variable}]", value);
        }

        /// <summary>
        /// Заменяет общие для всех запросов значения переменных
        /// </summary>
        /// <param name="query">Обрабатываемый текст</param>
        /// <returns></returns>
        private static string ReplaceCommonQueryVariables(string query)
        {
            var applicationDatabase = Config.Get<string>("ApplicationDatabase");

            var result = query;

            result = ReplaceQueryVariable(result, "FIELD_SEPARATOR",Common.FS);
            result = ReplaceQueryVariable(result, "ROW_SEPARATOR", Common.LS);
            result = ReplaceQueryVariable(result, "DATABASE_NAME", applicationDatabase);

            return result;
        }

        /// <summary>
        /// Выполняет загрузку нормализованного технологического журнала в базу данных
        /// </summary>
        /// <returns></returns>
        public static async Task LoadTechLogIntoDatabase()
        {
            var tempFolder = Config.Get<string>("TempFolder");
            var filePath = Path.Combine(tempFolder, "normalized_tech_log.csv");

            using (var connection = GetSqlConnection())
            {
                await connection.OpenAsync();

                await InsertFileIntoTable(connection, filePath, "Tldbmssql");
            }
        }

        /// <summary>
        /// Запускает сбор Extended Events
        /// </summary>
        /// <returns></returns>
        public static async Task StartCollectSqlTrace()
        {
            using (var connection = GetSqlConnection())
            {
                await connection.OpenAsync();

                await CreateSessionForJoin(connection);
                await StartSession(connection, SESSION_FORJOIN_NAME);
            }
        }

        /// <summary>
        /// Останаливает сбор Extended Events
        /// </summary>
        /// <returns></returns>
        public static async Task StopCollectSqlTrace()
        {
            using (var connection = GetSqlConnection())
            {
                await connection.OpenAsync();

                await StopSession(connection, SESSION_FORJOIN_NAME);
            }
        }

        /// <summary>
        /// Выполняет обработку трассировки Extended Events
        /// </summary>
        /// <returns></returns>
        public static async Task ProcessSqlTrace()
        {
            var tempFolder = Config.Get<string>("TempFolder");
            var filePath = Path.Combine(tempFolder, "normalized_sql_trace.csv");

            using (var filestream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var writer = new StreamWriter(filestream, Encoding.GetEncoding(1251)))
            using (var connection = GetSqlConnection())
            {
                await connection.OpenAsync();

                await NormalizeEventsForJoin(connection, writer);
            }
        }

        /// <summary>
        /// Выполняет загрузку нормализованной трассировки в таблицу СУБД
        /// </summary>
        /// <returns></returns>
        public static async Task LoadSqlTraceIntoDatabase()
        {
            var tempFolder = Config.Get<string>("TempFolder");
            var filePath = Path.Combine(tempFolder, "normalized_sql_trace.csv");

            using (var connection = GetSqlConnection())
            {
                await connection.OpenAsync();

                await InsertFileIntoTable(connection, filePath, "Sqlqueries");
            }
        }

        #endregion

        #region JoinWithTechLog

        /// <summary>
        /// Создает сессию Extended Events собирающую данные для соединения с ТЖ
        /// </summary>
        /// <param name="connection">SQL соединение</param>
        /// <param name="SqlTraceFolder">Каталог трассировки</param>
        /// <returns></returns>
        public static async Task CreateSessionForJoin(SqlConnection connection)
        {
            var sqlTraceFolder = Config.Get<string>("SqlTraceFolder");
            var filePath = Path.Combine(sqlTraceFolder, SESSION_FORJOIN_NAME + ".xel");
            var filterByDatabase = Config.Get<bool>("FilterByDatabase");

            // Удаляем старую сессию
            var deleteCommand = connection.CreateCommand();

            var commandText = ReplaceCommonQueryVariables(Properties.Resources.delete_session);
            commandText = ReplaceQueryVariable(commandText, "SESSION_NAME", SESSION_FORJOIN_NAME);

            deleteCommand.CommandText = commandText;

            await deleteCommand.ExecuteNonQueryAsync();

            // Создаем новую сессию
            var createCommand = connection.CreateCommand();

            if (filterByDatabase)
            {
                var databaseSql = Config.Get<string>("DatabaseSql");

                commandText = ReplaceCommonQueryVariables(Properties.Resources.create_session_for_join_filter);
                commandText = ReplaceQueryVariable(commandText, "DATABASE_NAME_FILTER", databaseSql);
            }
            else
            {
                commandText = ReplaceCommonQueryVariables(Properties.Resources.create_session_for_join);
            }

            commandText = ReplaceQueryVariable(commandText, "SESSION_NAME", SESSION_FORJOIN_NAME);
            commandText = ReplaceQueryVariable(commandText, "EVENT_FILE_PATH", filePath);

            createCommand.CommandText = commandText;

            await createCommand.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Заускает процесс нормализации ТЖ и записывает данные в выходной поток
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="logDirectoryPath"></param>
        /// <param name="outputStream"></param>
        /// <returns></returns>
        public static async Task NormalizeEventsForJoin(SqlConnection connection, StreamWriter outputStream)
        {
            var blockOptions = new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };

            var writeToOutputStream = new ActionBlock<string>(text => Common.WriteToOutputStream(text, outputStream));

            var normalizeEventForJoin = new TransformBlock<string, string>(NormalizeEventForJoin, blockOptions);

            normalizeEventForJoin.LinkTo(writeToOutputStream, new DataflowLinkOptions() { PropagateCompletion = true });

            var reader = await GetEventsReader(connection);

            while (await reader.ReadAsync())
            {
                var data = (string)reader.GetValue(0);

                await normalizeEventForJoin.SendAsync(data);
            }

            normalizeEventForJoin.Complete();

            await writeToOutputStream.Completion;
        }

        /// <summary>
        /// Нормализует данные события ТЖ
        /// </summary>
        /// <param name="data">Данные события</param>
        /// <returns></returns>
        private static async Task<string> NormalizeEventForJoin(string data)
        {
            var document = XDocument.Parse(data);

            var eventType = document.Element("event").Attribute("name").Value;
            var dateTime = document.Element("event").Attribute("timestamp").Value;

            string sql = "";

            if (eventType == "rpc_completed")
            {
                sql = GetDataValue(document, "statement");
            }
            else
            {
                sql = GetDataValue(document, "batch_text");
            }

            var normalizedSQl = await Common.GetNormalizedSql(sql);
            var duration = GetDataValue(document, "duration");
            var physical_reads = GetDataValue(document, "physical_reads");
            var logical_reads = GetDataValue(document, "logical_reads");
            var writes = GetDataValue(document, "writes");
            var cpu_time = GetDataValue(document, "cpu_time");

            var hash = await Common.GetMD5Hash(normalizedSQl);

            return 
                "\n" +
                Common.FS +
                sql +
                Common.FS +
                normalizedSQl + 
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
                Common.LS;
        }

        #endregion
    }
}
