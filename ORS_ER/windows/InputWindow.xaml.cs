using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using System.Xml.Linq;

namespace ORS_ER.windows
{
    /// <summary>
    /// Interaction logic for InputWindow.xaml
    /// </summary>
    public partial class InputWindow : Window
    {
        public (string, dynamic) Value = ("", null);
        public string Code = "";
        public string Type = "";

        public InputWindow(string Code, (string, dynamic) Value, string Type)
        {
            InitializeComponent();
            this.Code = Code;
            this.Value = Value;
            this.Type = Type;
            NameTextBox.Text = Value.Item1;
            if (Value.Item2 != null)
            {
                ValueTextBox.Text = Value.Item2.ToString();
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            bool success = true;
            var candidateName = NameTextBox.Text?.Trim() ?? string.Empty;

            if (!CodeGenerator.IsValidLanguageIndependentIdentifier(candidateName) || IsCSharpKeyword(candidateName))
            {
                MessageBox.Show("Invalid variable name. Use a valid identifier (letters/digits/_), not starting with a digit, and not a C# keyword.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.Value.Item1 = candidateName;
            if (this.Type.Contains("String"))
            {
                this.Value.Item2 = "\"" + ValueTextBox.Text.Replace("\"", "") + "\"";
            }
            else if (this.Type.Contains("Binary"))
            {
                bool val = false;
                success = bool.TryParse(ValueTextBox.Text, out val);
                this.Value.Item2 = val;
            }
            else
            {
                double val = 0.0;
                success = double.TryParse(ValueTextBox.Text.ToLower(), out val);
                this.Value.Item2 = val;
            }

            if (!success)
            {
                MessageBox.Show("Invalid value entered. Please enter a valid value.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            this.Code = $"dynamic {candidateName} = {this.Value.Item2} ;";
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

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
