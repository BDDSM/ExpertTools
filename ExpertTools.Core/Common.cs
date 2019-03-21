using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace ExpertTools.Core
{
    /// <summary>
    /// Provides common methods and objects
    /// </summary>
    public static partial class Common
    {
        #region Analyze_Types

        /// <summary>
        /// Collection data for analyze requests by cpu utilization, io and duration
        /// </summary>
        public const string QA = "Анализ запросов";

        #endregion

        /// <summary>
        /// Field separator for CSV files
        /// </summary>
        public const string FS = "<F>";
        /// <summary>
        /// Row separator for CSV files
        /// </summary>
        public const string LS = "<L>";

        /// <summary>
        /// Timers storage
        /// </summary>
        private static Dictionary<string, DateTime> _beginnedTimers = new Dictionary<string, DateTime>();

        /// <summary>
        /// Starts a new timer
        /// </summary>
        /// <param name="timerName">Timer name</param>
        public static void StartTimer(string timerName)
        {
            if (_beginnedTimers.ContainsKey(timerName))
            {
                throw new Exception($"Таймер с именем {timerName} уже запущен");
            }

            _beginnedTimers[timerName] = DateTime.Now;
        }

        /// <summary>
        /// Stops a timer and returs timespan between starting and ending
        /// </summary>
        /// <param name="timerName">Timer name</param>
        /// <returns>Timespan</returns>
        public static TimeSpan EndTimer(string timerName)
        {
            if (!_beginnedTimers.ContainsKey(timerName))
            {
                throw new Exception($"Таймер с именем {timerName} не запущен");
            }

            var timeSpan = DateTime.Now.Subtract(_beginnedTimers[timerName]);

            _beginnedTimers.Remove(timerName);

            return timeSpan;
        }

        /// <summary>
        /// Returns MD5 hash of the string
        /// </summary>
        /// <param name="data">String for hash</param>
        /// <returns>Text present of MD5 hash</returns>
        public static async Task<string> GetMD5Hash(string data)
        {
            var md5 = MD5.Create();

            var hashBytes = await Task.FromResult(md5.ComputeHash(Encoding.Default.GetBytes(data)));

            var hashBytesString = await Task.FromResult(BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower());

            return hashBytesString;
        }

        /// <summary>
        /// Replaces a [variable] format variable in the text
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="variable">Variable name</param>
        /// <param name="value">Variable value</param>
        /// <returns></returns>
        public static void SetVariableValue(ref string text, string variable, string value)
        {
            text = text.Replace($"[{variable}]", value);
        }

        /// <summary>
        /// Check a write posibility in the folder
        /// </summary>
        /// <param name="path">Folder path</param>
        /// <returns>true or false</returns>
        public static bool CheckFolderWriting(string path)
        {
            var tempFile = Path.Combine(path, Path.GetRandomFileName());

            try
            {
                var s = File.Create(tempFile);
                s.Close();
                s.Dispose();
                File.Delete(tempFile);

                return true;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Returns a normalized request statement
        /// </summary>
        /// <param name="sql">Request statement</param>
        /// <returns>Normalized request statement</returns>
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
        /// Returns available types of an analyze
        /// </summary>
        /// <returns></returns>
        public static string[] GetAnalyzeTypes()
        {
            return new string[] { QA };
        }

        /// <summary>
        /// Returns the stream for writing handled data
        /// </summary>
        /// <param name="filePath">Path to the output file</param>
        /// <returns></returns>
        public static StreamWriter GetOutputStream(string filePath)
        {
            var stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            var writer = new StreamWriter(stream, Encoding.GetEncoding(1251));

            return writer;
        }

        /// <summary>
        /// Writes the string into the stream
        /// </summary>
        /// <param name="data">Data for writing</param>
        /// <param name="writer">Output stream</param>
        /// <returns></returns>
        public static async Task WriteToOutputStream(string data, StreamWriter writer)
        {
            if (data != string.Empty)
            {
                await writer.WriteAsync("\n" + data);
            }
        }
    }
}
