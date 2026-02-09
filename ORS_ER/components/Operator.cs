using ORS_ER.connections;
using ORS_ER.windows;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace ORS_ER.components
{
    class Operator : Component
    {
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Operator);
        public Operator(Component component) : base(component)
        {
            this.Code = component.Code;
            IO newNode1 = new IO();
            IO newNode3 = new IO();
            newNode1.value = 0.0;
            newNode3.value = 0.0;
            Inputs.Add(newNode1.GetId(), newNode1);
            Outputs.Add(newNode3.GetId(), newNode3);
        }

        public Operator(string name, string description, string category) : base(name, description, category)
        {
            this.Code = "+";
            IO newNode1 = new IO();
            IO newNode3 = new IO();
            newNode1.value = 0.0;
            newNode3.value = 0.0;
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

            float textX = buttonRect.MidX - (font.MeasureText(Code) / 2);
            float textY = buttonRect.MidY + font.Size / 4;
            canvas.DrawText(Code, textX, textY, font, Paints.TextPaint);

            if (this.Outputs.First().Value.name == null)
            {
                this.Outputs.First().Value.name = "";
            }

            font.Size = 12;
            var textXX = this.Rect.MidX - (font.MeasureText("Name: " + this.Outputs.First().Value.name, Paints.TextPaint) / 2);
            var textYY = this.Rect.Top + font.Size;
            canvas.DrawText("Name: " + this.Outputs.First().Value.name, textXX, textYY, font, Paints.TextPaint);
        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SkiaSharp.SKRect(x - 45, y - 25, x + 45, y + 25);
            this.buttonRect = new SKRect(
            this.Rect.Left + 10,
            this.Rect.Top + 15,
            this.Rect.Right - 10,
            this.Rect.Bottom - 5);

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
            this.Code = $"dynamic {outputNode.name} = {inputs[0].name} {Code} {inputs[1].name};\n";
        }
    }
}
