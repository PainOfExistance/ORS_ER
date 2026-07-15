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
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Input);
        public BinaryInput(Component component) : base(component)
        {
            this.Code = component.Name.Split(" ")[0];
            Font = new SKFont();
            this.Value = ("bool", false);
            IO newNode = new IO();
            Outputs.Add(newNode.GetId(), newNode);
        }

        public BinaryInput(string name, string description, string category) : base(name, description, category)
        {
            this.Code = name.Split(" ")[0];
            Font = new SKFont();
            this.Value = ("bool", false);
            IO newNode = new IO();
            Outputs.Add(newNode.GetId(), newNode);
        }

        public override void Paint(SKCanvas canvas)
        {
            canvas.DrawRect(this.Rect, Paints.ComponentFill);
            Font.Size = 20;

            if (this.Selected)
                canvas.DrawRect(this.Rect, Paints.SelectedStroke);
            if (!this.Selected)
                canvas.DrawRect(this.Rect, Paints.ComponentStroke);

            foreach (var output in this.Outputs)
            {
                if (this.Value.Item2)
                    canvas.DrawCircle(output.Value.Node, 8, Paints.IOPaintActive);
                if (!this.Value.Item2)
                    canvas.DrawCircle(output.Value.Node, 8, Paints.IOPaint);
            }

            var label = "0";
            var fillPaint = Paints.ValueFalse;
            if (this.Value.Item2)
            {
                label = "1";
                fillPaint = Paints.ValueTrue;
            }
            canvas.DrawRoundRect(InteractionRect, 6, 6, fillPaint);
            canvas.DrawRoundRect(InteractionRect, 6, 6, Paints.ButtonStroke);
            // Draw center value label with white bold text so it remains visible against any fill
            try
            {
                var valuePaint = new SKPaint
                {
                    Color = SKColors.White,
                    IsAntialias = true,
                    TextSize = 14,
                    FakeBoldText = true,
                };
                float textX = Rect.MidX - (Font.MeasureText(label) / 2);
                float textY = Rect.MidY + Font.Size / 3;
                canvas.DrawText(label, textX, textY, Font, valuePaint);
            }
            catch { }

            // Draw editable label for the binary input (uses the first output's Name)
            try
            {
                var io = this.Outputs.Values.FirstOrDefault();
                var labelText = io != null && !string.IsNullOrWhiteSpace(io.Name) ? io.Name : "Label";
                var smallFont = new SKFont(Font.Typeface, 12);
                float lx = Rect.MidX - (smallFont.MeasureText(labelText) / 2);
                float ly = Rect.Top - 6;
                // Label should be red and bold
                var labelPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    IsAntialias = true,
                    TextSize = 12,
                    FakeBoldText = true
                };
                canvas.DrawText(labelText, lx, ly, smallFont, labelPaint);
            }
            catch { }
        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SKRect(x - 25, y - 25, x + 25, y + 25);
            this.InteractionRect = new SKRect(
                this.Rect.Left + 10,
                this.Rect.Top + 10,
                this.Rect.Right - 10,
                this.Rect.Bottom - 10);

            var delta = Rect.Width / (Outputs.Count + 1);
            string[] keys = Outputs.Keys.ToArray();
            for (int i = 0; i < Outputs.Count; i++)
            {
                Outputs[keys[i]].Node = new SKPoint(this.Rect.Left + delta * (i + 1), this.Rect.Bottom);
            }
        }

        public override void RunInternalSimulation(List<bool> vals)
        {
        }
    }
}