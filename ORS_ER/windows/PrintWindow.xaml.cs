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
    public partial class PrintWindow : Window
    {
        public (string, dynamic) Value = ("", null);
        public string Code = "";
        private readonly Dictionary<string, dynamic> variables;
        public PrintWindow(string Code, (string, dynamic) Value, Dictionary<string, dynamic> variables)
        {
            InitializeComponent();
            this.Code = Code;
            this.Value = Value;
            this.variables = variables;
            VariableComboBox.ItemsSource = variables.Keys;

            bool hasVariable = false;
            string querryKey = Value.Item1 == null ? "" : Value.Item1.ToString();
            if (variables.Count() != 0)
            {
                hasVariable = variables.ContainsKey(querryKey);
            }
            PrintVariableRadioButton.IsChecked = hasVariable;
            PrintTextRadioButton.IsChecked = !hasVariable;
            if (hasVariable)
            {
                VariableComboBox.SelectedItem = Value.Item1;
            }

            if (!hasVariable)
            {
                PrintTextTextBox.Text = Value.Item1;
            }

            Loaded += (_, __) => UpdateInputVisibility();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (PrintTextRadioButton.IsChecked == true)
            {
                SaveTextPrintValue();
                DialogResult = true;
                return;
            }

            SaveVariablePrintValue();
            DialogResult = true;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void PrintMode_Checked(object sender, RoutedEventArgs e)
        {
            UpdateInputVisibility();
        }

        private void UpdateInputVisibility()
        {
            var isTextMode = PrintTextRadioButton.IsChecked == true;
            VariableComboBox.Visibility = isTextMode ? Visibility.Collapsed : Visibility.Visible;
            PrintTextTextBox.Visibility = isTextMode ? Visibility.Visible : Visibility.Collapsed;
            if (isTextMode)
                VaraibleTextLabel.Content = "Set text to print:";
            else
                VaraibleTextLabel.Content = "Select variable to print:";

        }

        private void SaveVariablePrintValue()
        {
            var variableName = VariableComboBox.Text?.Trim() ?? string.Empty;
            Value.Item1 = variableName;
            if (string.IsNullOrWhiteSpace(variableName))
            {
                Code = "";
                return;
            }

            Code = $"Console.WriteLine({variableName});";
        }

        private void SaveTextPrintValue()
        {
            var textToPrint = PrintTextTextBox.Text?.Trim() ?? string.Empty;
            Value.Item1 = textToPrint;
            if (string.IsNullOrWhiteSpace(textToPrint))
            {
                Code = "";
                return;
            }

            var escapedText = EscapePrintText(textToPrint);
            Code = $"Console.WriteLine(\"{escapedText}\");";
        }

        private static string EscapePrintText(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }
    }
}
