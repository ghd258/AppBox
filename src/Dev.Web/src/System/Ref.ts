export class Ref<T> {
    public constructor(getter: () => T, setter: (T) => void) {
        this._getter = getter;
        this._setter = setter;
    }

    private readonly _getter: () => T;
    private readonly _setter: (T) => void;
    
    public get Value() {
        return this._getter();
    }
    
    public set Value(v) {
        this._setter(v);
    }
}