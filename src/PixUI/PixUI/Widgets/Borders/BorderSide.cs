namespace PixUI
{
    public enum BorderStyle
    {
        None,
        Solid,
    }

    /// <summary>
    /// A side of a border of a box.
    /// </summary>
    public readonly struct BorderSide
    {
        public static readonly BorderSide Empty = new BorderSide(Color.Empty, 0, BorderStyle.None);

        public readonly Color Color;
        public readonly float Width;
        public readonly BorderStyle Style;

        public BorderSide(Color color, float width = 1, BorderStyle style = BorderStyle.Solid)
        {
            Color = color;
            Width = width;
            Style = style;
        }

        internal void ApplyPaint(Paint paint)
        {
            paint.Style = PaintStyle.Stroke;
            paint.Color = Style == BorderStyle.Solid ? Color : Color.Empty;
            paint.StrokeWidth = Style == BorderStyle.Solid ? Width : 0;
        }

        public BorderSide Lerp(BorderSide a, BorderSide b, double t)
        {
            if (t == 0) return a;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (t == 1) return b;
            var width = FloatUtils.Lerp(a.Width, b.Width, t);
            if (width < 0f)
                return BorderSide.Empty;
            if (a.Style == b.Style)
                return new BorderSide(Color.Lerp(a.Color, b.Color, t)!.Value, width, a.Style);

            var colorA = a.Style == BorderStyle.Solid ? a.Color : a.Color.WithAlpha(0);
            var colorB = b.Style == BorderStyle.Solid ? b.Color : b.Color.WithAlpha(0);
            return new BorderSide(Color.Lerp(colorA, colorB, t)!.Value, width);
        }

        public BorderSide Clone() => new BorderSide(Color, Width, Style);
    }
}