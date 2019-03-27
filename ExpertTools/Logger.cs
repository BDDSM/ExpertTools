using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ExpertTools
{
    public static class Logger
    {
        private static object locker = new object();

        private static StreamWriter fileStream;

        public static bool FileLogEnabled { get; private set; } = false;

        public static LogLevel Level { get; set; } = LogLevel.Info;

        static Logger()
        {
            AppDomain.CurrentDomain.ProcessExit += LoggerDestructor;
        }

        public static void EnableFileLog(string filePath = "")
        {
            if (string.IsNullOrEmpty(filePath.Trim()))
            {
                filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
            }

            Common.CheckFolderWriting(Path.GetDirectoryName(filePath));

            FileLogEnabled = true;

            fileStream = new StreamWriter(filePath);
            fileStream.AutoFlush = true;
        }

        public static void Log(Exception ex, LogLevel level = LogLevel.Info)
        {
            Log(ex.Message + Environment.NewLine + ex.StackTrace, level);
        }

        public static void Log(string text, LogLevel level = LogLevel.Info)
        {
            if (level <= Level)
            {
                text = $"{DateTime.Now.ToString("HH:mm:ss.fffffff")} | {text}";

                lock (locker)
                {
                    if (FileLogEnabled)
                    {
                        fileStream.WriteLine(text);
                    }
                }
            }
        }

        private static void LoggerDestructor(object sender, EventArgs args)
        {
            if (FileLogEnabled)
            {
                fileStream.Close();
                fileStream.Dispose();
            }
        }
    }

    public enum LogLevel
    {
        Info,
        Debug
    }
}
