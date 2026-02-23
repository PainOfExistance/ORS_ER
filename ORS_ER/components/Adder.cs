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
    class Adder : Component
    {
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Operator);
        public Adder(Component component) : base(component)
        {
            this.Code = component.Name.Split(" ")[0];
            Font = new SKFont();
            this.Value = ("bool", new bool[2] { false, false });
            IO newNode1 = new IO();
            IO newNode2 = new IO();
            IO newNode3 = new IO();
            IO newNode4 = new IO();
            newNode4.IfTrue = "0";
            IO newNode5 = new IO();
            newNode5.IfTrue = "1";
            Inputs.Add(newNode1.GetId(), newNode1);
            Inputs.Add(newNode2.GetId(), newNode2);

            if (this.Name.Contains("Full"))
                Inputs.Add(newNode3.GetId(), newNode3);

            Outputs.Add(newNode4.GetId(), newNode4);
            Outputs.Add(newNode5.GetId(), newNode5);
        }

        public Adder(string name, string description, string category) : base(name, description, category)
        {
            this.Code = name.Split(" ")[0];
            Font = new SKFont();
            this.Value = ("bool", new bool[2] { false, false });
            IO newNode1 = new IO();
            IO newNode2 = new IO();
            IO newNode3 = new IO();
            IO newNode4 = new IO();
            newNode4.IfTrue = "0";
            IO newNode5 = new IO();
            newNode5.IfTrue = "1";
            Inputs.Add(newNode1.GetId(), newNode1);
            Inputs.Add(newNode2.GetId(), newNode2);

            if (this.Name.Contains("Full"))
                Inputs.Add(newNode3.GetId(), newNode3);

            Outputs.Add(newNode4.GetId(), newNode4);
            Outputs.Add(newNode5.GetId(), newNode5);
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
                if (this.Value.Item2[int.Parse(output.Value.IfTrue)])
                    canvas.DrawCircle(output.Value.Node, 8, Paints.IOPaintActive);
                if (!this.Value.Item2[int.Parse(output.Value.IfTrue)])
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
            this.Rect = new SKRect(x - 60, y - 45, x + 60, y + 45);
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
            if (this.Name.Contains("Full"))
            {
                bool xor1 = vals[1] ^ vals[2];
                bool and1 = vals[1] && vals[2];
                bool and2 = vals[0] && xor1;
                bool xor2 = vals[0] ^ xor1;
                bool xor3 = and1 ^ and2;
                this.Value = ("bool", new bool[2] { xor3, xor2 });
            }
            if (!this.Name.Contains("Full"))
            {
                bool XorVal = vals[0] ^ vals[1];
                bool AndVal = vals[0] && vals[1];
                this.Value = ("bool", new bool[2] { AndVal, XorVal });
            }
        }
    }
}
