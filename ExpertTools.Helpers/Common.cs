using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace ExpertTools.Helpers
{
    /// <summary>
    /// Предоставляет общие методы и объекты для работы остальных модулей
    /// </summary>
    public static class Core
    {
        /// <summary>
        /// Хранилище таймеров
        /// </summary>
        private static Dictionary<string, DateTime> _beginnedTimers = new Dictionary<string, DateTime>();

        /// <summary>
        /// Разделитель полей для файлов CSV
        /// </summary>
        public const string FS = "<F>";

        /// <summary>
        /// Разделитель строк для файлов CSV
        /// </summary>
        public const string LS = "<L>";

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
        /// Заканчивает замер времени и возвращает время работы в миллисекундах
        /// </summary>
        /// <param name="timerName">Наименование таймера</param>
        /// <returns>Время работы таймера</returns>
        public static double EndTimer(string timerName)
        {
            if (!_beginnedTimers.ContainsKey(timerName))
            {
                throw new Exception($"Таймер с именем {timerName} не запущен");
            }

            var seconds = DateTime.Now.Subtract(_beginnedTimers[timerName]).TotalMilliseconds;

            _beginnedTimers.Remove(timerName);

            return seconds;
        }

        /// <summary>
        /// Возвращает MD5 хэш для переданной строки
        /// </summary>
        /// <param name="data">Данные для хэширования</param>
        /// <returns>Хэш</returns>
        public static async Task<string> GetMD5Hash(string data)
        {
            var md5 = MD5.Create();

            var hashBytes = await Task.FromResult(md5.ComputeHash(Encoding.Default.GetBytes(data)));

            var hashBytesString = await Task.FromResult(BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower());

            return hashBytesString;
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
