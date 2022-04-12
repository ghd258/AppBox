import * as System from '@/System'
import * as PixUI from '@/PixUI'

export class HoverDecoration {
    public readonly Widget: PixUI.Widget;
    public readonly ShapeBuilder: System.Func<PixUI.ShapeBorder>;
    public readonly Elevation: number;

    public readonly HoverColor: Nullable<PixUI.Color>;
    // internal readonly BlendMode BlendMode;

    private _overlayEntry: Nullable<PixUI.OverlayEntry>;

    public constructor(widget: PixUI.Widget, shapeBuilder: System.Func<PixUI.ShapeBorder>, elevation: number, hoverColor: Nullable<PixUI.Color> = null) {
        this.Widget = widget;
        this.ShapeBuilder = shapeBuilder;
        this.Elevation = elevation;
        this.HoverColor = hoverColor;
    }

    public Show() {
        this._overlayEntry ??= new PixUI.OverlayEntry(new HoverDecorator(this));
        this.Widget.Overlay?.Show(this._overlayEntry);
    }

    public Hide() {
        this._overlayEntry?.Remove();
    }

    public AttachHoverChangedEvent(widget: PixUI.IMouseRegion) {
        widget.MouseRegion.HoverChanged.Add(this._OnHoverChanged, this);
    }

    private _OnHoverChanged(hover: boolean) {
        if (hover)
            this.Show();
        else
            this.Hide();
    }
}

export class HoverDecorator extends PixUI.Widget {
    private readonly _owner: HoverDecoration;
    private readonly _shape: PixUI.ShapeBorder;

    public constructor(owner: HoverDecoration) {
        super();
        this._owner = owner;
        this._shape = owner.ShapeBuilder();
    }

    public HitTest(x: number, y: number, result: PixUI.HitTestResult): boolean {
        return false; //Can't hit
    }

    public Layout(availableWidth: number, availableHeight: number) {
        //do nothing
    }

    public Paint(canvas: PixUI.Canvas, area: Nullable<PixUI.IDirtyArea> = null) {
        let widget = this._owner.Widget;
        let pt2Win = widget.LocalToWindow(0, 0);
        let bounds = PixUI.Rect.FromLTWH(pt2Win.X, pt2Win.Y, widget.W, widget.H);

        let path = this._shape.GetOuterPath(bounds);

        // draw shadow
        if (this._owner.Elevation > 0) {
            canvas.save();
            canvas.clipPath(path, CanvasKit.ClipOp.Difference, false);
            PixUI.Utils.DrawShadow(canvas, path, PixUI.Colors.Black, this._owner.Elevation, false, this.Root!.Window.ScaleFactor);
            canvas.restore();
        }

        // draw hover color
        if (this._owner.HoverColor != null) {
            canvas.save();
            canvas.clipPath(path, CanvasKit.ClipOp.Intersect, false);
            let paint = PixUI.PaintUtils.Shared(this._owner.HoverColor);
            // paint.BlendMode = _owner.BlendMode;
            canvas.drawPath(path, paint);
            canvas.restore();
        }
        path.delete();
    }

    public Init(props: Partial<HoverDecorator>): HoverDecorator {
        Object.assign(this, props);
        return this;
    }
}
