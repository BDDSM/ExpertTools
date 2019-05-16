using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ExpertTools.TechLog
{
    /// <summary>
    /// Provides static methods for working with the technology log
    /// </summary>
    public static class TechLogHelper
    {
        /// <summary>
        /// Returns paths of the log files
        /// </summary>
        /// <param name="parentFolder">Path to the parent folder of the log</param>
        /// <returns>Array of file paths</returns>
        public static string[] GetLogFiles(string parentFolder)
        {
            return Directory.GetFiles(parentFolder, "*.log", SearchOption.AllDirectories);
        }

        /// <summary>
        /// Returns paths of the log files
        /// </summary>
        /// <param name="logCfg">LogCfg class instance</param>
        /// <returns>Array of file paths</returns>
        public static string[] GetLogFiles(LogCfg logCfg)
        {
            List<string> files = new List<string>();

            foreach (var path in logCfg.GetLogFoldersPaths())
            {
                files.AddRange(Directory.GetFiles(path, "*.log", SearchOption.AllDirectories));
            }

            return files.Distinct().ToArray();
        }

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
        /// Returns "true" if this line is the starting line of the event, else returns "false"
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
        /// Returns a value of the property which convetred to the T type
        /// Specially properties:
        /// DateTime - time of the event.
        /// Duration - duration of the event.
        /// EventType - type of the event.
        /// </summary>
        /// <param name="data">Event data</param>
        /// <param name="propertyName">Property name</param>
        /// <returns></returns>
        public static T GetPropertyValue<T>(string data, string propertyName)
        {
            var value = GetPropertyValue(data, propertyName);

            return (T)Convert.ChangeType(value, typeof(T));
        }

        /// <summary>
        /// Returns a value of the property. 
        /// Specially properties:
        /// DateTime - time of the event.
        /// Duration - duration of the event.
        /// EventType - type of the event.
        /// </summary>
        /// <param name="data">Event data</param>
        /// <param name="propertyName">Property name</param>
        /// <returns></returns>
        public static string GetPropertyValue(string data, string propertyName)
        {
            string value;

            if (propertyName.ToUpper() == "DATETIME")
            {
                if (data.Length < 11)
                {
                    throw new Exception("This line is not a start line of event");
                }

                value = data.Substring(0, data.IndexOf("-", 10));
            }
            else if (propertyName.ToUpper() == "DURATION")
            {
                if (data.Length < 9)
                {
                    throw new Exception("This line is not a start line of event");
                }

                var startIndex = data.IndexOf("-", 8) + 1;
                var endIndex = data.IndexOf(",", startIndex);

                value = data.Substring(startIndex, endIndex - startIndex);
            }
            else if (propertyName.ToUpper() == "EVENTTYPE")
            {
                var startIndex = data.IndexOf(",") + 1;
                var endIndex = data.IndexOf(",", startIndex);

                value = data.Substring(startIndex, endIndex - startIndex);
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

            return value;
        }

        /// <summary>
        /// Returns the first line of the event context
        /// </summary>
        /// <param name="context">Event context</param>
        /// <returns>First line of the event context</returns>
        public static string GetFirstLineContext(string context)
        {
            var firstLine = context;

            var lastIndex = firstLine.IndexOf("\n");

            if (lastIndex > 0)
            {
                firstLine = firstLine.Substring(0, lastIndex);
            }

            firstLine = firstLine.Trim();

            return firstLine;
        }

        /// <summary>
        /// Returns the last line of the event context
        /// </summary>
        /// <param name="context">Event context</param>
        /// <returns>Last line of the event context</returns>
        public static string GetLastLineContext(string context)
        {
            var lastLine = context;

            var firstIndex = lastLine.LastIndexOf("\t");

            if (firstIndex > 0)
            {
                lastLine = lastLine.Substring(firstIndex + 1);
            }

            lastLine = lastLine.Trim();

            return lastLine;
        }

        /// <summary>
        /// Reads the log file and posts each event data to the nextBlock
        /// </summary>
        /// <param name="logPath">Path to the log file</param>
        /// <param name="nextBlock"></param>
        public static async Task ReadLogFile(string logPath, ITargetBlock<string> nextBlock)
        {
            var fileDate = GetTlDateTime(logPath);

            using (var inputStream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(inputStream))
            {
                string eventData = "";

                while (!reader.EndOfStream)
                {
                    var currentLine = await reader.ReadLineAsync();

                    var isNewEventLine = IsNewEventLine(currentLine);

                    if (isNewEventLine && eventData != "")
                    {
                        eventData = string.Concat(fileDate, ":", eventData);

                        await nextBlock.SendAsync(eventData);

                        eventData = "";
                    }

                    eventData = string.Concat(eventData, string.Concat((eventData == string.Empty ? "" : "\n"), currentLine));
                }

                if (eventData != "")
                {
                    eventData = string.Concat(fileDate, ":", eventData);

                    await nextBlock.SendAsync(eventData);
                }
            }
        }
    }
}
