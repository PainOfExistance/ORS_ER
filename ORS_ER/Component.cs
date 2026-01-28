using SkiaSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;

namespace ORS_ER.components
{
    public class IO
    {
        public dynamic? value { get; set; }
        public string? name { get; set; }
        public SKPoint node { get; set; } = new SKPoint();
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
        virtual public void OffsetRect(int x, int y)
        {
            var rect = Rect;
            var dx = x - rect.MidX;
            var dy = y - rect.MidY;

            rect.Offset(dx, dy);
            Rect = rect;
            var node= new SKPoint();
            for (int i=0; i<Inputs.Count(); i++)
            {
                node = Inputs[i].node;
                node.Offset(dx, dy);
                Inputs[i].node = node;
            }
            for (int i=0; i<Outputs.Count(); i++)
            {
                node = Outputs[i].node;
                node.Offset(dx, dy);
                Outputs[i].node = node;
            }
        }

        virtual public (Component, IO) HitTest(SKPoint world, bool _isConnecting)
        {
            foreach (var io in Inputs)
            {
                var nodeRect = SKRect.Create(io.node.X - 5, io.node.Y - 5, 10, 10);
                if (nodeRect.Contains(world))
                {
                    return (this, io);
                }
            }
            foreach (var io in Outputs)
            {
                var nodeRect = SKRect.Create(io.node.X - 5, io.node.Y - 5, 10, 10);
                if (nodeRect.Contains(world))
                {
                    return (this, io);
                }
            }
            if (Rect.Contains(world))
            {
                return (this, null);
            }
            return (null, null);

        }

    }
}
