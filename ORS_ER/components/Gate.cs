using Microsoft.CodeAnalysis;
using ORS_ER.connections;
using ORS_ER.windows;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using static ORS_ER.connections.ValueRegistry;

namespace ORS_ER.components
{
    class Gate : Component
    {
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Gate);
        public Gate(Component component) : base(component)
        {
            this.Code = component.Name.Split(" ")[0];
            base.font = new SKFont();
            this.Value = ("bool", false);
            IO newNode1 = new IO();
            IO newNode2 = new IO();
            IO newNode3 = new IO();
            Inputs.Add(newNode1.GetId(), newNode1);
            if (this.Code != "NOT")
                Inputs.Add(newNode2.GetId(), newNode2);
            else
                this.Value = ("bool", true);
            Outputs.Add(newNode3.GetId(), newNode3);
        }

        public Gate(string name, string description, string category) : base(name, description, category)
        {
            this.Code = name.Split(" ")[0];
            base.font = new SKFont();
            this.Value = ("bool", false);
            IO newNode1 = new IO();
            IO newNode2 = new IO();
            IO newNode3 = new IO();
            Inputs.Add(newNode1.GetId(), newNode1);
            if (this.Code != "NOT")
                Inputs.Add(newNode2.GetId(), newNode2);
            else
                this.Value = ("bool", true);
            Outputs.Add(newNode3.GetId(), newNode3);
        }

        public override void Paint(SKCanvas canvas)
        {
            canvas.DrawRect(this.Rect, Paints.ComponentFill);
            font.Size = 20;

            if (this.Selected)
                canvas.DrawRect(this.Rect, Paints.SelectedStroke);
            else
                canvas.DrawRect(this.Rect, Paints.ComponentStroke);

            foreach (var input in this.Inputs)
            {
                canvas.DrawCircle(input.Value.node, 8, Paints.IOPaint);
            }

            foreach (var output in this.Outputs)
            {
                if (this.Value.Item2)
                    canvas.DrawCircle(output.Value.node, 8, Paints.IOPaintActive);
                else
                    canvas.DrawCircle(output.Value.node, 8, Paints.IOPaint);
            }

            canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ButtonFill);
            canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ButtonStroke);

            float textX = Rect.MidX - (font.MeasureText(this.Code) / 2);
            float textY = Rect.MidY + font.Size / 3;
            canvas.DrawText(this.Code, textX, textY, font, Paints.ButtonTextPaint);
        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SKRect(x - 35, y - 35, x + 35, y + 35);
            this.buttonRect = new SKRect(
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

            delta = Rect.Width / (Inputs.Count + 1);
            keys = Inputs.Keys.ToArray();
            for (int i = 0; i < Inputs.Count; i++)
            {
                Inputs[keys[i]].node = new SKPoint(this.Rect.Left + delta * (i + 1), this.Rect.Top);
            }
        }

        public override void GenerateCode(bool val1, bool val2)
        {
            switch (this.Code)
            {
                case "AND":
                    this.Value = (this.Value.Item1, val1 & val2);
                    break;
                case "OR":
                    this.Value = (this.Value.Item1, val1 | val2);
                    break;
                case "NOT":
                    this.Value = (this.Value.Item1, !val1);
                    break;
                case "XOR":
                    this.Value = (this.Value.Item1, val1 ^ val2);
                    break;
                case "NOR":
                    this.Value = (this.Value.Item1, !(val1 | val2));
                    break;
                case "XNOR":
                    this.Value = (this.Value.Item1, !(val1 ^ val2));
                    break;
                case "NAND":
                    this.Value = (this.Value.Item1, !(val1 & val2));
                    break;
                default:
                    this.Value = (this.Value.Item1, val1 | val2);
                    break;
            }
        }
    }
}
