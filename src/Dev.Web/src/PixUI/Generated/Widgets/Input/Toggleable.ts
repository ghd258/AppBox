import * as PixUI from '@/PixUI'

export abstract class Toggleable extends PixUI.Widget implements PixUI.IMouseRegion {
    private static readonly $meta_PixUI_IMouseRegion = true;

    protected constructor() {
        super();
        this.MouseRegion = new PixUI.MouseRegion(() => PixUI.Cursors.Hand);
        this.MouseRegion.PointerTap.Add(this.OnTap, this);
    }

    protected _value: PixUI.State<Nullable<boolean>>;
    private _triState: boolean = false;
    protected _positionController: PixUI.AnimationController;
    #MouseRegion: PixUI.MouseRegion;
    public get MouseRegion() {
        return this.#MouseRegion;
    }

    private set MouseRegion(value) {
        this.#MouseRegion = value;
    }

    protected InitState(value: PixUI.State<Nullable<boolean>>, tristate: boolean) {
        this._triState = tristate;
        this._value = this.Bind(value, PixUI.BindingOptions.AffectsVisual);
        this._positionController =
            new PixUI.AnimationController(100, value.Value != null && value.Value ? 1 : 0);
        this._positionController.ValueChanged.Add(this.OnPositionValueChanged, this);
    }

    private OnTap(e: PixUI.PointerEvent) {
        //TODO: skip on readonly
        //TODO: 考虑只切换true与false，中间状态只能程序改变，目前true->null->false循环

        if (this._value.Value == null)
            this._value.Value = false;
        else if (this._value.Value == true)
            this._value.Value = this._triState ? null : false;
        else
            this._value.Value = true;
    }

    private AnimateToValue() {
        if (this._triState) {
            if (this._value.Value == null)
                this._positionController.SetValue(0);
            if (this._value.Value == null || this._value.Value == true)
                this._positionController.Forward();
            else
                this._positionController.Reverse();
        } else {
            if (this._value.Value != null && this._value.Value == true)
                this._positionController.Forward();
            else
                this._positionController.Reverse();
        }
    }

    private OnPositionValueChanged() {
        this.Invalidate(PixUI.InvalidAction.Repaint);
    }

    public OnStateChanged(state: PixUI.StateBase, options: PixUI.BindingOptions) {
        if ((state === this._value)) {
            this.AnimateToValue();
            return;
        }

        super.OnStateChanged(state, options);
    }
}
