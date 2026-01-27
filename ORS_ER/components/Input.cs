using SkiaSharp;

namespace ORS_ER.components
{
    class Input : Component
    {
        private readonly SKPaint componentFill = new()
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.DarkOliveGreen,
            IsAntialias = true,
        };

        private readonly SKPaint componentStroke = new()
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.LightSeaGreen,
            StrokeWidth = 2,
            IsAntialias = true,
        };

        private readonly SKPaint textPaint = new()
        {
            Color = SKColors.WhiteSmoke,
            IsAntialias = true,
            TextSize = 20,
        };

        private readonly SKPaint selectedStroke = new()
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Red,
            StrokeWidth = 4,
            IsAntialias = true,
        };

        private SKPoint portPoint = new();


        private readonly SKPaint portPaint = new()
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.Black,
            IsAntialias = true,
        };

        public Input(Component component) : base(component)
        {
            Outputs.Add(new IO("neke", 2));
        }

        public Input(string name, string description, string category) : base(name, description, category)
        {
            Outputs.Add(new IO("neke", 2));
        }

        public override void Paint(SKCanvas canvas)
        {
            canvas.DrawRect(this.Rect, componentFill);

            if (this.Selected)
                canvas.DrawRect(this.Rect, selectedStroke);
            else
                canvas.DrawRect(this.Rect, componentStroke);

            canvas.DrawCircle(this.portPoint, 8, portPaint);

            canvas.DrawText(this.Name, this.Rect.MidX - (textPaint.MeasureText(this.Name) / 2), this.Rect.MidY + (textPaint.TextSize / 2), textPaint);
        }

        public override void CreateRect(int x, int y)
        {
            this.Rect = new SkiaSharp.SKRect(x - 75, y - 25, x + 75, y + 25);
            this.portPoint = new SKPoint(this.Rect.MidX, this.Rect.Bottom);
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }
}
