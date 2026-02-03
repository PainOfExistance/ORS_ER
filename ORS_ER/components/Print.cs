using ORS_ER.connections;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace ORS_ER.components
{
    class Print : Component
    {
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Print);
        public int Index = 0;
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

            if (this.Selected)
                canvas.DrawRect(this.Rect, Paints.SelectedStroke);
            else
                canvas.DrawRect(this.Rect, Paints.ComponentStroke);

            foreach (var input in this.Inputs)
            {
                canvas.DrawCircle(input.Value.node, 8, Paints.IOPaint);
            }

            if (this.Inputs.Values.First().value == null)
                canvas.DrawText($"{this.Name} {this.Index}", this.Rect.MidX - (Paints.TextPaint.MeasureText($"{this.Name} {this.Index}") / 2), this.Rect.MidY + (Paints.TextPaint.TextSize / 2), Paints.TextPaint);
            else
                canvas.DrawText($"{this.Name} {this.Index}: {this.Inputs.First().Value.value}", this.Rect.Left + (Paints.TextPaint.MeasureText($"{this.Name} {this.Index}: {this.Inputs.First().Value.value}")/4), this.Rect.MidY + (Paints.TextPaint.TextSize / 2), Paints.TextPaint);
            //TODO make drawing dynamic based on input text length
        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SkiaSharp.SKRect(x - 75, y - 25, x + 75, y + 25);
            var delta = Rect.Width / (Inputs.Count + 1);
            string[] keys = Inputs.Keys.ToArray();
            for (int i = 0; i < Inputs.Count; i++)
            {
                Inputs[keys[i]].node = new SKPoint(this.Rect.Left + delta * (i + 1), this.Rect.Top);
            }
        }

        public override void GenerateCode()
        {
            this.Code = $"Console.WriteLine(\"{this.Name} {this.Index}: \" +  {this.Inputs.First().Value.name});\n";
        }
    }
}
