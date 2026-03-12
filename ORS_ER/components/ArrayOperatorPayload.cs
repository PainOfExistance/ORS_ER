using System.Text.Json;

namespace ORS_ER.components;

public sealed class ArrayOperatorPayload
{
    public string ResultName { get; set; } = "";
    public string ArrayName { get; set; } = "";
    public string Operation { get; set; } = "";
    public int? Index { get; set; }
    public string? IndexToken { get; set; }
    public string? Value { get; set; }

    public string ToDisplayText()
    {
        if (string.IsNullOrWhiteSpace(ArrayName) || string.IsNullOrWhiteSpace(Operation))
            return "Configure array operation";

        if (Operation == "Length" && !string.IsNullOrWhiteSpace(ResultName))
            return $"{ResultName} = {ArrayName}.Length";

        if (Operation == "Sort")
        {
            if (string.IsNullOrWhiteSpace(ResultName))
                return $"Sort({ArrayName})";

            return $"{ResultName} = Sort({ArrayName})";
        }

        if (Operation == "Get" && !string.IsNullOrWhiteSpace(ResultName))
            return $"{ResultName} = {ArrayName}[{IndexToken ?? Index?.ToString()}]";

        if (Operation == "Set")
        {
            if (string.IsNullOrWhiteSpace(ResultName))
                return $"Set({ArrayName}, {IndexToken ?? Index?.ToString()}, {Value})";

            return $"{ResultName} = Set({ArrayName}, {IndexToken ?? Index?.ToString()})";
        }

        return "Configure array operation";
    }

    public static bool TryParse(string? code, out ArrayOperatorPayload payload)
    {
        payload = new ArrayOperatorPayload();

        if (string.IsNullOrWhiteSpace(code))
            return false;

        try
        {
            var parsed = JsonSerializer.Deserialize<ArrayOperatorPayload>(code);
            if (parsed is null)
                return false;

            payload = parsed;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
