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
            Font = new SKFont();
            IO newNode = new IO();
            IO newNode1 = new IO();
            Outputs.Add(newNode.GetId(), newNode);
            Inputs.Add(newNode1.GetId(), newNode1);
        }

        public Input(string name, string description, string category) : base(name, description, category)
        {
            Font = new SKFont();
            IO newNode = new IO();
            IO newNode1 = new IO();
            Outputs.Add(newNode.GetId(), newNode);
            Inputs.Add(newNode1.GetId(), newNode1);
        }

        public override void Paint(SKCanvas canvas)
        {
            canvas.DrawRect(this.Rect, Paints.ComponentFill);
            Font.Size = 20;

            if (this.IsBroken)
            {
                canvas.DrawRect(this.Rect, Paints.BrokenBlock);
                canvas.DrawRect(this.Rect, Paints.BrokenBlockStroke);
            }
            if (!this.IsBroken && this.Selected)
                canvas.DrawRect(this.Rect, Paints.SelectedStroke);
            if (!this.IsBroken && !this.Selected)
                canvas.DrawRect(this.Rect, Paints.ComponentStroke);

            foreach (var output in Outputs)
            {
                canvas.DrawCircle(output.Value.Node, 8, Paints.IOPaint);
            }

            foreach (var input in Inputs)
            {
                canvas.DrawCircle(input.Value.Node, 8, Paints.IOPaint);
            }

            canvas.DrawRoundRect(InteractionRect, 6, 6, Paints.ButtonFill);
            canvas.DrawRoundRect(InteractionRect, 6, 6, Paints.ButtonStroke);

            var textX = InteractionRect.MidX - (Font.MeasureText(this.Code) / 2);
            var textY = InteractionRect.MidY + Font.Size / 4;
            if (this.Code == "")
            {
                var label = "Make a Numerical variable";
                if (this.Name.Contains("String"))
                {
                    label = "Make a String variable";
                }
                if (this.Name.Contains("Binary"))
                {
                    label = "Make a Binary variable";
                }

                while (InteractionRect.Width < (Font.MeasureText(label) + 5))
                {
                    Font.Size--;
                }

                textX = InteractionRect.MidX - (Font.MeasureText(label) / 2);
                canvas.DrawText(label, textX, textY, Font, Paints.ButtonTextPaint);
            }
            if (this.Code != "")
            {
                Font.Size = 20;
                string[] parts = this.Code.Split(' ');
                string displayCode = parts[1] + " = " + parts[3];

                while (InteractionRect.Width < (Font.MeasureText(displayCode) + 5))
                {
                    Font.Size--;
                }

                textX = InteractionRect.MidX - (Font.MeasureText(displayCode) / 2);
                canvas.DrawText(displayCode, textX, textY, Font, Paints.ButtonTextPaint);
            }

            var labelText = "NUM";
            if (this.Name.Contains("String"))
            {
                labelText = "STR";
            }
            if (this.Name.Contains("Binary"))
            {
                labelText = "BIN";
            }

            Font.Size = 12;
            var textXX = this.Rect.Left + (Font.MeasureText(labelText, Paints.TextPaint) / 5);
            var textYY = this.Rect.Top + Font.Size;
            canvas.DrawText(labelText, textXX, textYY, Font, Paints.TextPaint);
        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SkiaSharp.SKRect(x - 100, y - 50, x + 100, y + 50);
            this.InteractionRect = new SKRect(
                this.Rect.Left + (int)this.Rect.Width / 8,
                this.Rect.Top + (int)this.Rect.Height / 4,
                this.Rect.Right - (int)this.Rect.Width / 8,
                this.Rect.Bottom - (int)this.Rect.Height / 4);

            var delta = Rect.Width / (Outputs.Count + 1);
            string[] keys = Outputs.Keys.ToArray();
            for (int outputIndex = 0; outputIndex < Outputs.Count; outputIndex++)
            {
                Outputs[keys[outputIndex]].Node = new SKPoint(this.Rect.Left + delta * (outputIndex + 1), this.Rect.Bottom);
            }

            delta = Rect.Width / (Inputs.Count + 1);
            keys = Inputs.Keys.ToArray();
            for (int inputIndex = 0; inputIndex < Inputs.Count; inputIndex++)
            {
                Inputs[keys[inputIndex]].Node = new SKPoint(this.Rect.Left + delta * (inputIndex + 1), this.Rect.Top);
            }
        }

        public override void GenerateCode()
        {
            RegistryId key = RegistryId.Global;
            if (this.IsInsideIf != "")
            {
                key = this.IsInsideIf.Split('_')[0];
            }
            if (key.IsGlobal && this.IsInsideWhile != "")
            {
                key = this.IsInsideWhile.Split('_')[0];
            }

            var entry = new ValueRegistry.RegistryEntry
            {
                ScopeId = key,
                BlockId = new RegistryId(this.GetId()),
                Key = new RegistryKey(this.Value.Item1),
                Value = this.Value.Item2
            };

            ValueRegistry.RegisterLocalValue(key, entry.Key, entry);
        }
    }
}