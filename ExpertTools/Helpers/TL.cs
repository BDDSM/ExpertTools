using System.Threading.Tasks;
using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;
using System.Xml;
using System.Xml.Linq;

namespace ExpertTools.Helpers
{
    public static class TL
    {
        public static string DataBaseFilter { get; set; } = "";

        #region Common

        /// <summary>
        /// Возвращает пути ко всем (включая вложенные) файлам *.log
        /// </summary>
        /// <param name="logDirectoryPath">Каталог для поиска файлов</param>
        /// <returns>Массив путей файлов *.log</returns>
        public static string[] GetLogFilesPaths(string logDirectoryPath)
        {
            return Directory.GetFiles(logDirectoryPath, "*.log", SearchOption.AllDirectories);
        }

        /// <summary>
        /// Удаляет файл logcfg.xml из каталога conf
        /// </summary>
        public static void DeleteLogCfg()
        {
            var techLogConfFolder = Config.Get<string>("TechLogConfFolder");
            var logcfgPath = Path.Combine(techLogConfFolder, "logcfg.xml");

            if (File.Exists(logcfgPath)) File.Delete(logcfgPath);
        }

        /// <summary>
        /// Возвращает число для установки в свойство history техжурнала
        /// </summary>
        /// <param name="minutes">Количество минут сбора данных</param>
        /// <returns></returns>
        private static int GetCollectPeriod(int minutes)
        {
            return (int)Math.Ceiling((double)minutes / 60);
        }

        /// <summary>
        /// Создает конфигурационный файл технологического журнала
        /// </summary>
        public static void CreateLogCfg()
        {
            var techLogConfFolder = Config.Get<string>("TechLogConfFolder");
            var techLogFolder = Config.Get<string>("TechLogFolder");
            var collectPeriod = GetCollectPeriod(Config.Get<int>("CollectPeriod"));
            var logcfgPath = Path.Combine(techLogConfFolder, "logcfg.xml");

            if (File.Exists(logcfgPath)) File.Delete(logcfgPath);

            using (var writer = new StreamWriter(logcfgPath))
            {
                var document = new XDocument();
                var namespase = XNamespace.Get("http://v8.1c.ru/v8/tech-log");

                var configElem = new XElement(namespase + "config");
                document.Add(configElem);

                var logElem = new XElement(namespase + "log");
                logElem.Add(new XAttribute("history", collectPeriod.ToString()));
                logElem.Add(new XAttribute("location", techLogFolder));
                configElem.Add(logElem);

                var eventElem = new XElement(namespase + "event");
                logElem.Add(eventElem);

                var eq1Elem = new XElement(namespase + "eq");
                eq1Elem.Add(new XAttribute("property", "name"));
                eq1Elem.Add(new XAttribute("value", "DBMSSQL"));
                eventElem.Add(eq1Elem);

                if (DataBaseFilter != "")
                {
                    var eq2Elem = new XElement(namespase + "eq");
                    eq2Elem.Add(new XAttribute("property", "p:processName"));
                    eq2Elem.Add(new XAttribute("value", DataBaseFilter));
                    eventElem.Add(eq2Elem);
                }

                var SQLHelperropElem = new XElement(namespase + "property");
                SQLHelperropElem.Add(new XAttribute("name", "Sql"));
                logElem.Add(SQLHelperropElem);

                var usrPropElem = new XElement(namespase + "property");
                usrPropElem.Add(new XAttribute("name", "Usr"));
                logElem.Add(usrPropElem);

                var contextPropElem = new XElement(namespase + "property");
                contextPropElem.Add(new XAttribute("name", "Context"));
                logElem.Add(contextPropElem);

                writer.Write(document.ToString());
            }
        }

        /// <summary>
        /// Запускает сбор технологического журнала
        /// </summary>
        public static void StartCollectTechLog()
        {
            CreateLogCfg();
        }

        /// <summary>
        /// Останавливает сбор технологического журнала
        /// </summary>
        public static void StopCollectTechLog()
        {
            DeleteLogCfg();
        }

        /// <summary>
        /// Выполняет обработку технологического журнала
        /// </summary>
        /// <returns></returns>
        public static async Task ProcessTechLog()
        {
            var tempFolder = Config.Get<string>("TempFolder");
            var filePath = Path.Combine(tempFolder, "normalized_tech_log.csv");
            var techLogFolder = Config.Get<string>("TechLogFolder");

            using (var filestream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var writer = new StreamWriter(filestream, Encoding.GetEncoding(1251)))
            {
                await NormalizeEventsForJoin(techLogFolder, writer);
            }
        }

        #endregion

        #region ParseMethods

        /// <summary>
        /// Если переданная строка - это начало описания нового события, то возвращает true
        /// </summary>
        /// <param name="data">Строка текста</param>
        /// <returns></returns>
        public static async Task<bool> IsNewEventLineAsync(string data)
        {
            string pattern = @"\d\d:\d\d.\d+-";

            return await Task.FromResult(Regex.IsMatch(data, pattern));
        }

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
        /// Возвращает значение свойства Sql из описания события
        /// </summary>
        /// <param name="data">Строка с описанием события</param>
        /// <returns>Значение свойства Sql</returns>
        public static async Task<string> GetSqlAsync(string data)
        {
            if (!data.Contains("Sql=")) return "";

            var sql = data;
            var endIndex = 0;

            int startIndex = sql.IndexOf("Sql=\"");

            if (startIndex != -1)
            {
                sql = sql.Substring(startIndex + 5);
                endIndex = sql.IndexOf("\"");
                sql = sql.Substring(0, endIndex);
            }
            else
            {
                startIndex = sql.IndexOf("Sql=\'");

                if (startIndex != -1)
                {
                    sql = sql.Substring(startIndex + 5);
                    endIndex = sql.IndexOf("'");

                    if (endIndex > 0)
                    {
                        sql = sql.Substring(0, endIndex);
                    }
                }
                else
                {
                    startIndex = sql.IndexOf("Sql=");

                    sql = sql.Substring(startIndex + 4);
                    endIndex = sql.IndexOf(",");

                    if (endIndex > 0)
                    {
                        sql = sql.Substring(0, endIndex);
                    }
                }
            }

            return await Task.FromResult(sql.Trim());
        }

