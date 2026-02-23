using Microsoft.CodeAnalysis;
using ORS_ER.connections;
using ORS_ER.windows;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static ORS_ER.connections.ValueRegistry;

namespace ORS_ER.components
{
    class Operator : Component
    {
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Operator);
        public Operator(Component component) : base(component)
        {
            this.Code = component.Code;
            IO newNode1 = new IO();
            IO newNode3 = new IO();
            Inputs.Add(newNode1.GetId(), newNode1);
            Outputs.Add(newNode3.GetId(), newNode3);
        }

        public Operator(string name, string description, string category) : base(name, description, category)
        {
            this.Code = "==";
            IO newNode1 = new IO();
            IO newNode3 = new IO();
            Inputs.Add(newNode1.GetId(), newNode1);
            Outputs.Add(newNode3.GetId(), newNode3);
        }

        public override void Paint(SKCanvas canvas)
        {
            canvas.DrawRect(this.Rect, Paints.ComponentFill);
            Font.Size = 20;

            if (this.IsBroken)
            {
                canvas.DrawRect(this.Rect, Paints.BrokenBlock);
                canvas.DrawRect(this.Rect, Paints.BrokenBlockStroke);
            }
            if (!this.IsBroken && this.Selected)
                canvas.DrawRect(this.Rect, Paints.SelectedStroke);
            if (!this.IsBroken && !this.Selected)
                canvas.DrawRect(this.Rect, Paints.ComponentStroke);

            foreach (var input in Inputs)
            {
                canvas.DrawCircle(input.Value.Node, 8, Paints.IOPaint);
            }

            foreach (var output in Outputs)
            {
                canvas.DrawCircle(output.Value.Node, 8, Paints.IOPaint);
            }

            canvas.DrawRect(this.InteractionRect, Paints.ButtonFill);
            canvas.DrawRect(this.InteractionRect, Paints.ButtonStroke);

            float textX = InteractionRect.MidX - (Font.MeasureText(Code) / 2);
            float textY = InteractionRect.MidY + Font.Size / 4;
            if (this.Code == "==")
            {
                textX = InteractionRect.MidX - (Font.MeasureText("+") / 2);
                canvas.DrawText("+", textX, textY, Font, Paints.ButtonTextPaint);
            }
            if (this.Code != "==")
            {
                string[] parts = this.Code.Split(' ');
                string displayCode = parts[1] + " = " + parts[3] + " " + parts[4] + " " + parts[5];

                while (InteractionRect.Width < (Font.MeasureText(displayCode) + 5))
                {
                    Font.Size--;
                }

                textX = InteractionRect.MidX - (Font.MeasureText(displayCode) / 2);
                canvas.DrawText(displayCode, textX, textY, Font, Paints.ButtonTextPaint);
            }

        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SkiaSharp.SKRect(x - 100, y - 50, x + 100, y + 50);
            this.InteractionRect = new SKRect(
                this.Rect.Left + (int)this.Rect.Width / 8,
                this.Rect.Top + (int)this.Rect.Height / 4,
                this.Rect.Right - (int)this.Rect.Width / 8,
                this.Rect.Bottom - (int)this.Rect.Height / 4);

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

        public override void GenerateCode()
        {
            string[] parts = this.Code.Split(' ');
            string variableName = this.Value.Item1;
            string variable1 = "";
            string variable2 = "";
            string operation = this.Value.Item2;

            variable1 = parts[3];
            if (this.Value.Item2 != "NOT")
                variable2 = parts[5];

            string key = "";
            if (this.IsInsideIf != "")
                key = this.IsInsideIf.Split('_')[0];
            if (key == "" && this.IsInsideWhile != "")
                key = this.IsInsideWhile.Split('_')[0];

            dynamic operand1 = ValueRegistry.GetLocalValue(key, variable1);
            dynamic operand2 = ValueRegistry.GetLocalValue(key, variable2);

            if (operand1 is not RegistryEntry)
            {
                if (double.TryParse(variable1, out double doubleResult))
                {
                    operand1 = doubleResult;
                }
                if (operand1 is not double && bool.TryParse(variable1, out bool boolResult))
                {
                    operand1 = boolResult;
                }
                if (operand1 is not double && operand1 is not bool)
                    operand1 = variable1.ToString();
            }
            if (operand2 is not RegistryEntry)
            {
                if (double.TryParse(variable2, out double doubleResult))
                {
                    operand2 = doubleResult;
                }
                if (operand2 is not double && bool.TryParse(variable2, out bool boolResult))
                {
                    operand2 = boolResult;
                }
                if (operand2 is not double && operand2 is not bool)
                    operand2 = variable2.ToString();
            }

            operand1 = operand1 is RegistryEntry entry1 ? entry1.Value : operand1;
            operand2 = operand2 is RegistryEntry entry2 ? entry2.Value : operand2;

            switch (operation)
            {
                case "AND":
                    this.Value = (this.Value.Item1, operand1 & operand2);
                    break;
                case "OR":
                    this.Value = (this.Value.Item1, operand1 | operand2);
                    break;
                case "NOT":
                    this.Value = (this.Value.Item1, !operand1);
                    break;
                case "XOR":
                    this.Value = (this.Value.Item1, operand1 ^ operand2);
                    break;
                case "NOR":
                    this.Value = (this.Value.Item1, !(operand1 | operand2));
                    break;
                case "XNOR":
                    this.Value = (this.Value.Item1, !(operand1 ^ operand2));
                    break;
                case "NAND":
                    this.Value = (this.Value.Item1, !(operand1 & operand2));
                    break;
                case "==":
                    this.Value = (this.Value.Item1, operand1 == operand2);
                    break;
                case "!=":
                    this.Value = (this.Value.Item1, operand1 != operand2);
                    break;
                case "<":
                    this.Value = (this.Value.Item1, operand1 < operand2);
                    break;
                case "<=":
                    this.Value = (this.Value.Item1, operand1 <= operand2);
                    break;
                case ">":
                    this.Value = (this.Value.Item1, operand1 > operand2);
                    break;
                case ">=":
                    this.Value = (this.Value.Item1, operand1 >= operand2);
                    break;
                case "+":
                    this.Value = (this.Value.Item1, operand1 + operand2);
                    break;
                case "-":
                    this.Value = (this.Value.Item1, operand1 - operand2);
                    break;
                case "*":
                    this.Value = (this.Value.Item1, operand1 * operand2);
                    break;
                case "/":
                    this.Value = (this.Value.Item1, operand1 / operand2);
                    break;
                case "%":
                    this.Value = (this.Value.Item1, operand1 % operand2);
                    break;
                case "^":
                    this.Value = (this.Value.Item1, Math.Pow(operand1, operand2));
                    break;
                default:
                    this.Value = (this.Value.Item1, operand1 | operand2);
                    break;
            }

            ValueRegistry.RegisterLocalValue(key, variableName, new ValueRegistry.RegistryEntry { BlockId = this.GetId(), Name = variableName, Value = this.Value.Item2 });
            this.Value = (this.Value.Item1, operation);
        }
    }
}
