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
    /// Interaction logic for InputWindow.xaml
    /// </summary>
    public partial class InputWindow : Window
    {
        public string? ResultName { get; private set; }
        public string? ResultValue { get; private set; }

        public InputWindow(string name, dynamic value)
        {
            InitializeComponent();
            NameTextBox.Text = name;
            ValueTextBox.Text = value?.ToString() ?? "";
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            ResultName = NameTextBox.Text;
            ResultValue = ValueTextBox.Text;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ResultName = NameTextBox.Text;
            ResultValue = ValueTextBox.Text;
        }
    }
}
