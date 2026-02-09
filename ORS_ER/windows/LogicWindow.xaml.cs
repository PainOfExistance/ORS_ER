using System.CodeDom.Compiler;
using System.Windows;

namespace ORS_ER.windows
{
    /// <summary>
    /// Interaction logic for LogicWindow.xaml
    /// </summary>
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
                "AND",
                "OR",
                "NOT",
                "XOR",
                "NOR",
                "XNOR",
                "NAND"
                };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var candidateName = NameTextBox.Text?.Trim() ?? string.Empty;

            if (!CodeGenerator.IsValidLanguageIndependentIdentifier(candidateName) || IsCSharpKeyword(candidateName))
            {
                MessageBox.Show("Invalid variable name. Use a valid identifier (letters/digits/_), not starting with a digit, and not a C# keyword.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            name = candidateName;

            if (LogicTypeComboBox.SelectedItem != null && NameTextBox.Text != "")
            {
                op = (string)LogicTypeComboBox.SelectedItem;
                name = NameTextBox.Text;
                DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            LogicTypeComboBox.SelectedItem = null;
            op = "";
            DialogResult = false;
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
    }
}
