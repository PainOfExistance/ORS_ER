using ORS_ER.connections;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;

namespace ORS_ER.windows
{
    public partial class LogicWindow : Window
    {
        public (string, dynamic) Value = ("", null);
        public string Code = "";
        public LogicWindow(string Code, (string, dynamic) Value)
        {
            InitializeComponent();
            this.Code = Code;
            this.Value = Value;
            LogicTypeComboBox.ItemsSource = new List<string>
                {
                "+",
                "-",
                "*",
                "/",
                "%",
                "^",
                "==",
                "!=",
                "<",
                "<=",
                ">",
                ">=",
                };

            var parts = Code.Split(" ").ToList();
            foreach(var part in parts)
            {
                Debug.WriteLine($"Part: {part}");
            }
            if (parts.Count > 1 && parts[0] != "")
            {
                NameTextBox.Text = parts[1];
                Variable1.Text = parts[3];
                Variable2.Text = parts[5];
                LogicTypeComboBox.SelectedItem = parts[4];
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var candidateName = NameTextBox.Text?.Trim() ?? string.Empty;
            var leftOperand = Variable1.Text?.Trim() ?? string.Empty;
            var rightOperand = Variable2.Text?.Trim() ?? string.Empty;

            if (IsCSharpKeyword(leftOperand) || IsCSharpKeyword(rightOperand) || LogicTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Invalid variable name or not selected item.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.Value.Item1 = candidateName;
            this.Value.Item2 = LogicTypeComboBox.SelectedItem;
            this.Code = $"dynamic {candidateName} = {Variable1.Text.Replace('"', '\"')} {LogicTypeComboBox.SelectedItem} {Variable2.Text.Replace('"', '\"')} ;".Replace("  ", " ");
            DialogResult = true;
        }


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }


        private static bool IsCSharpKeyword(string name) => name switch
        {
            "abstract" or "as" or "base" or "bool" or "break" or "byte" or "case" or "catch" or "char" or "checked" or "class" or
            "const" or "continue" or "decimal" or "default" or "delegate" or "do" or "double" or "else" or "enum" or "event" or
            "explicit" or "extern" or "finally" or "fixed" or "float" or "for" or "foreach" or "goto" or "if" or
            "implicit" or "in" or "int" or "interface" or "internal" or "is" or "lock" or "long" or "namespace" or "new" or
            "null" or "object" or "operator" or "out" or "override" or "params" or "private" or "protected" or "public" or
            "readonly" or "ref" or "return" or "sbyte" or "sealed" or "short" or "sizeof" or "stackalloc" or "static" or
            "string" or "struct" or "switch" or "this" or "throw" or "try" or "typeof" or "uint" or "ulong" or
            "unchecked" or "unsafe" or "ushort" or "using" or "virtual" or "void" or "volatile" or "while" => true,
            _ => false
        };

        private void LogicTypeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (LogicTypeComboBox.SelectedItem == "NOT")
            {
                Variable1.IsEnabled = false;
                Variable1.Text = "";
                return;
            }

            Variable1.IsEnabled = true;
        }
    }
}
