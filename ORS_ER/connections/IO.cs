using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORS_ER.connections
{
    public class IO
    {
        private string Id = Guid.NewGuid().ToString();

        public dynamic? value { get; set; }
        public string? name { get; set; }
        public SKPoint node { get; set; } = new SKPoint();

        // Supports fan-in/fan-out:
        // - Inputs can have multiple incoming connections (from multiple outputs).
        // - Outputs can drive multiple connections (to multiple inputs).
        public List<string> inputConnectionIds { get; set; } = [];
        public List<string> outputConnectionIds { get; set; } = [];

        public IO() { }

        public IO(string name, dynamic value)
        {
            this.name = name;
            this.value = value;
        }

        public string GetId()
        {
            return Id;
        }

        public void SetId(string id)
        {
            Id = id;
        }

        private static string JsonString(string? s)
        {
            s ??= "";
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");
        }

        private static string JsonStringList(IEnumerable<string> ids)
        {
            var b = new StringBuilder("[");
            bool first = true;
            foreach (var id in ids.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                if (!first) b.Append(',');
                first = false;
                b.Append('"').Append(JsonString(id)).Append('"');
            }
            b.Append(']');
            return b.ToString();
        }

        public string ToJson()
        {
            // New schema (supports multiple connections). Still readable with AllowTrailingCommas.
            return $"{{\n" +
                $"\"id\": \"{JsonString(Id)}\",\n" +
                $"\"name\": \"{JsonString(name)}\",\n" +
                $"\"value\": \"{JsonString(value?.ToString())}\",\n" +
                $"\"inputIds\": {JsonStringList(inputConnectionIds)},\n" +
                $"\"outputIds\": {JsonStringList(outputConnectionIds)}\n" +
                $"}}\n";
        }
    }
}
