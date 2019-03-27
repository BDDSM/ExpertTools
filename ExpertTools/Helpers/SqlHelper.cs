using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Xml.Linq;
using System.Linq;

namespace ExpertTools
{
    /// <summary>
    /// Provides the static methods for working with DBMS
    /// <summary>
    public static class SqlHelper
    {
        /// <summary>
        /// Creates, opens and returns the sql connection
        /// To use with the "using" operator
        /// </summary>
        public static async Task<SqlConnection> GetSqlConnection(ISqlAnalyzerSettings analyzerSettings)
        {
            var str = GetSqlConnectionStringBuilder(analyzerSettings);

            var connection = new SqlConnection(str.ToString());
            await connection.OpenAsync();

            return connection;
        }

        /// <summary>
        /// Creates, opens and returns the sql connection
        /// To use with the "using" operator
        /// </summary>
        public static async Task<SqlConnection> GetSqlConnection(SqlConnectionStringBuilder connStrBuilder)
        {
            var connection = new SqlConnection(connStrBuilder.ToString());
            await connection.OpenAsync();

            return connection;
        }

        /// <summary>
        /// Checks connection with the sql server instance
        /// </summary>
        /// <param name="analyzerSettings">Settings of the analyzer</param>
        /// <returns></returns>
        public static async Task CheckConnection(ISqlAnalyzerSettings analyzerSettings)
        {
            using (var connection = await GetSqlConnection(analyzerSettings))
            {
                
            }
        }

        /// <summary>
        /// Check the database existence
        /// </summary>
        /// <param name="analyzerSettings">Settings of the analyzer</param>
        /// <returns></returns>
        public static async Task CheckDatabase(ISqlAnalyzerSettings analyzerSettings)
        {
            using (var connection = await GetSqlConnection(analyzerSettings))
            {
                var crCmd = connection.CreateCommand();

                crCmd.CommandText = "SELECT * FROM sys.databases WHERE name = 'ExpertTools'";

                if (await crCmd.ExecuteScalarAsync() != null)
                {
                    throw new Exception("Database already exists! Please, delete the application database before running this script!");
                }
            }   
        }

        /// <summary>
        /// Creates the application database
        /// </summary>
        /// <param name="analyzerSettings">Settings of the analyzer</param>
        /// <returns></returns>
        public static async Task CreateDatabase(ISqlAnalyzerSettings analyzerSettings)
        {
            using (var connection = await GetSqlConnection(analyzerSettings))
            {
                var crCmd = connection.CreateCommand();
                crCmd.CommandText = "CREATE DATABASE ExpertTools";
                await crCmd.ExecuteNonQueryAsync();

                var crsCmd = connection.CreateCommand();
                crsCmd.CommandText = Properties.Resources.CreateDatabase;
                await crsCmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Returns a value of the "Data" element
        /// </summary>
        /// <param name="document">Instance of the XDocument class with a data of the event</param>
        /// <param name="dataName">Name of the "Data" element</param>
        /// <returns>Value of the "Data" element</returns>
        public static string GetDataValue(XDocument document, string dataName)
        {
            var value = document.Element("event").Elements("data").FirstOrDefault(c => c.Attribute("name").Value == dataName).Element("value").Value;

            return value ?? "";
        }

        /// <summary>
        /// Returns a value of the "Action" element
        /// </summary>
        /// <param name="document">Instance of the XDocument class with a data of the event</param>
        /// <param name="dataName">Name of the "Action" element</param>
        /// <returns>Value of the "Action" element</returns>
        public static string GetActionValue(XDocument document, string actionName)
        {
            var value = document.Element("event").Elements("action").FirstOrDefault(c => c.Attribute("name").Value == actionName).Element("value").Value;

            return value ?? "";
        }

        /// <summary>
        /// Inserts a data of the CSV file into the table of the database
        /// </summary>
        /// <param name="analyzerSettings">Settings of the analyzer</param>
        /// <param name="tableName">Table name of the database</param>
        /// <param name="filePath">Path to the "*.xel" files</param>
        /// <returns></returns>
        public static async Task InsertFileIntoTable(ISqlAnalyzerSettings analyzerSettings, string tableName, string filePath)
        {
            using (var connection = await GetSqlConnection(analyzerSettings))
            {
                await InsertFileIntoTable(connection, tableName, filePath);
            }
        }

        /// <summary>
        /// Inserts a data of the CSV file into the table of the database
        /// </summary>
        /// <param name="connection">Sql connection</param>
        /// <param name="tableName">Table name of the database</param>
        /// <param name="filePath">Path to the "*.xel" files</param>
        /// <returns></returns>
        public static async Task InsertFileIntoTable(SqlConnection connection, string tableName, string filePath)
        {
            var command = connection.CreateCommand();
            command.CommandTimeout = 600;

            command.CommandText =
                $"BULK INSERT [ExpertTools].[dbo].[{tableName}]" +
                $"{Environment.NewLine}FROM '{filePath}'" +
                $"{Environment.NewLine}WITH (FIRSTROW = 1, CODEPAGE = '1251', FIELDTERMINATOR = '{Common.FS}', ROWTERMINATOR = '{Common.RS}')";

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Returns the cleaned request statement
        /// </summary>
        /// <param name="data">Request statement</param>
        /// <returns>Очищенный текст запроса</returns>
        public static async Task<string> CleanSql(string data)
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

        /// <summary>
        /// Returnes the SqlConnectionSqlBuilder by analyzerSettings
        /// </summary>
        /// <param name="analyzerSettings">Settings of the analyzer</param>
        /// <returns></returns>
        public static SqlConnectionStringBuilder GetSqlConnectionStringBuilder(ISqlAnalyzerSettings analyzerSettings)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder()
            {
                DataSource = analyzerSettings.SqlServer,
                IntegratedSecurity = analyzerSettings.IntegratedSecurity,
                UserID = analyzerSettings.SqlUser,
                Password = analyzerSettings.SqlUserPassword
            };

            return builder;
        }
    }
}
