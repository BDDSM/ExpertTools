using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using ExpertTools.Model;

namespace ExpertTools.Helpers
{
    public static class Config
    {
        // Настройки технологического журнала
        public static string TechLogConfFolder { get; set; } = "";
        public static string TechLogFolder { get; set; } = "";
        public static string Database1CEnterprise { get; set; } = "";
        // Настройки СУБД
        public static string SqlServer { get; set; } = "";
        public static bool WindowsAuthentication { get; set; } = false;
        public static string SqlUser { get; set; } = "";
        public static string SqlPassword { get; set; } = "";
        public static string SqlTraceFolder { get; set; } = "";
        public static string DatabaseSql { get; set; } = "";
        // Общие настройки
        public static AnalyzeType AnalyzeType { get; set; } = 0;
        public static int CollectPeriod { get; set; } = 0;
        public static string TempFolder { get; set; } = "";
        public static bool FilterByDatabase { get; set; } = false;
        public static string ApplicationDatabase { get; set; } = "";
        public static bool ClearFoldersAfter { get; set; } = false;
        public static bool WriteLog { get; set; } = false;

        static Config()
        {
            AnalyzeType = AnalyzeType.QueriesAnalyze;
            ApplicationDatabase = "ExpertTools";
            SqlServer = "localhost";
            WindowsAuthentication = true;
            CollectPeriod = 20;
            FilterByDatabase = false;
            ClearFoldersAfter = false;
            WriteLog = true;
        }

        /// <summary>
        /// Загружает выбранный файл настройки
        /// </summary>
        /// <param name="path">Путь к файлу настроек</param>
        /// <returns></returns>
        public static async Task SaveConfig(string path)
        {
            using (var stream = new StreamWriter(path))
            {
                await SaveConfigElement(stream, () => ApplicationDatabase);
                await SaveConfigElement(stream, () => TechLogConfFolder);
                await SaveConfigElement(stream, () => TechLogFolder);
                await SaveConfigElement(stream, () => SqlServer);
                await SaveConfigElement(stream, () => WindowsAuthentication);
                await SaveConfigElement(stream, () => SqlUser);
                await SaveConfigElement(stream, () => SqlPassword);
                await SaveConfigElement(stream, () => SqlTraceFolder);
                await SaveConfigElement(stream, () => CollectPeriod);
                await SaveConfigElement(stream, () => TempFolder);
                await SaveConfigElement(stream, () => FilterByDatabase);
                await SaveConfigElement(stream, () => Database1CEnterprise);
                await SaveConfigElement(stream, () => DatabaseSql);
                await SaveConfigElement(stream, () => ClearFoldersAfter);
                await SaveConfigElement(stream, () => WriteLog);
            }
        }

        private static async Task SaveConfigElement<T>(StreamWriter writer, Expression<Func<T>> property, string comment = "")
        {
            var me = property.Body as MemberExpression;

            var name = me.Member.Name;
            var value = ((PropertyInfo)me.Member).GetValue(property).ToString();

            if (comment != "")
            {
                await writer.WriteLineAsync($"// {comment}");
            }

            await writer.WriteLineAsync($"{name} = {value}");
        }

        /// <summary>
        /// Сохраняет настройки по выбранному пути
        /// </summary>
        /// <param name="path">Путь к файлу настроек</param>
        /// <returns></returns>
        public static async Task LoadConfig(string path)
        {
            var thisType = typeof(Config);

            using (var stream = new StreamReader(path))
            {
                while(!stream.EndOfStream)
                {
                    var data = await stream.ReadLineAsync();

                    if (data.Trim() == "") continue;

                    var pair = data.Split('=');

                    if (pair.Count() != 2)
                    {
                        throw new Exception("Ошибка чтения файла конфигурации");
                    }

                    var name = pair[0].Trim();
                    var value = pair[1].Trim();

                    var property = thisType.GetProperty(name);

                    if (property != null)
                    {
                        property.SetValue(name, Convert.ChangeType(value, property.PropertyType));
                    }
                }
            }
        }

        /// <summary>
        /// Проверяет настройки на корректность заполнения
        /// </summary>
        /// <returns></returns>
        public static void CheckSettings()
        {
            CheckFieldFilling(AnalyzeType.ToString(), "Тип анализа");
            CheckFieldFilling(ApplicationDatabase, "База данных приложения");
            CheckFieldFilling(TechLogConfFolder, "Каталог настроек технологического журнала");
            CheckFieldFilling(TechLogFolder, "Каталог логов технологического журнала");
            CheckFieldFilling(SqlServer, "Адрес СУБД");

            if (!WindowsAuthentication)
            {
                CheckFieldFilling(SqlUser, "Пользователь СУБД");
                CheckFieldFilling(SqlPassword, "Пароль пользователя СУБД");
            }

            CheckFieldFilling(SqlTraceFolder, "Каталог логов СУБД");
            CheckFieldFilling(CollectPeriod.ToString(), "Период сбора данных");
            CheckFieldFilling(TempFolder, "Каталог временных файлов");

            if (FilterByDatabase)
            {
                CheckFieldFilling(Database1CEnterprise, "База данных 1С");
                CheckFieldFilling(DatabaseSql, "База данных СУБД");
            }
        }

        /// <summary>
        /// Проверяет каталоги данных и возвращает true, если требуется их очистка
        /// </summary>
        /// <returns></returns>
        public static bool CheckFolders()
        {
            CheckFolder(TechLogConfFolder, true);

            CheckFolder(TempFolder, true);

            if (!CheckFolderEmpty(TempFolder))
            {
                return false;
            }

            CheckFolder(SqlTraceFolder);

            if (!CheckFolderEmpty(SqlTraceFolder))
            {
                return false;
            }

            CheckFolder(TechLogFolder);

            if (!CheckFolderEmpty(TechLogFolder))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Проверяет существование папки и возможность записи в нее, при необходимости
        /// </summary>
        /// <param name="path">Путь к папке</param>
        /// <param name="checkWritinig">Признак необходимости проверки возможности записи</param>
        private static void CheckFolder(string path, bool checkWritinig = false)
        {
            CheckFolderExist(path);

            if (checkWritinig)
            {
                CheckFolderWriting(path);
            }
        }

        /// <summary>
        /// Проверяет существование папки
        /// </summary>
        /// <param name="path">Путь к папке</param>
        private static void CheckFolderExist(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new Exception($"Каталог по пути \"{path}\" не найден");
            }
        }

        /// <summary>
        /// Проверяет возможность записи в папку
        /// </summary>
        /// <param name="path">Путь к папке</param>
        private static void CheckFolderWriting(string path)
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

        /// <summary>
        /// Проверяет наличие вложенных элементов в папке
        /// </summary>
        /// <param name="path">Путь к папке</param>
        /// <returns></returns>
        private static bool CheckFolderEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        /// <summary>
        /// Проверяет заполнение значения настройки
        /// </summary>
        /// <param name="value">Значение настройки</param>
        /// <param name="msgName"></param>
        private static void CheckFieldFilling(string value, string msgName)
        {
            if (value == string.Empty)
            {
                throw new Exception($"Не указано значение настройки \"{msgName}\"");
            }
        }
    }
}
