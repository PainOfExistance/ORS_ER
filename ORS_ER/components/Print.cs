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

        public Print(Component component) : base(component)
        {
            Font = new SKFont();
            IO newNode = new IO();
            IO newNode1 = new IO();
            Outputs.Add(newNode.GetId(), newNode);
            Inputs.Add(newNode1.GetId(), newNode1);
        }

        public Print(string name, string description, string category) : base(name, description, category)
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
                textX = InteractionRect.MidX - (Font.MeasureText("+") / 2);
                canvas.DrawText("+", textX, textY, Font, Paints.ButtonTextPaint);
            }
            if (this.Code != "")
            {
                string[] parts = this.Code.Split('(');
                string displayCode = parts[1].Split(')')[0];

                while (InteractionRect.Width < (Font.MeasureText(displayCode) + 5))
                {
                    Font.Size--;
                }

                textX = InteractionRect.MidX - (Font.MeasureText(displayCode) / 2);
                canvas.DrawText(displayCode, textX, textY, Font, Paints.ButtonTextPaint);
            }
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

            var registryEntry = ValueRegistry.GetLocalValue(key, new RegistryKey(this.Value.Item1));
            if (registryEntry is not RegistryEntry)
            {
                Console.WriteLine(this.Value.Item1);
            }
            else
            {
                Console.WriteLine(this.Value.Item1 + ": " + registryEntry?.Value);
            }
        }
    }
}
