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
                return Config.ApplicationDatabase;
            }
            set
            {
                Config.ApplicationDatabase = value;
                RaisePropertyChanged(() => ApplicationDatabase);
            }
        }

        public string TechLogConfFolder
        {
            get
            {
                return Config.TechLogConfFolder;
            }
            set
            {
                Config.TechLogConfFolder = value;
                RaisePropertyChanged(() => TechLogConfFolder);
            }
        }

        public string TechLogFolder
        {
            get
            {
                return Config.TechLogFolder;
            }
            set
            {
                Config.TechLogFolder = value;
                RaisePropertyChanged(() => TechLogFolder);
            }
        }

        public bool WriteLog
        {
            get
            {
                return Config.WriteLog;
            }
            set
            {
                Config.WriteLog = value;
                RaisePropertyChanged(() => WriteLog);
            }
        }

        public string SqlServer
        {
            get
            {
                return Config.SqlServer;
            }
            set
            {
                Config.SqlServer = value;
                RaisePropertyChanged(() => SqlServer);
            }
        }

        public bool WindowsAuthentication
        {
            get
            {
                return Config.WindowsAuthentication;
            }
            set
            {
                Config.WindowsAuthentication = value;
                RaisePropertyChanged(() => WindowsAuthentication);
                SqlUser = "";
                SqlPassword = "";
            }
        }

        public string SqlUser
        {
            get
            {
                return Config.SqlUser;
            }
            set
            {
                Config.SqlUser = value;
                RaisePropertyChanged(() => SqlUser);
            }
        }

        public string SqlPassword
        {
            get
            {
                return Config.SqlPassword;
            }
            set
            {
                Config.SqlPassword = value;
                RaisePropertyChanged(() => SqlPassword);
            }
        }

        public string SqlTraceFolder
        {
            get
            {
                return Config.SqlTraceFolder;
            }
            set
            {
                Config.SqlTraceFolder = value;
                RaisePropertyChanged(() => SqlTraceFolder);
            }
        }

        public int CollectPeriod
        {
            get
            {
                return Config.CollectPeriod;
            }
            set
            {
                Config.CollectPeriod = value;
                RaisePropertyChanged(() => CollectPeriod);
            }
        }

        public string TempFolder
        {
            get
            {
                return Config.TempFolder;
            }
            set
            {
                Config.TempFolder = value;
                RaisePropertyChanged(() => TempFolder);
            }
        }

        public bool FilterByDatabase
        {
            get
            {
                return Config.FilterByDatabase;
            }
            set
            {
                Config.FilterByDatabase = value;
                RaisePropertyChanged(() => FilterByDatabase);
                Database1CEnterprise = "";
                DatabaseSql = "";
            }
        }

        public string Database1CEnterprise
        {
            get
            {
                return Config.Database1CEnterprise;
            }
            set
            {
                Config.Database1CEnterprise = value;
                RaisePropertyChanged(() => Database1CEnterprise);
            }
        }

        public string DatabaseSql
        {
            get
            {
                return Config.DatabaseSql;
            }
            set
            {
                Config.DatabaseSql = value;
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
            StartCmd = new RelayCommand(StartNew);
            SaveSettingsCmd = new RelayCommand(SaveSettings);
            LoadSettingsCmd = new RelayCommand(LoadSettings);
            SelectDatabaseCmd = new RelayCommand(SelectDatabase);
        }

        private async void StartNew()
        {
            StartTimer();

            Enabled = true;

            await UpdateState("Выполняется");

            try
            {
                var analyzer = new Core.QueriesAnalyzer(TechLogConfFolder, TechLogFolder, SqlTraceFolder, TempFolder, CollectPeriod);

                if (FilterByDatabase)
                {
                    analyzer.SetDatabaseFilter(Database1CEnterprise);
                }

                await analyzer.Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            await UpdateState("Завершено");

            Enabled = false;

            StopTimer();
        }

        private async void Start()
        {
            StartTimer();

            Enabled = true;

            try
            {
                if (!await CheckBeforeStart())
                {
                    return;
                }

                object analyzer = new QueriesAnalyze();

                if (analyzer is ITlAnalyzer tc)
                {
                    await UpdateState("Запуск технологического журнала");

                    await tc.StartCollectTlData();
                }

                if (analyzer is ISqlAnalyzer sc)
                {
                    await UpdateState("Запуск сессии XE");

                    await sc.StartCollectSqlData();
                }

                await UpdateState("Сбор данных");

                await Task.Delay(Config.CollectPeriod * 60 * 1000);

                if (analyzer is ITlAnalyzer tcs)
                {
                    await UpdateState("Остановка технологического журнала");

                    tcs.StopCollectTlData();
                }

                if (analyzer is ISqlAnalyzer scs)
                {
                    await UpdateState("Остановка сессии XE");

                    await scs.StopCollectSqlData();
                }

                if (analyzer is ITlAnalyzer th)
                {
                    await UpdateState("Обработка технологического журнала");

                    await th.HandleTlData();
                }

                if (analyzer is ISqlAnalyzer sh)
                {
                    await UpdateState("Обработка сессии XE");

                    await sh.HandleSqlData();
                }

                if (analyzer is ITlAnalyzer tl)
                {
                    await UpdateState("Загрузка обработанных данных технологического журнала");

                    await tl.LoadTlDataIntoDatabase();
                }

                if (analyzer is ISqlAnalyzer sl)
                {
                    await UpdateState("Загрузка обработанных данных сессии XE");

                    await sl.LoadSqlDataIntoDatabase();
                }

                await UpdateState("Выполнено");
            }
            catch (Exception ex)
            {
                await UpdateState("Ошибка");

                MessageBox.Show(ex.Message, "Ошибка");
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

            if (Config.WriteLog && writeLog)
            {
                await Common.WriteLog(State);
            }
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

                    RaisePropertyChanged("");
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

        private async Task<bool> CheckBeforeStart()
        {
            await UpdateState("Подготовка");

            Config.CheckSettings();

            await SQL.CreateDatabaseIfNotExists();

            if (!Config.CheckFolders())
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
                    return false;
                }
            }

            return true;
        }
    }
}