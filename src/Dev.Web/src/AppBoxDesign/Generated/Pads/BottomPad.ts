import * as AppBoxDesign from '@/AppBoxDesign'
import * as System from '@/System'
import * as PixUI from '@/PixUI'

export class BottomPad extends PixUI.View {
    private readonly _tabController: PixUI.TabController<string>;

    public constructor() {
        super();
        this._tabController = new PixUI.TabController<string>(new System.List<string>().Init(
            [
                "Problems", "Usages", "Output"
            ]));

        this.Child = new PixUI.Container().Init(
            {
                Height: PixUI.State.op_Implicit_From(190),
                Child: new PixUI.TabView<string>(this._tabController, BottomPad.BuildTab, BottomPad.BuildBody, false, 40).Init(
                    {SelectedTabColor: PixUI.Colors.White, TabBarBgColor: new PixUI.Color(0xFFF3F3F3)}),
            });
    }

    private static BuildTab(title: string, isSelected: PixUI.State<boolean>): PixUI.Widget {
        let textColor = PixUI.RxComputed.Make1(isSelected, selected => selected ? PixUI.Theme.FocusedColor : PixUI.Colors.Black
        );

        return new PixUI.Text(PixUI.State.op_Implicit_From(title)).Init({TextColor: textColor});
    }

    private static BuildBody(title: string): PixUI.Widget {
        if (title == "Problems") {
            return new PixUI.DataGrid<AppBoxDesign.CodeProblem>(AppBoxDesign.DesignStore.ProblemsController);
        }

        return new PixUI.Container().Init(
            {
                Padding: PixUI.State.op_Implicit_From(PixUI.EdgeInsets.All(10)),
                BgColor: PixUI.State.op_Implicit_From(PixUI.Colors.White),
                Child: new PixUI.Text(PixUI.State.op_Implicit_From(title)),
            });
    }

    public Init(props: Partial<BottomPad>): BottomPad {
        Object.assign(this, props);
        return this;
    }
}
