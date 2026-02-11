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
            font.Size = 20;

            if (this.Selected)
                canvas.DrawRect(this.Rect, Paints.SelectedStroke);
            else
                canvas.DrawRect(this.Rect, Paints.ComponentStroke);

            foreach (var input in Inputs)
            {
                canvas.DrawCircle(input.Value.node, 8, Paints.IOPaint);
            }

            foreach (var output in Outputs)
            {
                canvas.DrawCircle(output.Value.node, 8, Paints.IOPaint);
            }

            canvas.DrawRect(this.buttonRect, Paints.ButtonFill);
            canvas.DrawRect(this.buttonRect, Paints.ButtonStroke);

            float textX = buttonRect.MidX - (font.MeasureText(Code) / 2);
            float textY = buttonRect.MidY + font.Size / 4;
            canvas.DrawText(Code.Replace("dynamic", "").Replace(" ", ""), textX, textY, font, Paints.TextPaint);
        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SkiaSharp.SKRect(x - 100, y - 50, x + 100, y + 50);
            this.buttonRect = new SKRect(
                this.Rect.Left + (int)this.Rect.Width / 4,
                this.Rect.Top + (int)this.Rect.Height / 4,
                this.Rect.Right - (int)this.Rect.Width / 4,
                this.Rect.Bottom - (int)this.Rect.Height / 4);

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

        public override void GenerateCode()
        {
            string[] parts = this.Code.Split(' ');
            string var = this.Value.Item1;
            string var1 = "";
            string var2 = "";
            string op = this.Value.Item2;

            if (this.Value.Item2 == "NOT")
            {
                var1 = parts[3];
            }
            else
            {
                var1 = parts[3];
                var2 = parts[5];
            }

            dynamic variable1 = ValueRegistry.GetGlobalValue(var1);
            dynamic variable2 = ValueRegistry.GetGlobalValue(var2);

            if (variable1 is not RegistryEntry)
            {
                if (this.IsInsideIf != "")
                {
                    string key = this.IsInsideIf.Split('_')[0];
                    variable1 = ValueRegistry.GetLocalValue(key, var1);
                }
                else if (this.IsInsideWhile != "")
                {
                    string key = this.IsInsideWhile.Split('_')[0];
                    variable1 = ValueRegistry.GetLocalValue(key, var1);
                }

                if (double.TryParse(var1, out double doubleResult))
                {
                    variable1 = doubleResult;
                }
                else if (bool.TryParse(var1, out bool boolResult))
                {
                    variable1 = boolResult;
                }
                else
                {
                    variable1 = var1.ToString();
                }
            }
            else if (variable2 is not RegistryEntry)
            {
                if (this.IsInsideIf != "")
                {
                    string key = this.IsInsideIf.Split('_')[0];
                    variable2 = ValueRegistry.GetLocalValue(key, var2);
                }
                else if (this.IsInsideWhile != "")
                {
                    string key = this.IsInsideWhile.Split('_')[0];
                    variable2 = ValueRegistry.GetLocalValue(key, var2);
                }

                if (double.TryParse(var2, out double doubleResult))
                {
                    variable2 = doubleResult;
                }
                else if (bool.TryParse(var2, out bool boolResult))
                {
                    variable2 = boolResult;
                }
                else
                {
                    variable2 = var2.ToString();
                }
            }

            variable1 = variable1 is RegistryEntry entry1 ? entry1.Value : variable1;
            variable2 = variable2 is RegistryEntry entry2 ? entry2.Value : variable2;

            switch (op)
            {
                case "AND":
                    this.Value = (this.Value.Item1, variable1 & variable2);
                    break;
                case "OR":
                    this.Value = (this.Value.Item1, variable1 | variable2);
                    break;
                case "NOT":
                    this.Value = (this.Value.Item1, !variable1);
                    break;
                case "XOR":
                    this.Value = (this.Value.Item1, variable1 ^ variable2);
                    break;
                case "NOR":
                    this.Value = (this.Value.Item1, !(variable1 | variable2));
                    break;
                case "XNOR":
                    this.Value = (this.Value.Item1, !(variable1 ^ variable2));
                    break;
                case "NAND":
                    this.Value = (this.Value.Item1, !(variable1 & variable2));
                    break;
                case "==":
                    this.Value = (this.Value.Item1, variable1 == variable2);
                    break;
                case "!=":
                    this.Value = (this.Value.Item1, variable1 != variable2);
                    break;
                case "<":
                    this.Value = (this.Value.Item1, variable1 < variable2);
                    break;
                case "<=":
                    this.Value = (this.Value.Item1, variable1 <= variable2);
                    break;
                case ">":
                    this.Value = (this.Value.Item1, variable1 > variable2);
                    break;
                case ">=":
                    this.Value = (this.Value.Item1, variable1 >= variable2);
                    break;
                case "+":
                    this.Value = (this.Value.Item1, variable1 + variable2);
                    break;
                case "-":
                    this.Value = (this.Value.Item1, variable1 - variable2);
                    break;
                case "*":
                    this.Value = (this.Value.Item1, variable1 * variable2);
                    break;
                case "/":
                    this.Value = (this.Value.Item1, variable1 / variable2);
                    break;
                case "%":
                    this.Value = (this.Value.Item1, variable1 % variable2);
                    break;
                case "^":
                    this.Value = (this.Value.Item1, Math.Pow(variable1, variable2));
                    break;
                default:
                    this.Value = (this.Value.Item1, variable1 | variable2);
                    break;
            }

            if(ValueRegistry.GetGlobalValue(var) is RegistryEntry)
            {
                ValueRegistry.RegisterGlobalValue(var, new ValueRegistry.RegistryEntry { BlockId = this.GetId(), Name = var, Value = this.Value.Item2 });
            }
            else if (this.IsInsideIf != "")
            {
                string key = this.IsInsideIf.Split('_')[0];
                ValueRegistry.RegisterLocalValue(key, var, new ValueRegistry.RegistryEntry { BlockId = this.GetId(), Name = var, Value = this.Value.Item2 });
            }
            else if (this.IsInsideWhile != "")
            {
                string key = this.IsInsideWhile.Split('_')[0];
                ValueRegistry.RegisterLocalValue(key, var, new ValueRegistry.RegistryEntry { BlockId = this.GetId(), Name = var, Value = this.Value.Item2 });
            }
            else
            {
                ValueRegistry.RegisterGlobalValue(var, new ValueRegistry.RegistryEntry { BlockId = this.GetId(), Name = var, Value = this.Value.Item2 });
            }

            this.Value = (this.Value.Item1, op);
        }
    }
}
