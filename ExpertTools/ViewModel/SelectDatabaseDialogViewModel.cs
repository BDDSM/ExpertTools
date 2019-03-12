using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using ExpertTools.Helpers;

namespace ExpertTools.ViewModel
{
    public class SelectDatabaseDialogViewModel : ViewModelBase
    {
        private DatabaseItem selectedItem;
        public DatabaseItem SelectedItem
        {
            get { return selectedItem; }
            set { Set(() => SelectedItem, ref selectedItem, value); }
        }

        private ObservableCollection<DatabaseItem> databases;
        public ObservableCollection<DatabaseItem> Databases
        {
            get { return databases; }
            set { Set(() => Databases, ref databases, value); }
        }

        public SelectDatabaseDialogViewModel()
        {
            Databases = new ObservableCollection<DatabaseItem>();
            Databases.CollectionChanged += (sender, args) => { RaisePropertyChanged(() => Databases); };

            UpdateDatabasesList();
        }

        private async void UpdateDatabasesList()
        {
            Databases = new ObservableCollection<DatabaseItem>(await Common.GetBases());
        }
    }
}
