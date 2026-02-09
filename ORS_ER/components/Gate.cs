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
        public Gate(Component component) : base(component)
        {
            IO newNode1 = new IO();
            IO newNode3 = new IO();
            Inputs.Add(newNode1.GetId(), newNode1);
            Outputs.Add(newNode3.GetId(), newNode3);
        }

        public Gate(string name, string description, string category) : base(name, description, category)
        {
            IO newNode1 = new IO();
            IO newNode3 = new IO();
            Inputs.Add(newNode1.GetId(), newNode1);
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

            string label = "+";
            if (string.IsNullOrEmpty(this.Code))
            {
                label = "+";
            }
            else
            {
                label = this.Code;
            }

            float textX = buttonRect.MidX - (font.MeasureText(label) / 2);
            float textY = buttonRect.MidY + font.Size / 4;
            canvas.DrawText(label, textX, textY, font, Paints.TextPaint);
        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SkiaSharp.SKRect(x - 100, y - 15, x + 100, y + 50);
            this.buttonRect = new SKRect(
                this.Rect.Left + (int)this.Rect.Width / 4,
                this.Rect.Top + (int)this.Rect.Height / 4,
                this.Rect.Right - (int)this.Rect.Width / 4,
                this.Rect.Bottom - (int)this.Rect.Height / 4);

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

        public override void GenerateCode()
        {
            var inputs = Inputs.Values.ToArray();
            var outputNode = Outputs.Values.First();

            switch (operation)
            {
                case "AND":
                    this.Code = $"dynamic {outputNode.name} = {inputs[0].name} & {inputs[1].name};\n";
                    break;
                case "OR":
                    this.Code = $"dynamic {outputNode.name} = {inputs[0].name} | {inputs[1].name};\n";
                    break;
                case "NOT":
                    this.Code = $"dynamic {outputNode.name} = !{inputs[0].name};\n";
                    break;
                case "XOR":
                    this.Code = $"dynamic {outputNode.name} = {inputs[0].name} ^ {inputs[1].name};\n";
                    break;
                case "NOR":
                    this.Code = $"dynamic {outputNode.name} = !({inputs[0].name} | {inputs[1].name});\n";
                    break;
                case "XNOR":
                    this.Code = $"dynamic {outputNode.name} = !({inputs[0].name} ^ {inputs[1].name});\n";
                    break;
                case "NAND":
                    this.Code = $"dynamic {outputNode.name} = !({inputs[0].name} && {inputs[1].name});\n";
                    break;
                default:
                    this.Code = $"dynamic {outputNode.name} = {inputs[0].name} | {inputs[1].name};\n";
                    break;
            }
        }
    }
}
