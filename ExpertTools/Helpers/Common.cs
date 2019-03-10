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
                await writer.WriteAsync(data);
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

            int startIndex = normalizedSql.IndexOf("sp_executesql");
            int endIndex;
            if (startIndex > 0)
            {
                normalizedSql = normalizedSql.Substring(startIndex + 16);
                endIndex = normalizedSql.IndexOf("'");
                normalizedSql = normalizedSql.Substring(0, endIndex);
            }

            endIndex = normalizedSql.IndexOf("p_0:");

            if (endIndex > 0)
            {
                normalizedSql = normalizedSql.Substring(0, endIndex);
            }

            normalizedSql = Regex.Replace(normalizedSql, @"@P\d+", "?");
            normalizedSql = Regex.Replace(normalizedSql, "\n", "");

            return await Task.FromResult(normalizedSql.Trim());
        }

        /// <summary>
        /// Очищает каталоги логов и временных файлов
        /// </summary>
        public static void ClearFolders()
        {
            ClearFolder(Config.Get<string>("TechLogFolder"));

            ClearFolder(Config.Get<string>("SqlTraceFolder"));

            ClearFolder(Config.Get<string>("TempFolder"));
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
        /// Проверяет каталоги логов и временных файло на наличие вложенных элементов
        /// </summary>
        public static bool CheckFolders()
        {
            var result = true;

            if (!CheckFolder(Config.Get<string>("TechLogFolder"))) result = false;

            if (!CheckFolder(Config.Get<string>("SqlTraceFolder"))) result = false;

            if (!CheckFolder(Config.Get<string>("TempFolder"))) result = false;

            CheckWriteInFolder(Config.Get<string>("TechLogConfFolder"));

            return result;
        }

        /// <summary>
        /// Проверяет переданный каталог на наличие вложенных элементов
        /// </summary>
        /// <param name="folder">Каталог</param>
        public static bool CheckFolder(string folder)
        {
            if (!Directory.Exists(folder))
            {
                throw new Exception($"Каталог по пути \"{folder}\" не обнаружен");
            }

            return !Directory.EnumerateFileSystemEntries(folder).Any();
        }

        private static void CheckWriteInFolder(string path)
        {
            var tempFile = Path.Combine(path, Path.GetRandomFileName());

            try
            {
                var s = File.Create(tempFile);
                s.Close();
                File.Delete(tempFile);
            }
            catch
            {
                throw new Exception($"Каталог по пути \"{path}\" не доступен для записи");
            }
        }

        public static async Task WriteLog(string text)
        {
            var path = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "log.txt");

            using (var writer = new StreamWriter(path, true))
            {
                await writer.WriteLineAsync($"{DateTime.Now} : {text}, Memory={Environment.WorkingSet / 1000000}");
            }
        }
    }
}
