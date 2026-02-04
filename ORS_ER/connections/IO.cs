using SkiaSharp;
using System;

namespace ORS_ER.connections
{
    public class IO
    {
        private string Id = Guid.NewGuid().ToString();
        public dynamic? value { get; set; }
        public string? name { get; set; }
        public SKPoint node { get; set; } = new SKPoint();
        public string inputConnectionId = "";
        public string outputConnectionId = "";
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
        public string ToJson()
        {
            return $"{{\n" +
                $"\"id\": \"{Id}\",\n" +
                $"\"name\": \"{name}\",\n" +
                $"\"value\": \"{value}\",\n" +
                $"\"inputId\": \"{inputConnectionId}\",\n" +
                $"\"outputId\": \"{outputConnectionId}\"\n" +
                $"}}\n";
        }
    }
}
