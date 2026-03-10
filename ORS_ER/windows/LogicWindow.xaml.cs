using ORS_ER.connections;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace ORS_ER.windows
{
    public partial class LogicWindow : Window
    {
        private enum OperandType
        {
            Unknown,
            Number,
            Boolean,
            String,
            Array
        }

        public (string, dynamic) Value = ("", null);
        public string Code = "";
        private readonly Dictionary<string, dynamic> variables;
        private readonly List<string> allOperations = new()
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
        public LogicWindow(string Code, (string, dynamic) Value, Dictionary<string, dynamic> variables)
        {
            InitializeComponent();
            this.Code = Code;
            this.Value = Value;
            this.variables = variables;
            LogicTypeComboBox.ItemsSource = allOperations;
            var scalarVariables = variables.Where(kv => kv.Value is not ArrayValue).Select(kv => kv.Key).ToList();
            Variable1.ItemsSource = scalarVariables;
            Variable2.ItemsSource = scalarVariables;

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

            UpdateOperationOptions();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var candidateName = NameTextBox.Text?.Trim() ?? string.Empty;
            var leftInput = Variable1.Text?.Trim() ?? string.Empty;
            var rightInput = Variable2.Text?.Trim() ?? string.Empty;

            if (LogicTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Invalid variable name or not selected item.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var leftType = GetOperandType(leftInput);
            var rightType = GetOperandType(rightInput);
            if (!IsCompatible(leftType, rightType))
            {
                MessageBox.Show("Operands must be the same type.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!IsOperationAllowed(LogicTypeComboBox.SelectedItem?.ToString(), leftType))
            {
                MessageBox.Show("Operation is not valid for the selected operand types.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (IsCSharpKeyword(candidateName))
            {
                MessageBox.Show("Invalid variable name or not selected item.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (IsReservedVariableReference(leftInput) || IsReservedVariableReference(rightInput))
            {
                MessageBox.Show("Invalid variable name or not selected item.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(candidateName))
            {
                MessageBox.Show("Variable name cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var leftOperand = BuildOperand(leftInput);
            var rightOperand = BuildOperand(rightInput);

            Value.Item1 = candidateName;
            Value.Item2 = LogicTypeComboBox.SelectedItem;
            Code = $"dynamic {candidateName} = {leftOperand} {LogicTypeComboBox.SelectedItem} {rightOperand} ;".Replace("  ", " ");
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
                Variable1.Text = string.Empty;
                return;
            }

            Variable1.IsEnabled = true;
        }

        private void VariableInput_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateOperationOptions();
        }

        private void VariableInput_KeyUp(object sender, KeyEventArgs e)
        {
            UpdateOperationOptions();
        }

        private void VariableInput_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateOperationOptions();
        }

        private bool IsVariableReference(string value) => variables.ContainsKey(value);

        private bool IsReservedVariableReference(string value) => IsVariableReference(value) && IsCSharpKeyword(value);

        private bool IsSameReference(string candidateName, string input) => IsVariableReference(input) && candidateName == input;

        private string BuildOperand(string input)
        {
            if (IsVariableReference(input))
            {
                return input;
            }

            if (IsBooleanLiteral(input))
            {
                return input.ToLowerInvariant();
            }

            if (IsNumericLiteral(input) || IsQuotedLiteral(input))
            {
                return input;
            }

            return $"\"{EscapeStringLiteral(input)}\"";
        }

        private void UpdateOperationOptions()
        {
            var leftType = GetOperandType(Variable1.Text?.Trim() ?? string.Empty);
            var rightType = GetOperandType(Variable2.Text?.Trim() ?? string.Empty);
            var options = GetOperationsForType(leftType, rightType);
            LogicTypeComboBox.ItemsSource = options;

            if (LogicTypeComboBox.SelectedItem != null && !options.Contains(LogicTypeComboBox.SelectedItem))
            {
                LogicTypeComboBox.SelectedItem = null;
            }
        }

        private IReadOnlyList<string> GetOperationsForType(OperandType leftType, OperandType rightType)
        {
            if (leftType == OperandType.Unknown && rightType == OperandType.Unknown)
            {
                return allOperations;
            }

            if (leftType == OperandType.Unknown)
            {
                return GetOperationsForType(rightType);
            }

            if (rightType == OperandType.Unknown)
            {
                return GetOperationsForType(leftType);
            }

            if (leftType != rightType)
            {
                return Array.Empty<string>();
            }

            return GetOperationsForType(leftType);
        }

        private IReadOnlyList<string> GetOperationsForType(OperandType type)
        {
            if (type == OperandType.Number)
            {
                return new List<string> { "+", "-", "*", "/", "%", "^", "==", "!=", "<", "<=", ">", ">=" };
            }

            if (type == OperandType.Boolean)
            {
                return new List<string> { "==", "!=", "^" };
            }

            if (type == OperandType.String)
            {
                return new List<string> { "+", "==", "!=" };
            }

            if (type == OperandType.Array)
            {
                return Array.Empty<string>();
            }

            return allOperations;
        }

        private OperandType GetOperandType(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return OperandType.Unknown;
            }

            if (IsVariableReference(input))
            {
                return GetValueType(variables[input]);
            }

            if (IsBooleanLiteral(input))
            {
                return OperandType.Boolean;
            }

            if (IsNumericLiteral(input))
            {
                return OperandType.Number;
            }

            return OperandType.String;
        }

        private static OperandType GetValueType(object? value)
        {
            if (value == null)
            {
                return OperandType.Unknown;
            }

            if (value is bool)
            {
                return OperandType.Boolean;
            }

            if (value is string)
            {
                return OperandType.String;
            }

            if (value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal)
            {
                return OperandType.Number;
            }

            if (value is ArrayValue)
            {
                return OperandType.Array;
            }

            if (value is JsonElement element)
            {
                return GetJsonElementType(element);
            }

            return OperandType.String;
        }

        private static OperandType GetJsonElementType(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Number)
            {
                return OperandType.Number;
            }

            if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
            {
                return OperandType.Boolean;
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                return OperandType.String;
            }

            return OperandType.String;
        }

        private static bool IsCompatible(OperandType leftType, OperandType rightType)
        {
            if (leftType == OperandType.Unknown || rightType == OperandType.Unknown)
            {
                return false;
            }

            return leftType == rightType;
        }

        private static bool IsOperationAllowed(string? operation, OperandType type)
        {
            if (string.IsNullOrWhiteSpace(operation))
            {
                return false;
            }

            if (type == OperandType.Number)
            {
                return operation is "+" or "-" or "*" or "/" or "%" or "^" or "==" or "!=" or "<" or "<=" or ">" or ">=";
            }

            if (type == OperandType.Boolean)
            {
                return operation is "==" or "!=" or "^";
            }

            if (type == OperandType.String)
            {
                return operation is "+" or "==" or "!=";
            }

            return false;
        }

        private static bool IsQuotedLiteral(string input)
        {
            return input.Length >= 2 && input.StartsWith("\"") && input.EndsWith("\"");
        }

        private static bool IsBooleanLiteral(string input)
        {
            return string.Equals(input, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(input, "false", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNumericLiteral(string input)
        {
            return double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }

        private static string EscapeStringLiteral(string input)
        {
            return input.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
