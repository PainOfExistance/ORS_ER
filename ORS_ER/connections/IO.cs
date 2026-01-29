using ORS_ER.components;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace ORS_ER.connections
{
    public class IO
    {
        private readonly string Id = Guid.NewGuid().ToString();
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
    }
}
