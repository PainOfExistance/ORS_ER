using ORS_ER.connections;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;


namespace ORS_ER.components
{
    class BinaryOutput : Component
    {
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Print);
        public BinaryOutput(Component component) : base(component)
        {
            this.Code = component.Name.Split(" ")[0];
            base.font = new SKFont();
            this.Value = ("bool", false);
            IO newNode = new IO();
            Inputs.Add(newNode.GetId(), newNode);
        }

        public BinaryOutput(string name, string description, string category) : base(name, description, category)
        {
            this.Code = name.Split(" ")[0];
            base.font = new SKFont();
            this.Value = ("bool", false);
            IO newNode = new IO();
            Inputs.Add(newNode.GetId(), newNode);
        }

        public override void Paint(SKCanvas canvas)
        {
            canvas.DrawRect(this.Rect, Paints.ComponentFill);
            font.Size = 20;

            if (this.Selected)
                canvas.DrawRect(this.Rect, Paints.SelectedStroke);
            else
                canvas.DrawRect(this.Rect, Paints.ComponentStroke);

            foreach (var input in this.Inputs)
            {
                canvas.DrawCircle(input.Value.node, 8, Paints.IOPaint);
            }

            string label;
            if (!this.Value.Item2)
            {
                canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ValueFalse);
                canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ButtonStroke);
                label = "0";
            }
            else
            {
                canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ValueTrue);
                canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ButtonStroke);
                label = "1";
            }

            float textX = Rect.MidX - (font.MeasureText(label) / 2);
            float textY = Rect.MidY + font.Size / 3;
            canvas.DrawText(label, textX, textY, font, Paints.ButtonTextPaint);
        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SKRect(x - 25, y - 25, x + 25, y + 25);
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
        }

        public override void GenerateCode(List<bool> vals)
        {
            this.Value = ("bool", vals[0]);
        }
    }
}