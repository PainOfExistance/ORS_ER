using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for IfWindow.xaml
    /// </summary>
    public partial class IfWindow : Window
    {
        public (string, dynamic) Value = ("", null);
        public string Code = "";
        public IfWindow(string Code, (string, dynamic) Value)
        {
            InitializeComponent();
            this.Code = Code;
            this.Value = Value;
            LogicTypeComboBox.ItemsSource = new List<string>
                {
                "==",
                "!=",
                "<",
                "<=",
                ">",
                ">="
                };

            var parts = Code.Split(" ").ToList();
            Debug.WriteLine($"Code: {parts.Count()}");
            if (parts.Count > 0 && parts[0] != "")
            {
                Variable1.Text = parts[1];
                Variable2.Text = parts[3];
                LogicTypeComboBox.SelectedItem = parts[2];
            }
        }
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var candidateName1 = Variable1.Text?.Trim() ?? string.Empty;
            var candidateName2 = Variable2.Text?.Trim() ?? string.Empty;

            if (IsCSharpKeyword(candidateName1) || IsCSharpKeyword(candidateName2) || LogicTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Invalid variable name or not selected item.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.Value.Item2 = LogicTypeComboBox.SelectedItem;
            this.Code = $"if( {Variable1.Text.Replace('"', '\"')} {LogicTypeComboBox.SelectedItem} {Variable2.Text.Replace('"', '\"')} )".Replace("  ", " ");
            DialogResult = true;
        }
        private static bool IsCSharpKeyword(string name) => name switch
        {
            "abstract" or "as" or "base" or "bool" or "break" or "byte" or "case" or "catch" or "char" or "checked" or "class" or
            "const" or "continue" or "decimal" or "default" or "delegate" or "do" or "double" or "else" or "enum" or "event" or
            "explicit" or "extern" or "false" or "finally" or "fixed" or "float" or "for" or "foreach" or "goto" or "if" or
            "implicit" or "in" or "int" or "interface" or "internal" or "is" or "lock" or "long" or "namespace" or "new" or
            "null" or "object" or "operator" or "out" or "override" or "params" or "private" or "protected" or "public" or
            "readonly" or "ref" or "return" or "sbyte" or "sealed" or "short" or "sizeof" or "stackalloc" or "static" or
            "string" or "struct" or "switch" or "this" or "throw" or "true" or "try" or "typeof" or "uint" or "ulong" or
            "unchecked" or "unsafe" or "ushort" or "using" or "virtual" or "void" or "volatile" or "while" => true,
            _ => false
        };
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
