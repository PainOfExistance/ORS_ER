using ORS_ER.connections;
using SkiaSharp;

namespace ORS_ER.components
{
    internal sealed class If : Component
    {
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.If);

        public If(Component component) : base(component)
        {
            // condition
            var cond = new IO { value = false };
            Inputs.Add(cond.GetId(), cond);
            IO newNode1 = new IO();
            IO newNode2 = new IO();
            Outputs.Add(newNode1.GetId(), newNode1);
            Outputs.Add(newNode2.GetId(), newNode2);
        }

        public If(string name, string description, string category, int runningIndex) : base(name, description, category, runningIndex)
        {
            var cond = new IO { value = false };
            Inputs.Add(cond.GetId(), cond);
            IO newNode1 = new IO();
            IO newNode2 = new IO();
            Outputs.Add(newNode1.GetId(), newNode1);
            Outputs.Add(newNode2.GetId(), newNode2);
        }

        public override void Paint(SKCanvas canvas)
        {
            canvas.DrawRect(Rect, Paints.ComponentFill);

            if (Selected)
                canvas.DrawRect(Rect, Paints.SelectedStroke);
            else
                canvas.DrawRect(Rect, Paints.ComponentStroke);

            foreach (var input in Inputs.Values)
                canvas.DrawCircle(input.node, 8, Paints.IOPaint);

            foreach (var output in Outputs.Values)
                canvas.DrawCircle(output.node, 8, Paints.IOPaint);

            font.Size = 14;
            const string label = "IF";
            var textX = Rect.MidX - (font.MeasureText(label, Paints.TextPaint) / 2);
            var textY = Rect.MidY + font.Size / 4;
            canvas.DrawText(label, textX, textY, font, Paints.TextPaint);
        }

        public override void CreateRect(int x, int y)
        {
            Rect = new SKRect(x - 55, y - 30, x + 55, y + 30);

            // 1 input (top center)
            var inKey = Inputs.Keys.First();
            Inputs[inKey].node = new SKPoint(Rect.MidX, Rect.Top);

            // 2 outputs (bottom left/right)
            var outKeys = Outputs.Keys.ToArray();
            Outputs[outKeys[0]].node = new SKPoint(Rect.Left + Rect.Width * 0.33f, Rect.Bottom);
            Outputs[outKeys[1]].node = new SKPoint(Rect.Left + Rect.Width * 0.66f, Rect.Bottom);
        }

        public override void GenerateCode(ValueRegistry valueRegistry)
        {
            // Control-flow not supported in current linear parser; emit a comment so build/run still works.
            var cond = Inputs.Values.First();
            Code = $"// IF ({cond.name}) => then/else branching not implemented in parser\\n";
        }
    }
}