        /// <summary>
        /// Возвращает значение свойства Duration из описания события
        /// </summary>
        /// <param name="data">Строка с описанием события</param>
        /// <returns>Значение свойства Duration</returns>
        public static async Task<int> GetDurationAsync(string data)
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
        /// <param name="lastLine">Если true, то будет возвращена только последняя строка контекста</param>
        /// <returns>Значение свойства Context</returns>
        public static async Task<string> GetContextAsync(string data)
        {
            var context = "";

            var startIndex = data.IndexOf("Context=");

            if (startIndex > 0)
            {
                context = data.Substring(startIndex + 9);

                var lastIndex = context.IndexOf("'");

                if (lastIndex > 0)
                {
                    context = context.Substring(0, lastIndex);
                }
            }

            context = context.Trim().Replace("\"", "");

            return await Task.FromResult(context);
        }

        /// <summary>
        /// Возвращает первую строку контекста (асинхронно)
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Первая строка контекста</returns>
        public static async Task<string> GetFirstLineContextAsync(string context)
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
        /// Возвращает последнюю строку контекста (асинхронно)
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Последняя строка контекста</returns>
        public static async Task<string> GetLastLineContextAsync(string context)
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
        public static async Task<string> GetUserAsync(string data)
        {
            var startIndex = data.IndexOf("Usr=");
            var user = "";

            if (startIndex > 0)
            {
                user = data.Substring(startIndex + 4);
                int endIndex = user.IndexOf(",");
                user = user.Substring(0, endIndex);
            }

            user = user.Trim();

            return await Task.FromResult(user);
        }

        #endregion

        #region NormalizeForJoinWithSQLTrace

        /// <summary>
        /// Нормализует технологический журнал и выводит в переданный поток для дальнейшего соединения с трассировкой SQL
        /// </summary>
        /// <param name="outputStream">Поток для вывода данных</param>
        public static async Task NormalizeEventsForJoin(string logDirectoryPath, StreamWriter outputStream)
        {
            var logFiles = GetLogFilesPaths(logDirectoryPath);

            var blockOptions = new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };

            var writeToOutputStream = new ActionBlock<string>(text => Common.WriteToOutputStream(text, outputStream));

            var normalizeEventForJoin = new TransformBlock<string, string>(NormalizeEventForJoin, blockOptions);

            var readFile = new ActionBlock<string>(async text => await ReadFile(text, "DBMSSQL", normalizeEventForJoin), blockOptions);

            normalizeEventForJoin.LinkTo(writeToOutputStream, new DataflowLinkOptions() { PropagateCompletion = true });

            foreach (var file in logFiles)
            {
                await readFile.SendAsync(file);
            }

            readFile.Complete();

            await readFile.Completion.ContinueWith(c => normalizeEventForJoin.Complete());

            await writeToOutputStream.Completion;
        }

        #endregion

        #region BlockMethods

        private static async Task<string> NormalizeEventForJoin(string data)
        {
                var sql = await GetSqlAsync(data);

                if (sql == string.Empty) return "";

                var normalizedSql = await Common.GetNormalizedSql(sql);
                var user = await GetUserAsync(data);
                var context = await GetContextAsync(data);
                var firstLine = await GetFirstLineContextAsync(context);
                var lastLine = await GetLastLineContextAsync(context);

                var hash = await Common.GetMD5Hash(normalizedSql);

                return "\n" + 
                    Common.FS +
                    sql +
                    Common.FS +
                    normalizedSql + 
                    Common.FS + 
                    user + 
                    Common.FS +
                    firstLine +
                    Common.FS +
                    lastLine +
                    Common.FS + 
                    hash + 
                    Common.LS;
        }

        #endregion

        #region CommonBlockMethods

        /// <summary>
        /// Читает переданный файл и отправляет разобранные события в targetBlock
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <param name="eventName">Имя события для отбора</param>
        /// <param name="targetBlock">Дальнейший блок цепочки</param>
        /// <returns></returns>
        private static async Task ReadFile(string filePath, string eventName, ITargetBlock<string> targetBlock)
        {
            using (var inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(inputStream))
            {
                var currentEvent = "";

                while (!reader.EndOfStream)
                {
                    var currentLine = await reader.ReadLineAsync();

                    if (await IsNewEventLineAsync(currentLine) && await IsEventAsync(currentLine, eventName) && currentEvent != string.Empty)
                    {
                        await targetBlock.SendAsync(currentEvent);
                        currentEvent = "";
                    }

                    currentEvent += currentEvent == string.Empty ? currentLine : $"\n{currentLine}";
                }
            }
        }

        #endregion

        public static Task WaitStartCollectData()
        {
            var tcs = new TaskCompletionSource<bool>();

            var path = Config.Get<string>("TechLogFolder");

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

        /// <summary>
        /// Названия событией технологического журнала
        /// </summary>
        public enum Event
        {
            DBMSSQL,
            TLOCK,
            TDEADLOCK,
            TTIMEOUT,
            CALL,
            SCALL
        }
    }
}
