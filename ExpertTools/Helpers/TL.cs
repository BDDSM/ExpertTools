using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Security.Cryptography;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;
using System.Xml;
using System.Xml.Linq;
using ExpertTools.Model;

namespace ExpertTools.Helpers
{
    public static class TL
    {
        #region Common

        /// <summary>
        /// Возвращает дату файла технологического журнала
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns></returns>
        public static string GetFileDate(string filePath)
        {
            var info = Path.GetFileNameWithoutExtension(filePath);

            return "20" + info.Substring(0, 2) + "-" + info.Substring(2, 2) + "-" + info.Substring(4, 2) + " " + info.Substring(6, 2);
        }

        /// <summary>
        /// Возвращает пути ко всем (включая вложенные) файлам *.log
        /// </summary>
        /// <returns>Массив путей файлов *.log</returns>
        public static string[] GetLogFilesPaths()
        {
            return Directory.GetFiles(Config.TechLogFolder, "*.log", SearchOption.AllDirectories);
        }

        /// <summary>
        /// Удаляет файл logcfg.xml из каталога conf
        /// </summary>
        public static void DeleteLogCfg()
        {
            var logcfgPath = Path.Combine(Config.TechLogConfFolder, "logcfg.xml");

            if (File.Exists(logcfgPath)) File.Delete(logcfgPath);
        }

        /// <summary>
        /// Возвращает число для установки в свойство history техжурнала
        /// </summary>
        /// <param name="minutes">Количество минут сбора данных</param>
        /// <returns></returns>
        public static int GetCollectPeriod(int minutes)
        {
            return (int)Math.Ceiling((double)minutes / 60);
        }

        /// <summary>
        /// Возвращает период возникновения события
        /// </summary>
        /// <param name="data">Данные события</param>
        /// <returns></returns>
        public static string GetEventDateTime(string data)
        {
            return data.Substring(0, data.IndexOf("-", 10));
        }

        /// <summary>
        /// Ожидает появления первой папки rphost в каталоге сбора технологического журнала
        /// </summary>
        /// <returns></returns>
        public static Task WaitStartCollectData()
        {
            var tcs = new TaskCompletionSource<bool>();

            var path = Config.TechLogFolder;

            var watcher = new FileSystemWatcher
            {
                NotifyFilter = NotifyFilters.DirectoryName,
                Path = path,
                Filter = "rphost*",
                EnableRaisingEvents = true,
            };

            watcher.Created += (sender, args) =>
            {
                watcher.EnableRaisingEvents = false;
                tcs.TrySetResult(true);
                watcher.Dispose();
            };

            return tcs.Task;
        }

        private static string GetLogCfgText(string analyzerName)
        {
            var text = "";

            if (Config.FilterByDatabase)
            {
                text = Properties.Resources.ResourceManager.GetString("Tl" + analyzerName + "DbFilter");
            }
            else
            {
                text = Properties.Resources.ResourceManager.GetString("Tl" + analyzerName);
            }

            Common.SetVariableValue(ref text, "Database1CEnterprise", Config.Database1CEnterprise);
            Common.SetVariableValue(ref text, "CollectPeriod", GetCollectPeriod(Config.CollectPeriod).ToString());
            Common.SetVariableValue(ref text, "TechLogFolder", Config.TechLogFolder);

            return text;
        }

        #endregion

        #region ParseMethods

        /// <summary>
        /// Проверяет тип события и возвращает результат проверки
        /// </summary>
        /// <param name="eventName">Тип проверяемого события</param>
        /// <returns>Истина/Ложь</returns>
        public static async Task<bool> IsEventAsync(string data, string eventName)
        {
            return await Task.FromResult(data.Contains($",{eventName.ToUpper()},"));
        }

