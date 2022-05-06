import * as System from '@/System'
import * as PixUI from '@/PixUI'

export class Center extends PixUI.SingleChildWidget {
    public Layout(availableWidth: number, availableHeight: number) {
        let width = this.CacheAndCheckAssignWidth(availableWidth);
        let height = this.CacheAndCheckAssignHeight(availableHeight);

        if (this.Child != null) {
            this.Child.Layout(width, height);
            this.Child.SetPosition((width - this.Child.W) / 2, (height - this.Child.H) / 2);
        }

        this.SetSize(width, height);
    }

    public Init(props: Partial<Center>): Center {
        Object.assign(this, props);
        return this;
    }
}