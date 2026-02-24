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

            if (this.IsBroken)
            {
                canvas.DrawRect(this.Rect, Paints.BrokenBlock);
                canvas.DrawRect(this.Rect, Paints.BrokenBlockStroke);
            }
            if (!this.IsBroken && this.Selected)
                canvas.DrawRect(this.Rect, Paints.SelectedStroke);
            if (!this.IsBroken && !this.Selected)
                canvas.DrawRect(this.Rect, Paints.ComponentStroke);

            canvas.DrawRoundRect(InteractionRect, 6, 6, Paints.ButtonFill);
            canvas.DrawRoundRect(InteractionRect, 6, 6, Paints.ButtonStroke);
            canvas.Restore();

            Font.Size = 20;
            var input1 = Inputs.Values.First();
            var input2 = Inputs.Values.Last();
            canvas.DrawCircle(input1.Node, 8, Paints.IOPaint);
            canvas.DrawCircle(input2.Node, 8, Paints.IOPaint);

            var textXX = input1.Node.X - (Font.MeasureText("↑", Paints.TextPaint) / 2);
            var textYY = input1.Node.Y - 10;
            canvas.DrawText("↑", textXX, textYY, Font, Paints.TextPaint);

            string[] labels = { "F", "T" };
            var output1 = Outputs.Values.First();
            var output2 = Outputs.Values.Last();
            canvas.DrawCircle(output1.Node, 8, Paints.IOPaint);
            canvas.DrawCircle(output2.Node, 8, Paints.IOPaint);

            textXX = output1.Node.X + 10;
            textYY = output1.Node.Y + Font.Size / 3;
            canvas.DrawText(labels[0], textXX, textYY, Font, Paints.TextPaint);

            textXX = output2.Node.X - 20;
            textYY = output2.Node.Y + Font.Size / 3;
            canvas.DrawText(labels[1], textXX, textYY, Font, Paints.TextPaint);

            var textX = InteractionRect.MidX - (Font.MeasureText(this.Code) / 2);
            var textY = InteractionRect.MidY + Font.Size / 4;
            if (this.Code == "")
            {
                textX = InteractionRect.MidX - (Font.MeasureText("WHILE") / 2);
                canvas.DrawText("WHILE", textX, textY, Font, Paints.ButtonTextPaint);
            }
            if (this.Code != "")
            {
                string[] parts = this.Code.Split(' ');
                string displayCode = "WHILE " + parts[1] + parts[2] + parts[3];

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
            this.Rect = new SkiaSharp.SKRect(x - 75, y - 75, x + 75, y + 75);
            this.InteractionRect = new SKRect(
                this.Rect.Left + (int)this.Rect.Width / 6,
                this.Rect.Top + (int)this.Rect.Height / 6,
                this.Rect.Right - (int)this.Rect.Width / 6,
                this.Rect.Bottom - (int)this.Rect.Height / 6);

            SKMatrix matrix = SKMatrix.CreateRotationDegrees(45, Rect.MidX, Rect.MidY);

            Outputs.First().Value.Node = matrix.MapPoint(new SKPoint(this.Rect.Left, this.Rect.Bottom));
            Outputs.Last().Value.Node = matrix.MapPoint(new SKPoint(this.Rect.Right, this.Rect.Top));

            Inputs.First().Value.Node = matrix.MapPoint(new SKPoint(this.Rect.Right, this.Rect.Bottom));
            Inputs.Last().Value.Node = matrix.MapPoint(new SKPoint(this.Rect.Left, this.Rect.Top));
        }
        public override void GenerateCode()
        {
            string[] codeParts = this.Code.Split(' ');
            string leftOperandName = codeParts[1];
            string rightOperandName = codeParts[3];
            string comparisonOperator = codeParts[2];

            dynamic? leftOperandValue = "";
            dynamic? rightOperandValue = "";

            ValueRegistry.AddLocalRegistry(
                new RegistryId(this.GetId()),
                new RegistryId(this.IsInsideIf != "" ? this.IsInsideIf.Split('_')[0] : this.IsInsideWhile.Split('_')[0]));

            RegistryId key = RegistryId.Global;
            if (this.IsInsideIf != "")
                key = this.IsInsideIf.Split('_')[0];
            if (key.IsGlobal && this.IsInsideWhile != "")
                key = this.IsInsideWhile.Split('_')[0];

            leftOperandValue = ValueRegistry.GetLocalValue(key, new RegistryKey(leftOperandName));
            rightOperandValue = ValueRegistry.GetLocalValue(key, new RegistryKey(rightOperandName));

            if (leftOperandValue is not RegistryEntry)
            {
                if (double.TryParse(leftOperandName, out double doubleResult))
                {
                    leftOperandValue = doubleResult;
                }
                if (leftOperandValue is not double && bool.TryParse(leftOperandName, out bool boolResult))
                {
                    leftOperandValue = boolResult;
                }
                if (leftOperandValue is not double && leftOperandValue is not bool)
                    leftOperandValue = leftOperandName.ToString();
            }
            if (rightOperandValue is not RegistryEntry)
            {
                if (double.TryParse(rightOperandName, out double doubleResult))
                {
                    rightOperandValue = doubleResult;
                }
                if (rightOperandValue is not double && bool.TryParse(rightOperandName, out bool boolResult))
                {
                    rightOperandValue = boolResult;
                }
                if (rightOperandValue is not double && rightOperandValue is not bool)
                    rightOperandValue = rightOperandName.ToString();
            }


            leftOperandValue = leftOperandValue is RegistryEntry entry1 ? entry1.Value : leftOperandValue;
            rightOperandValue = rightOperandValue is RegistryEntry entry2 ? entry2.Value : rightOperandValue;

            switch (comparisonOperator)
            {
                case "==":
                    this.Value = (this.Value.Item1, leftOperandValue == rightOperandValue);
                    break;
                case "!=":
                    this.Value = (this.Value.Item1, leftOperandValue != rightOperandValue);
                    break;
                case "<":
                    this.Value = (this.Value.Item1, leftOperandValue < rightOperandValue);
                    break;
                case "<=":
                    this.Value = (this.Value.Item1, leftOperandValue <= rightOperandValue);
                    break;
                case ">":
                    this.Value = (this.Value.Item1, leftOperandValue > rightOperandValue);
                    break;
                case ">=":
                    this.Value = (this.Value.Item1, leftOperandValue >= rightOperandValue);
                    break;
                default:
                    this.Value = (this.Value.Item1, leftOperandValue == rightOperandValue);
                    break;
            }

            this.Outputs.Last().Value.IfTrue = "True";
            this.Outputs.First().Value.IfTrue = "False";

            if (this.Value.Item2)
            {
                Outputs.Values.FirstOrDefault(o => o.IfTrue == "False")?.IfTrue = "";
            }
            if (!this.Value.Item2)
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