        /// <summary>
        /// Возвращает очищенный текст запроса
        /// </summary>
        /// <param name="data">Строка с описанием события</param>
        /// <returns>Очищенный текст запроса</returns>
        public static async Task<string> ClearSql(string data)
        {
            if (!data.Contains("Sql=")) return "";

            string sql = data.Substring(data.IndexOf("Sql=", StringComparison.OrdinalIgnoreCase) + 4);

            int startIndex = sql.IndexOf("sp_executesql", StringComparison.OrdinalIgnoreCase);
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

            var startChar = sql[0];

            if (startChar == '\'')
            {
                endIndex = sql.IndexOf('\'', 1);
                sql = sql.Substring(1, endIndex);
            }
            else if (startChar == '"')
            {
                endIndex = sql.IndexOf('"', 1);
                sql = sql.Substring(1, endIndex);
            }
            else
            {
                endIndex = sql.IndexOf(',');

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

            return await Task.FromResult(sql.Trim());
        }

        /// <summary>
        /// Возвращает значение свойства Duration из описания события
        /// </summary>
        /// <param name="data">Строка с описанием события</param>
        /// <returns>Значение свойства Duration</returns>
        public static async Task<int> GetDuration(string data)
        {
            var startIndex = data.IndexOf("-");
            var endIndex = data.IndexOf(",", startIndex);
            string duration = data.Substring(startIndex, endIndex);

            return await Task.FromResult(int.Parse(duration));
        }

        /// <summary>
        /// Возвращает значение свойства Context из описания события
        /// </summary>
        /// <param name="data">Строка с описанием события</param>
        /// <returns>Значение свойства Context</returns>
        public static async Task<string> GetContext(string data)
        {
            if (!data.Contains(",Context")) return "";

            int startIndex = data.IndexOf(",Context=", StringComparison.OrdinalIgnoreCase) + 9;
            int endIndex;

            var startChar = data[startIndex];

            switch (startChar)
            {
                case '"':
                    startIndex += 1;
                    endIndex = data.LastIndexOf("\"", StringComparison.OrdinalIgnoreCase);
                    break;
                case '\'':
                    startIndex += 1;
                    endIndex = data.LastIndexOf("'", StringComparison.OrdinalIgnoreCase);
                    break;
                default:
                    endIndex = data.IndexOf(",", startIndex, StringComparison.OrdinalIgnoreCase);
                    break;
            }

            string context;

            if (endIndex > 0)
            {
                context = data.Substring(startIndex, endIndex - startIndex);

            }
            else
            {
                context = data.Substring(startIndex);
            }

            context = context.Replace("\"", "'");

            return await Task.FromResult(context.Trim());
        }

        /// <summary>
        /// Возвращает значение свойства t:clientID из описания события
        /// </summary>
        /// <param name="data">Строка с описанием события</param>
        /// <returns>Значение свойства t:clientID</returns>
        public static async Task<string> GetClientId(string data)
        {
            if (!data.Contains(",t:clientID=")) return "";

            int startIndex = data.IndexOf(",t:clientID=", StringComparison.OrdinalIgnoreCase) + 12;
            var value = data.Substring(startIndex);
            int endIndex = value.IndexOf(",");

            if (endIndex > 0)
            {
                value = value.Substring(0, endIndex);
            }

            return await Task.FromResult(value.Trim());
        }

        /// <summary>
        /// Возвращает значение свойства t:connectId из описания события
        /// </summary>
        /// <param name="data">Строка с описанием события</param>
        /// <returns>Значение свойства t:connectId</returns>
        public static async Task<string> GetConnectId(string data)
        {
            if (!data.Contains(",t:connectID=")) return "";

            int startIndex = data.IndexOf(",t:connectID=", StringComparison.OrdinalIgnoreCase) + 13;
            var value = data.Substring(startIndex);
            int endIndex = value.IndexOf(",");

            if (endIndex > 0)
            {
                value = value.Substring(0, endIndex);
            }
 
            return await Task.FromResult(value.Trim());
        }

        /// <summary>
        /// Возвращает первую строку контекста
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Первая строка контекста</returns>
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
        /// Возвращает последнюю строку контекста
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Последняя строка контекста</returns>
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
        /// Возвращает значение свойства Usr из описания события (асинхронно)
        /// </summary>
        /// <param name="data">Строка с описанием события</param>
        /// <returns>Значение свойства Usr</returns>
        public static async Task<string> GetUser(string data)
        {
            var startIndex = data.IndexOf("Usr=", StringComparison.OrdinalIgnoreCase);
            var user = "";

            if (startIndex > 0)
            {
                user = data.Substring(startIndex + 4);
                int endIndex = user.IndexOf(",");

                if (endIndex > 0)
                {
                    user = user.Substring(0, endIndex);
                }
            }

            user = user.Trim();

            return await Task.FromResult(user);
        }

        #endregion

        /// <summary>
        /// Читает построчно файл технологического журнала, группирует события и отдает их на обработку ответственным блокам
        /// </summary>
        /// <param name="events">Набор событий</param>
        /// <param name="filePath">Путь к файлу технологического журнала</param>
        /// <returns></returns>
        public static async Task ReadFile(HashSet<EventDescription> events, string filePath)
        {
            var fileDate = GetFileDate(filePath);

            using (var inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(inputStream))
            {
                string eventText = "";
                EventDescription eventDescription = null;

                while (!reader.EndOfStream)
                {
                    var currentLine = await reader.ReadLineAsync();

                    var currentEventDescription = events.FirstOrDefault(c => currentLine.Contains($",{c.Name},"));

                    if (currentEventDescription != null && eventText != string.Empty)
                    {
                        await eventDescription.TargetBlock.SendAsync(fileDate + ":" + eventText);

                        eventText = "";
                    }

                    eventText = string.Concat(eventText, eventText == string.Empty ? currentLine : $"\n{currentLine}");

                    if (currentEventDescription != null)
                    {
                        eventDescription = currentEventDescription;
                    }
                }

                // Не забудем отправить последнее событие, оно в цикл не попадает
                if (eventDescription != null && eventText != string.Empty)
                {
                    await eventDescription.TargetBlock.SendAsync(fileDate + ":" + eventText);

                    eventText = "";
                }
            }
        }

        /// <summary>
        /// Создает конфигурационный файл технологического журнала
        /// </summary>
        public static async Task CreateLogCfg(string analyzerName)
        {
            var lofCfgText = GetLogCfgText(analyzerName);

            var logcfgPath = Path.Combine(Config.TechLogConfFolder, "logcfg.xml");

            if (File.Exists(logcfgPath)) File.Delete(logcfgPath);

            using (var writer = new StreamWriter(logcfgPath))
            {
                await writer.WriteAsync(lofCfgText);
            }
        }
    }
}
