using ORS_ER.connections;
using SkiaSharp;

namespace ORS_ER.components
{
    internal sealed class While : Component
    {
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.While);

        public While(Component component) : base(component)
        {
            var cond = new IO { value = false };
            Inputs.Add(cond.GetId(), cond);
            IO newNode1 = new IO();
            IO newNode2 = new IO();
            Outputs.Add(newNode1.GetId(), newNode1);
            Outputs.Add(newNode2.GetId(), newNode2);
        }

        public While(string name, string description, string category) : base(name, description, category)
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
            const string label = "WHILE";
            var textX = Rect.MidX - (font.MeasureText(label, Paints.TextPaint) / 2);
            var textY = Rect.MidY + font.Size / 4;
            canvas.DrawText(label, textX, textY, font, Paints.TextPaint);
        }

        public override void CreateRect(int x, int y)
        {
            Rect = new SKRect(x - 65, y - 30, x + 65, y + 30);

            var inKey = Inputs.Keys.First();
            Inputs[inKey].node = new SKPoint(Rect.MidX, Rect.Top);

            var outKey = Outputs.Keys.First();
            Outputs[outKey].node = new SKPoint(Rect.MidX, Rect.Bottom);
        }

        public override void GenerateCode()
        {
            var cond = Inputs.Values.First();
            Code = $"// WHILE ({cond.name}) => looping not implemented in parser\\n";
        }
    }
}