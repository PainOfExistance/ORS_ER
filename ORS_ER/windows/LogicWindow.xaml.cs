using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ORS_ER.windows
{
    /// <summary>
    /// Interaction logic for LogicWindow.xaml
    /// </summary>
    public partial class LogicWindow : Window
    {
        public string op = "==";
        public LogicWindow(string operation)
        {
            InitializeComponent();
            op = operation;
            LogicTypeComboBox.Text = operation;
            LogicTypeComboBox.ItemsSource = new List<string>
            {
                "==",
                "!=",
                "<",
                "<=",
                ">",
                ">="
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (LogicTypeComboBox.SelectedItem != null)
            {
                op = (string)LogicTypeComboBox.SelectedItem;
                DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            LogicTypeComboBox.SelectedItem = null;
            op = "";
            DialogResult = false;
        }
    }
}
