using System.Globalization;
using System.Text.Json;

namespace ORS_ER.connections;

public enum ArrayElementType
{
    Number,
    Boolean,
    String
}

public sealed class ArrayValue
{
    public ArrayElementType ElementType { get; }
    public List<double> NumberItems { get; } = [];
    public List<bool> BooleanItems { get; } = [];
    public List<string> StringItems { get; } = [];

    public int Length => ElementType switch
    {
        ArrayElementType.Number => NumberItems.Count,
        ArrayElementType.Boolean => BooleanItems.Count,
        ArrayElementType.String => StringItems.Count,
        _ => 0
    };

    private ArrayValue(ArrayElementType elementType)
    {
        ElementType = elementType;
    }

    public static ArrayValue Create(ArrayElementType elementType, int length)
    {
        var arrayValue = new ArrayValue(elementType);
        arrayValue.FillDefaults(length);
        return arrayValue;
    }

    public static bool TryCreate(ArrayElementType elementType, IEnumerable<string> items, out ArrayValue arrayValue, out string? error)
    {
        arrayValue = new ArrayValue(elementType);
        foreach (var raw in items)
        {
            if (!TryParseElement(elementType, raw, out var value))
            {
                error = $"Invalid {elementType} element: {raw}";
                arrayValue = new ArrayValue(elementType);
                return false;
            }

            arrayValue.Add(value);
        }

        error = null;
        return true;
    }

    public static bool TryParseElement(ArrayElementType elementType, string input, out object value)
    {
        value = "";
        var trimmed = input.Trim();

        if (elementType == ArrayElementType.Number && double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            value = number;
            return true;
        }

        if (elementType == ArrayElementType.Boolean && bool.TryParse(trimmed, out var boolean))
        {
            value = boolean;
            return true;
        }

        if (elementType == ArrayElementType.String)
        {
            value = UnquoteString(trimmed);
            return true;
        }

        return false;
    }

    public object GetElement(int index)
    {
        EnsureIndex(index);
        return ElementType switch
        {
            ArrayElementType.Number => NumberItems[index],
            ArrayElementType.Boolean => BooleanItems[index],
            ArrayElementType.String => StringItems[index],
            _ => ""
        };
    }

    public void SetElement(int index, object value)
    {
        EnsureIndex(index);
        if (ElementType == ArrayElementType.Number)
            NumberItems[index] = (double)value;

        if (ElementType == ArrayElementType.Boolean)
            BooleanItems[index] = (bool)value;

        if (ElementType == ArrayElementType.String)
            StringItems[index] = (string)value;
    }

    public void Sort()
    {
        if (ElementType == ArrayElementType.Number)
            NumberItems.Sort();

        if (ElementType == ArrayElementType.Boolean)
            BooleanItems.Sort();

        if (ElementType == ArrayElementType.String)
            StringItems.Sort(StringComparer.Ordinal);
    }

    public object ToSerializableModel()
    {
        return new
        {
            elementType = ElementType.ToString(),
            items = ToObjectList()
        };
    }

    public string ToDisplayString()
    {
        var items = ToObjectList().Select(FormatDisplayItem);
        return $"[{string.Join(", ", items)}]";
    }

    public string ToCsvString()
    {
        var items = ToObjectList().Select(FormatCsvItem);
        return string.Join(", ", items);
    }

    public IReadOnlyList<string> ToItemStrings()
    {
        if (ElementType == ArrayElementType.Number)
            return NumberItems.Select(n => n.ToString(CultureInfo.InvariantCulture)).ToList();

        if (ElementType == ArrayElementType.Boolean)
            return BooleanItems.Select(b => b.ToString().ToLowerInvariant()).ToList();

        if (ElementType == ArrayElementType.String)
            return StringItems.ToList();

        return [];
    }

