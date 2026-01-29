using ORS_ER.connections;
using SkiaSharp;
using System.Diagnostics;
using System.Windows.Input;

namespace ORS_ER.components
{
    class Input : Component
    {
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Input);
        SKRect buttonRect { get; set; }
        public Input(Component component) : base(component)
        {
            IO newNode = new IO();
            Outputs.Add(newNode.GetId(), newNode);
        }

        public Input(string name, string description, string category) : base(name, description, category)
        {
            IO newNode = new IO();
            Outputs.Add(newNode.GetId(), newNode);
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
                canvas.DrawCircle(output.Value.node, 8, Paints.IOPaint);
            }

            if (this.Outputs[this.Outputs.Keys.ToArray()[0]].value == null)
            {
                canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ButtonFill);
                canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ButtonStroke);

                const string label = "+";
                var textX = buttonRect.MidX - (Paints.ButtonTextPaint.MeasureText(label) / 2);
                var textY = buttonRect.MidY + (Paints.ButtonTextPaint.TextSize / 2) - 2;
                canvas.DrawText(label, textX, textY, Paints.ButtonTextPaint);
            }
            else
            {
                float textX = 0;
                float textY = 0;
                foreach (var output in this.Outputs)
                {
                    var nameText = output.Value.name.ToString() ?? "null";
                    var valueText = output.Value.value.ToString() ?? "null";
                    var fullText = $"{nameText}: {valueText}";
                    textX = this.Rect.MidX - (Paints.TextPaint.MeasureText(fullText) / 2);
                    textY = this.Rect.Top + (Paints.TextPaint.TextSize) + 5;
                    canvas.DrawText(fullText, textX, textY, Paints.TextPaint);
                }

                var buttonRect = new SKRect(
                    this.Rect.Left + (int)this.Rect.Width / 4,
                    this.Rect.Top + 3 * ((int)this.Rect.Height / 4),
                    this.Rect.Right - (int)this.Rect.Width / 4,
                    this.Rect.Bottom - 5);
                canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ButtonFill);
                canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ButtonStroke);
                const string label = "Eddit";
                textX = buttonRect.MidX - (Paints.ButtonTextPaint.MeasureText(label) / 2);
                textY = buttonRect.MidY + (Paints.ButtonTextPaint.TextSize / 2) - 2;
                canvas.DrawText(label, textX, textY, Paints.ButtonTextPaint);
            }
        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SkiaSharp.SKRect(x - 75, y - 25, x + 75, y + 25);
            this.buttonRect = new SKRect(
                this.Rect.Left + (int)this.Rect.Width / 4,
                this.Rect.Top + (int)this.Rect.Height / 4,
                this.Rect.Right - (int)this.Rect.Width / 4,
                this.Rect.Bottom - (int)this.Rect.Height / 4);

            var delta = Rect.Width / (Outputs.Count + 1);
            string[] keys = Outputs.Keys.ToArray();
            for (int i = 0; i < Outputs.Count; i++)
            {
                Outputs[keys[i]].node = new SKPoint(this.Rect.Left + delta * (i + 1), this.Rect.Bottom);
            }
        }

        public override void OffsetRect(int x, int y)
        {
            base.OffsetRect(x, y);
            var rect = this.buttonRect;
            var dx = x - rect.MidX;
            var dy = y - rect.MidY;
            rect.Offset(dx, dy);
            this.buttonRect = rect;

        }

        public override (string, Component, IO?)? HitTest(SKPoint world)
        {
            (string, Component, IO?)? baseReturn = base.HitTest(world);
            if (this.buttonRect.Contains(world))
            {
                Debug.WriteLine("Button hit!");
                return ("button", this, null);
            }
            return baseReturn;
        }
    }
}