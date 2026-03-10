using ORS_ER.components;
using ORS_ER.connections;
using System;
using System.CodeDom.Compiler;
using System.Text.Json;
using System.Windows;

namespace ORS_ER.windows;

public partial class ArrayOperatorWindow : Window
{
    public (string, dynamic) Value = ("", null);
    public string Code = "";
    private readonly Dictionary<string, dynamic> variables;
    private readonly List<string> operations = ["Get", "Set", "Length", "Sort"];

    public ArrayOperatorWindow(string code, (string, dynamic) value, Dictionary<string, dynamic> variables)
    {
        InitializeComponent();
        Code = code;
        Value = value;
        this.variables = variables;

        ArrayComboBox.ItemsSource = variables.Keys
            .Where(key => variables[key] is ArrayValue || variables[key] is string)
            .ToList();
        OperationComboBox.ItemsSource = operations;
        IndexComboBox.ItemsSource = GetIndexVariables();
        ArrayComboBox.SelectionChanged += (_, __) => UpdateValueOptions();
        OperationComboBox.SelectionChanged += (_, __) => UpdateValueOptions();

        LoadExisting();
        UpdateInputs();
        UpdateValueOptions();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var resultName = ResultNameTextBox.Text?.Trim() ?? "";
        var arrayName = ArrayComboBox.Text?.Trim() ?? "";
        var operation = OperationComboBox.SelectedItem?.ToString() ?? "";

        var resultRequired = operation is "Get" or "Length";
        if (resultRequired && !IsValidIdentifier(resultName))
        {
            MessageBox.Show("Result name is required for get/length.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!resultRequired && !string.IsNullOrWhiteSpace(resultName) && !IsValidIdentifier(resultName))
        {
            MessageBox.Show("Invalid result variable name.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!variables.TryGetValue(arrayName, out var targetValue))
        {
            MessageBox.Show("Select a valid array or string variable.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var array = targetValue as ArrayValue;
        var stringTarget = targetValue as string;
        if (array is null && stringTarget is null)
        {
            MessageBox.Show("Select a valid array or string variable.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(operation))
        {
            MessageBox.Show("Select an operation.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (operation == "Get" || operation == "Set")
        {
            var indexToken = IndexComboBox.Text?.Trim() ?? "";
            if (!TryResolveIndexToken(indexToken, out var index))
            {
                MessageBox.Show("Index must be a number or integer variable.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (index < 0)
            {
                MessageBox.Show("Index must be non-negative.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (array is not null && index >= array.Length)
            {
                MessageBox.Show("Index must not be greater than length of the array.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (stringTarget is not null && index >= NormalizeStringValue(stringTarget).Length)
            {
                MessageBox.Show("Index must not be greater than length of the string.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var valueToken = ValueComboBox.Text?.Trim() ?? "";
            if (operation == "Set" && array is not null && !TryValidateArrayValueToken(array.ElementType, valueToken))
            {
                MessageBox.Show("Value does not match array element type.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (operation == "Set" && stringTarget is not null && !TryValidateStringValueToken(valueToken))
            {
                MessageBox.Show("Value must be a single character for string targets.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        var payload = new ArrayOperatorPayload
        {
            ResultName = resultName,
            ArrayName = arrayName,
            Operation = operation,
            IndexToken = IndexComboBox.Text?.Trim(),
            Index = int.TryParse(IndexComboBox.Text?.Trim(), out var parsed) ? parsed : null,
            Value = ValueComboBox.Text?.Trim()
        };

        Code = JsonSerializer.Serialize(payload);
        Value = (resultName, operation);
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OperationComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateInputs();
    }

    private void UpdateInputs()
    {
        var operation = OperationComboBox.SelectedItem?.ToString() ?? "";
        var needsIndex = operation is "Get" or "Set";
        var needsValue = operation == "Set";

        IndexPanel.Visibility = needsIndex ? Visibility.Visible : Visibility.Collapsed;
        ValuePanel.Visibility = needsValue ? Visibility.Visible : Visibility.Collapsed;

        if (!needsIndex)
            IndexComboBox.Text = "";

        if (!needsValue)
            ValueComboBox.Text = "";
    }

    private void LoadExisting()
    {
        if (!ArrayOperatorPayload.TryParse(Code, out var payload))
            return;

        ResultNameTextBox.Text = payload.ResultName;
        ArrayComboBox.Text = payload.ArrayName;
        OperationComboBox.SelectedItem = payload.Operation;
        IndexComboBox.Text = payload.IndexToken ?? payload.Index?.ToString() ?? "";
        ValueComboBox.Text = payload.Value ?? "";
    }

    private static bool IsValidIdentifier(string name)
    {
        if (!CodeGenerator.IsValidLanguageIndependentIdentifier(name))
            return false;

        return !IsCSharpKeyword(name);
    }

    private List<string> GetIndexVariables()
    {
        return variables.Where(kv => IsNumericValue(kv.Value)).Select(kv => kv.Key).ToList();
    }

    private void UpdateValueOptions()
    {
        var targetName = ArrayComboBox.Text?.Trim() ?? "";
        if (!variables.TryGetValue(targetName, out var targetValue))
        {
            ValueComboBox.ItemsSource = Array.Empty<string>();
            return;
        }

        if (targetValue is ArrayValue arrayValue)
        {
            ValueComboBox.ItemsSource = GetValueVariables(arrayValue.ElementType);
            return;
        }

        if (targetValue is string)
        {
            ValueComboBox.ItemsSource = GetStringValueVariables();
            return;
        }

        ValueComboBox.ItemsSource = Array.Empty<string>();
    }

    private List<string> GetValueVariables(ArrayElementType elementType)
    {
        if (elementType == ArrayElementType.Number)
            return variables.Where(kv => IsNumericValue(kv.Value)).Select(kv => kv.Key).ToList();

        if (elementType == ArrayElementType.Boolean)
            return variables.Where(kv => IsBooleanValue(kv.Value)).Select(kv => kv.Key).ToList();

        if (elementType == ArrayElementType.String)
            return variables.Where(kv => IsStringValue(kv.Value)).Select(kv => kv.Key).ToList();

        return [];
    }

    private List<string> GetStringValueVariables()
    {
        return variables.Where(kv => IsStringValue(kv.Value)).Select(kv => kv.Key).ToList();
    }

    private bool TryResolveIndexToken(string token, out int index)
    {
        index = 0;
        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (int.TryParse(token, out index))
            return true;

        if (!variables.TryGetValue(token, out var value))
            return false;

        return TryConvertToIndex(value, out index);
    }

    private static bool TryConvertToIndex(object? value, out int index)
    {
        index = 0;
        if (value is null)
            return false;

        if (value is byte or sbyte or short or ushort or int or uint or long or ulong)
        {
            index = Convert.ToInt32(value);
            return true;
        }

        if (value is float or double or decimal)
        {
            var number = Convert.ToDouble(value);
            if (Math.Abs(number % 1) > 0)
                return false;

            index = (int)number;
            return true;
        }

        if (value is string stringValue && int.TryParse(stringValue, out var parsed))
        {
            index = parsed;
            return true;
        }

        return false;
    }

    private bool TryValidateArrayValueToken(ArrayElementType elementType, string token)
    {
        if (ArrayValue.TryParseElement(elementType, token, out _))
            return true;

        if (!variables.TryGetValue(token, out var value))
            return false;

        return TryConvertElementValue(elementType, value, out object resolved);
    }

    private bool TryValidateStringValueToken(string token)
    {
        if (IsSingleCharacter(token))
            return true;

        if (!variables.TryGetValue(token, out var value))
            return false;

        if (!TryConvertStringValue(value, out string normalized))
            return false;

        return IsSingleCharacter(normalized);
    }

    private static bool TryConvertElementValue(ArrayElementType elementType, object? value, out object result)
    {
        result = "";
        if (value is null)
            return false;

        if (elementType == ArrayElementType.Number)
        {
            if (IsNumericValue(value))
            {
                result = Convert.ToDouble(value);
                return true;
            }

            return false;
        }

        if (elementType == ArrayElementType.Boolean)
        {
            if (value is bool boolean)
            {
                result = boolean;
                return true;
            }

            if (value is string stringValue && bool.TryParse(stringValue, out var parsedBool))
            {
                result = parsedBool;
                return true;
            }

            return false;
        }

        if (elementType == ArrayElementType.String)
        {
            if (TryConvertStringValue(value, out var stringResult))
            {
                result = stringResult;
                return true;
            }

            return false;
        }

        return false;
    }

    private static bool TryConvertStringValue(object? value, out string result)
    {
        result = string.Empty;
        if (value is null)
            return false;

        if (value is string stringValue)
        {
            result = NormalizeStringValue(stringValue);
            return true;
        }

        return false;
    }

    private static bool IsNumericValue(object? value)
    {
        if (value is null)
            return false;

        if (value is byte or sbyte or short or ushort or int or uint or long or ulong)
            return true;

        if (value is float or double or decimal)
            return true;

        return false;
    }

    private static bool IsBooleanValue(object? value)
    {
        if (value is bool)
            return true;

        if (value is string stringValue && bool.TryParse(stringValue, out _))
            return true;

        return false;
    }

    private static bool IsStringValue(object? value)
    {
        if (value is string)
            return true;

        return false;
    }

    private static bool IsSingleCharacter(string? input)
    {
        var normalized = NormalizeStringValue(input);
        return normalized.Length == 1;
    }

    private static string NormalizeStringValue(string? input)
    {
        var value = input?.Trim() ?? string.Empty;
        if (value.Length < 2)
            return value;

        if (!value.StartsWith("\"") || !value.EndsWith("\""))
            return value;

        var unquoted = value[1..^1];
        return unquoted.Replace("\\\"", "\"").Replace("\\\\", "\\");
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
}
