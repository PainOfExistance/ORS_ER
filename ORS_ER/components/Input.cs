using ORS_ER.connections;
using ORS_ER.windows;
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
            base.font = new SKFont();
            IO newNode = new IO();
            Outputs.Add(newNode.GetId(), newNode);
        }

        public Input(string name, string description, string category) : base(name, description, category)
        {
            base.font = new SKFont();
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
                canvas.DrawCircle(output.Value.node, 8, Paints.IOPaint);
            }

            if (this.Outputs.First().Value.name == null || this.Outputs.First().Value.name == "")
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
                foreach (var output in this.Outputs)
                {
                    var nameText = output.Value.name?.ToString() ?? "null";
                    var valueText = output.Value.value?.ToString() ?? "null";
                    var fullText = $"{nameText}: {valueText}";

                    var textWidth = font.MeasureText(fullText, Paints.TextPaint);
                    while (textWidth > ((buttonRect.Left - Rect.Left)-5))
                    {
                        font.Size--;
                        textWidth = font.MeasureText(fullText, Paints.TextPaint);
                    }
                    textX = this.Rect.Left+5;
                    textY = this.Rect.MidY + font.Size / 4;

                    canvas.DrawText(fullText, textX, textY, font, Paints.TextPaint);
                }
                font.Size = 20;
                canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ButtonFill);
                canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ButtonStroke);

                const string label = "+";
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

            var delta = Rect.Width / (Outputs.Count + 1);
            string[] keys = Outputs.Keys.ToArray();
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

        public override (string, Component, IO?)? HitTest(SKPoint world)
        {
            (string, Component, IO?)? baseReturn = base.HitTest(world);
            if (this.buttonRect.Contains(world))
            {
                var dlg = new InputWindow(Outputs.First().Value.name, Outputs.First().Value.value);

                if (dlg.ShowDialog() == true)
                {
                    if ((dlg.ResultName != "" && dlg.ResultName != null))
                    {
                        Outputs.First().Value.name = dlg.ResultName;
                        Outputs.First().Value.value = dlg.ResultValue;
                        this.buttonRect = new SKRect(
                        this.Rect.Left + (3 * ((int)this.Rect.Width / 4)),
                        this.Rect.Top + 5,
                        this.Rect.Right - 5,
                        this.Rect.Bottom - 5);
                    }
                    else
                    {
                        Outputs.First().Value.name = null;
                        Outputs.First().Value.value = null;
                        this.buttonRect = new SKRect(
                        this.Rect.Left + (int)this.Rect.Width / 4,
                        this.Rect.Top + (int)this.Rect.Height / 4,
                        this.Rect.Right - (int)this.Rect.Width / 4,
                        this.Rect.Bottom - (int)this.Rect.Height / 4);
                    }
                }
                Debug.WriteLine("Button hit!");
                return ("button", this, null);
            }
            return baseReturn;
        }

        public override void GenerateCode()
        {
            this.Code = $"dynamic {this.Outputs.First().Value.name} = {this.Outputs.First().Value.value};\n";
        }
    }
}