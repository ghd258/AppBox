import * as PixUI from '@/PixUI'

export class ToolboxPad extends PixUI.View {

    public constructor() {
        super();
        this.Child = new PixUI.Text(PixUI.State.op_Implicit_From("Toolbox"));
    }

}
