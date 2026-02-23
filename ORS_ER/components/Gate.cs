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
            Font = new SKFont();
            this.Value = ("bool", false);
            IO newNode1 = new IO();
            IO newNode2 = new IO();
            IO newNode3 = new IO();
            Inputs.Add(newNode1.GetId(), newNode1);
            var isNotGate = this.Code == "NOT";
            if (!isNotGate)
                Inputs.Add(newNode2.GetId(), newNode2);
            if (isNotGate)
                this.Value = ("bool", true);
            Outputs.Add(newNode3.GetId(), newNode3);
        }

        public Gate(string name, string description, string category) : base(name, description, category)
        {
            this.Code = name.Split(" ")[0];
            Font = new SKFont();
            this.Value = ("bool", false);
            IO newNode1 = new IO();
            IO newNode2 = new IO();
            IO newNode3 = new IO();
            Inputs.Add(newNode1.GetId(), newNode1);
            var isNotGate = this.Code == "NOT";
            if (!isNotGate)
                Inputs.Add(newNode2.GetId(), newNode2);
            if (isNotGate)
                this.Value = ("bool", true);
            Outputs.Add(newNode3.GetId(), newNode3);
        }

        public override void Paint(SKCanvas canvas)
        {
            canvas.DrawRect(this.Rect, Paints.ComponentFill);
            Font.Size = 20;

            if (this.Selected)
                canvas.DrawRect(this.Rect, Paints.SelectedStroke);
            if (!this.Selected)
                canvas.DrawRect(this.Rect, Paints.ComponentStroke);

            foreach (var input in this.Inputs)
            {
                canvas.DrawCircle(input.Value.Node, 8, Paints.IOPaint);
            }

            foreach (var output in this.Outputs)
            {
                if (this.Value.Item2)
                    canvas.DrawCircle(output.Value.Node, 8, Paints.IOPaintActive);
                if (!this.Value.Item2)
                    canvas.DrawCircle(output.Value.Node, 8, Paints.IOPaint);
            }

            canvas.DrawRoundRect(InteractionRect, 6, 6, Paints.ButtonFill);
            canvas.DrawRoundRect(InteractionRect, 6, 6, Paints.ButtonStroke);

            float textX = Rect.MidX - (Font.MeasureText(this.Code) / 2);
            float textY = Rect.MidY + Font.Size / 3;
            canvas.DrawText(this.Code, textX, textY, Font, Paints.ButtonTextPaint);
        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SKRect(x - 45, y - 45, x + 45, y + 45);
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

            delta = Rect.Width / (Inputs.Count + 1);
            keys = Inputs.Keys.ToArray();
            for (int i = 0; i < Inputs.Count; i++)
            {
                Inputs[keys[i]].Node = new SKPoint(this.Rect.Left + delta * (i + 1), this.Rect.Top);
            }
        }

        public override void RunInternalSimulation(List<bool> vals)
        {
            bool val1 = vals.First();
            bool val2 = vals.Last();
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
