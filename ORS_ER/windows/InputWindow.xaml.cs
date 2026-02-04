using System.Windows;
using System.CodeDom.Compiler;

namespace ORS_ER.windows
{
    /// <summary>
    /// Interaction logic for InputWindow.xaml
    /// </summary>
    public partial class InputWindow : Window
    {
        private readonly string _inputName;
        public string? ResultName { get; private set; }
        public dynamic? ResultValue { get; private set; }

        public InputWindow(string inputName, string name, dynamic value)
        {
            InitializeComponent();
            _inputName = inputName;
            NameTextBox.Text = name;
            if (value is bool)
            {
                ValueTextBox.Text = value?.ToString().ToLower() ?? "";

            }
            else
            {
                ValueTextBox.Text = value?.ToString() ?? "";
            }
            ResultName = name;
            ResultValue = value;
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

            ResultName = candidateName;
            if (_inputName.Contains("String"))
            {
                ResultValue = "\"" + ValueTextBox.Text.Replace("\"", "") + "\"";
            }
            else if (_inputName.Contains("Binary"))
            {
                bool val = false;
                success = bool.TryParse(ValueTextBox.Text, out val);
                ResultValue = val;
            }
            else
            {
                double val = 0.0;
                success = double.TryParse(ValueTextBox.Text.ToLower(), out val);
                ResultValue = val;
            }

            if (!success)
            {
                MessageBox.Show("Invalid value entered. Please enter a valid value.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
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
