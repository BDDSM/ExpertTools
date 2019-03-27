using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ExpertTools
{
    public static class Logger
    {
        public const ConsoleColor QUESTION_COLOR = ConsoleColor.Yellow;
        public const ConsoleColor INFO_COLOR = ConsoleColor.Cyan;
        public const ConsoleColor SUCCESS_COLOR = ConsoleColor.Green;
        public const ConsoleColor NORMAL_COLOR = ConsoleColor.White;
        public const ConsoleColor ERROR_COLOR = ConsoleColor.Red;

        private static object locker = new object();

        private static StreamWriter fileStream;

        public static bool ConsoleLogEnabled { get; set; } = false;

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

        public static void Info(string text, LogLevel level = LogLevel.Info)
        {
            Log(text, INFO_COLOR, level);
        }

        public static void Error(Exception ex, LogLevel level = LogLevel.Info)
        {
            Error(ex.ToString() + Environment.NewLine + ex.StackTrace, level);
        }

        public static void Error(string text, LogLevel level = LogLevel.Info)
        {
            Log(text, ERROR_COLOR, level);
        }

        public static void Success(string text, LogLevel level = LogLevel.Info)
        {
            Log(text, SUCCESS_COLOR, level);
        }

        public static void Log(string text, LogLevel level = LogLevel.Info)
        {
            Log(text, NORMAL_COLOR, level);
        }

        private static void Log(string text, ConsoleColor color, LogLevel level = LogLevel.Info)
        {
            if (level <= Level)
            {
                text = $"{DateTime.Now.ToString("HH:mm:ss.fffffff")} | {text}";

                lock (locker)
                {
                    if (ConsoleLogEnabled)
                    {
                        Console.ForegroundColor = color;

                        Console.WriteLine(text);

                        Console.ForegroundColor = NORMAL_COLOR;
                    }

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
