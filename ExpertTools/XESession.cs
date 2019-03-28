using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using System.IO;

namespace ExpertTools
{
    /// <summary>
    /// Represents an extended events session of the MSSQL
    /// </summary>
    public class XESession
    {
        ISqlAnalyzerSettings _settings;

        #region Comparison_Types

        /// <summary>
        /// Equal
        /// </summary>
        public const string EQ_CT = "=";
        /// <summary>
        /// No equal
        /// </summary>
        public const string NE_CT = "<>";
        /// <summary>
        /// Greater or equal
        /// </summary>
        public const string GE_CT = ">=";
        /// <summary>
        /// Greater
        /// </summary>
        public const string GT_CT = ">";
        /// <summary>
        /// Less or equal
        /// </summary>
        public const string LE_CT = "<=";
        /// <summary>
        /// Less
        /// </summary>
        public const string LT_CT = "<";

        #endregion

        #region XE_Events

        /// <summary>
        /// "sqlserver.rpc_completed" event
        /// </summary>
        public const string RPC_COMPLETED_EV = "sqlserver.rpc_completed";
        /// <summary>
        /// "sqlserver.sql_batch_completed" event
        /// </summary>
        public const string SQL_BATCH_COMPLETED_EV = "sqlserver.sql_batch_completed";
        /// <summary>
        /// "sqlserver.sql_statement_completed" event
        /// </summary>
        public const string SQL_STATEMENT_COMPLETED_EV = "sqlserver.sql_statement_completed";
        /// <summary>
        /// "sqlserver.sp_statement_completed" event
        /// </summary>
        public const string SP_STATEMENT_COMPLETED_EV = "sqlserver.sp_statement_completed";

        #endregion

        #region XE_Fields

        /// <summary>
        /// Field of the database name
        /// </summary>
        public const string DATABASE_NAME_F = "sqlserver.database_name";
        /// <summary>
        /// Field of the database id
        /// </summary>
        public const string DATABASE_ID_F = "sqlserver.database_id";
        /// <summary>
        /// Field of the plan handle
        /// </summary>
        public const string PLAN_HANDLE_F = "sqlserver.plan_handle";
        /// <summary>
        /// Field of the duration
        /// </summary>
        public const string DURATION_F = "duration";

        #endregion

        /// <summary>
        /// Events of the extended events session
        /// </summary>
        public List<Event> Events { get; private set; } = new List<Event>();

        /// <summary>
        /// Target of the extended events session
        /// </summary>
        public Target FileTarget { get; private set; }

        /// <summary>
        /// Initialize a new instance of the XESession class
        /// </summary>
        /// <param name="analyzerSettings">analyzer settings</param>
        public XESession(ISqlAnalyzerSettings analyzerSettings)
        {
            _settings = analyzerSettings;
        }

        /// <summary>
        /// Adds a new "EVENT" element
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Event AddEvent(string name)
        {
            var newElem = new Event(name);

            Events.Add(newElem);

            return newElem;
        }

        /// <summary>
        /// Add a new "TARGET" element
        /// </summary>
        /// <param name="outputFolderPath">Path to the parent folder of the "*.xel" files</param>
        /// <returns></returns>
        public void SetTargetFile(string outputFolderPath)
        {
            var filePath = Path.Combine(outputFolderPath, $"{_settings.Name}.xel");

            var newElem = new Target(filePath);

            FileTarget = newElem;
        }

        /// <summary>
        /// Adds a new "WHERE" elemnt to the "EVENT" element
        /// </summary>
        /// <param name="ev">"EVENT" element</param>
        /// <param name="field">Field name</param>
        /// <param name="comparisonType">Comparison type</param>
        /// <param name="value">Field value</param>
        public void AddFilter(Event ev, string field, string comparisonType, string value)
        {
            var newElem = new Filter(field, comparisonType, value);

            ev.Filters.Add(newElem);
        }

        /// <summary>
        /// Adds a new "WHERE" element to all "EVENT" elements
        /// </summary>
        /// <param name="field">Field name</param>
        /// <param name="comparisonType">Comparison type</param>
        /// <param name="value">Field value</param>
        public void AddFilter(string field, string comparisonType, string value)
        {
            var newElem = new Filter(field, comparisonType, value);

            Events.ForEach(c => c.Filters.Add(newElem));
        }

        /// <summary>
        /// Adds a new "ACTION" element to the "EVENT" element
        /// </summary>
        /// <param name="ev">"EVENT" element</param>
        /// <param name="field">Field name</param>
        public void AddAction(Event ev, string field)
        {
            var newElem = new Action(field);

            ev.Actions.Add(newElem);
        }

        /// <summary>
        /// Adds a new "ACTION" element to all "EVENT" elements
        /// </summary>
        /// <param name="field">Field name</param>
        public void AddAction(string field)
        {
            var newElem = new Action(field);

            Events.ForEach(c => c.Actions.Add(newElem));
        }

        /// <summary>
        /// Creates this XE session (replaces if it`s exists)
        /// </summary>
        public async Task Create()
        {
            using (var connection = await SqlHelper.GetSqlConnection(_settings))
            {
                await Create(connection);
            }
        }

