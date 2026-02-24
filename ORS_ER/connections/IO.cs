using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORS_ER.connections
{
    public class IO
    {
        private string _id = Guid.NewGuid().ToString();
        public string IfTrue { get; set; } = "";
        public SKPoint Node { get; set; } = new SKPoint();
        public List<string> InputConnectionIds { get; set; } = [];
        public List<string> OutputConnectionIds { get; set; } = [];
        public IO() { }

        public string GetId()
        {
            return _id;
        }

        public void SetId(string id)
        {
            _id = id;
        }

        private static string JsonString(string? inputValue)
        {
            inputValue ??= "";
            return inputValue.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");
        }

        private static string JsonStringList(IEnumerable<string> connectionIds)
        {
            var builder = new StringBuilder("[");
            bool first = true;
            foreach (var id in connectionIds.Where(connectionId => !string.IsNullOrWhiteSpace(connectionId)))
            {
                if (!first) builder.Append(',');
                first = false;
                builder.Append('"').Append(JsonString(id)).Append('"');
            }
            builder.Append(']');
            return builder.ToString();
        }

        public string ToJson()
        {
            return $"{{" +
                $"\"id\": \"{JsonString(_id)}\"," +
                $"\"ifTrue\": \"{JsonString(IfTrue)}\"," +
                $"\"inputIds\": {JsonStringList(InputConnectionIds)}," +
                $"\"outputIds\": {JsonStringList(OutputConnectionIds)}" +
                $"}}";
        }
    }
}
