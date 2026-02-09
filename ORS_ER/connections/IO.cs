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
        public string IfTrue { get; set; } = "";
        public SKPoint node { get; set; } = new SKPoint();
        public List<string> inputConnectionIds { get; set; } = [];
        public List<string> outputConnectionIds { get; set; } = [];
        public IO() { }

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
            return $"{{\n" +
                $"\"id\": \"{JsonString(Id)}\",\n" +
                $"\"inputIds\": {JsonStringList(inputConnectionIds)},\n" +
                $"\"outputIds\": {JsonStringList(outputConnectionIds)}\n" +
                $"}}\n";
        }
    }
}
