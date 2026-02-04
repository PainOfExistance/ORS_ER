using ORS_ER.connections;
using SkiaSharp;

namespace ORS_ER.components
{
    class BinaryPrint : Component
    {
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Print);
        SKRect valueRect { get; set; }
        public BinaryPrint(Component component) : base(component)
        {
            IO newNode = new IO();
            newNode.value = false;
            Inputs.Add(newNode.GetId(), newNode);
        }

        public BinaryPrint(string name, string description, string category, int runningIndex) : base(name, description, category, runningIndex)
        {
            IO newNode = new IO();
            newNode.value = false;
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

            var inputNode = Inputs.Values.First();
            string label;
            if (inputNode.value == null || (bool)inputNode.value == false)
            {
                canvas.DrawRoundRect(valueRect, 6, 6, Paints.ValueFalse);
                canvas.DrawRoundRect(valueRect, 6, 6, Paints.ButtonStroke);
                label = "0";
            }
            else
            {
                canvas.DrawRoundRect(valueRect, 6, 6, Paints.ValueTrue);
                canvas.DrawRoundRect(valueRect, 6, 6, Paints.ButtonStroke);
                label = "1";
            }

            float textX = Rect.MidX - (font.MeasureText(label) / 2);
            float textY = Rect.MidY + font.Size / 3;
            canvas.DrawText(label, textX, textY, font, Paints.ButtonTextPaint);
        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SKRect(x - 25, y - 25, x + 25, y + 25);
            this.valueRect = new SKRect(
                this.Rect.Left + 10,
                this.Rect.Top + 10,
                this.Rect.Right - 10,
                this.Rect.Bottom - 10);

            var delta = Rect.Width / (Inputs.Count + 1);
            string[] keys = Inputs.Keys.ToArray();
            for (int i = 0; i < Inputs.Count; i++)
            {
                Inputs[keys[i]].node = new SKPoint(this.Rect.Left + delta * (i + 1), this.Rect.Top);
            }
        }
        
        public override (float, float) OffsetRect(int x, int y)
        {
            (float, float) dxdy = base.OffsetRect(x, y);
            var rect = this.valueRect;
            rect.Offset(dxdy.Item1, dxdy.Item2);
            this.valueRect = rect;
            return dxdy;
        }
        
        public override void GenerateCode()
        {
            var inputNode = Inputs.Values.First();
            this.Code = $"Console.WriteLine(\"{Name}: \" +  {inputNode.name});\n";
        }

    }
}
