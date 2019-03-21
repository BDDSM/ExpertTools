using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Linq;
using System.Xml.Linq;

namespace ExpertTools.Core
{
    /// <summary>
    /// Provides static methods for working with technology log
    /// </summary>
    public partial class TLHelper
    {
        /// <summary>
        /// Returns a number to set the "history" attribute of the "log" element
        /// </summary>
        /// <param name="minutes">Minutes</param>
        /// <returns></returns>
        public static int GetCollectPeriod(int minutes)
        {
            return (int)Math.Ceiling((double)minutes / 60);
        }

        /// <summary>
        /// Returns paths of log files
        /// </summary>
        /// <param name="logcfg">Logcfg instance</param>
        /// <returns></returns>
        public static string[] GetLogFiles(Logcfg logcfg)
        {
            List<string> files = new List<string>();

            foreach (var path in logcfg.GetLogPaths())
            {
                files.AddRange(Directory.GetFiles(path, "*.log", SearchOption.AllDirectories));
            }

            return files.Distinct().ToArray();
        }

        /// <summary>
        /// Waits appearance of first folder on the collection data folder
        /// </summary>
        /// <returns></returns>
        public static Task WaitStartCollectData(Logcfg logcfg)
        {
            var tcs = new TaskCompletionSource<bool>();

            var logPaths = logcfg.GetLogPaths();

            FileSystemWatcher[] watchers = new FileSystemWatcher[logPaths.Length];

            for (int x = 0; x < logPaths.Length; x++)
            {
                var watcher = new FileSystemWatcher
                {
                    NotifyFilter = NotifyFilters.DirectoryName,
                    Path = logPaths[x],
                    Filter = "*"
                };

                watcher.Created += (sender, args) =>
                {
                    watchers.ToList().ForEach(c => c.EnableRaisingEvents = false);
                    tcs.TrySetResult(true);
                    watchers.ToList().ForEach(c => c.Dispose());
                };

                watchers[x] = watcher;
            }

            watchers.ToList().ForEach(c => c.EnableRaisingEvents = true);

            return tcs.Task;
        }

        #region Parse_Methods

        /// <summary>
        /// Returns the date of the log file
        /// </summary>
        /// <param name="filePath">Path to the log file</param>
        /// <returns></returns>
        public static string GetTlDateTime(string filePath)
        {
            var info = Path.GetFileNameWithoutExtension(filePath);

            return "20" + info.Substring(0, 2) + "-" + info.Substring(2, 2) + "-" + info.Substring(4, 2) + " " + info.Substring(6, 2);
        }

        /// <summary>
        /// Returns a value of the property
        /// </summary>
        /// <param name="data">Event data</param>
        /// <param name="propertyName">Property name</param>
        /// <returns></returns>
        public static async Task<string> GetPropertyValue(string data, string propertyName)
        {
            string value;

            if (propertyName == Logcfg.DATETIME_PR)
            {
                value = await GetEventDateTime(data);
            }
            else if (propertyName == Logcfg.DURATION_PR)
            {
                value = await GetDurationValue(data);
            }
            else
            {
                int startIndex = data.IndexOf($"{propertyName}=", StringComparison.OrdinalIgnoreCase);

                if (startIndex == -1) return "";

                startIndex += propertyName.Length + 1;

                value = data.Substring(startIndex);

                var startChar = value[0];

                int endIndex;

                switch (startChar)
                {
                    case '\'':
                        startIndex = 1;
                        endIndex = value.IndexOf('\'', 1) - 1;
                        break;
                    case '"':
                        startIndex = 1;
                        endIndex = value.IndexOf('"', 1) - 1;
                        break;
                    case ',':
                        return "";
                    default:
                        startIndex = 0;
                        endIndex = value.IndexOf(',', 1);
                        break;
                }

                if (endIndex > 0)
                {
                    value = value.Substring(startIndex, endIndex);
                }
            }

            value = value.Trim();

            return await Task.FromResult(value);
        }

        /// <summary>
        /// Returns the date and the time of the event
        /// </summary>
        /// <param name="data">Event data</param>
        /// <returns></returns>
        public static async Task<string> GetEventDateTime(string data)
        {
            return await Task.FromResult(data.Substring(0, data.IndexOf("-", 10)));
        }

        /// <summary>
        /// Returns a duration of the event
        /// </summary>
        /// <param name="data">Event data</param>
        /// <returns>Duration</returns>
        private static async Task<string> GetDurationValue(string data)
        {
            var startIndex = data.IndexOf("-");
            var endIndex = data.IndexOf(",", startIndex);
            string duration = data.Substring(startIndex, endIndex);

            return await Task.FromResult(duration);
        }

