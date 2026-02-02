using ORS_ER.connections;
using ORS_ER.windows;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

namespace ORS_ER.components
{
    class BinaryInput : Component
    {
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.BinaryInput);
        SKRect valueRect { get; set; }
        public BinaryInput(Component component) : base(component)
        {
            base.font = new SKFont();
            IO newNode = new IO();
            newNode.name = component.Name;
            newNode.value = false;
            Outputs.Add(newNode.GetId(), newNode);
        }

        public BinaryInput(string name, string description, string category) : base(name, description, category)
        {
            base.font = new SKFont();
            IO newNode = new IO();
            newNode.name = name;
            newNode.value = false;
            Outputs.Add(newNode.GetId(), newNode);
        }

        public override void Paint(SKCanvas canvas)
        {
            canvas.DrawRect(this.Rect, Paints.ComponentFill);
            font.Size = 20;

            if (this.Selected)
                canvas.DrawRect(this.Rect, Paints.SelectedStroke);
            else
                canvas.DrawRect(this.Rect, Paints.ComponentStroke);

            foreach (var output in this.Outputs)
            {
                canvas.DrawCircle(output.Value.node, 8, Paints.IOPaint);
            }

            string label;
            if ((bool)this.Outputs.First().Value.value == false)
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

            var delta = Rect.Width / (Outputs.Count + 1);
            string[] keys = Outputs.Keys.ToArray();
            for (int i = 0; i < Outputs.Count; i++)
            {
                Outputs[keys[i]].node = new SKPoint(this.Rect.Left + delta * (i + 1), this.Rect.Bottom);
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

        public override (string, Component, IO?)? HitTest(SKPoint world)
        {
            (string, Component, IO?)? baseReturn = base.HitTest(world);
            if (this.valueRect.Contains(world))
            {
                this.Outputs.First().Value.value = !this.Outputs.First().Value.value;
                return ("button", this, null);
            }
            return baseReturn;
        }

        public override void GenerateCode()
        {
            string s = (bool)this.Outputs.First().Value.value ? "true" : "false";
            this.Code = $"dynamic {this.Outputs.First().Value.name} = {s};\n";
        }
    }
}
