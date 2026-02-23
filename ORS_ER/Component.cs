using ORS_ER.connections;
using ORS_ER.windows;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace ORS_ER.components
{
    abstract public class Component(string name, string description, string category)
    {
        private string _id = Guid.NewGuid().ToString();
        public string Name { get; set; } = name;
        public (string, dynamic) Value { get; set; }
        public string Description { get; set; } = description;
        public string Category { get; set; } = category;
        public string Code { get; set; } = "";
        public bool Selected { get; set; } = false;
        public string IsInsideIf { get; set; } = "";
        public string IsInsideWhile { get; set; } = "";
        public bool IsBroken { get; set; } = false;
        public Dictionary<string, IO> Inputs = new Dictionary<string, IO>();
        public Dictionary<string, IO> Outputs = new Dictionary<string, IO>();
        public SKRect Rect { get; set; }
        public SKRect InteractionRect { get; set; }
        public SKFont Font = new SKFont();
        public Component(Component component) : this(component.Name, component.Description, component.Category)
        {
            this._id = component._id;
            this.Code = component.Code;
            this.Inputs = component.Inputs;
            this.Outputs = component.Outputs;
        }

        public string GetId()
        {
            return _id;
        }
        public void SetId(string id)
        {
            _id = id;
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
                node = Inputs[i].Node;
                node.Offset(dx, dy);
                Inputs[i].Node = node;
            }

            foreach (var i in Outputs.Keys)
            {
                node = Outputs[i].Node;
                node.Offset(dx, dy);
                Outputs[i].Node = node;
            }

            rect = this.InteractionRect;
            rect.Offset(dx, dy);
            this.InteractionRect = rect;
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
                if (HitPoint(io.Value.Node, world, hitRadius2))
                {
                    this.Selected = false;
                    return ("input", this, io.Value);
                }

            foreach (var io in Outputs)
                if (HitPoint(io.Value.Node, world, hitRadius2))
                {
                    this.Selected = false;
                    return ("output", this, io.Value);
                }

            var local = world;
            if (this is While || this is If)
            {
                var inv = SKMatrix.CreateRotationDegrees(45, Rect.MidX, Rect.MidY);
                local = inv.MapPoint(world);
            }

            if (this.InteractionRect.Contains(local))
            {
                this.IsBroken = false;
                this.Selected = true;

                if (this.GetType() == typeof(BinaryInput))
                {
                    this.Value = ("bool", !this.Value.Item2);
                    return ("button", this, null);
                }

                if (this.GetType() == typeof(BinaryOutput) || this.GetType() == typeof(Gate) || this.GetType() == typeof(Adder) || this.GetType() == typeof(SubCircuitComponent))
                {
                    return ("button", this, null);
                }

                if (this.Name.Contains("Operator"))
                {
                    var dlg = new LogicWindow(this.Code, this.Value);
                    if (dlg.ShowDialog() == true)
                    {
                        this.Code = dlg.Code;
                        this.Value = dlg.Value;
                        Debug.WriteLine($"LogicWindow returned: {this.Value.Item2}");
                    }
                    return ("button", this, null);
                }

                if (this.Name.Contains("Input"))
                {
                    var dlg = new InputWindow(this.Code, this.Value, this.Name);
                    if (dlg.ShowDialog() == true)
                    {
                        this.Code = dlg.Code;
                        this.Value = dlg.Value;
                    }
                    return ("button", this, null);
                }

                if (this.Name.Contains("Print"))
                {
                    var dlg = new PrintWindow(this.Code, this.Value);
                    if (dlg.ShowDialog() == true)
                    {
                        this.Code = dlg.Code;
                        this.Value = dlg.Value;
                    }
                    return ("button", this, null);
                }

                if (this.Name.Contains("If"))
                {
                    var dlg = new IfWindow(this.Code, this.Value);
                    if (dlg.ShowDialog() == true)
                    {
                        this.Code = dlg.Code;
                        this.Value = dlg.Value;
                    }
                    return ("button", this, null);
                }

                var fallbackDialog = new IfWindow(this.Code, this.Value);
                if (fallbackDialog.ShowDialog() == true)
                {
                    this.Code = fallbackDialog.Code;
                    this.Value = fallbackDialog.Value;
                }

                return ("button", this, null);
            }

            if (Rect.Contains(local))
            {
                this.IsBroken = false;
                this.Selected = true;
                return ("rect", this, null);
            }

            this.Selected = false;
            return null;
        }
        virtual public void Reset() { }
        virtual public void GenerateCode() { }
        virtual public void RunInternalSimulation(List<bool> vals) { }
        virtual public string ToJson()
        {
            static string JsonString(string? s)
            {
                s ??= "";
                return s.Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("\r", "\\r")
                        .Replace("\n", "\\n")
                        .Replace("\t", "\\t");
            }

            string valueJson;
            try
            {
                var nameJson = JsonSerializer.Serialize(Value.Item1 ?? "");
                var v = Value.Item2;
                var valuePart = v switch
                {
                    null => "null",
                    string s => JsonSerializer.Serialize(s),
                    bool b => b ? "true" : "false",
                    byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal =>
                        Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? "0",
                    JsonElement je => je.GetRawText(),
                    _ => JsonSerializer.Serialize(v.ToString() ?? "")
                };

                valueJson = $"{{\"name\":{nameJson},\"value\":{valuePart}}}";
            }
            catch
            {
                valueJson = "{\"name\":\"\",\"value\":null}";
            }

            var inputBuilder = new StringBuilder("\"inputs\": [");
            bool firstIn = true;
            foreach (var input in Inputs)
            {
                if (!firstIn)
                    inputBuilder.Append(',');
                firstIn = false;
                inputBuilder.Append(input.Value.ToJson());
            }
            inputBuilder.Append(']');
            var inputJsons = inputBuilder.ToString();

            var outputBuilder = new StringBuilder("\"outputs\": [");
            bool firstOut = true;
            foreach (var output in Outputs)
            {
                if (!firstOut)
                    outputBuilder.Append(',');
                firstOut = false;
                outputBuilder.Append(output.Value.ToJson());
            }
            outputBuilder.Append(']');
            var outputJsons = outputBuilder.ToString();

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
                $"\"value\": {valueJson},\n" +
                $"\"isInsideIf\": \"{JsonString(IsInsideIf)}\",\n" +
                $"\"isInsideWhile\": \"{JsonString(IsInsideWhile)}\",\n" +
                $"\"description\": \"{Description}\",\n" +
                $"\"category\": \"{Category}\",\n" +
                $"{inputJsons},\n" +
                $"{outputJsons},\n" +
                $"}}\n";
        }
    }
}

