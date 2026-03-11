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
            Font = new SKFont();
            this.Value = ("bool", false);
            IO newNode = new IO();
            Inputs.Add(newNode.GetId(), newNode);
        }

        public BinaryOutput(string name, string description, string category) : base(name, description, category)
        {
            this.Code = name.Split(" ")[0];
            Font = new SKFont();
            this.Value = ("bool", false);
            IO newNode = new IO();
            Inputs.Add(newNode.GetId(), newNode);
        }

        public override void Paint(SKCanvas canvas)
        {
            canvas.DrawRect(this.Rect, Paints.ComponentFill);
            Font.Size = 20;

            if (this.Selected)
                canvas.DrawRect(this.Rect, Paints.SelectedStroke);
            if (!this.Selected)
                canvas.DrawRect(this.Rect, Paints.ComponentStroke);

            foreach (var input in this.Inputs)
            {
                canvas.DrawCircle(input.Value.Node, 8, Paints.InputIOPaint);
            }

            var label = "0";
            var fillPaint = Paints.ValueFalse;
            if (this.Value.Item2)
            {
                label = "1";
                fillPaint = Paints.ValueTrue;
            }
            canvas.DrawRoundRect(InteractionRect, 6, 6, fillPaint);
            canvas.DrawRoundRect(InteractionRect, 6, 6, Paints.ButtonStroke);

            float textX = Rect.MidX - (Font.MeasureText(label) / 2);
            float textY = Rect.MidY + Font.Size / 3;
            canvas.DrawText(label, textX, textY, Font, Paints.ButtonTextPaint);
        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SKRect(x - 25, y - 25, x + 25, y + 25);
            this.InteractionRect = new SKRect(
                this.Rect.Left + 10,
                this.Rect.Top + 10,
                this.Rect.Right - 10,
                this.Rect.Bottom - 10);

            var delta = Rect.Width / (Inputs.Count + 1);
            string[] keys = Inputs.Keys.ToArray();
            for (int inputIndex = 0; inputIndex < Inputs.Count; inputIndex++)
            {
                Inputs[keys[inputIndex]].Node = new SKPoint(this.Rect.Left + delta * (inputIndex + 1), this.Rect.Top);
            }
        }

        public override void RunInternalSimulation(List<bool> vals)
        {
            this.Value = ("bool", vals[0]);
        }
    }
}