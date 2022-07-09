using System;

namespace PixUI
{
    [TSNoInitializer]
    public sealed class IconPainter : IDisposable
    {
        public IconPainter(Action onFontLoaded)
        {
            _onFontLoaded = onFontLoaded;
        }

        private readonly Action _onFontLoaded;
        private Font? _cachedFont;
        private ushort _cachedGlyphId;
        private bool _loading;

        public void Paint(Canvas canvas, float size, in Color color, in IconData data,
            float offsetX = 0, float offsetY = 0)
        {
            if (_cachedFont == null)
            {
                var typeface = FontCollection.Instance.TryMatchFamilyFromAsset(data.FontFamily);
                if (typeface == null && !_loading)
                {
                    _loading = true;
                    FontCollection.Instance.StartLoadFontFromAsset(data.AssemblyName,
                        data.AssetPath, data.FontFamily);
                    FontCollection.Instance.FontChanged += _OnFontChanged;
                    return;
                }

                _cachedFont = new Font(typeface, size);
                _cachedGlyphId = _cachedFont.GetGlyphId(data.CodePoint);
            }

            var paint = PaintUtils.Shared(color);
            canvas.DrawGlyph(_cachedGlyphId, offsetX, size + offsetY, 0, 0, _cachedFont!, paint);
        }

        private void _OnFontChanged()
        {
            FontCollection.Instance.FontChanged -= _OnFontChanged;
            _onFontLoaded();
        }

        public void Reset()
        {
            _cachedFont?.Dispose();
            _cachedFont = null;
            _cachedGlyphId = 0;
            _loading = false;
        }

        public void Dispose()
        {
            _cachedFont?.Dispose();
        }
    }
}