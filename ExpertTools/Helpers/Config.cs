using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using System.Linq.Expressions;

namespace ExpertTools.Helpers
{
    public static class Config
    {
        private static readonly Dictionary<string, string> settings = new Dictionary<string, string>();

        static Config()
        {
            settings["ApplicationDatabase"] = "ExpertTools";
            settings["TechLogConfFolder"] = "";
            settings["TechLogFolder"] = "";
            settings["SqlServer"] = "localhost";
            settings["WindowsAuthentication"] = "true";
            settings["SqlUser"] = "";
            settings["SqlPassword"] = "";
            settings["SqlTraceFolder"] = "";
            settings["CollectPeriod"] = "20";
            settings["TempFolder"] = "";
            settings["FilterByDatabase"] = "False";
            settings["Database1CEnterprise"] = "";
            settings["DatabaseSql"] = "";
            settings["ClearFoldersAfter"] = "False";
            settings["WriteLog"] = "True";
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
                foreach(var kv in settings)
                {
                    await stream.WriteLineAsync($"{kv.Key} = {kv.Value}");
                }
            }
        }

        /// <summary>
        /// Сохраняет настройки по выбранному пути
        /// </summary>
        /// <param name="path">Путь к файлу настроек</param>
        /// <returns></returns>
        public static async Task LoadConfig(string path)
        {
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

                    settings[pair[0].Trim()] = pair[1].Trim();
                }
            }
        }

        /// <summary>
        /// Устанавливает значение настройки
        /// </summary>
        /// <param name="name">Имя настройки</param>
        /// <param name="value">Значение настройки</param>
        public static void Set(string name, object value)
        {
            if (!settings.ContainsKey(name))
            {
                throw new Exception($"Параметр {name} не существует");
            }

            settings[name] = value.ToString();
        }

        /// <summary>
        /// Устанавливает значение настройки с именем переданного свойства
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="setting"></param>
        /// <param name="value"></param>
        public static void Set<T>(Expression<Func<T>> setting, object value)
        {
            var me = setting.Body as MemberExpression;

            Set(me.Member.Name, value);
        }

        /// <summary>
        /// Возвращает значение настройки
        /// </summary>
        /// <param name="name">Имя настройки</param>
        /// <param name="throwExceptionIfNotSetted">Если установлено true, то при незаданной настройке будет вызвано исключение</param>
        /// <returns>Значение настройки</returns>
        public static T Get<T>(string name)
        {
            if (!settings.ContainsKey(name))
            {
                throw new Exception($"Параметр {name} не существует");
            }

            T value = settings[name] == "" ? default(T) : (T)Convert.ChangeType(settings[name], typeof(T));

            return value;
        }

        /// <summary>
        /// Возвращает значение настройки с именем переданного свойства
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="setting"></param>
        /// <returns></returns>
        public static T Get<T>(Expression<Func<T>> setting)
        {
            var me = setting.Body as MemberExpression;

            return Get<T>(me.Member.Name);
        }

        /// <summary>
        /// Проверяет настройки на корректность заполнения
        /// </summary>
        /// <returns></returns>
        public static bool Check()
        {
            if (Get<string>("ApplicationDatabase") == "") return false;
            if (Get<string>("TechLogConfFolder") == "") return false;
            if (Get<string>("TechLogFolder") == "") return false;
            if (Get<string>("SqlServer") == "") return false;

            if (!Get<bool>("WindowsAuthentication"))
            {
                if (Get<string>("SqlUser") == "") return false;
                if (Get<string>("SqlPassword") == "") return false;
            }

            if (Get<string>("SqlTraceFolder") == "") return false;
            if (Get<string>("CollectPeriod") == "") return false;
            if (Get<string>("TempFolder") == "") return false;

            if (!Get<bool>("FilterByDatabase"))
            {
                if (Get<string>("Database1CEnterprise") == "") return false;
                if (Get<string>("DatabaseSql") == "") return false;
            }

            return true;
        }

        /// <summary>
        /// Устанавливает значения свойств объекта по соответствию имен с настройками
        /// </summary>
        /// <param name="obj">Обрабатываемый объект</param>
        public static void SetPropertiesValue(object obj)
        {
            var type = obj.GetType();

            var data = settings.ToArray();

            foreach (var setting in data)
            {
                var property = type.GetProperty(setting.Key);

                if (property != null)
                {
                    property.SetValue(obj, Convert.ChangeType(setting.Value, property.PropertyType));
                }
            }
        }
    }
}
