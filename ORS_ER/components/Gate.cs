using ORS_ER.connections;
using ORS_ER.windows;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace ORS_ER.components
{
    class Gate : Component
    {
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Gate);
        string operation = "==";
        SKRect buttonRect { get; set; }
        public Gate(Component component) : base(component)
        {
            IO newNode1 = new IO();
            IO newNode2 = new IO();
            IO newNode3 = new IO();
            newNode3.name = $"{this.Name}{this.Index}";
            Inputs.Add(newNode1.GetId(), newNode1);
            Inputs.Add(newNode2.GetId(), newNode2);
            Outputs.Add(newNode3.GetId(), newNode3);
        }

        public Gate(string name, string description, string category, int runningIndex) : base(name, description, category, runningIndex)
        {
            IO newNode1 = new IO();
            IO newNode2 = new IO();
            IO newNode3 = new IO();
            newNode3.name = $"{this.Name}{this.Index}".Replace(" ", "");
            Inputs.Add(newNode1.GetId(), newNode1);
            Inputs.Add(newNode2.GetId(), newNode2);
            Outputs.Add(newNode3.GetId(), newNode3);
        }

        public override void Paint(SKCanvas canvas)
        {
            canvas.DrawRect(this.Rect, Paints.ComponentFill);
            font.Size = 20;

            if (this.Selected)
                canvas.DrawRect(this.Rect, Paints.SelectedStroke);
            else
                canvas.DrawRect(this.Rect, Paints.ComponentStroke);

            foreach (var input in Inputs)
            {
                canvas.DrawCircle(input.Value.node, 8, Paints.IOPaint);
            }

            foreach (var output in Outputs)
            {
                canvas.DrawCircle(output.Value.node, 8, Paints.IOPaint);
            }

            canvas.DrawRect(this.buttonRect, Paints.ButtonFill);
            canvas.DrawRect(this.buttonRect, Paints.ButtonStroke);

            float textX = buttonRect.MidX - (font.MeasureText(operation) / 2);
            float textY = buttonRect.MidY + font.Size / 4;
            canvas.DrawText(operation, textX, textY, font, Paints.TextPaint);
        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SkiaSharp.SKRect(x - 45, y - 25, x + 45, y + 25);
            this.buttonRect = new SKRect(
            this.Rect.Left + 10,
            this.Rect.Top + 10,
            this.Rect.Right - 10,
            this.Rect.Bottom - 10);

            var delta = Rect.Width / (Inputs.Count + 1);
            string[] keys = Inputs.Keys.ToArray();
            for (int i = 0; i < Inputs.Count; i++)
            {
                Inputs[keys[i]].node = new SKPoint(this.Rect.Left + delta * (i + 1), this.Rect.Top);
            }

            delta = Rect.Width / (Outputs.Count + 1);
            keys = Outputs.Keys.ToArray();
            for (int i = 0; i < Outputs.Count; i++)
            {
                Outputs[keys[i]].node = new SKPoint(this.Rect.Left + delta * (i + 1), this.Rect.Bottom);
            }
        }

        public override (float, float) OffsetRect(int x, int y)
        {
            (float, float) dxdy = base.OffsetRect(x, y);
            var rect = this.buttonRect;
            rect.Offset(dxdy.Item1, dxdy.Item2);
            this.buttonRect = rect;
            return dxdy;
        }

        public override void GenerateCode()
        {
            var inputs = Inputs.Values.ToArray();
            var outputNode = Outputs.Values.First();
            this.Code = $"dynamic {outputNode.name} = {inputs[0].name} {operation} {inputs[^1].name};\n";
        }

        public override (string, Component, IO?)? HitTest(SKPoint world)
        {
            (string, Component, IO?)? baseReturn = base.HitTest(world);
            if (this.buttonRect.Contains(world))
            {
                var dlg = new LogicWindow(operation);

                if (dlg.ShowDialog() == true)
                {
                    this.operation = dlg.op;
                }
                return ("button", this, null);
            }
            return baseReturn;
        }

        public override string ToJson()
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
                $"{outputJsons},\n" +
                $"\"index\": \"{this.Index}\"\n" +
                $"}}\n";
        }
    }
}