    public string ToCodeLiteral()
    {
        if (ElementType == ArrayElementType.Number)
            return $"new double[] {{ {string.Join(", ", NumberItems.Select(n => n.ToString(CultureInfo.InvariantCulture)))} }}";

        if (ElementType == ArrayElementType.Boolean)
            return $"new bool[] {{ {string.Join(", ", BooleanItems.Select(b => b.ToString().ToLowerInvariant()))} }}";

        return $"new string[] {{ {string.Join(", ", StringItems.Select(s => $"\"{EscapeString(s)}\""))} }}";
    }

    public ArrayValue Clone()
    {
        var copy = Create(ElementType, 0);
        if (ElementType == ArrayElementType.Number)
            copy.NumberItems.AddRange(NumberItems);

        if (ElementType == ArrayElementType.Boolean)
            copy.BooleanItems.AddRange(BooleanItems);

        if (ElementType == ArrayElementType.String)
            copy.StringItems.AddRange(StringItems);

        return copy;
    }

    public static bool TryRead(JsonElement element, out ArrayValue arrayValue)
    {
        arrayValue = new ArrayValue(ArrayElementType.Number);

        if (element.ValueKind != JsonValueKind.Object)
            return false;

        if (!element.TryGetProperty("elementType", out var elementTypeProperty))
            return false;

        if (!element.TryGetProperty("items", out var itemsProperty))
            return false;

        if (itemsProperty.ValueKind != JsonValueKind.Array)
            return false;

        if (!Enum.TryParse<ArrayElementType>(elementTypeProperty.GetString(), true, out var elementType))
            return false;

        var parsed = new ArrayValue(elementType);

        foreach (var item in itemsProperty.EnumerateArray())
        {
            if (elementType == ArrayElementType.Number && item.TryGetDouble(out var number))
                parsed.NumberItems.Add(number);

            if (elementType == ArrayElementType.Boolean && (item.ValueKind == JsonValueKind.True || item.ValueKind == JsonValueKind.False))
                parsed.BooleanItems.Add(item.GetBoolean());

            if (elementType == ArrayElementType.String && item.ValueKind == JsonValueKind.String)
                parsed.StringItems.Add(item.GetString() ?? "");
        }

        arrayValue = parsed;
        return true;
    }

    private void FillDefaults(int length)
    {
        if (ElementType == ArrayElementType.Number)
            NumberItems.AddRange(Enumerable.Repeat(0d, length));

        if (ElementType == ArrayElementType.Boolean)
            BooleanItems.AddRange(Enumerable.Repeat(false, length));

        if (ElementType == ArrayElementType.String)
            StringItems.AddRange(Enumerable.Repeat("", length));
    }

    private void Add(object value)
    {
        if (ElementType == ArrayElementType.Number)
            NumberItems.Add((double)value);

        if (ElementType == ArrayElementType.Boolean)
            BooleanItems.Add((bool)value);

        if (ElementType == ArrayElementType.String)
            StringItems.Add((string)value);
    }

    private void EnsureIndex(int index)
    {
        if (index < 0 || index >= Length)
            throw new IndexOutOfRangeException($"Index {index} out of range.");
    }

    private static string UnquoteString(string input)
    {
        if (input.Length < 2)
            return input;

        if (!input.StartsWith("\"") || !input.EndsWith("\""))
            return input;

        var value = input[1..^1];
        return value.Replace("\\\"", "\"").Replace("\\\\", "\\");
    }

    private static string EscapeString(string input)
    {
        return input.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string FormatDisplayItem(object value)
    {
        if (value is string s)
            return s;

        if (value is bool b)
            return b.ToString().ToLowerInvariant();

        if (value is double d)
            return d.ToString(CultureInfo.InvariantCulture);

        return value.ToString() ?? "";
    }

    private static string FormatCsvItem(object value)
    {
        if (value is string s)
            return s;

        return FormatDisplayItem(value);
    }

    private List<object> ToObjectList()
    {
        if (ElementType == ArrayElementType.Number)
            return NumberItems.Cast<object>().ToList();

        if (ElementType == ArrayElementType.Boolean)
            return BooleanItems.Cast<object>().ToList();

        return StringItems.Cast<object>().ToList();
    }
}
