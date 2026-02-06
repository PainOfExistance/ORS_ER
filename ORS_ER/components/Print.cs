using ORS_ER.connections;
using SkiaSharp;
using System.Diagnostics;

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

            foreach (var input in Inputs)
            {
                canvas.DrawCircle(input.Value.node, 8, Paints.IOPaint);
            }

            var inputNode = Inputs.Values.First();
            var label = $"{Name} {Index}";
            if (inputNode.value == null)
                canvas.DrawText(label, Rect.MidX - (Paints.TextPaint.MeasureText(label) / 2), Rect.MidY + (Paints.TextPaint.TextSize / 2), Paints.TextPaint);
            else
                canvas.DrawText($"{label}: {inputNode.value}", Rect.Left + (Paints.TextPaint.MeasureText($"{label}: {inputNode.value}") / 4), Rect.MidY + (Paints.TextPaint.TextSize / 2), Paints.TextPaint);
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
            var inputNode = Inputs.Values.First();
            this.Code = $"Console.WriteLine(\"{Name} {Index}: \" +  {inputNode.name});\n";
        }
    }
}
