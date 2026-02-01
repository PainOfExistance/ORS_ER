using SkiaSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ORS_ER.connections;

namespace ORS_ER.components
{
    abstract public class Component(string name, string description, string category)
    {
        private string Id = Guid.NewGuid().ToString();
        public string Name { get; set; } = name;
        public string Description { get; set; } = description;
        public string Category { get; set; } = category;
        public string Code { get; set; } = "";
        public bool Selected { get; set; } = false;
        public Dictionary<string, IO> Inputs = new Dictionary<string, IO>();
        public Dictionary<string, IO> Outputs = new Dictionary<string, IO>();
        public SKRect Rect { get; set; }
        public SKFont font = new SKFont();

        public Component(Component component) : this(component.Name, component.Description, component.Category)
        {
            this.Id = component.Id;
            this.Code = component.Code;
            this.Inputs = component.Inputs;
            this.Outputs = component.Outputs;
        }

        public string GetId()
        {
            return Id;
        }
        public void SetId(string id)
        {
            Id = id;
        }
        abstract public void Paint(SKCanvas canvas);
        abstract public void CreateRect(int x, int y);
        virtual public (float, float) OffsetRect(int x, int y)
        {
            var rect = Rect;
            var dx = x - rect.MidX;
            var dy = y - rect.MidY;

            rect.Offset(dx, dy);
            Rect = rect;

            var node = new SKPoint();
            foreach (var i in Inputs.Keys)
            {
                node = Inputs[i].node;
                node.Offset(dx, dy);
                Inputs[i].node = node;
            }

            foreach (var i in Outputs.Keys)
            {
                node = Outputs[i].node;
                node.Offset(dx, dy);
                Outputs[i].node = node;
            }

            return (dx, dy);
        }
        virtual public (string, Component, IO?)? HitTest(SKPoint world)
        {
            const float hitRadius = 8f;
            var hitRadius2 = hitRadius * hitRadius;

            static bool HitPoint(SKPoint a, SKPoint b, float r2)
            {
                var dx = a.X - b.X;
                var dy = a.Y - b.Y;
                return (dx * dx + dy * dy) <= r2;
            }

            foreach (var io in Inputs)
                if (HitPoint(io.Value.node, world, hitRadius2))
                {
                    Debug.WriteLine("Hit Input");
                    return ("input", this, io.Value);
                }

            foreach (var io in Outputs)
                if (HitPoint(io.Value.node, world, hitRadius2))
                {
                    Debug.WriteLine("Hit Output");
                    return ("output", this, io.Value);
                }

            if (Rect.Contains(world))
            {
                Debug.WriteLine("Hit rect");
                return ("rect", this, null);
            }

            return null;
        }
        abstract public void GenerateCode();
        virtual public string ToJson()
        {
            string inputJsons = "\"inputs\": [";
            foreach (var input in this.Inputs)
            {
                inputJsons += input.Value.ToJson();
            }
            inputJsons = inputJsons.TrimEnd(',', '\n', '\r', '\t', ' ') + "]";

            string outputJsons = "\"outputs\": [";
            foreach (var output in this.Outputs)
            {
                outputJsons += output.Value.ToJson();
            }
            outputJsons = outputJsons.TrimEnd(',', '\n', '\r', '\t', ' ') + "]";

            return $"{{\n" +
                $"\"name\": \"{this.Name}\",\n" +
                $"\"id\": \"{this.GetId()}\",\n" +
                $"\"x\": {this.Rect.MidX},\n" +
                $"\"y\": {this.Rect.MidY},\n" +
                $"\"code\": \"{this.Code.TrimEnd(',', '\n').Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t")}\",\n" +
                $"\"description\": \"{this.Description}\",\n" +
                $"\"category\": \"{this.Category}\",\n" +
                $"{inputJsons},\n" +
                $"{outputJsons}\n" +
                $"}}\n";
        }
    }
}

