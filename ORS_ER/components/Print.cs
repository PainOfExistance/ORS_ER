using Microsoft.CodeAnalysis;
using ORS_ER.connections;
using ORS_ER.windows;
using SkiaSharp;
using System.Diagnostics;

namespace ORS_ER.components
{
    class Print : Component
    {
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Print);
        public int Index = 0;
        string printValue = "";
        SKRect buttonRect { get; set; }

        public Print(Component component) : base(component)
        {
            IO newNode = new IO();
            Inputs.Add(newNode.GetId(), newNode);
        }

        public Print(string name, string description, string category, int runningIndex) : base(name, description, category, runningIndex)
        {
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

            foreach (var input in Inputs)
            {
                canvas.DrawCircle(input.Value.node, 8, Paints.IOPaint);
            }


            if (string.IsNullOrEmpty(printValue))
            {
                canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ButtonFill);
                canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ButtonStroke);

                const string label = "+";
                var textX = buttonRect.MidX - (font.MeasureText(label) / 2);
                var textY = buttonRect.MidY + font.Size / 4;

                canvas.DrawText(label, textX, textY, font, Paints.ButtonTextPaint);
            }
            else
            {
                float textX = 0;
                float textY = 0;

                var textWidth = font.MeasureText(printValue, Paints.TextPaint);
                while (textWidth > ((buttonRect.Left - Rect.Left) - 5))
                {
                    font.Size--;
                    textWidth = font.MeasureText(printValue, Paints.TextPaint);
                }

                textX = this.Rect.Left + 5;
                textY = this.Rect.MidY + font.Size / 4;

                canvas.DrawText(printValue, textX, textY, font, Paints.TextPaint);
                font.Size = 20;
                canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ButtonFill);
                canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ButtonStroke);

                string label = "+";
                textX = buttonRect.MidX - (font.MeasureText(label) / 2);
                textY = buttonRect.MidY + font.Size / 4;
                canvas.DrawText(label, textX, textY, font, Paints.ButtonTextPaint);
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

            var delta = Rect.Width / (Inputs.Count + 1);
            string[] keys = Inputs.Keys.ToArray();
            for (int i = 0; i < Inputs.Count; i++)
            {
                Inputs[keys[i]].node = new SKPoint(this.Rect.Left + delta * (i + 1), this.Rect.Top);
            }

            if (!string.IsNullOrEmpty(printValue))
            {
                this.buttonRect = new SKRect(
                this.Rect.Left + (3 * ((int)this.Rect.Width / 4)),
                this.Rect.Top + 5,
                this.Rect.Right - 5,
                this.Rect.Bottom - 5);
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

        public override (string, Component, IO?)? HitTest(SKPoint world)
        {
            (string, Component, IO?)? baseReturn = base.HitTest(world);
            if (this.buttonRect.Contains(world))
            {
                var dlg = new PrintWindow(this.printValue);

                if (dlg.ShowDialog() == true && dlg.ResultName != "")
                {

                }
                else
                {

                }

                return ("button", this, null);
            }
            return baseReturn;
        }

        public override void GenerateCode()
        {
            var inputNode = Inputs.Values.First();
            this.Code = $"Console.WriteLine(\"{Name} {Index}: \" +  {inputNode.name});\n";
        }
    }
}
