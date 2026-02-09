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
        public string? ResultName { get; private set; }
        public dynamic? ResultValue { get; private set; }
        public PrintWindow(string name, dynamic value)
        {
            InitializeComponent();
            VariableComboBox.SelectedItem = name;
            ResultName = name;
            ResultValue = value;

        }
    }
}
