using ORS_ER.connections;
using SkiaSharp;
using System;
using System.Linq;

namespace ORS_ER.components;

class ArrayOperator : Component
{
    private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Operator);

    public ArrayOperator(Component component) : base(component)
    {
        Code = component.Code;
        IO input = new IO();
        IO output = new IO();
        Inputs.Add(input.GetId(), input);
        Outputs.Add(output.GetId(), output);
    }

    public ArrayOperator(string name, string description, string category) : base(name, description, category)
    {
        Code = "";
        IO input = new IO();
        IO output = new IO();
        Inputs.Add(input.GetId(), input);
        Outputs.Add(output.GetId(), output);
    }

    public override void Paint(SKCanvas canvas)
    {
        canvas.DrawRect(Rect, Paints.ComponentFill);
        Font.Size = 20;

        if (IsBroken)
        {
            canvas.DrawRect(Rect, Paints.BrokenBlock);
            canvas.DrawRect(Rect, Paints.BrokenBlockStroke);
        }
        if (!IsBroken && Selected)
            canvas.DrawRect(Rect, Paints.SelectedStroke);
        if (!IsBroken && !Selected)
            canvas.DrawRect(Rect, Paints.ComponentStroke);

        foreach (var input in Inputs)
            canvas.DrawCircle(input.Value.Node, 8, Paints.InputIOPaint);

        foreach (var output in Outputs)
            canvas.DrawCircle(output.Value.Node, 8, Paints.IOPaint);

        canvas.DrawRect(InteractionRect, Paints.ButtonFill);
        canvas.DrawRect(InteractionRect, Paints.ButtonStroke);

        var label = "Configure array operation";
        if (ArrayOperatorPayload.TryParse(Code, out var payload))
            label = payload.ToDisplayText();

        var textX = InteractionRect.MidX - (Font.MeasureText(label) / 2);
        var textY = InteractionRect.MidY + Font.Size / 4;

        while (InteractionRect.Width < (Font.MeasureText(label) + 5))
            Font.Size--;

        textX = InteractionRect.MidX - (Font.MeasureText(label) / 2);
        canvas.DrawText(label, textX, textY, Font, Paints.ButtonTextPaint);
    }

    public override void CreateRect(int x, int y)
    {
        Rect = new SKRect(x - 100, y - 50, x + 100, y + 50);
        InteractionRect = new SKRect(
            Rect.Left + (int)Rect.Width / 8,
            Rect.Top + (int)Rect.Height / 4,
            Rect.Right - (int)Rect.Width / 8,
            Rect.Bottom - (int)Rect.Height / 4);

        var delta = Rect.Width / (Outputs.Count + 1);
        var outputKeys = Outputs.Keys.ToArray();
        for (int outputIndex = 0; outputIndex < Outputs.Count; outputIndex++)
            Outputs[outputKeys[outputIndex]].Node = new SKPoint(Rect.Left + delta * (outputIndex + 1), Rect.Bottom);

        delta = Rect.Width / (Inputs.Count + 1);
        var inputKeys = Inputs.Keys.ToArray();
        for (int inputIndex = 0; inputIndex < Inputs.Count; inputIndex++)
            Inputs[inputKeys[inputIndex]].Node = new SKPoint(Rect.Left + delta * (inputIndex + 1), Rect.Top);
    }

    public override void GenerateCode()
    {
        if (!ArrayOperatorPayload.TryParse(Code, out var payload))
            throw new InvalidOperationException("Array operation not configured.");

        var scopeId = ResolveScopeId();
        var arrayEntry = ValueRegistry.GetLocalValue(scopeId, new RegistryKey(payload.ArrayName));
        if (arrayEntry is null)
            throw new InvalidOperationException($"Array variable '{payload.ArrayName}' not found.");

        var registryEntry = arrayEntry.Value;
        if (registryEntry.Value is ArrayValue arrayValue)
        {
            ApplyArrayOperation(scopeId, payload, arrayValue);
            return;
        }

        if (registryEntry.Value is string stringValue)
        {
            ApplyStringOperation(scopeId, payload, stringValue);
            return;
        }

        throw new InvalidOperationException($"Array variable '{payload.ArrayName}' not found.");
    }

    private void ApplyArrayOperation(RegistryId scopeId, ArrayOperatorPayload payload, ArrayValue arrayValue)
    {
        if (payload.Operation == "Get")
        {
            EnsureResultName(payload);
            RegisterValue(scopeId, payload.ResultName, arrayValue.GetElement(ResolveIndex(scopeId, payload)));
        }

        if (payload.Operation == "Length")
        {
            EnsureResultName(payload);
            RegisterValue(scopeId, payload.ResultName, arrayValue.Length);
        }

        if (payload.Operation == "Sort")
            ApplySort(scopeId, payload, arrayValue);

        if (payload.Operation == "Set")
            ApplySet(scopeId, payload, arrayValue, ResolveIndex(scopeId, payload));
    }

    private void ApplyStringOperation(RegistryId scopeId, ArrayOperatorPayload payload, string stringValue)
    {
        var normalized = NormalizeStringValue(stringValue);
        var index = ResolveIndex(scopeId, payload);
        EnsureStringIndex(normalized, index);

        if (payload.Operation == "Get")
        {
            EnsureResultName(payload);
            RegisterValue(scopeId, payload.ResultName, normalized[index].ToString());
        }

        if (payload.Operation == "Length")
        {
            EnsureResultName(payload);
            RegisterValue(scopeId, payload.ResultName, normalized.Length);
        }

        if (payload.Operation == "Sort")
            ApplyStringSort(scopeId, payload, normalized);

        if (payload.Operation == "Set")
            ApplyStringSet(scopeId, payload, normalized, index);
    }

    private RegistryId ResolveScopeId()
    {
        var scopeId = RegistryId.Global;

        if (IsInsideIf != "")
            scopeId = IsInsideIf.Split('_')[0];

        if (scopeId.IsGlobal && IsInsideWhile != "")
            scopeId = IsInsideWhile.Split('_')[0];

        return scopeId;
    }

    private void ApplySort(RegistryId scopeId, ArrayOperatorPayload payload, ArrayValue arrayValue)
    {
        arrayValue.Sort();
        RegisterValue(scopeId, payload.ArrayName, arrayValue);
        RegisterOptionalResult(scopeId, payload, arrayValue);
    }

    private void ApplySet(RegistryId scopeId, ArrayOperatorPayload payload, ArrayValue arrayValue, int index)
    {
        var resolved = ResolveArrayValue(scopeId, arrayValue.ElementType, payload.Value);
        arrayValue.SetElement(index, resolved);
        RegisterValue(scopeId, payload.ArrayName, arrayValue);
        RegisterOptionalResult(scopeId, payload, arrayValue);
    }

    private int ResolveIndex(RegistryId scopeId, ArrayOperatorPayload payload)
    {
        var token = payload.IndexToken;
        if (string.IsNullOrWhiteSpace(token))
            token = payload.Index?.ToString();

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Index not provided.");

        if (int.TryParse(token, out var literalIndex))
            return literalIndex;

        var entry = ValueRegistry.GetLocalValue(scopeId, new RegistryKey(token));
        if (entry is null)
            throw new InvalidOperationException("Index variable not found.");

        var entryValue = entry.Value.Value;
        if (!TryConvertToIndex(entryValue, out int index))
            throw new InvalidOperationException("Index variable must be an integer.");

        return index;
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

    private object ResolveArrayValue(RegistryId scopeId, ArrayElementType elementType, string? token)
    {
        var valueToken = token?.Trim() ?? string.Empty;
        if (valueToken.Length == 0)
            throw new InvalidOperationException("Array value not provided.");

        if (ArrayValue.TryParseElement(elementType, valueToken, out var literal))
            return literal;

        var entry = ValueRegistry.GetLocalValue(scopeId, new RegistryKey(valueToken));
        if (entry is null)
            throw new InvalidOperationException("Array value must be a literal or variable of the same type.");

        if (!TryConvertElementValue(elementType, entry.Value.Value, out object resolved))
            throw new InvalidOperationException("Array value must be a literal or variable of the same type.");

        return resolved;
    }

    private static bool TryConvertElementValue(ArrayElementType elementType, object? value, out object result)
    {
        result = "";
        if (value is null)
            return false;

        if (elementType == ArrayElementType.Number)
        {
            if (value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal)
            {
                result = Convert.ToDouble(value);
                return true;
            }

            if (value is string stringValue && double.TryParse(stringValue, out var parsedNumber))
            {
                result = parsedNumber;
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

    private void RegisterValue(RegistryId scopeId, string name, dynamic value)
    {
        Value = (name, value);
        ValueRegistry.RegisterLocalValue(scopeId, new RegistryKey(name), new ValueRegistry.RegistryEntry
        {
            BlockId = new RegistryId(GetId()),
            Key = new RegistryKey(name),
            Value = value
        });
    }

    private static void EnsureStringIndex(string value, int index)
    {
        if (index < 0 || index >= value.Length)
            throw new IndexOutOfRangeException($"Index {index} out of range.");
    }

    private static string NormalizeStringValue(string input)
    {
        var trimmed = input.Trim();
        if (trimmed.Length < 2)
            return trimmed;

        if (!trimmed.StartsWith("\"") || !trimmed.EndsWith("\""))
            return trimmed;

        var unquoted = trimmed[1..^1];
        return unquoted.Replace("\\\"", "\"").Replace("\\\\", "\\");
    }

    private void ApplyStringSort(RegistryId scopeId, ArrayOperatorPayload payload, string value)
    {
        var sorted = new string(value.OrderBy(ch => ch).ToArray());
        RegisterValue(scopeId, payload.ArrayName, sorted);
        RegisterOptionalResult(scopeId, payload, sorted);
    }

    private void ApplyStringSet(RegistryId scopeId, ArrayOperatorPayload payload, string value, int index)
    {
        var replacement = ResolveStringToken(scopeId, payload.Value);
        if (replacement.Length != 1)
            throw new InvalidOperationException("String replacement must be a single character.");

        var updated = value.Remove(index, 1).Insert(index, replacement);
        RegisterValue(scopeId, payload.ArrayName, updated);
        RegisterOptionalResult(scopeId, payload, updated);
    }

    private static void EnsureResultName(ArrayOperatorPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.ResultName))
            throw new InvalidOperationException("Result name is required for this operation.");
    }

    private void RegisterOptionalResult(RegistryId scopeId, ArrayOperatorPayload payload, dynamic value)
    {
        if (string.IsNullOrWhiteSpace(payload.ResultName))
            return;

        RegisterValue(scopeId, payload.ResultName, value);
    }

    private string ResolveStringToken(RegistryId scopeId, string? token)
    {
        var valueToken = token?.Trim() ?? string.Empty;
        if (valueToken.Length == 0)
            return string.Empty;

        var entry = ValueRegistry.GetLocalValue(scopeId, new RegistryKey(valueToken));
        if (entry is not null)
        {
            if (TryConvertStringValue(entry.Value.Value, out string resolved))
                return resolved;
        }

        return NormalizeStringValue(valueToken);
    }
}
