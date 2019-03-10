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
using System.Windows.Navigation;
using System.Windows.Shapes;
using GalaSoft.MvvmLight.CommandWpf;

namespace ExpertTools.View
{
    public partial class DirectoryPicker : UserControl
    {
        public static readonly DependencyProperty TitleProperty;
        public static readonly DependencyProperty PathProperty;
        public static readonly DependencyProperty CommandProperty;
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        public string Path
        {
            get { return (string)GetValue(PathProperty); }
            set { SetValue(PathProperty, value); }
        }
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        static DirectoryPicker()
        {
            TitleProperty = DependencyProperty.Register("Title", 
                typeof(string), 
                typeof(DirectoryPicker),
                new FrameworkPropertyMetadata()
                {
                    BindsTwoWayByDefault = true,
                    DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });

            PathProperty = DependencyProperty.Register("Path", 
                typeof(string), 
                typeof(DirectoryPicker),
                new FrameworkPropertyMetadata()
                {
                    BindsTwoWayByDefault = true,
                    DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });

            CommandProperty = DependencyProperty.Register("Command", 
                typeof(ICommand), 
                typeof(DirectoryPicker),
                new FrameworkPropertyMetadata()
                {
                    BindsTwoWayByDefault = true,
                    DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
        }

        public DirectoryPicker()
        {
            InitializeComponent();

            Command = new RelayCommand(SelectCatalog);
        }

        private void SelectCatalog()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (Path != string.Empty)
            {
                dialog.SelectedPath = Path;
            }

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Path = dialog.SelectedPath;
            }
        }
    }
}