        /// <summary>
        /// Clears a request statement from parameter values and stored procedure calls
        /// </summary>
        /// <param name="data">Request statement</param>
        /// <returns></returns>
        public static async Task<string> ClearSql(string data)
        {
            string sql = data;

            int startIndex = data.IndexOf("sp_executesql", StringComparison.OrdinalIgnoreCase);

            int endIndex;

            if (startIndex > 0)
            {
                sql = sql.Substring(startIndex + 16);

                endIndex = sql.IndexOf("', N'@P", StringComparison.OrdinalIgnoreCase);

                if (endIndex > 0)
                {
                    sql = sql.Substring(0, endIndex);
                }
            }

            endIndex = sql.IndexOf("p_0:", StringComparison.OrdinalIgnoreCase);

            if (endIndex > 0)
            {
                sql = sql.Substring(0, endIndex);
            }

            sql = sql.Trim();

            return await Task.FromResult(sql);
        }

        /// <summary>
        /// Returns true if this line is a starting line of the event, else returns false
        /// </summary>
        /// <param name="line">Text line</param>
        /// <returns>True or false</returns>
        public static bool IsNewEventLine(string line)
        {
            if (line.Length < 6) return false;

            if (line[2] == ':' && line[5] == '.') return true;

            return false;
        }

        /// <summary>
        /// Returns a first line of the event context
        /// </summary>
        /// <param name="context">Event context</param>
        /// <returns>First line of the event context</returns>
        public static async Task<string> GetFirstLineContext(string context)
        {
            var firstLine = context;

            var lastIndex = firstLine.IndexOf("\n");

            if (lastIndex > 0)
            {
                firstLine = firstLine.Substring(0, lastIndex);
            }

            firstLine = firstLine.Trim();

            return await Task.FromResult(firstLine);
        }

        /// <summary>
        /// Returns a last line of the event context
        /// </summary>
        /// <param name="context">Event context</param>
        /// <returns>Last line of the event context</returns>
        public static async Task<string> GetLastLineContext(string context)
        {
            var lastLine = context;

            var firstIndex = lastLine.LastIndexOf("\t");

            if (firstIndex > 0)
            {
                lastLine = lastLine.Substring(firstIndex + 1);
            }

            lastLine = lastLine.Trim();

            return await Task.FromResult(lastLine);
        }

        /// <summary>
        /// Reads file line by line, groups event lines and sends the result to the next block
        /// </summary>
        /// <param name="events">Events</param>
        /// <param name="filePath">Path to the log file</param>
        /// <returns></returns>
        public static async Task ReadFile(HashSet<(string eventName, ITargetBlock<string> targetBlock)> events, string filePath)
        {
            var fileDate = GetTlDateTime(filePath);

            using (var inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(inputStream))
            {
                // Текст текущего события
                string eventText = "";
                // Признак необходимости обработки считываемых строк события
                bool skipLine = false;
                // Текущее событие
                (string eventName, ITargetBlock<string> targetBlock) currentEvent = default;

                while (!reader.EndOfStream)
                {
                    // Текущая считанная строка технологического журнала
                    var currentLine = await reader.ReadLineAsync();
                    // Признак того, строка нового события это или нет
                    var newEventLine = IsNewEventLine(currentLine);
                    // Если это не новая строка и обработка не требуется, то переходим к следующей
                    if (!newEventLine && skipLine) continue;

                    if (newEventLine)
                    {
                        // Сначала, если есть что, отправляем в обработку
                        if (currentEvent != default)
                        {
                            await currentEvent.targetBlock.SendAsync(string.Concat(fileDate, ":", eventText));
                            // Очищаем переменную, хранящую текст события
                            eventText = "";
                        }
                        // Узнаем, есть ли тип нового события в списке к обработке
                        currentEvent = events.FirstOrDefault(c => currentLine.Contains($",{c.eventName},"));
                        // Если нет, то пропускаем все следующие до нового события
                        if (currentEvent == default) skipLine = true;
                    }

                    // Соединяем строки текущего события
                    eventText = string.Concat(eventText, string.Concat((eventText == string.Empty ? "" : "\n"), currentLine));
                }

                // Теперь надо обработать последнее событие, т.к. оно не попадает в основной цикл
                if (currentEvent != default) await currentEvent.targetBlock.SendAsync(string.Concat(fileDate, ":", eventText));
            }
        }

        #endregion
    }
}
