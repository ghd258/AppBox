import * as System from '@/System'
import * as PixUI from '@/PixUI'

export class MainMenu extends PixUI.Widget {
    public constructor(items: System.IEnumerable<PixUI.MenuItem>) {
        super();
        this._children = new System.List<PixUI.MenuItemWidget>();
        this._controller = new PixUI.MenuController();
        this.BuildMenuItemWidgets(items);
    }

    private readonly _children: System.IList<PixUI.MenuItemWidget>;
    private readonly _controller: PixUI.MenuController;

    public set BackgroudColor(value: PixUI.Color) {
        this._controller.BackgroundColor = (value).Clone();
    }

    public set Color(value: PixUI.Color) {
        this._controller.Color = (value).Clone();
    }

    private BuildMenuItemWidgets(items: System.IEnumerable<PixUI.MenuItem>) {
        for (const item of items) {
            let child = new PixUI.MenuItemWidget(item, 0, false, this._controller);
            child.Parent = this;
            this._children.Add(child);
        }
    }

    public VisitChildren(action: System.Func2<PixUI.Widget, boolean>) {
        for (const child of this._children) {
            if (action(child)) break;
        }
    }

    public Layout(availableWidth: number, availableHeight: number) {
        let width = this.CacheAndCheckAssignWidth(availableWidth);
        let height = this.CacheAndCheckAssignHeight(availableHeight);
        this.SetSize(width, height);

        if (this.IsLayout) return; //只布局一次，除非强制重布
        this.IsLayout = true;

        let offsetX = 0;
        for (const child of this._children) {
            child.Layout(Number.POSITIVE_INFINITY, height);
            child.SetPosition(offsetX, 0);
            offsetX += child.W;
        }
    }

    public Init(props: Partial<MainMenu>): MainMenu {
        Object.assign(this, props);
        return this;
    }
}
