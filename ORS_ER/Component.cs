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
    public enum HitTarget
    {
        Input,
        Output,
        Rect,
        Button
    }

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
            foreach (var inputId in Inputs.Keys)
            {
                node = Inputs[inputId].Node;
                node.Offset(dx, dy);
                Inputs[inputId].Node = node;
            }

            foreach (var outputId in Outputs.Keys)
            {
                node = Outputs[outputId].Node;
                node.Offset(dx, dy);
                Outputs[outputId].Node = node;
            }

            rect = this.InteractionRect;
            rect.Offset(dx, dy);
            this.InteractionRect = rect;
        }
        virtual public (HitTarget, Component, IO?)? HitTest(SKPoint world)
        {
            const float hitRadius = 8f;
            var hitRadius2 = hitRadius * hitRadius;

            static bool HitPoint(SKPoint nodePoint, SKPoint targetPoint, float radiusSquared)
            {
                var dx = nodePoint.X - targetPoint.X;
                var dy = nodePoint.Y - targetPoint.Y;
                return (dx * dx + dy * dy) <= radiusSquared;
            }

            foreach (var io in Inputs)
                if (HitPoint(io.Value.Node, world, hitRadius2))
                {
                    this.Selected = false;
                    return (HitTarget.Input, this, io.Value);
                }

            foreach (var io in Outputs)
                if (HitPoint(io.Value.Node, world, hitRadius2))
                {
                    this.Selected = false;
                    return (HitTarget.Output, this, io.Value);
                }

            var local = world;
            if (this is While || this is If)
            {
                // Rotate the hit-test point back for diamond-shaped blocks.
                var inv = SKMatrix.CreateRotationDegrees(45, Rect.MidX, Rect.MidY);
                local = inv.MapPoint(world);
            }

            if (this.InteractionRect.Contains(local))
            {
                this.IsBroken = false;
                this.Selected = true;

                if (this.GetType() == typeof(BinaryInput))
                {
                    // Toggle the boolean input on click.
                    this.Value = ("bool", !this.Value.Item2);
                    return (HitTarget.Button, this, null);
                }

                if (this.GetType() == typeof(BinaryOutput) || this.GetType() == typeof(Gate) || this.GetType() == typeof(Adder) || this.GetType() == typeof(SubCircuitComponent))
                {
                    return (HitTarget.Button, this, null);
                }

                if (this.GetType() == typeof(Operator))
                {
                    // Operator dialogs allow editing the embedded expression/value.
                    var dlg = new LogicWindow(this.Code, this.Value);
                    if (dlg.ShowDialog() == true)
                    {
                        this.Code = dlg.Code;
                        this.Value = dlg.Value;
                        Debug.WriteLine($"LogicWindow returned: {this.Value.Item2}");
                    }
                    return (HitTarget.Button, this, null);
                }

                if (this.GetType() == typeof(Input))
                {
                    // Input dialog configures the variable/value.
                    var dlg = new InputWindow(this.Code, this.Value, this.Name);
                    if (dlg.ShowDialog() == true)
                    {
                        this.Code = dlg.Code;
                        this.Value = dlg.Value;
                    }
                    return (HitTarget.Button, this, null);
                }

                if (this.GetType() == typeof(Print))
                {
                    // Print dialog configures output formatting.
                    var dlg = new PrintWindow(this.Code, this.Value);
                    if (dlg.ShowDialog() == true)
                    {
                        this.Code = dlg.Code;
                        this.Value = dlg.Value;
                    }
                    return (HitTarget.Button, this, null);
                }

                if (this.GetType() == typeof(If) || this.GetType() == typeof(While))
                {
                    // If/While dialog configures the condition.
                    var dlg = new IfWindow(this.Code, this.Value);
                    if (dlg.ShowDialog() == true)
                    {
                        this.Code = dlg.Code;
                        this.Value = dlg.Value;
                    }
                    return (HitTarget.Button, this, null);
                }
            }

            if (Rect.Contains(local))
            {
                this.IsBroken = false;
                this.Selected = true;
                return (HitTarget.Rect, this, null);
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
                // Serialize dynamic value payloads into a compact JSON representation.
                var nameJson = JsonSerializer.Serialize(Value.Item1 ?? "");
                var valueObject = Value.Item2;
                var valuePart = valueObject switch
                {
                    null => "null",
                    string s => JsonSerializer.Serialize(s),
                    bool b => b ? "true" : "false",
                    byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal =>
                        Convert.ToString(valueObject, System.Globalization.CultureInfo.InvariantCulture) ?? "0",
                    JsonElement je => je.GetRawText(),
                    _ => JsonSerializer.Serialize(valueObject.ToString() ?? "")
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

