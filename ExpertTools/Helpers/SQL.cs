using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Xml.Linq;
using System.Threading.Tasks.Dataflow;
using System.Text.RegularExpressions;
using System.Text;
using System.Data;

namespace ExpertTools.Helpers
{
    public static class SQL
    {
        /// <summary>
        /// Возвращает соединение с windows аутентификацией
        /// </summary>
        /// <param name="server">Адрес сервера</param>
        /// <returns></returns>
        public static async Task<SqlConnection> GetSqlConnection()
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder
            {
                DataSource = Config.SqlServer
            };

            if (Config.WindowsAuthentication)
            {
                connectionStringBuilder.IntegratedSecurity = true;
            }
            else
            {
                connectionStringBuilder.UserID = Config.SqlUser;
                connectionStringBuilder.Password = Config.SqlPassword;
            }

            var connection = new SqlConnection(connectionStringBuilder.ToString());
            await connection.OpenAsync();

            return connection;
        }

        /// <summary>
        /// Создает базу данных приложения, если она отсутствует
        /// </summary>
        /// <returns></returns>
        public static async Task CreateDatabaseIfNotExists()
        {
            using (var connection = await GetSqlConnection())
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
        }

        /// <summary>
        /// Запускает сессию Extended Events
        /// </summary>
        /// <param name="connection">SQL соединение</param>
        /// <param name="sessionName">Имя сеанса</param>
        /// <returns></returns>
        public static async Task StartSession(string sessionName)
        {
            using (var connection = await GetSqlConnection())
            {
                var command = connection.CreateCommand();

                var commandText = ReplaceCommonQueryVariables(Properties.Resources.start_session);
                Common.SetVariableValue(ref commandText, "SESSION_NAME", sessionName);

                command.CommandText = commandText;

                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Загружает CSV файл в таблицу
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns></returns>
        public static async Task InsertFileIntoTable(string filePath, string tableName)
        {
            using (var connection = await GetSqlConnection())
            {
                var command = connection.CreateCommand();
                command.CommandTimeout = 600;

                var commandText = ReplaceCommonQueryVariables(Properties.Resources.bulk_insert);
                Common.SetVariableValue(ref commandText, "BULK_FILE_PATH", filePath);
                Common.SetVariableValue(ref commandText, "TABLE_NAME", tableName);

                command.CommandText = commandText;

                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Останавливает сессию Extended Events
        /// </summary>
        /// <param name="connection">SQL соединение</param>
        /// <param name="sessionName">Имя сеанса</param>
        /// <returns></returns>
        public static async Task StopSession(string sessionName)
        {
            using (var connection = await GetSqlConnection())
            {
                var command = connection.CreateCommand();

                var commandText = ReplaceCommonQueryVariables(Properties.Resources.stop_session);
                Common.SetVariableValue(ref commandText, "SESSION_NAME", sessionName);

                command.CommandText = commandText;

                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Предоставляет поток для чтения событи Extended Events
        /// </summary>
        /// <param name="connection">SQL соединение</param>
        /// <param name="sessionName">Имя сессии XE</param>
        /// <returns></returns>
        public static async Task<SqlDataReader> GetEventsReader(SqlConnection connection, string sessionName)
        {
            var filePath = Path.Combine(Config.SqlTraceFolder, $"{sessionName}*.xel");

            var command = connection.CreateCommand();

            var commandText = ReplaceCommonQueryVariables(Properties.Resources.get_session_events);
            Common.SetVariableValue(ref commandText, "SESSION_FILES_PATH", filePath);

            command.CommandText = commandText;

            return await command.ExecuteReaderAsync();
        }

        /// <summary>
        /// Возвращает значение элемента Data события Extended Events
        /// </summary>
        /// <param name="document">XDocument объект с данными события</param>
        /// <param name="dataName">Имя элемента Data</param>
        /// <returns>Значение элемента Data</returns>
        public static string GetDataValue(XDocument document, string dataName)
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
        public static string GetActionValue(XDocument document, string actionName)
        {
            var value = document.Element("event").Elements("action").FirstOrDefault(c => c.Attribute("name").Value == actionName).Element("value").Value;

            return value ?? "";
        }

        /// <summary>
        /// Заменяет общие для всех запросов значения переменных
        /// </summary>
        /// <param name="query">Обрабатываемый текст</param>
        /// <returns></returns>
        public static string ReplaceCommonQueryVariables(string query)
        {
            var result = query;

            Common.SetVariableValue(ref result, "FIELD_SEPARATOR",Common.FS);
            Common.SetVariableValue(ref result, "ROW_SEPARATOR", Common.LS);
            Common.SetVariableValue(ref result, "DATABASE_NAME", Config.ApplicationDatabase);

            return result;
        }

        /// <summary>
        /// Запускает сбор данных XE
        /// </summary>
        /// <returns></returns>
        public static async Task StartCollectSqlData(string sessionName)
        {
            await CreateSession(sessionName);
            await StartSession(sessionName);
        }

        private static string GetCreateSessionCommandText(string sessionName, string outputFilePath)
        {
            string command = "";

            if (Config.FilterByDatabase)
            {
                command = Properties.Resources.ResourceManager.GetString("Xe" + sessionName + "DbFilter");
                Common.SetVariableValue(ref command, "DATABASE_NAME_FILTER", Config.DatabaseSql);
            }
            else
            {
                command = Properties.Resources.ResourceManager.GetString("Xe" + sessionName);
            }

            Common.SetVariableValue(ref command, "SESSION_NAME", sessionName);
            Common.SetVariableValue(ref command, "EVENT_FILE_PATH", outputFilePath);

            return command;
        }

        /// <summary>
        /// Создает сессию Extended Events
        /// </summary>
        /// <param name="sessionName">Навание сессии XE</param>
        /// <returns></returns>
        public static async Task CreateSession(string sessionName)
        {
            using (var connection = await GetSqlConnection())
            {
                var filePath = Path.Combine(Config.SqlTraceFolder, sessionName + ".xel");

                // Удаляем старую сессию
                var deleteCommand = connection.CreateCommand();

                var commandText = ReplaceCommonQueryVariables(Properties.Resources.delete_session);
                Common.SetVariableValue(ref commandText, "SESSION_NAME", sessionName);

                deleteCommand.CommandText = commandText;

                await deleteCommand.ExecuteNonQueryAsync();

                // Создаем новую сессию
                var createCommand = connection.CreateCommand();
                createCommand.CommandText = GetCreateSessionCommandText(sessionName, filePath);

                await createCommand.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Возвращает очищенный текст запроса
        /// </summary>
        /// <param name="data">Строка запроса</param>
        /// <returns>Очищенный текст запроса</returns>
        public static async Task<string> ClearSql(string data)
        {
            string sql = data;

            int startIndex = sql.IndexOf("sp_executesql", StringComparison.OrdinalIgnoreCase);
            int endIndex;

            if (startIndex > 0)
            {
                sql = sql.Substring(startIndex + 16);

                endIndex = sql.IndexOf("',N'@P");

                if (endIndex > 0)
                {
                    sql = sql.Substring(0, endIndex);
                }
            }

            return await Task.FromResult(sql.Trim());
        }
    }
}
