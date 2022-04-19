import * as System from '@/System'
import * as PixUI from '@/PixUI'

export class RoundedRectangleBorder extends PixUI.OutlinedBorder {
    public readonly BorderRadius: PixUI.BorderRadius;

    public constructor(side: Nullable<PixUI.BorderSide> = null, borderRadius: Nullable<PixUI.BorderRadius> = null) {
        super((side)?.Clone());
        this.BorderRadius = (borderRadius ?? PixUI.BorderRadius.Empty).Clone();
    }

    public GetOuterPath(rect: PixUI.Rect): PixUI.Path {
        let rrect = this.BorderRadius.ToRRect((rect).Clone());
        rrect.Deflate(this.Side.Width, this.Side.Width);
        let path = new CanvasKit.Path();
        path.addRRect(rrect);
        return path;
    }

    public GetInnerPath(rect: PixUI.Rect): PixUI.Path {
        let rrect = this.BorderRadius.ToRRect((rect).Clone());
        let path = new CanvasKit.Path();
        path.addRRect(rrect);
        return path;
    }

    public LerpTo(to: Nullable<PixUI.ShapeBorder>, tween: PixUI.ShapeBorder, t: number) {
        throw new System.NotImplementedException();
    }

    public Clone(): PixUI.ShapeBorder {
        throw new System.NotImplementedException();
    }

    public Paint(canvas: PixUI.Canvas, rect: PixUI.Rect) {
        if (this.Side.Style == PixUI.BorderStyle.None)
            return;

        let width = this.Side.Width;
        if (width == 0) {
            let paint = PixUI.PaintUtils.Shared();
            this.Side.ApplyPaint(paint);
            let rrect = this.BorderRadius.ToRRect((rect).Clone());
            canvas.drawRRect(rrect, paint);
        } else {
            let outer = this.BorderRadius.ToRRect((rect).Clone());
            let inner = PixUI.RRect.FromCopy(outer);
            inner.Deflate(width, width);
            let paint = PixUI.PaintUtils.Shared(this.Side.Color);
            canvas.drawDRRect(outer, inner, paint);
        }
    }
}
