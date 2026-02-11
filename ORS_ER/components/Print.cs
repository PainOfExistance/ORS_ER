using Microsoft.CodeAnalysis;
using ORS_ER.connections;
using ORS_ER.windows;
using SkiaSharp;
using System.Diagnostics;
using System.IO;
using static ORS_ER.connections.ValueRegistry;

namespace ORS_ER.components
{
    class Print : Component
    {
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Print);
        string printValue = "";

        public Print(Component component) : base(component)
        {
            base.font = new SKFont();
            IO newNode = new IO();
            IO newNode1 = new IO();
            Outputs.Add(newNode.GetId(), newNode);
            Inputs.Add(newNode1.GetId(), newNode1);
        }

        public Print(string name, string description, string category) : base(name, description, category)
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

            if (this.Selected)
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
            canvas.DrawText(this.Code, textX, textY, font, Paints.ButtonTextPaint);
        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SkiaSharp.SKRect(x - 100, y - 50, x + 100, y + 50);
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

            delta = Rect.Width / (Inputs.Count + 1);
            keys = Inputs.Keys.ToArray();
            for (int i = 0; i < Inputs.Count; i++)
            {
                Inputs[keys[i]].node = new SKPoint(this.Rect.Left + delta * (i + 1), this.Rect.Top);
            }
        }

        public override void GenerateCode()
        {
            if (this.IsInsideIf != "")
            {
                string key = this.IsInsideIf.Split('_')[0];
                var variable = ValueRegistry.GetLocalValue(key, this.Value.Item1);
                if (variable is not RegistryEntry)
                {
                    Console.WriteLine(this.Value.Item1);

                }
                else
                {
                    Console.WriteLine(this.Value.Item1 + ": " + variable?.Value);
                }
            }
            else if (this.IsInsideWhile != "")
            {
                string key = this.IsInsideWhile.Split('_')[0];
                var variable = ValueRegistry.GetLocalValue(key, this.Value.Item1);
                if (variable is not RegistryEntry)
                {
                    Console.WriteLine(this.Value.Item1);

                }
                else
                {
                    Console.WriteLine(this.Value.Item1 + ": " + variable?.Value);
                }
            }
            else
            {
                var variable = ValueRegistry.GetGlobalValue(this.Value.Item1);
                if (variable is not RegistryEntry)
                {
                    Console.WriteLine(this.Value.Item1);

                }
                else
                {
                    Console.WriteLine(this.Value.Item1 + ": " + variable?.Value);
                }
            }
        }
    }
}
