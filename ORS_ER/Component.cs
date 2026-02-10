using ORS_ER.connections;
using ORS_ER.windows;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace ORS_ER.components
{
    abstract public class Component(string name, string description, string category)
    {
        private string Id = Guid.NewGuid().ToString();
        public string Name { get; set; } = name;
        public (string, dynamic) Value { get; set; }
        public string Description { get; set; } = description;
        public string Category { get; set; } = category;
        public string Code { get; set; } = "";
        public bool Selected { get; set; } = false;
        public string IsInsideIf { get; set; } = "";
        public string IsInsideWhile { get; set; } = "";
        public Dictionary<string, IO> Inputs = new Dictionary<string, IO>();
        public Dictionary<string, IO> Outputs = new Dictionary<string, IO>();
        public SKRect Rect { get; set; }
        public SKRect buttonRect { get; set; }
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
        virtual public void OffsetRect(int x, int y)
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

            rect = this.buttonRect;
            rect.Offset(dx, dy);
            this.buttonRect = rect;
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
                    this.Selected = false;
                    return ("input", this, io.Value);
                }

            foreach (var io in Outputs)
                if (HitPoint(io.Value.node, world, hitRadius2))
                {
                    this.Selected = false;
                    return ("output", this, io.Value);
                }

            if (this.buttonRect.Contains(world))
            {
                this.Selected = true;
                if (this.Name.Contains("Operator"))
                {
                    var dlg = new LogicWindow(this.Code, this.Value);
                    if (dlg.ShowDialog() == true)
                    {
                        this.Value = dlg.Value;
                        this.Code = dlg.Code;
                    }
                }
                else if (this.Name.Contains("Input"))
                {
                    var dlg = new InputWindow(this.Code, this.Value, this.Name);
                    if (dlg.ShowDialog() == true)
                    {
                        this.Code = dlg.Code;
                        this.Value = dlg.Value;
                    }
                }
                else if (this.Name.Contains("Print"))
                {
                    var dlg = new PrintWindow(this.Code, this.Value);
                    if (dlg.ShowDialog() == true)
                    {
                        this.Code = dlg.Code;
                        this.Value = dlg.Value;
                    }
                }
                else if (this.Name.Contains("If"))
                {
                    var dlg = new IfWindow(this.Code, this.Value);
                    if (dlg.ShowDialog() == true)
                    {
                        this.Code = dlg.Code;
                        this.Value = dlg.Value;
                    }
                }
                else
                {
                    var dlg = new IfWindow(this.Code, this.Value);
                    if (dlg.ShowDialog() == true)
                    {
                        this.Code = dlg.Code;
                        this.Value = dlg.Value;
                    }
                }

                    return ("button", this, null);
            }

            if (Rect.Contains(world))
            {
                this.Selected = true;
                return ("rect", this, null);
            }

            this.Selected = false;
            return null;
        }
        abstract public void GenerateCode();
        virtual public string ToJson()
        {
            var inputBuilder = new StringBuilder("\"inputs\": [");
            foreach (var input in Inputs)
            {
                inputBuilder.Append(input.Value.ToJson());
            }
            inputBuilder.Append(']');
            var inputJsons = inputBuilder.ToString().TrimEnd(',', '\n', '\r', '\t', ' ');

            var outputBuilder = new StringBuilder("\"outputs\": [");
            foreach (var output in Outputs)
            {
                outputBuilder.Append(output.Value.ToJson());
            }
            outputBuilder.Append(']');
            var outputJsons = outputBuilder.ToString().TrimEnd(',', '\n', '\r', '\t', ' ');

            return $"{{\n" +
                $"\"name\": \"{Name}\",\n" +
                $"\"id\": \"{GetId()}\",\n" +
                $"\"x\": {Rect.MidX},\n" +
                $"\"y\": {Rect.MidY},\n" +
                $"\"code\": \"{Code.TrimEnd(',', '\n').Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t")}\",\n" +
                $"\"description\": \"{Description}\",\n" +
                $"\"category\": \"{Category}\",\n" +
                $"{inputJsons},\n" +
                $"{outputJsons},\n" +
                $"}}\n";
        }
    }
}

