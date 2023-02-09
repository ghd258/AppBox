#if !__WEB__
using System;

namespace PixUI
{
    public enum SKPaintHinting
    {
        NoHinting = 0,
        Slight = 1,
        Normal = 2,
        Full = 3,
    }

    public unsafe class Paint : SKObject, ISKSkipObjectRegistration
    {
        // private SKFont font;
        // private bool lcdRenderText;

        private Paint(IntPtr handle, bool owns) : base(handle, owns) { }

        public Paint() : this(SkiaApi.sk_compatpaint_new(), true)
        {
            if (Handle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Unable to create a new Paint instance.");
            }
        }

        protected override void DisposeNative() => SkiaApi.sk_compatpaint_delete(Handle);

        internal static Paint? GetObject(IntPtr handle) =>
            handle == IntPtr.Zero ? null : new Paint(handle, true);

        #region ====Properties====

        public bool AntiAlias
        {
            get => SkiaApi.sk_paint_is_antialias(Handle);
            set
            {
                SkiaApi.sk_paint_set_antialias(Handle, value);
                // UpdateFontEdging (value);
            }
        }

        public bool IsStroke
        {
            get => Style != PaintStyle.Fill;
            set => Style = value ? PaintStyle.Stroke : PaintStyle.Fill;
        }

        public PaintStyle Style
        {
            get => SkiaApi.sk_paint_get_style(Handle);
            set => SkiaApi.sk_paint_set_style(Handle, value);
        }

        public Color Color
        {
            get => SkiaApi.sk_paint_get_color(Handle);
            set => SkiaApi.sk_paint_set_color(Handle, (uint)value);
        }

        public float StrokeWidth
        {
            get => SkiaApi.sk_paint_get_stroke_width(Handle);
            set => SkiaApi.sk_paint_set_stroke_width(Handle, value);
        }

        public BlendMode BlendMode
        {
            get => SkiaApi.sk_paint_get_blendmode(Handle);
            set => SkiaApi.sk_paint_set_blendmode(Handle, value);
        }

        public float StrokeMiter
        {
            get => SkiaApi.sk_paint_get_stroke_miter(Handle);
            set => SkiaApi.sk_paint_set_stroke_miter(Handle, value);
        }

        public StrokeCap StrokeCap
        {
            get => SkiaApi.sk_paint_get_stroke_cap(Handle);
            set => SkiaApi.sk_paint_set_stroke_cap(Handle, value);
        }

        public StrokeJoin StrokeJoin
        {
            get => SkiaApi.sk_paint_get_stroke_join(Handle);
            set => SkiaApi.sk_paint_set_stroke_join(Handle, value);
        }

        public SKTypeface Typeface
        {
            get => throw new NotImplementedException();
            set
            {
                /*TODO:*/
            }
        }

        public float TextSize
        {
            get => throw new NotImplementedException();
            set
            {
                /*TODO:*/
            }
        }

        public Shader? Shader
        {
            get => Shader.GetObject(SkiaApi.sk_paint_get_shader(Handle));
            set => SkiaApi.sk_paint_set_shader(Handle, value?.Handle ?? IntPtr.Zero);
        }

        public MaskFilter? MaskFilter
        {
            get => MaskFilter.GetObject(SkiaApi.sk_paint_get_maskfilter(Handle));
            set => SkiaApi.sk_paint_set_maskfilter(Handle, value?.Handle ?? IntPtr.Zero);
        }

        public ColorFilter? ColorFilter
        {
            get => ColorFilter.GetObject(SkiaApi.sk_paint_get_colorfilter(Handle));
            set => SkiaApi.sk_paint_set_colorfilter(Handle, value?.Handle ?? IntPtr.Zero);
        }

        public ImageFilter? ImageFilter
        {
            get => ImageFilter.GetObject(SkiaApi.sk_paint_get_imagefilter(Handle));
            set => SkiaApi.sk_paint_set_imagefilter(Handle, value?.Handle ?? IntPtr.Zero);
        }

        public PathEffect? PathEffect
        {
            get => PathEffect.GetObject(SkiaApi.sk_paint_get_path_effect(Handle));
            set => SkiaApi.sk_paint_set_path_effect(Handle, value?.Handle ?? IntPtr.Zero);
        }

        #endregion

        // private void UpdateFontEdging (bool antialias)
        // {
        //     var edging = SKFontEdging.Alias;
        //     if (antialias) {
        //         edging = lcdRenderText
        //             ? SKFontEdging.SubpixelAntialias
        //             : SKFontEdging.Antialias;
        //     }
        //     GetFont ().Edging = edging;
        // }
    }
}
#endif