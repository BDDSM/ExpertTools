using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System.Windows.Forms;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;

namespace ExpertTools.ViewModel
{
    public class QueriesAnalyzeViewModel : ViewModelBase
    {
        private string sqlServer;
        [Setting]
        public string SqlServer
        {
            get { return sqlServer; }
            set { Set(() => SqlServer, ref sqlServer, value); }
        }

        private bool integratedSecurity;
        [Setting]
        public bool IntegratedSecurity
        {
            get { return integratedSecurity; }
            set { Set(() => IntegratedSecurity, ref integratedSecurity, value); }
        }

        private string sqlUser;
        [Setting]
        public string SqlUser
        {
            get { return sqlUser; }
            set { Set(() => SqlUser, ref sqlUser, value); }
        }

        private string sqlUserPassword;
        [Setting]
        public string SqlUserPassword
        {
            get { return sqlUserPassword; }
            set { Set(() => SqlUserPassword, ref sqlUserPassword, value); }
        }

        private string sqlTraceFolder;
        [Setting]
        public string SqlTraceFolder
        {
            get { return sqlTraceFolder; }
            set { Set(() => SqlTraceFolder, ref sqlTraceFolder, value); }
        }

        private string tlConfFolder;
        [Setting]
        public string TlConfFolder
        {
            get { return tlConfFolder; }
            set { Set(() => TlConfFolder, ref tlConfFolder, value); }
        }

        private string tlFolder;
        [Setting]
        public string TlFolder
        {
            get { return tlFolder; }
            set { Set(() => TlFolder, ref tlFolder, value); }
        }

        private string tempFolder;
        [Setting]
        public string TempFolder
        {
            get { return tempFolder; }
            set { Set(() => TempFolder, ref tempFolder, value); }
        }

        private int collectPeriod;
        [Setting]
        public int CollectPeriod
        {
            get { return collectPeriod; }
            set { Set(() => CollectPeriod, ref collectPeriod, value); }
        }

        private bool filterByDatabase;
        [Setting]
        public bool FilterByDatabase
        {
            get { return filterByDatabase; }
            set { Set(() => FilterByDatabase, ref filterByDatabase, value); }
        }

        private string database1C;
        [Setting]
        public string Database1C
        {
            get { return database1C; }
            set { Set(() => Database1C, ref database1C, value); }
        }

        private string databaseSQL;
        [Setting]
        public string DatabaseSQL
        {
            get { return databaseSQL; }
            set { Set(() => DatabaseSQL, ref databaseSQL, value); }
        }

        private bool filterByDuration;
        [Setting]
        public bool FilterByDuration
        {
            get { return filterByDuration; }
            set { Set(() => FilterByDuration, ref filterByDuration, value); }
        }

        private int duration;
        [Setting]
        public int Duration
        {
            get { return duration; }
            set { Set(() => Duration, ref duration, value); }
        }

        public RelayCommand SelectTlConfFolderCmd { get; set; }
        public RelayCommand SelectTlFolderCmd { get; set; }
        public RelayCommand SelectSqlTraceFolderCmd { get; set; }
        public RelayCommand SelectTempFolderCmd { get; set; }

        public RelayCommand StartCmd { get; set; }

        public RelayCommand LoadSettingsCmd { get; set; }
        public RelayCommand SaveSettingsCmd { get; set; }


        public QueriesAnalyzeViewModel()
        {
            SelectTlConfFolderCmd = new RelayCommand(SelectTlConfFolder);
            SelectTlFolderCmd = new RelayCommand(SelectTlFolder);
            SelectSqlTraceFolderCmd = new RelayCommand(SelectSqlTraceConfFolder);
            SelectTempFolderCmd = new RelayCommand(SelectTempFolder);
            StartCmd = new RelayCommand(Start);
            LoadSettingsCmd = new RelayCommand(LoadSettings);
            SaveSettingsCmd = new RelayCommand(SaveSettings);

            IntegratedSecurity = true;
        }

        private void SelectTlConfFolder()
        {
            TlConfFolder = SelectFolder(TlConfFolder);
        }

        private void SelectTlFolder()
        {
            TlFolder = SelectFolder(TlFolder);
        }

        private void SelectSqlTraceConfFolder()
        {
            SqlTraceFolder = SelectFolder(SqlTraceFolder);
        }

        private void SelectTempFolder()
        {
            TempFolder = SelectFolder(TempFolder);
        }

        private async void Start()
        {
            MessengerInstance.Send(StartMessage.AnalyzeStarted());

            try
            {
                var settings = Common.GetAnalyzerSettings<QueriesAnalyzeViewModel, QueriesAnalyzerSettings>(this);

                var analyzer = new QueriesAnalyzer(settings);
                await analyzer.Run();

                MessengerInstance.Send(StartMessage.SuccessfullyCompeled());
            }
            catch
            {
                MessengerInstance.Send(StartMessage.UnsuccessfullyCompeled());
            }
        }

        private void SaveSettings()
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "Config file (*.cfg)|*.cfg"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Common.WriteConfigFile(this, dialog.FileName);
            }
        }

        private void LoadSettings()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Config file (*.cfg)|*.cfg",
                Multiselect = false
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Common.ReadConfigFile(this, dialog.FileName);
            }
        }

        private string SelectFolder(string currentPath = "")
        {
            var dialog = new FolderBrowserDialog();
            dialog.SelectedPath = currentPath;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.SelectedPath;
            }
            else
            {
                return currentPath;
            }
        }
    }
}