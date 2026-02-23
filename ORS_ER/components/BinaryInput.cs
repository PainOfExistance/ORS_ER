using ORS_ER.connections;
using ORS_ER.windows;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

namespace ORS_ER.components
{
    class BinaryInput : Component
    {
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Input);
        public BinaryInput(Component component) : base(component)
        {
            this.Code = component.Name.Split(" ")[0];
            base.font = new SKFont();
            this.Value = ("bool", false);
            IO newNode = new IO();
            Outputs.Add(newNode.GetId(), newNode);
        }

        public BinaryInput(string name, string description, string category) : base(name, description, category)
        {
            this.Code = name.Split(" ")[0];
            base.font = new SKFont();
            this.Value = ("bool", false);
            IO newNode = new IO();
            Outputs.Add(newNode.GetId(), newNode);
        }

        public override void Paint(SKCanvas canvas)
        {
            canvas.DrawRect(this.Rect, Paints.ComponentFill);
            font.Size = 20;

            if (this.Selected)
                canvas.DrawRect(this.Rect, Paints.SelectedStroke);
            else
                canvas.DrawRect(this.Rect, Paints.ComponentStroke);

            foreach (var output in this.Outputs)
            {
                if (this.Value.Item2)
                    canvas.DrawCircle(output.Value.node, 8, Paints.IOPaintActive);
                else
                    canvas.DrawCircle(output.Value.node, 8, Paints.IOPaint);
            }

            string label;
            if (this.Value.Item2 == false)
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

            var delta = Rect.Width / (Outputs.Count + 1);
            string[] keys = Outputs.Keys.ToArray();
            for (int i = 0; i < Outputs.Count; i++)
            {
                Outputs[keys[i]].node = new SKPoint(this.Rect.Left + delta * (i + 1), this.Rect.Bottom);
            }
        }

        public override void RunInternalSimulation(List<bool> vals)
        {
        }
    }
}