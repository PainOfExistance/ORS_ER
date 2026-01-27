using SkiaSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;

namespace ORS_ER.components
{
    public class IO
    {
        public dynamic value { get; set; }
        public string name { get; set; }
        public IO() { }
        public IO(string name, dynamic value)
        {
            this.name = name;
            this.value = value;
        }
    }

    abstract public class Component(string name, string description, string category)
    {
        private string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = name;
        public string Description { get; set; } = description;
        public string Category { get; set; } = category;
        public string Code { get; set; } = "";
        public List<IO> Inputs = new List<IO>();
        public List<IO> Outputs = new List<IO>();
        public bool Selected { get; set; } = false;
        public SKRect Rect { get; set; }

        public Component(Component component) : this(component.Name, component.Description, component.Category)
        {
            this.Id = component.Id;
            this.Code = component.Code;
            this.Inputs = component.Inputs;
            this.Outputs = component.Outputs;
        }

        abstract public void Paint(SKCanvas canvas);

        abstract public void CreateRect(int x, int y);

        abstract public override string ToString();

    }
}
