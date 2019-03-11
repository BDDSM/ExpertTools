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
using System.Text.RegularExpressions;

namespace ExpertTools.View
{
    public partial class TextField : UserControl
    {
        public static readonly DependencyProperty TitleProperty;
        public static readonly DependencyProperty ValueProperty;
        public static readonly DependencyProperty MaxLengthProperty;
        public static readonly DependencyProperty OnlyDigitsProperty;

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
        public int MaxLength
        {
            get { return (int)GetValue(MaxLengthProperty); }
            set { SetValue(MaxLengthProperty, value); }
        }
        public bool OnlyDigits
        {
            get { return (bool)GetValue(OnlyDigitsProperty); }
            set { SetValue(OnlyDigitsProperty, value); }
        }

        static TextField()
        {
            TitleProperty = DependencyProperty.Register("Title", 
                typeof(string), 
                typeof(TextField), 
                new FrameworkPropertyMetadata() {
                    BindsTwoWayByDefault = true,
                    DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });

            ValueProperty = DependencyProperty.Register("Value", 
                typeof(string), 
                typeof(TextField),
                new FrameworkPropertyMetadata()
                {
                    BindsTwoWayByDefault = true,
                    DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });

            MaxLengthProperty = DependencyProperty.Register("MaxLength", 
                typeof(int), 
                typeof(TextField),
                new FrameworkPropertyMetadata()
                {
                    BindsTwoWayByDefault = true,
                    DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });

            OnlyDigitsProperty = DependencyProperty.Register("OnlyDigits",
                typeof(bool),
                typeof(TextField),
                new FrameworkPropertyMetadata()
                {
                    BindsTwoWayByDefault = true,
                    DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
        }

        public TextField()
        {
            InitializeComponent();
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (OnlyDigits)
            {
                e.Handled = !Regex.IsMatch(e.Text, @"\d");
            }
        }
    }
}
