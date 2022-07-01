#if !__WEB__
using System;
using System.Globalization;

namespace PixUI
{
    public readonly struct Color : IEquatable<Color>
    {
        public static readonly Color Empty;

        private readonly uint color;

        public Color(uint value)
        {
            color = value;
        }

        public Color(byte red, byte green, byte blue, byte alpha)
        {
            color = (uint)((alpha << 24) | (red << 16) | (green << 8) | blue);
        }

        public Color(byte red, byte green, byte blue)
        {
            color = (0xff000000u | (uint)(red << 16) | (uint)(green << 8) | blue);
        }

        public Color WithRed(byte red) => new Color(red, Green, Blue, Alpha);

        public Color WithGreen(byte green) => new Color(Red, green, Blue, Alpha);

        public Color WithBlue(byte blue) => new Color(Red, Green, blue, Alpha);

        public Color WithAlpha(byte alpha) => new Color(Red, Green, Blue, alpha);

        public byte Alpha => (byte)((color >> 24) & 0xff);
        public byte Red => (byte)((color >> 16) & 0xff);
        public byte Green => (byte)((color >> 8) & 0xff);
        public byte Blue => (byte)((color) & 0xff);

        public bool IsOpaque => Alpha == 0xFF;

        // public readonly float Hue
        // {
        //     get
        //     {
        //         ToHsv(out var h, out _, out _);
        //         return h;
        //     }
        // }

        // public static SKColor FromHsl (float h, float s, float l, byte a = 255)
        // {
        // 	var colorf = SKColorF.FromHsl (h, s, l);
        //
        // 	// RGB results from 0 to 255
        // 	var r = colorf.Red * 255f;
        // 	var g = colorf.Green * 255f;
        // 	var b = colorf.Blue * 255f;
        //
        // 	return new SKColor ((byte)r, (byte)g, (byte)b, a);
        // }
        //
        // public static SKColor FromHsv (float h, float s, float v, byte a = 255)
        // {
        // 	var colorf = SKColorF.FromHsv (h, s, v);
        //
        // 	// RGB results from 0 to 255
        // 	var r = colorf.Red * 255f;
        // 	var g = colorf.Green * 255f;
        // 	var b = colorf.Blue * 255f;
        //
        // 	return new SKColor ((byte)r, (byte)g, (byte)b, a);
        // }

        // public readonly void ToHsl (out float h, out float s, out float l)
        // {
        // 	// RGB from 0 to 255
        // 	var r = Red / 255f;
        // 	var g = Green / 255f;
        // 	var b = Blue / 255f;
        //
        // 	var colorf = new SKColorF (r, g, b);
        // 	colorf.ToHsl (out h, out s, out l);
        // }

        // public readonly void ToHsv (out float h, out float s, out float v)
        // {
        // 	// RGB from 0 to 255
        // 	var r = Red / 255f;
        // 	var g = Green / 255f;
        // 	var b = Blue / 255f;
        //
        // 	var colorf = new SKColorF (r, g, b);
        // 	colorf.ToHsv (out h, out s, out v);
        // }

        public readonly override string ToString() => $"#{Alpha:x2}{Red:x2}{Green:x2}{Blue:x2}";

        public readonly bool Equals(Color obj) => obj.color == color;

        public readonly override bool Equals(object other) => other is Color f && Equals(f);

        public static bool operator ==(Color left, Color right) => left.Equals(right);

        public static bool operator !=(Color left, Color right) => !left.Equals(right);

        public readonly override int GetHashCode() => color.GetHashCode();

        public static implicit operator Color(uint color) => new Color(color);

        public static explicit operator uint(Color color) => color.color;

        public static Color? Lerp(Color? a, Color? b, double t) => ColorUtils.Lerp(a, b, t);

        public static Color Parse(string hexString)
        {
            if (!TryParse(hexString, out var color))
                throw new ArgumentException("Invalid hexadecimal color string.", nameof(hexString));
            return color;
        }

        public static bool TryParse(string hexString, out Color color)
        {
            if (string.IsNullOrWhiteSpace(hexString))
            {
                // error
                color = Color.Empty;
                return false;
            }

            // clean up string
            hexString = hexString.Trim().ToUpperInvariant();
            if (hexString[0] == '#')
                hexString = hexString.Substring(1);

            var len = hexString.Length;
            if (len == 3 || len == 4)
            {
                byte a;
                // parse [A]
                if (len == 4)
                {
                    if (!byte.TryParse(string.Concat(hexString[len - 4], hexString[len - 4]),
                            NumberStyles.HexNumber,
                            CultureInfo.InvariantCulture, out a))
                    {
                        // error
                        color = Color.Empty;
                        return false;
                    }
                }
                else
                {
                    a = 255;
                }

                // parse RGB
                if (!byte.TryParse(string.Concat(hexString[len - 3], hexString[len - 3]),
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture, out var r) ||
                    !byte.TryParse(string.Concat(hexString[len - 2], hexString[len - 2]),
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture, out var g) ||
                    !byte.TryParse(string.Concat(hexString[len - 1], hexString[len - 1]),
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture, out var b))
                {
                    // error
                    color = Color.Empty;
                    return false;
                }

                // success
                color = new Color(r, g, b, a);
                return true;
            }

            if (len == 6 || len == 8)
            {
                // parse [AA]RRGGBB
                if (!uint.TryParse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                        out var number))
                {
                    // error
                    color = Color.Empty;
                    return false;
                }

                // success
                color = (Color)number;

                // alpha was not provided, so use 255
                if (len == 6)
                {
                    color = color.WithAlpha(255);
                }

                return true;
            }

            // error
            color = Color.Empty;
            return false;
        }
    }
}
#endif