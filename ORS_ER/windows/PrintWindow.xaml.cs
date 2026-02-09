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
        public PrintWindow(string name)
        {
            InitializeComponent();
            VariableComboBox.SelectedItem = VariableComboBox.Items.IndexOf(name);
            ResultName = name;
        }
    }
}
