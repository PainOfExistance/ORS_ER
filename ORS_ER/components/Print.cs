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

        public Print(string name, string description, string category) : base(name, description, category)
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

            canvas.DrawText($"{this.Name} {this.Index}", this.Rect.MidX - (Paints.TextPaint.MeasureText(this.Name) / 2), this.Rect.MidY + (Paints.TextPaint.TextSize / 2), Paints.TextPaint);
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

        public override string ToJson()
        {
            string inputJsons = "\"inputs\": [";
            foreach (var input in this.Inputs)
            {
                inputJsons += input.Value.ToJson();
            }
            inputJsons += "]";

            string outputJsons = "";
            foreach (var output in this.Outputs)
            {
                outputJsons += output.Value.ToJson();
            }

            return $"\"{this.Name}\": {{\n" +
                $"\"id\": \"{this.GetId()}\",\n" +
                $"\"x\": {this.Rect.MidX},\n" +
                $"\"y\": {this.Rect.MidY},\n" +
                $"\"code\": \"{this.Code}\",\n" +
                $"\"description\": \"{this.Description}\",\n" +
                $"\"category\": \"{this.Category}\",\n" +
                $"{inputJsons},\n" +
                $"{outputJsons},\n" +
                $"\"index\": \"{this.Index}\"\n" +
                $"}}\n";
        }
    }
}
