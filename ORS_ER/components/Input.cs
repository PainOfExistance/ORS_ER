using ORS_ER.connections;
using ORS_ER.windows;
using SkiaSharp;
using System.Xml.Linq;

namespace ORS_ER.components
{
    class Input : Component
    {
        private static ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Input);
        public Input(Component component) : base(component)
        {
            base.font = new SKFont();
            IO newNode = new IO();
            IO newNode1 = new IO();
            Outputs.Add(newNode.GetId(), newNode);
            Inputs.Add(newNode1.GetId(), newNode1);
        }

        public Input(string name, string description, string category) : base(name, description, category)
        {
            base.font = new SKFont();
            IO newNode = new IO();
            IO newNode1 = new IO();
            Outputs.Add(newNode.GetId(), newNode);
            Inputs.Add(newNode1.GetId(), newNode1);
        }

        public override void Paint(SKCanvas canvas)
        {
            canvas.DrawRect(this.Rect, Paints.ComponentFill);
            font.Size = 20;

            if (this.IsBroken)
            {
                canvas.DrawRect(this.Rect, Paints.BrokenBlock);
                canvas.DrawRect(this.Rect, Paints.BrokenBlockStroke);
            }
            else if (this.Selected)
                canvas.DrawRect(this.Rect, Paints.SelectedStroke);
            else
                canvas.DrawRect(this.Rect, Paints.ComponentStroke);

            foreach (var output in Outputs)
            {
                canvas.DrawCircle(output.Value.node, 8, Paints.IOPaint);
            }

            foreach (var input in Inputs)
            {
                canvas.DrawCircle(input.Value.node, 8, Paints.IOPaint);
            }

            canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ButtonFill);
            canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ButtonStroke);

            var textX = buttonRect.MidX - (font.MeasureText(this.Code) / 2);
            var textY = buttonRect.MidY + font.Size / 4;
            if (this.Code == "")
            {
                textX = buttonRect.MidX - (font.MeasureText("+") / 2);
                canvas.DrawText("+", textX, textY, font, Paints.ButtonTextPaint);
            }
            else
            {
                string[] parts = this.Code.Split(' ');
                string displayCode = parts[1] + " = " + parts[3];

                while (buttonRect.Width < (font.MeasureText(displayCode) + 5))
                {
                    font.Size--;
                }

                textX = buttonRect.MidX - (font.MeasureText(displayCode) / 2);
                canvas.DrawText(displayCode, textX, textY, font, Paints.ButtonTextPaint);
            }

            string label = "";
            if (this.Name.Contains("String"))
            {
                label = "STR";
            }
            else if (this.Name.Contains("Binary"))
            {
                label = "BIN";
            }
            else
            {
                label = "NUM";
            }

            font.Size = 12;
            var textXX = this.Rect.Left + (font.MeasureText(label, Paints.TextPaint) / 5);
            var textYY = this.Rect.Top + font.Size;
            canvas.DrawText(label, textXX, textYY, font, Paints.TextPaint);
        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SkiaSharp.SKRect(x - 100, y - 50, x + 100, y + 50);
            this.buttonRect = new SKRect(
                this.Rect.Left + (int)this.Rect.Width / 8,
                this.Rect.Top + (int)this.Rect.Height / 4,
                this.Rect.Right - (int)this.Rect.Width / 8,
                this.Rect.Bottom - (int)this.Rect.Height / 4);

            var delta = Rect.Width / (Outputs.Count + 1);
            string[] keys = Outputs.Keys.ToArray();
            for (int i = 0; i < Outputs.Count; i++)
            {
                Outputs[keys[i]].node = new SKPoint(this.Rect.Left + delta * (i + 1), this.Rect.Bottom);
            }

            delta = Rect.Width / (Inputs.Count + 1);
            keys = Inputs.Keys.ToArray();
            for (int i = 0; i < Inputs.Count; i++)
            {
                Inputs[keys[i]].node = new SKPoint(this.Rect.Left + delta * (i + 1), this.Rect.Top);
            }
        }

        public override void GenerateCode()
        {
            string key = "";
            if (this.IsInsideIf != "")
            {
                key = this.IsInsideIf.Split('_')[0];
            }
            else if (this.IsInsideWhile != "")
            {
                key = this.IsInsideWhile.Split('_')[0];
            }

            ValueRegistry.RegisterLocalValue(key, this.Value.Item1, new ValueRegistry.RegistryEntry { BlockId = this.GetId(), Name = this.Value.Item1, Value = this.Value.Item2 });
        }
    }
}