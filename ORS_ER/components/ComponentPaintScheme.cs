using SkiaSharp;

namespace ORS_ER.components
{
    enum ComponentPaintScheme
    {
        Input,
        Print,
        Logic,
        Gate,
        Operator,
        If,
        While,
    }

    class ComponentPaints
    {
        public SKPaint ComponentFill { get; }
        public SKPaint ComponentStroke { get; }
        public SKPaint TextPaint { get; }
        public SKPaint SelectedStroke { get; }
        public SKPaint IOPaint { get; }
        public SKPaint InputIOPaint { get; }
        public SKPaint IOPaintActive { get; }
        public SKPaint LineStroke { get; }
        public SKPaint SelectedLineStroke { get; }
        public SKPaint ButtonFill { get; }
        public SKPaint ButtonStroke { get; }
        public SKPaint ButtonTextPaint { get; }
        public SKPaint ValueTrue { get; }
        public SKPaint ValueFalse { get; }
        public SKPaint BrokenBlock { get; }
        public SKPaint BrokenBlockStroke { get; }

        private ComponentPaints(
            SKColor fill,
            SKColor stroke,
            SKColor text,
            SKColor selected,
            SKColor io,
            float strokeWidth = 2,
            float selectedStrokeWidth = 4,
            float textSize = 20,
            float lineStrokeWidth = 4)
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

            InputIOPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.Gray,
                IsAntialias = true,
            };

            IOPaintActive = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.OrangeRed,
                IsAntialias = true,
            };

            LineStroke = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = io,
                StrokeWidth = lineStrokeWidth,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
            };

            SelectedLineStroke = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.OrangeRed,
                StrokeWidth = lineStrokeWidth,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
            };

            ButtonFill = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.White.WithAlpha(80),
                IsAntialias = true,
            };

            ButtonStroke = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black.WithAlpha(140),
                StrokeWidth = 2,
                IsAntialias = true,
            };

            ButtonTextPaint = new SKPaint
            {
                Color = SKColors.WhiteSmoke,
                IsAntialias = true,
                TextSize = 14,
            };

            ValueTrue = new SKPaint
            {
                Color = SKColors.Green,
                IsAntialias = true,
                TextSize = 14,
            };

            ValueFalse = new SKPaint
            {
                Color = SKColors.Red,
                IsAntialias = true,
                TextSize = 14,
            };

            BrokenBlock = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.HotPink,
                IsAntialias = true,
            };

            BrokenBlockStroke = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = lineStrokeWidth * 2,
                Color = SKColors.HotPink,
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
                text: SKColors.Black,
                selected: SKColors.Red,
                io: SKColors.Black),
            ComponentPaintScheme.Logic => new ComponentPaints(
                fill: SKColors.Teal,
                stroke: SKColors.CadetBlue,
                text: SKColors.WhiteSmoke,
                selected: SKColors.Red,
                io: SKColors.Black),
            ComponentPaintScheme.Gate => new ComponentPaints(
                fill: SKColors.Teal,
                stroke: SKColors.CadetBlue,
                text: SKColors.WhiteSmoke,
                selected: SKColors.Red,
                io: SKColors.Black),
            ComponentPaintScheme.Operator => new ComponentPaints(
                fill: SKColors.DarkGoldenrod,
                stroke: SKColors.Goldenrod,
                text: SKColors.Black,
                selected: SKColors.Red,
                io: SKColors.Black),
            ComponentPaintScheme.If => new ComponentPaints(
                fill: SKColors.SlateBlue,
                stroke: SKColors.MediumPurple,
                text: SKColors.WhiteSmoke,
                selected: SKColors.Red,
                io: SKColors.Black,
                textSize: 16),
            ComponentPaintScheme.While => new ComponentPaints(
                fill: SKColors.DarkSlateBlue,
                stroke: SKColors.SteelBlue,
                text: SKColors.WhiteSmoke,
                selected: SKColors.Red,
                io: SKColors.Black,
                textSize: 16),
            _ => throw new ArgumentOutOfRangeException(nameof(scheme), scheme, null)
        };
    }
}
