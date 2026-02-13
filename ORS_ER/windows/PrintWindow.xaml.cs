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
    /// Interaction logic for PrintWindow.xaml
    /// </summary>
    public partial class PrintWindow : Window
    {
        public (string, dynamic) Value = ("", null);
        public string Code = "";
        public PrintWindow(string Code, (string, dynamic) Value)
        {
            InitializeComponent();
            this.Code = Code;
            this.Value = Value;
            VariableComboBox.Text = Value.Item1;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Value.Item1 = VariableComboBox.Text?.Trim() ?? string.Empty;
            if (VariableComboBox.Text?.Trim() == "")
            {
                this.Code = "";
            }
            else
            {
                this.Code = $"Console.WriteLine({this.Value.Item1});";
            }

            DialogResult = true;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
