using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System.Windows.Forms;
using System.IO;
using ExpertTools.Helpers;
using System.Data.SqlClient;
using System.Windows.Threading;
using System.Xml.Linq;
using ExpertTools.View;

namespace ExpertTools.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        DateTime startTime;

        #region DependencyProperties

        private bool enabled;
        public bool Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                Set(() => Enabled, ref enabled, value);
            }
        }

        private string state;
        public string State
        {
            get
            {
                return state ?? "";
            }
            set
            {
                Set(() => State, ref state, value);
                Set(() => Title, ref title, $"Expert Tools: {value}");
            }
        }

        private string title;
        public string Title
        {
            get
            {
                return title ?? "Expert Tools";
            }
            set
            {
                Set(() => Title, ref title, value);
            }
        }

        private string timer;
        public string Timer
        {
            get
            {
                return timer ?? "";
            }
            set
            {
                Set(() => Timer, ref timer, value);
            }
        }

        public string ApplicationDatabase
        {
            get
            {
                return Config.Get(() => ApplicationDatabase);
            }
            set
            {
                Config.Set(() => ApplicationDatabase, value);
                RaisePropertyChanged(() => ApplicationDatabase);
            }
        }

        public string TechLogConfFolder
        {
            get
            {
                return Config.Get(() => TechLogConfFolder);
            }
            set
            {
                Config.Set(() => TechLogConfFolder, value);
                RaisePropertyChanged(() => TechLogConfFolder);
            }
        }

        public string TechLogFolder
        {
            get
            {
                return Config.Get(() => TechLogFolder);
            }
            set
            {
                Config.Set(() => TechLogFolder, value);
                RaisePropertyChanged(() => TechLogFolder);
            }
        }

        public bool WriteLog
        {
            get
            {
                return Config.Get(() => WriteLog);
            }
            set
            {
                Config.Set(() => WriteLog, value);
                RaisePropertyChanged(() => WriteLog);
            }
        }

        public string SqlServer
        {
            get
            {
                return Config.Get(() => SqlServer);
            }
            set
            {
                Config.Set(() => SqlServer, value);
                RaisePropertyChanged(() => SqlServer);
            }
        }

        public bool WindowsAuthentication
        {
            get
            {
                return Config.Get(() => WindowsAuthentication);
            }
            set
            {
                Config.Set(() => WindowsAuthentication, value);
                RaisePropertyChanged(() => WindowsAuthentication);
                SqlUser = "";
                SqlPassword = "";
            }
        }

        public string SqlUser
        {
            get
            {
                return Config.Get(() => SqlUser);
            }
            set
            {
                Config.Set(() => SqlUser, value);
                RaisePropertyChanged(() => SqlUser);
            }
        }

        public string SqlPassword
        {
            get
            {
                return Config.Get(() => SqlPassword);
            }
            set
            {
                Config.Set(() => SqlPassword, value);
                RaisePropertyChanged(() => SqlPassword);
            }
        }

        public string SqlTraceFolder
        {
            get
            {
                return Config.Get(() => SqlTraceFolder);
            }
            set
            {
                Config.Set(() => SqlTraceFolder, value);
                RaisePropertyChanged(() => SqlTraceFolder);
            }
        }

        public string CollectPeriod
        {
            get
            {
                return Config.Get(() => CollectPeriod);
            }
            set
            {
                Config.Set(() => CollectPeriod, value);
                RaisePropertyChanged(() => CollectPeriod);
            }
        }

        public string TempFolder
        {
            get
            {
                return Config.Get(() => TempFolder);
            }
            set
            {
                Config.Set(() => TempFolder, value);
                RaisePropertyChanged(() => TempFolder);
            }
        }

        public bool FilterByDatabase
        {
            get
            {
                return Config.Get(() => FilterByDatabase);
            }
            set
            {
                Config.Set(() => FilterByDatabase, value);
                RaisePropertyChanged(() => FilterByDatabase);
                Database1CEnterprise = "";
                DatabaseSql = "";
            }
        }

        public string Database1CEnterprise
        {
            get
            {
                return Config.Get(() => Database1CEnterprise);
            }
            set
            {
                Config.Set(() => Database1CEnterprise, value);
                RaisePropertyChanged(() => Database1CEnterprise);
            }
        }

        public string DatabaseSql
        {
            get
            {
                return Config.Get(() => DatabaseSql);
            }
            set
            {
                Config.Set(() => DatabaseSql, value);
                RaisePropertyChanged(() => DatabaseSql);
            }
        }

        #endregion

        #region DependencyCommands

        public RelayCommand StartCmd { get; set; }
        public RelayCommand LoadSettingsCmd { get; set; }
        public RelayCommand SaveSettingsCmd { get; set; }
        public RelayCommand SelectDatabaseCmd { get; set; }

        #endregion

        public MainViewModel()
        {
            StartCmd = new RelayCommand(Start);
            SaveSettingsCmd = new RelayCommand(SaveSettings);
            LoadSettingsCmd = new RelayCommand(LoadSettings);
            SelectDatabaseCmd = new RelayCommand(SelectDatabase);

            SetTestData();
        }

        private async void Start()
        {
            StartTimer();

            Enabled = true;

            try
            {
                await UpdateState("Подготовка");

                if (!Config.Check())
                {
                    throw new Exception("Не заполнены обязательные для заполнения поля");
                }

                using (var connection = SQL.GetSqlConnection())
                {
                    await connection.OpenAsync();

                    await SQL.CreateDatabaseIfNotExists(connection);
                }

                if (!Common.CheckFolders())
                {
                    var result = MessageBox.Show("Для продолжения работы каталоги логов и временных файлов необходимо очистить, продолжить?", State, MessageBoxButtons.YesNo);

                    if (result == DialogResult.Yes)
                    {
                        await UpdateState("Очистка каталогов");

                        Common.ClearFolders();
                    }
                    else
                    {
                        await UpdateState("Отменено");
                        Enabled = false;
                        StopTimer();
                    }
                }

                await UpdateState("Запуск сбора данных");

                TL.StartCollectTechLog();

                await UpdateState("Ожидание начала сбора данных");

                await TL.WaitStartCollectData();

                await SQL.StartCollectSqlTrace();

                await UpdateState("Сбор данных");

                // Ждем сбора данных
                await Task.Delay(Config.Get<int>("CollectPeriod") * 60 * 1000);

                await UpdateState("Остановка сбора данных");

                TL.StopCollectTechLog();
                await SQL.StopCollectSqlTrace();

                await UpdateState("Обработка данных технологического журнала");

                await TL.ProcessTechLog();

                await UpdateState("Обработка данных Extended Events");

                GC.Collect();

                await SQL.ProcessSqlTrace();

                await UpdateState("Загрузка результатов обработки в базу данных");

                GC.Collect();

                await SQL.LoadTechLogIntoDatabase();
                await SQL.LoadSqlTraceIntoDatabase();

                if (Config.Get<bool>("ClearFoldersAfter"))
                {
                    await UpdateState("Очистка каталогов");

                    Common.ClearFolders();
                }

                await UpdateState("Выполнено");

                GC.Collect();
            }
            catch (Exception ex)
            {
                await UpdateState("Ошибка");

                MessageBox.Show(ex.Message);
            }

            StopTimer();

            Enabled = false;
        }

        /// <summary>
        /// Обновляет строку состояния и заголовок окна
        /// </summary>
        /// <param name="state"></param>
        private async Task UpdateState(string state, bool writeLog = true)
        {
            State = state;

            if (Config.Get<bool>("WriteLog") && writeLog)
            {
                await Common.WriteLog(State);
            }
        }

        private void SetTestData()
        {
            TechLogFolder = @"C:\TechLogTrace";
            TechLogConfFolder = @"C:\Program Files\1cv8\conf";
            TempFolder = @"C:\Users\akpaev.e.ENTERPRISE\Desktop\Temp";
            SqlTraceFolder = @"C:\MSSQLTrace";
        }

        private void StartTimer()
        {
            startTime = DateTime.Now;

            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);

            dispatcherTimer.Tick += (sender, args) => {
                var tmin = Math.Truncate(DateTime.Now.Subtract(startTime).TotalMinutes).ToString();
                var tsec = DateTime.Now.Subtract(startTime).Seconds.ToString();
                Timer = $"{tmin} мин. {tsec} сек.";
            };

            dispatcherTimer.Start();
        }

        private void StopTimer()
        {
            dispatcherTimer.Stop();
        }

        private async void SaveSettings()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Config file (*.cfg)|*.cfg"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    await Config.SaveConfig(dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private async void LoadSettings()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Config file (*.cfg)|*.cfg"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    await Config.LoadConfig(dialog.FileName);

                    Config.SetPropertiesValue(this);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void SelectDatabase()
        {
            var dialog = new SelectDatabaseDialog();

            var result = dialog.ShowDialog();

            if (result != null && (bool)result)
            {
                Database1CEnterprise = dialog.SelectedItem?.Base1C;
                DatabaseSql = dialog.SelectedItem?.BaseSql;
            }
        }
    }
}