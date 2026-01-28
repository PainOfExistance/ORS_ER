using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace ORS_ER.components
{
    class Print : Component
    {
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Print);

        public Print(Component component) : base(component)
        {
            Inputs.Add(new IO("name", 2));

        }

        public Print(string name, string description, string category) : base(name, description, category)
        {
            Inputs.Add(new IO("name", 2));
        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SkiaSharp.SKRect(x - 75, y - 25, x + 75, y + 25);
            var delta = Rect.Width / (Inputs.Count + 1);
            for (int i = 0; i < Inputs.Count; i++)
            {
                Inputs[i].node = new SKPoint(this.Rect.Left + delta * (i + 1), this.Rect.Top);
            }
        }

        public override void Paint(SKCanvas canvas)
        {
            canvas.DrawRect(this.Rect, Paints.ComponentFill);

            if (this.Selected)
                canvas.DrawRect(this.Rect, Paints.SelectedStroke);
            else
                canvas.DrawRect(this.Rect, Paints.ComponentStroke);

            foreach (var input in this.Inputs)
            {
                canvas.DrawCircle(input.node, 8, Paints.IOPaint);
            }

            canvas.DrawText(this.Name, this.Rect.MidX - (Paints.TextPaint.MeasureText(this.Name) / 2), this.Rect.MidY + (Paints.TextPaint.TextSize / 2), Paints.TextPaint);
        }
    }
}
