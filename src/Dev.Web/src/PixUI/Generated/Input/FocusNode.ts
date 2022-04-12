import * as System from '@/System'
import * as PixUI from '@/PixUI'

export class FocusNode {
    public readonly KeyDown = new System.Event<PixUI.KeyEvent>();
    public readonly KeyUp = new System.Event<PixUI.KeyEvent>();
    public readonly FocusChanged = new System.Event<boolean>();
    public readonly TextInput = new System.Event<string>();

    public RaiseKeyDown(theEvent: PixUI.KeyEvent) {
        this.KeyDown.Invoke(theEvent);
    }

    public RaiseKeyUp(theEvent: PixUI.KeyEvent) {
        this.KeyUp.Invoke(theEvent);
    }

    public RaiseFocusChanged(focused: boolean) {
        this.FocusChanged.Invoke(focused);
    }

    public RaiseTextInput(text: string) {
        this.TextInput.Invoke(text);
    }

    public Init(props: Partial<FocusNode>): FocusNode {
        Object.assign(this, props);
        return this;
    }
}
