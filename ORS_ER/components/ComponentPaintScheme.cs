using SkiaSharp;

namespace ORS_ER.components
{
    enum ComponentPaintScheme
    {
        Input,
        Print
    }

    class ComponentPaints
    {
        public SKPaint ComponentFill { get; }
        public SKPaint ComponentStroke { get; }
        public SKPaint TextPaint { get; }
        public SKPaint SelectedStroke { get; }
        public SKPaint IOPaint { get; }

        private ComponentPaints(
            SKColor fill,
            SKColor stroke,
            SKColor text,
            SKColor selected,
            SKColor io,
            float strokeWidth = 2,
            float selectedStrokeWidth = 4,
            float textSize = 20)
        {
            ComponentFill = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = fill,
                IsAntialias = true,
            };

            ComponentStroke = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = stroke,
                StrokeWidth = strokeWidth,
                IsAntialias = true,
            };

            TextPaint = new SKPaint
            {
                Color = text,
                IsAntialias = true,
                TextSize = textSize,
            };

            SelectedStroke = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = selected,
                StrokeWidth = selectedStrokeWidth,
                IsAntialias = true,
            };

            IOPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = io,
                IsAntialias = true,
            };
        }

        public static ComponentPaints Create(ComponentPaintScheme scheme) => scheme switch
        {
            ComponentPaintScheme.Input => new ComponentPaints(
                fill: SKColors.DarkOliveGreen,
                stroke: SKColors.LightSeaGreen,
                text: SKColors.WhiteSmoke,
                selected: SKColors.Red,
                io: SKColors.Black),
            ComponentPaintScheme.Print => new ComponentPaints(
                fill: SKColors.IndianRed,
                stroke: SKColors.YellowGreen,
                text: SKColors.LightCyan,
                selected: SKColors.Red,
                io: SKColors.Black),
            _ => throw new ArgumentOutOfRangeException(nameof(scheme), scheme, null)
        };
    }
}
