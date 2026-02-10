using ORS_ER.connections;
using SkiaSharp;

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
            Outputs.Add(newNode3.GetId(), newNode2);
            Outputs.Add(newNode4.GetId(), newNode3);
        }

        public While(string name, string description, string category) : base(name, description, category)
        {
            IO newNode1 = new IO();
            IO newNode2 = new IO();
            Inputs.Add(newNode1.GetId(), newNode1);
            Inputs.Add(newNode2.GetId(), newNode2);

            IO newNode3 = new IO();
            IO newNode4 = new IO();
            Outputs.Add(newNode3.GetId(), newNode2);
            Outputs.Add(newNode4.GetId(), newNode3);
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

            font.Size = 20;
            const string label = "While";
            var textX = Rect.MidX - (font.MeasureText(label, Paints.TextPaint) / 2);
            var textY = Rect.MidY + font.Size / 4;
            canvas.DrawText(label, textX, textY, font, Paints.TextPaint);
            canvas.RotateDegrees(45, Rect.MidX, Rect.MidY);
        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SkiaSharp.SKRect(x - 100, y - 15, x + 100, y + 50);
            this.buttonRect = new SKRect(
                this.Rect.Left + (int)this.Rect.Width / 4,
                this.Rect.Top + (int)this.Rect.Height / 4,
                this.Rect.Right - (int)this.Rect.Width / 4,
                this.Rect.Bottom - (int)this.Rect.Height / 4);

            var delta = Rect.Width / (Inputs.Count + 1);
            string[] keys = Inputs.Keys.ToArray();
            for (int i = 0; i < Inputs.Count; i++)
            {
                Inputs[keys[i]].node = new SKPoint(this.Rect.Left + delta * (i + 1), this.Rect.Top);
            }

            delta = Rect.Width / (Outputs.Count + 1);
            keys = Outputs.Keys.ToArray();
            for (int i = 0; i < Outputs.Count; i++)
            {
                Outputs[keys[i]].node = new SKPoint(this.Rect.Left + delta * (i + 1), this.Rect.Bottom);
            }
        }
        public override void GenerateCode()
        {
            string[] parts = this.Code.Split(' ');
            string var1 = parts[1];
            string var2 = parts[3];
            string op = parts[2];

            dynamic variable1 = "";
            dynamic variable2 = "";

            ValueRegistry.AddLocalRegistry(this.GetId());
            if (this.IsInsideIf != "")
            {
                string key = this.IsInsideIf.Split('_')[0];
                variable1 = ValueRegistry.GetLocalValue(key, var1);
                variable2 = ValueRegistry.GetLocalValue(key, var2);

                if (variable1 == null)
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
                else if (variable2 == null)
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

                if (variable1 == null)
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
                else if (variable2 == null)
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

                if (variable1 == null)
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
                else if (variable2 == null)
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

            if (this.Value.Item2)
            {
                Outputs.Values.FirstOrDefault(o => o.IfTrue == "False")?.IfTrue = "";
            }
            else
            {
                Outputs.Values.FirstOrDefault(o => o.IfTrue == "True")?.IfTrue = "";
            }
        }
    }
}