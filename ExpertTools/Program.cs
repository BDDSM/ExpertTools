using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ExpertTools
{
    class Program
    {
        static string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.cfg");

        static void Main(string[] args)
        {
            EnableLog();

            int analyzeType = GetAnalyzeType();

            bool useConfig = UseConfig(configPath);

            if (!useConfig)
            {
                configPath = "";
            }

            switch (analyzeType)
            {
                case 1:
                    StartQueriesAnalyze(useConfig);
                    break;
                default:
                    break;
            }

            Console.ReadKey();
        }
         
        static void EnableLog()
        {
            Logger.ConsoleLogEnabled = true;
            Logger.EnableFileLog();
        }

        static bool UseConfig(string configPath)
        {
            if (File.Exists(configPath))
            {
                return ReadValue<bool>("The configuration file has been found. Use it?");
            }
            else
            {
                return false;
            }
        }

        static void WriteConfig<T>(T analyzerSetting)
        {
            if (ReadValue<bool>("Write this settings to the config file?"))
            {
                Common.WriteConfigFile<T>(analyzerSetting, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.cfg"));
            }
        }

        #region Queries_Analyze

        static int GetAnalyzeType()
        {
            string msg =
                "Enter number of the analyze type:" +
                $"{Environment.NewLine}1. Queries analyze (Analyze of the 1C:Enterprise application context lines by CPU load, IO and request durations)";

            return SelectListItem(msg, 1);
        }

        static void StartQueriesAnalyze(bool useConfig)
        {
            var settings = GetQueriesAnalyzeSettings(useConfig);
            var analyzer = new QueriesAnalyzer(settings);
            analyzer.Run().Wait();
        }

        static QueriesAnalyzerSettings GetQueriesAnalyzeSettings(bool useConfig)
        {
            QueriesAnalyzerSettings settings;

            if (useConfig)
            {
                try
                {
                    settings = Common.ReadConfigFile<QueriesAnalyzerSettings>(configPath);

                    return settings;
                }
                catch (Exception ex)
                {
                    Error(ex.Message);
                }
            }

            settings = new QueriesAnalyzerSettings
            {
                SqlServer = ReadValue<string>("Enter a DBMS server address:"),
                IntegratedSecurity = ReadValue<bool>("Use sql connection with integrated security?")
            };

            if (!settings.IntegratedSecurity)
            {
                settings.SqlUser = ReadValue<string>("Enter a user of the sql server instance:");
                settings.SqlUserPassword = ReadValue<string>("Enter a password of the sql server instance user:");
            }

            settings.SqlTraceFolder = ReadValue<string>("Enter a path to the sql trace folder (this folder will be cleaned before starting):");

            settings.TlConfFolder = ReadValue<string>("Enter a path to the 1C:enterprise application \"conf\" folder:");
            settings.TlFolder = ReadValue<string>("Enter a path to the tech log folder (this folder will be cleaned before starting):");
            settings.TempFolder = ReadValue<string>("Enter a path to the temp folder (this folder will be cleaned before starting):");
            settings.CollectPeriod = ReadValue<int>("Specify the period of data collecting (in minutes):");
            settings.FilterByDatabase = ReadValue<bool>("Use filter by database?");

            if (settings.FilterByDatabase)
            {
                settings.Database1C = ReadValue<string>("Enter a name of the 1C:Enterprise base:");
                settings.DatabaseSQL = ReadValue<string>("Enter a name of the sql database:");
            }

            settings.FilterByDuration = ReadValue<bool>("Use filter by requests duration?");

            if (settings.FilterByDuration)
            {
                settings.Duration = ReadValue<int>("Enter a value of the \"Duration\" property (greater or equal):");
            }

            WriteConfig(settings);

            return settings;
        }

        #endregion

        #region Console

        static int SelectListItem(string msg, int listLength)
        {
            var value = ReadValue<int>(msg);

            if (value > 0 && value <= listLength)
            {
                return value;
            }
            else
            {
                Error("Entered value is not in the boundaries of the list");

                return SelectListItem(msg, listLength);
            }
        }

        static T ReadValue<T>(string msg, string errorMsg = "")
        {
            return (T)ReadValue(typeof(T), msg, errorMsg);
        }

        static object ReadValue(Type valueType, string msg, string errorMsg = "")
        {
            Question(msg);

            var value = Console.ReadLine().Trim('"');

            if (value.Trim() != string.Empty)
            {
                try
                {
                    return Convert.ChangeType(value, valueType);
                }
                catch
                {
                    Error("Incorrect value");

                    return ReadValue(valueType, msg, errorMsg);
                }
            }
            else
            {
                return ReadValue(valueType, msg, errorMsg);
            }
        }

        private static void Success(string msg)
        {
            Console.ForegroundColor = Logger.SUCCESS_COLOR;

            Console.WriteLine(msg);

            Console.ForegroundColor = Logger.NORMAL_COLOR;
        }

        private static void Question(string msg)
        {
            Console.ForegroundColor = Logger.QUESTION_COLOR;

            Console.WriteLine(msg);

            Console.ForegroundColor = Logger.NORMAL_COLOR;
        }

        private static void Error(string msg)
        {
            Console.ForegroundColor = Logger.ERROR_COLOR;

            Console.WriteLine(msg);

            Console.ForegroundColor = Logger.NORMAL_COLOR;
        }

        private static void Info(string msg)
        {
            Console.ForegroundColor = Logger.INFO_COLOR;

            Console.WriteLine(msg);

            Console.ForegroundColor = Logger.NORMAL_COLOR;
        }

        #endregion
    }
}
