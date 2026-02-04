using ORS_ER.connections;
using ORS_ER.windows;
using SkiaSharp;

namespace ORS_ER.components
{
    class Input : Component
    {
        private static ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Input);
        SKRect buttonRect { get; set; }
        public Input(Component component) : base(component)
        {
            base.font = new SKFont();
            IO newNode = new IO();
            if (component.Name.Contains("String"))
            {
                newNode.value = "";
            }
            else
            {
                newNode.value = 0.0;
            }
            Outputs.Add(newNode.GetId(), newNode);
        }

        public Input(string name, string description, string category, int runningIndex) : base(name, description, category, runningIndex)
        {
            base.font = new SKFont();
            IO newNode = new IO();
            if (name.Contains("String"))
            {
                newNode.value = "";
            }
            else if (name.Contains("Binary"))
            {
                newNode.value = false;
            }
            else
            {
                newNode.value = 0;
            }
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

            foreach (var output in Outputs)
            {
                canvas.DrawCircle(output.Value.node, 8, Paints.IOPaint);
            }

            var outputNode = Outputs.Values.First();
            if (string.IsNullOrEmpty(outputNode.name))
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
                var nameText = outputNode.name?.ToString() ?? "null";
                var valueText = outputNode.value?.ToString() ?? "null";
                if (outputNode.value is bool)
                {
                    valueText = valueText.ToLower();
                }
                var fullText = $"{nameText}: {valueText}";

                var textWidth = font.MeasureText(fullText, Paints.TextPaint);
                while (textWidth > ((buttonRect.Left - Rect.Left) - 5))
                {
                    font.Size--;
                    textWidth = font.MeasureText(fullText, Paints.TextPaint);
                }
                textX = this.Rect.Left + 5;
                textY = this.Rect.MidY + font.Size / 4;

                canvas.DrawText(fullText, textX, textY, font, Paints.TextPaint);
                font.Size = 20;
                canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ButtonFill);
                canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ButtonStroke);

                string label = "+";
                textX = buttonRect.MidX - (font.MeasureText(label) / 2);
                textY = buttonRect.MidY + font.Size / 4;
                canvas.DrawText(label, textX, textY, font, Paints.ButtonTextPaint);
            }

            string lab = "";
            if (this.Name.Contains("String"))
            {
                lab = "STR";
            }
            else if (this.Name.Contains("Binary"))
            {
                lab = "BIN";
            }
            else
            {
                lab = "NUM";
            }

            font.Size = 12;
            var textXX = this.Rect.Left + (font.MeasureText(lab, Paints.TextPaint) / 5);
            var textYY = this.Rect.Top + font.Size;
            canvas.DrawText(lab, textXX, textYY, font, Paints.TextPaint);
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

            if (!string.IsNullOrEmpty(Outputs.Values.First().name))
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
                var outputNode = Outputs.Values.First();
                var dlg = new InputWindow(Name, outputNode.name, outputNode.value);

                if (dlg.ShowDialog() == true)
                {
                    outputNode.name = dlg.ResultName;
                    outputNode.value = dlg.ResultValue;
                    this.buttonRect = new SKRect(
                    this.Rect.Left + (3 * ((int)this.Rect.Width / 4)),
                    this.Rect.Top + 5,
                    this.Rect.Right - 5,
                    this.Rect.Bottom - 5);
                }
                else
                {
                    outputNode.name = null;
                    outputNode.value = null;
                    this.buttonRect = new SKRect(
                    this.Rect.Left + (int)this.Rect.Width / 4,
                    this.Rect.Top + (int)this.Rect.Height / 4,
                    this.Rect.Right - (int)this.Rect.Width / 4,
                    this.Rect.Bottom - (int)this.Rect.Height / 4);
                }

                return ("button", this, null);
            }
            return baseReturn;
        }

        public override void GenerateCode()
        {
            var outputNode = Outputs.Values.First();
            if (this.Name.Contains("Binary"))
            {
                this.Code = $"dynamic {outputNode.name} = {outputNode.value.ToString().ToLower()};\n";
            }
            else
            {
                this.Code = $"dynamic {outputNode.name} = {outputNode.value};\n";
            }
        }
    }
}