using SkiaSharp;
using System.Diagnostics;

namespace ORS_ER.components
{
    class Input : Component
    {
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Input);

        public Input(Component component) : base(component)
        {
            Outputs.Add(new IO("name", 2));
        }

        public Input(string name, string description, string category) : base(name, description, category)
        {
            Outputs.Add(new IO("name", 2));
        }

        public override void Paint(SKCanvas canvas)
        {
            canvas.DrawRect(this.Rect, Paints.ComponentFill);

            if (this.Selected)
                canvas.DrawRect(this.Rect, Paints.SelectedStroke);
            else
                canvas.DrawRect(this.Rect, Paints.ComponentStroke);

            foreach (var output in this.Outputs)
            {
                canvas.DrawCircle(output.node, 8, Paints.IOPaint);
            }

            canvas.DrawText(this.Name, this.Rect.MidX - (Paints.TextPaint.MeasureText(this.Name) / 2), this.Rect.MidY + (Paints.TextPaint.TextSize / 2), Paints.TextPaint);
        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SkiaSharp.SKRect(x - 75, y - 25, x + 75, y + 25);
            var delta=Rect.Width/(Outputs.Count+1);
            for (int i=0; i<Outputs.Count; i++)
            {
                Outputs[i].node = new SKPoint(this.Rect.Left+delta*(i+1), this.Rect.Bottom);
            }
        }
    }
}