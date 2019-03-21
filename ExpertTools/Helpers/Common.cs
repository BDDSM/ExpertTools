using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;

namespace ExpertTools.Helpers
{
    /// <summary>
    /// Общие методы
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// Разделитель полей для файлов CSV
        /// </summary>
        public const string FS = "<SEPARATOR>";

        /// <summary>
        /// Разделитель строк для файлов CSV
        /// </summary>
        public const string LS = "<LINE>";

        /// <summary>
        /// Хранилище таймеров
        /// </summary>
        private static Dictionary<string, DateTime> _beginnedTimers = new Dictionary<string, DateTime>();

        /// <summary>
        /// Возвращает MD5 хэш для переданной строки
        /// </summary>
        /// <param name="data">Данные для хэширования</param>
        /// <returns>Хэш</returns>
        public static async Task<string> GetMD5Hash(string data)
        {
            var md5 = MD5.Create();

            var hashBytes = md5.ComputeHash(Encoding.Default.GetBytes(data));

            return await Task.FromResult(BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower());
        }

        /// <summary>
        /// Запускает замер времени
        /// </summary>
        /// <param name="timerName">Наименование таймера</param>
        public static void StartTimer(string timerName)
        {
            if (_beginnedTimers.ContainsKey(timerName))
            {
                throw new Exception($"Таймер с именем {timerName} уже запущен");
            }

            _beginnedTimers[timerName] = DateTime.Now;
        }

        /// <summary>
        /// Заканчивает замер времени и возвращает время работы в секундах
        /// </summary>
        /// <param name="timerName">Наименование таймера</param>
        /// <returns>Время работы таймера</returns>
        public static double EndTimer(string timerName)
        {
            if (!_beginnedTimers.ContainsKey(timerName))
            {
                throw new Exception($"Таймер с именем {timerName} не запущен");
            }

            var seconds = DateTime.Now.Subtract(_beginnedTimers[timerName]).TotalSeconds;

            _beginnedTimers.Remove(timerName);

            return seconds;
        }

        /// <summary>
        /// Записывает строку в поток
        /// </summary>
        /// <param name="data">Данные для записи</param>
        /// <param name="writer">Выходной поток для записи</param>
        /// <returns></returns>
        public static async Task WriteToOutputStream(string data, StreamWriter writer)
        {
            if (data != string.Empty)
            {
                await writer.WriteAsync("\n" + data);
            }
        }

        /// <summary>
        /// Возвращает нормализованный текст запроса для дальнейшего соединения с трассировкой SQL
        /// </summary>
        /// <param name="sql">Текст запроса, полученный методом GetSqlAsync</param>
        /// <returns>Нормализованный текст запроса</returns>
        public static async Task<string> GetNormalizedSql(string sql)
        {
            var normalizedSql = sql;

            normalizedSql = Regex.Replace(normalizedSql, @"@P\d+", "", RegexOptions.Compiled);
            normalizedSql = Regex.Replace(normalizedSql, @"#tt\d+", "#TEMPTABLE", RegexOptions.Compiled);
            normalizedSql = Regex.Replace(normalizedSql, @"\(\d+\)", "#NUM", RegexOptions.Compiled);
            normalizedSql = normalizedSql.Replace("\n", "");
            normalizedSql = normalizedSql.Replace("\r", "");
            normalizedSql = normalizedSql.Replace(" ", "");
            normalizedSql = normalizedSql.Replace("{", "");
            normalizedSql = normalizedSql.Replace("}", "");
            normalizedSql = normalizedSql.Replace("\"", "");
            normalizedSql = normalizedSql.Replace("'", "");
            normalizedSql = normalizedSql.Replace(".", "");
            normalizedSql = normalizedSql.Replace(",", "");
            normalizedSql = normalizedSql.Replace(";", "");
            normalizedSql = normalizedSql.Replace(":", "");
            normalizedSql = normalizedSql.Replace("@", "");
            normalizedSql = normalizedSql.Replace("?", "");
            normalizedSql = normalizedSql.Replace("=", "");

            return await Task.FromResult(normalizedSql.Trim().ToUpper());
        }

        /// <summary>
        /// Очищает каталоги логов и временных файлов
        /// </summary>
        public static void ClearFolders()
        {
            ClearFolder(Config.TechLogFolder);

            ClearFolder(Config.SqlTraceFolder);

            ClearFolder(Config.TempFolder);
        }

        /// <summary>
        /// Полностью очищает переданный каталог
        /// </summary>
        /// <param name="folder">Каталог для очистки</param>
        public static void ClearFolder(string folder)
        {
            foreach (var f in Directory.GetFiles(folder))
            {
                File.Delete(f);
            }

            foreach (var dir in Directory.GetDirectories(folder))
            {
                Directory.Delete(dir, true);
            }
        }

        /// <summary>
        /// Записывает данные в лог приложения
        /// </summary>
        /// <param name="text">Текст для записи</param>
        /// <returns></returns>
        public static async Task WriteLog(string text)
        {
            var path = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "log.txt");

            using (var writer = new StreamWriter(path, true))
            {
                await writer.WriteLineAsync($"{DateTime.Now} : {text}, Memory={Environment.WorkingSet / 1000000}");
            }
        }

        /// <summary>
        /// Возвращает список баз, если возможно
        /// </summary>
        /// <returns></returns>
        public static async Task<List<DatabaseItem>> GetBases()
        {
            var bases = new List<DatabaseItem>();

            try
            {
                var rootDirectory = Directory.GetParent(Config.TechLogConfFolder);

                var files = Directory.GetFiles(Path.Combine(rootDirectory.FullName, "srvinfo"), "1CV8Clst.lst", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(stream))
                    {
                        try
                        {
                            while (!reader.EndOfStream)
                            {
                                var data = await reader.ReadLineAsync();

                                if (data.Contains(",\"MSSQLServer\","))
                                {
                                    var lineData = data.Split(',');

                                    var base1C = lineData[1].Trim(new char[] { '"' });
                                    var baseSql = lineData[5].Trim(new char[] { '"' });

                                    bases.Add(new DatabaseItem(base1C, baseSql));
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return bases;
        }

        /// <summary>
        /// Возвращает поток для записи обработанных данных
        /// </summary>
        /// <param name="filePath">Путь к файлу в который открываается поток</param>
        /// <returns></returns>
        public static StreamWriter GetOutputStream(string filePath)
        {
            var contextStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            var contextWriter = new StreamWriter(contextStream, Encoding.GetEncoding(1251));

            return contextWriter;
        }

        /// <summary>
        /// Заменяет в тексте переменную формата [variable] на указанное значение
        /// </summary>
        /// <param name="text">Обрабатываемый текст</param>
        /// <param name="variable">Название переменной</param>
        /// <param name="value">Подсавляемое значение переменной</param>
        /// <returns></returns>
        public static void SetVariableValue(ref string text, string variable, string value)
        {
            text = text.Replace($"[{variable}]", value);
        }
    }
}
