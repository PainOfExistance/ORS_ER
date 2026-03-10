using ORS_ER.connections;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.Windows;

namespace ORS_ER.windows;

public partial class ArrayInputWindow : Window
{
    public (string, dynamic) Value = ("", null);
    public string Code = "";
    private readonly ObservableCollection<string> arrayItems = [];

    public ArrayInputWindow(string code, (string, dynamic) value)
    {
        InitializeComponent();
        Code = code;
        Value = value;

        ArrayTypeComboBox.ItemsSource = Enum.GetValues<ArrayElementType>();
        ArrayItemsListBox.ItemsSource = arrayItems;

        NameTextBox.Text = Value.Item1;
        if (Value.Item2 is ArrayValue arrayValue)
        {
            ArrayTypeComboBox.SelectedItem = arrayValue.ElementType;
            ArrayElementsRadio.IsChecked = true;
            foreach (var item in arrayValue.ToItemStrings())
                arrayItems.Add(item);
            UpdateArrayInputs();
            return;
        }

        ArrayTypeComboBox.SelectedItem = ArrayElementType.Number;
        ArrayElementsRadio.IsChecked = true;
        UpdateArrayInputs();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var candidateName = NameTextBox.Text?.Trim() ?? string.Empty;

        if (!CodeGenerator.IsValidLanguageIndependentIdentifier(candidateName) || IsCSharpKeyword(candidateName))
        {
            MessageBox.Show("Invalid variable name. Use a valid identifier (letters/digits/_), not starting with a digit, and not a C# keyword.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (ArrayTypeComboBox.SelectedItem is not ArrayElementType elementType)
        {
            MessageBox.Show("Select an array element type.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Value.Item1 = candidateName;

        if (ArrayLengthRadio.IsChecked == true)
        {
            if (!int.TryParse(ArrayLengthTextBox.Text?.Trim(), out var length))
            {
                MessageBox.Show("Length must be a number.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (length < 0)
            {
                MessageBox.Show("Length must be non-negative.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var arrayValue = ArrayValue.Create(elementType, length);
            Value.Item2 = arrayValue;
            Code = $"dynamic {candidateName} = {arrayValue.ToCodeLiteral()} ;";
            DialogResult = true;
            return;
        }

        var rawItems = arrayItems.Select(item => item.Trim()).Where(item => item.Length > 0).ToList();

        if (!ArrayValue.TryCreate(elementType, rawItems, out var parsed, out var error))
        {
            MessageBox.Show(error ?? "Invalid array input.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Value.Item2 = parsed;
        Code = $"dynamic {candidateName} = {parsed.ToCodeLiteral()} ;";
        DialogResult = true;
    }

    private void ArrayMode_Checked(object sender, RoutedEventArgs e)
    {
        UpdateArrayInputs();
    }

    private void AddElement_Click(object sender, RoutedEventArgs e)
    {
        var input = ArrayElementInputTextBox.Text?.Trim() ?? string.Empty;
        if (input.Length == 0)
            return;

        if (ArrayTypeComboBox.SelectedItem is not ArrayElementType elementType)
        {
            MessageBox.Show("Select an array element type.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!ArrayValue.TryParseElement(elementType, input, out _))
        {
            MessageBox.Show("Element does not match the selected type.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        arrayItems.Add(input);
        ArrayElementInputTextBox.Text = "";
    }

    private void RemoveSelected_Click(object sender, RoutedEventArgs e)
    {
        if (ArrayItemsListBox.SelectedItem is not string selected)
            return;

        arrayItems.Remove(selected);
    }

    private void ClearElements_Click(object sender, RoutedEventArgs e)
    {
        arrayItems.Clear();
    }

    private void UpdateArrayInputs()
    {
        var isElements = ArrayElementsRadio.IsChecked == true;
        ArrayElementsPanel.Visibility = isElements ? Visibility.Visible : Visibility.Collapsed;
        ArrayLengthPanel.Visibility = isElements ? Visibility.Collapsed : Visibility.Visible;
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

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
