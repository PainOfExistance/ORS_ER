using ORS_ER.connections;
using SkiaSharp;
using System.Diagnostics;
using System.Windows.Documents;
using static ORS_ER.connections.ValueRegistry;

namespace ORS_ER.components
{
    internal sealed class While : Component
    {
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.While);

        public While(Component component) : base(component)
        {
            IO newNode1 = new IO();
            IO newNode2 = new IO();
            Inputs.Add(newNode1.GetId(), newNode1);
            Inputs.Add(newNode2.GetId(), newNode2);

            IO newNode3 = new IO();
            IO newNode4 = new IO();
            newNode3.IfTrue = "True";
            newNode4.IfTrue = "False";
            Outputs.Add(newNode4.GetId(), newNode4);
            Outputs.Add(newNode3.GetId(), newNode3);
        }

        public While(string name, string description, string category) : base(name, description, category)
        {
            IO newNode1 = new IO();
            IO newNode2 = new IO();
            Inputs.Add(newNode1.GetId(), newNode1);
            Inputs.Add(newNode2.GetId(), newNode2);

            IO newNode3 = new IO();
            IO newNode4 = new IO();
            newNode3.IfTrue = "True";
            newNode4.IfTrue = "False";
            Outputs.Add(newNode4.GetId(), newNode4);
            Outputs.Add(newNode3.GetId(), newNode3);
        }

        public override void Paint(SKCanvas canvas)
        {
            canvas.Save();
            canvas.RotateDegrees(45, this.Rect.MidX, this.Rect.MidY);
            canvas.DrawRect(Rect, Paints.ComponentFill);
            if (Selected)
                canvas.DrawRect(Rect, Paints.SelectedStroke);
            else
                canvas.DrawRect(Rect, Paints.ComponentStroke);

            canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ButtonFill);
            canvas.DrawRoundRect(buttonRect, 6, 6, Paints.ButtonStroke);
            canvas.Restore();

            font.Size = 20;
            var input1 = Inputs.Values.First();
            var input2 = Inputs.Values.Last();
            canvas.DrawCircle(input1.node, 8, Paints.IOPaint);
            canvas.DrawCircle(input2.node, 8, Paints.IOPaint);

            var textXX = input1.node.X - (font.MeasureText("↑", Paints.TextPaint) / 2);
            var textYY = input1.node.Y - 10;
            canvas.DrawText("↑", textXX, textYY, font, Paints.TextPaint);

            string[] labels = { "F", "T" };
            var output1 = Outputs.Values.First();
            var output2 = Outputs.Values.Last();
            canvas.DrawCircle(output1.node, 8, Paints.IOPaint);
            canvas.DrawCircle(output2.node, 8, Paints.IOPaint);

            textXX = output1.node.X + 10;
            textYY = output1.node.Y + font.Size / 3;
            canvas.DrawText(labels[0], textXX, textYY, font, Paints.TextPaint);

            textXX = output2.node.X - 20;
            textYY = output2.node.Y + font.Size / 3;
            canvas.DrawText(labels[1], textXX, textYY, font, Paints.TextPaint);

            var textX = buttonRect.MidX - (font.MeasureText(this.Code) / 2);
            var textY = buttonRect.MidY + font.Size / 4;
            if (this.Code == "")
            {
                textX = buttonRect.MidX - (font.MeasureText("WHILE") / 2);
                canvas.DrawText("WHILE", textX, textY, font, Paints.ButtonTextPaint);
            }
            else
            {
                string[] parts = this.Code.Split(' ');
                string displayCode = "WHILE " + parts[1] + parts[2] + parts[3];

                while (buttonRect.Width < (font.MeasureText(displayCode) + 5))
                {
                    font.Size--;
                }

                textX = buttonRect.MidX - (font.MeasureText(displayCode) / 2);
                canvas.DrawText(displayCode, textX, textY, font, Paints.ButtonTextPaint);
            }
        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SkiaSharp.SKRect(x - 75, y - 75, x + 75, y + 75);
            this.buttonRect = new SKRect(
                this.Rect.Left + (int)this.Rect.Width / 6,
                this.Rect.Top + (int)this.Rect.Height / 6,
                this.Rect.Right - (int)this.Rect.Width / 6,
                this.Rect.Bottom - (int)this.Rect.Height / 6);

            SKMatrix matrix = SKMatrix.CreateRotationDegrees(45, Rect.MidX, Rect.MidY);

            Outputs.First().Value.node = matrix.MapPoint(new SKPoint(this.Rect.Left, this.Rect.Bottom));
            Outputs.Last().Value.node = matrix.MapPoint(new SKPoint(this.Rect.Right, this.Rect.Top));

            Inputs.First().Value.node = matrix.MapPoint(new SKPoint(this.Rect.Right, this.Rect.Bottom));
            Inputs.Last().Value.node = matrix.MapPoint(new SKPoint(this.Rect.Left, this.Rect.Top));
        }
        public override void GenerateCode()
        {
            string[] parts = this.Code.Split(' ');
            string var1 = parts[1];
            string var2 = parts[3];
            string op = parts[2];

            dynamic? variable1 = "";
            dynamic? variable2 = "";

            ValueRegistry.AddLocalRegistry(this.GetId());
            if (this.IsInsideIf != "")
            {
                string key = this.IsInsideIf.Split('_')[0];
                variable1 = ValueRegistry.GetLocalValue(key, var1);
                variable2 = ValueRegistry.GetLocalValue(key, var2);

                if (variable1 is not RegistryEntry)
                {
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
            }
            else if (this.IsInsideWhile != "")
            {
                string key = this.IsInsideWhile.Split('_')[0];
                variable1 = ValueRegistry.GetLocalValue(key, var1);
                variable2 = ValueRegistry.GetLocalValue(key, var2);

                if (variable1 is not RegistryEntry)
                {
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
            }
            else
            {
                variable1 = ValueRegistry.GetGlobalValue(var1);
                variable2 = ValueRegistry.GetGlobalValue(var2);

                if (variable1 is not RegistryEntry)
                {
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
            }

            variable1 = variable1 is RegistryEntry entry1 ? entry1.Value : variable1;
            variable2 = variable2 is RegistryEntry entry2 ? entry2.Value : variable2;

            switch (op)
            {
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
                default:
                    this.Value = (this.Value.Item1, variable1 == variable2);
                    break;
            }

            this.Outputs.Last().Value.IfTrue = "True";
            this.Outputs.First().Value.IfTrue = "False";

            if (this.Value.Item2)
            {
                Outputs.Values.FirstOrDefault(o => o.IfTrue == "False")?.IfTrue = "";
            }
            else
            {
                Outputs.Values.FirstOrDefault(o => o.IfTrue == "True")?.IfTrue = "";
            }
        }

        public override void Reset()
        {
            this.Outputs.Last().Value.IfTrue = "True";
            this.Outputs.First().Value.IfTrue = "False";
        }
    }
}