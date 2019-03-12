using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ExpertTools.ViewModel;

namespace ExpertTools.View
{
    /// <summary>
    /// Логика взаимодействия для SelectDatabaseDialog.xaml
    /// </summary>
    public partial class SelectDatabaseDialog : Window
    {
        public DatabaseItem SelectedItem { get; private set; }

        public SelectDatabaseDialog()
        {
            InitializeComponent();

            Owner = App.Current.MainWindow;

            DataContext = new SelectDatabaseDialogViewModel();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SetResult();
        }

        private void SetResult()
        {
            SelectedItem = ((SelectDatabaseDialogViewModel)DataContext).SelectedItem;

            DialogResult = true;
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SetResult();
        }
    }
}
