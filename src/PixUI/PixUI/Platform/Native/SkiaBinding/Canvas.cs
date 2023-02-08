#if !__WEB__
using System;
using System.Diagnostics.CodeAnalysis;

namespace PixUI
{
    public unsafe class Canvas : SKObject
    {
        // private const int PatchCornerCount = 4;
        // private const int PatchCubicsCount = 12;
        // private const double RadiansCircle = 2.0 * Math.PI;
        // private const double DegreesCircle = 360.0;

        public readonly SKSurface Surface;

        private Canvas(SKSurface surface, IntPtr handle, bool owns) : base(handle, owns)
        {
            Surface = surface;
        }

        protected override void DisposeNative() => SkiaApi.sk_canvas_destroy(Handle);

        internal static Canvas? GetObject(SKSurface surface, IntPtr handle, bool owns = true,
            bool unrefExisting = true) =>
            GetOrAddObject(handle, owns, unrefExisting, (h, o) => new Canvas(surface, h, o));


        #region ====Draw Methods====

        public void DrawLine(float x0, float y0, float x1, float y1, Paint paint)
        {
            if (paint == null)
                throw new ArgumentNullException(nameof(paint));
            SkiaApi.sk_canvas_draw_line(Handle, x0, y0, x1, y1, paint.Handle);
        }

        public void DrawRect(Rect rect, Paint paint)
        {
            if (paint == null)
                throw new ArgumentNullException(nameof(paint));
            SkiaApi.sk_canvas_draw_rect(Handle, &rect, paint.Handle);
        }

        public void DrawRect(float x, float y, float w, float h, Paint paint)
            => DrawRect(Rect.FromLTWH(x, y, w, h), paint);

        public void DrawRRect(RRect rect, Paint paint)
        {
            if (rect == null)
                throw new ArgumentNullException(nameof(rect));
            if (paint == null)
                throw new ArgumentNullException(nameof(paint));
            SkiaApi.sk_canvas_draw_rrect(Handle, rect.Handle, paint.Handle);
        }

        // ReSharper disable once InconsistentNaming
        public void DrawDRRect(RRect outer, RRect inner, Paint paint)
        {
            if (outer == null)
                throw new ArgumentNullException(nameof(outer));
            if (inner == null)
                throw new ArgumentNullException(nameof(inner));
            if (paint == null)
                throw new ArgumentNullException(nameof(paint));

            SkiaApi.sk_canvas_draw_drrect(Handle, outer.Handle, inner.Handle, paint.Handle);
        }

        public void DrawCircle(float cx, float cy, float radius, Paint paint)
        {
            if (paint == null)
                throw new ArgumentNullException(nameof(paint));
            SkiaApi.sk_canvas_draw_circle(Handle, cx, cy, radius, paint.Handle);
        }

        public void DrawArc(Rect oval, float startAngle, float sweepAngle, bool useCenter,
            Paint paint)
        {
            if (paint == null)
                throw new ArgumentNullException(nameof(paint));

            const float toDegrees = (float)(180 / Math.PI);
            SkiaApi.sk_canvas_draw_arc(Handle, &oval, startAngle * toDegrees,
                sweepAngle * toDegrees, useCenter, paint.Handle);
        }

        public void DrawPath(Path path, Paint paint)
        {
            if (paint == null)
                throw new ArgumentNullException(nameof(paint));
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            SkiaApi.sk_canvas_draw_path(Handle, path.Handle, paint.Handle);
        }

        public void DrawParagraph(Paragraph paragraph, float x, float y)
        {
            paragraph.Paint(this, x, y);
        }

        public void DrawGlyph(ushort glyphId, float posX, float posY, float originX, float originY,
            Font font, Paint paint)
        {
            var pos = new Point(posX, posY);
            var origin = new Point(originX, originY);
            SkiaApi.sk_canvas_draw_glyph(Handle, glyphId, &pos, &origin,
                font.Handle, paint.Handle);
        }

        public void DrawImage(Image image, float x, float y, Paint? paint = null)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            SkiaApi.sk_canvas_draw_image(Handle, image.Handle, x, y,
                paint?.Handle ?? IntPtr.Zero);
        }

        public void DrawImage(Image image, Rect source, Rect dest, Paint? paint = null)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            SkiaApi.sk_canvas_draw_image_rect(Handle, image.Handle, &source, &dest,
                paint?.Handle ?? IntPtr.Zero);
        }

        public void DrawShadow(Path path, Color color, float elevation, bool transparentOccluder,
            float devicePixelRatio)
            => SkiaApi.sk_canvas_draw_shadow(Handle, path.Handle, (uint)color, elevation,
                transparentOccluder, devicePixelRatio);

        #endregion

        #region ====Clip====

        public void ClipRect(Rect rect, ClipOp op, bool antialias) =>
            SkiaApi.sk_canvas_clip_rect_with_operation(Handle, &rect, op, antialias);

        public void ClipPath(Path path, ClipOp op, bool antialias)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            SkiaApi.sk_canvas_clip_path_with_operation(Handle, path.Handle, op, antialias);
        }

        #endregion

        #region ====Matrix====

        public void Translate(float dx, float dy)
        {
            if (dx == 0 && dy == 0)
                return;

            SkiaApi.sk_canvas_translate(Handle, dx, dy);
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public void Scale(float sx, float sy)
        {
            if (sx == 1 && sy == 1)
                return;
            SkiaApi.sk_canvas_scale(Handle, sx, sy);
        }

        public void Concat( /*ref*/ Matrix4 matrix)
        {
            // fixed (Matrix4* ptr = &matrix) {
            SkiaApi.sk_canvas_concat(Handle, &matrix);
            // }
        }

        public void SetMatrix(Matrix4 matrix)
        {
            SkiaApi.sk_canvas_set_matrix(Handle, &matrix);
        }

        public void ResetMatrix() => SkiaApi.sk_canvas_reset_matrix(Handle);

        // public void Transform(Matrix4 matrix)
        // {
        //     Concat(new Matrix4(matrix.M0, matrix.M4, matrix.M8, matrix.M12,
        //         matrix.M1, matrix.M5, matrix.M9, matrix.M13,
        //         matrix.M2, matrix.M6, matrix.M10, matrix.M14,
        //         matrix.M3, matrix.M7, matrix.M11, matrix.M15));
        // }

        public Matrix3 GetTotalMatrix()
        {
            Matrix3 matrix;
            SkiaApi.sk_canvas_get_total_matrix(Handle, &matrix);
            return matrix;
        }

        #endregion

        #region ====Save & Restore====

        public int SaveCount => SkiaApi.sk_canvas_get_save_count(Handle);

        public int Save() => SkiaApi.sk_canvas_save(Handle);

        public int SaveLayer(Paint? paint = null, Rect? bounds = null)
        {
            if (bounds == null)
                return SkiaApi.sk_canvas_save_layer(Handle, null, paint?.Handle ?? IntPtr.Zero);

            var rect = bounds.Value;
            return SkiaApi.sk_canvas_save_layer(Handle, &rect, paint?.Handle ?? IntPtr.Zero);
        }

        public void Restore() => SkiaApi.sk_canvas_restore(Handle);

        #endregion

        #region ====Clear====

        public void Clear() => Clear(Color.Empty);

        public void Clear(Color color) => SkiaApi.sk_canvas_clear(Handle, (uint)color);

        #endregion
    }
}
#endif