        /// <summary>
        /// Creates this XE session (replaces if it`s exists)
        /// </summary>
        /// <param name="connection">SQL connection</param>
        /// <returns></returns>
        public async Task Create(SqlConnection connection)
        {
            await Delete(connection);

            var command = connection.CreateCommand();

            command.CommandText = $"CREATE EVENT SESSION {_settings.Name} ON SERVER";

            // Add all events
            foreach (var ev in Events)
            {
                command.CommandText += $"{Environment.NewLine}{ev},";
            }

            // Delete the last comma
            command.CommandText = command.CommandText.Substring(0, command.CommandText.Length - 1);

            // Add file target
            command.CommandText += $"{Environment.NewLine}{FileTarget}";

            command.CommandText += $"{Environment.NewLine}WITH (MAX_DISPATCH_LATENCY=4 SECONDS)";

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Starts this XE session
        /// </summary>
        public async Task Start()
        {
            using (var connection = await SqlHelper.GetSqlConnection(_settings))
            {
                await Start(connection);
            }
        }

        /// <summary>
        /// Starts this XE session
        /// </summary>
        /// <param name="connection">SQL connection</param>
        /// <returns></returns>
        public async Task Start(SqlConnection connection)
        {
            var command = connection.CreateCommand();

            command.CommandText =
                $"ALTER EVENT SESSION {_settings.Name} ON SERVER" +
                $"{Environment.NewLine}STATE = start;";

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Stops this XE session
        /// </summary>
        /// <param name="delete">Delete this session after stopping</param>
        /// <returns></returns>
        public async Task Stop(bool delete = false)
        {
            using (var connection = await SqlHelper.GetSqlConnection(_settings))
            {
                await Stop(connection, delete);
            }
        }

        /// <summary>
        /// Stops this XE session
        /// </summary>
        /// <param name="connection">SQL connection</param>
        /// <returns></returns>
        public async Task Stop(SqlConnection connection, bool delete = false)
        {
            var command = connection.CreateCommand();

            command.CommandText =
                $"ALTER EVENT SESSION {_settings.Name} ON SERVER" +
                $"{Environment.NewLine}STATE = stop;";

            await command.ExecuteNonQueryAsync();

            if (delete)
            {
                await Delete(connection);
            }
        }

        /// <summary>
        /// Deletes this XE session if it`s exists
        /// </summary>
        public async Task Delete()
        {
            using (var connection = await SqlHelper.GetSqlConnection(_settings))
            {
                await Delete(connection);
            }
        }

        /// <summary>
        /// Deletes this XE session if it`s exists
        /// </summary>
        /// <param name="connection">SQL connection</param>
        /// <returns></returns>
        public async Task Delete(SqlConnection connection)
        {
            var command = connection.CreateCommand();

            command.CommandText =
                $"IF EXISTS(SELECT * FROM sys.server_event_sessions WHERE name = '{_settings.Name}')" +
                $"{Environment.NewLine}  DROP EVENT session {_settings.Name} ON SERVER;";

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Returns a reader of the events stream
        /// </summary>
        public async Task<SqlDataReader> GetEventsReader()
        {
            var connection = await SqlHelper.GetSqlConnection(_settings);

            var command = connection.CreateCommand();

            command.CommandText = $"SELECT event_data FROM sys.fn_xe_file_target_read_file('{FileTarget.FilePathForReader}*.xel', null, null, null)";

            var reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);

            return reader;
        }

        /// <summary>
        /// Represents an "EVENT" element of the extended events session
        /// </summary>
        public class Event
        {
            public string Name { get; private set; }

            public List<Filter> Filters { get; private set; } = new List<Filter>();

            public List<Action> Actions { get; private set; } = new List<Action>();

            public Event(string name)
            {
                Name = name;
            }

            public override string ToString()
            {
                string value = $"ADD EVENT {Name}";

                value += "(";

                if (Actions.Count != 0)
                {
                    value += "\n\tACTION(";

                    bool firstAction = true;

                    foreach (var action in Actions)
                    {
                        var prefix = firstAction ? "" : ",";

                        value += $"{prefix}{action}";

                        firstAction = false;
                    }

                    value += ")";
                }

                if (Filters.Count != 0)
                {
                    bool firstFilter = true;

                    foreach (var filter in Filters)
                    {
                        var prefix = firstFilter ? $"{Environment.NewLine}\tWHERE" : " AND ";

                        value += $"{prefix}{filter}";

                        firstFilter = false;
                    }
                }

                value += ")";

                return value;
            }
        }

        /// <summary>
        /// Represents an "WHERE" element of the "EVENT" extended events session element
        /// </summary>
        public class Filter
        {
            public string Field { get; private set; }
            public string ComparisonType { get; private set; }
            public string Value { get; private set; }

            public Filter(string field, string comparisonType, string value)
            {
                Field = field;
                ComparisonType = comparisonType;
                Value = value;
            }

            public override string ToString()
            {
                var value = int.TryParse(Value, out int t) ? $"({Value})" : $"N'{Value}'";

                return $"([{Field}]{ComparisonType}{value})";
            }
        }

        /// <summary>
        /// Represents an "ACTION" element of the "EVENT" extended events session element
        /// </summary>
        public class Action
        {
            public string Field { get; private set; }

            public Action(string field)
            {
                Field = field;
            }

            public override string ToString()
            {
                return Field;
            }
        }

        /// <summary>
        /// Represents an "TARGET" element of the extended events session
        /// </summary>
        public class Target
        {
            public string FilePath { get; private set; }

            public string FilePathForReader
            {
                get
                {
                    var fileName = Path.GetFileNameWithoutExtension(FilePath);
                    var directory = Path.GetDirectoryName(FilePath);
                    return Path.Combine(directory, fileName);
                }
            }

            public Target(string filePath)
            {
                FilePath = filePath;
            }

            public override string ToString()
            {
                return $"ADD TARGET package0.event_file(SET filename = N'{FilePath}',max_file_size=(10240))";
            }
        }
    }
}
