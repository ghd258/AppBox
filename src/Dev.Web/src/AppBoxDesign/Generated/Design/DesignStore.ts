import * as System from '@/System'
import * as AppBoxDesign from '@/AppBoxDesign'
import * as PixUI from '@/PixUI'
/// <summary>
/// 设计时全局状态
/// </summary>
export class DesignStore {
    public static readonly ActiveSidePad: PixUI.State<AppBoxDesign.SidePadType> = PixUI.State.op_Implicit_From(AppBoxDesign.SidePadType.DesignTree);

    public static readonly TreeController: PixUI.TreeController<AppBoxDesign.DesignNode> = new PixUI.TreeController<AppBoxDesign.DesignNode>(AppBoxDesign.DesignTreePad.BuildTreeNode, n => n.Children!);

    public static readonly DesignerController: PixUI.TabController<AppBoxDesign.DesignNode> = new PixUI.TabController<AppBoxDesign.DesignNode>(new System.List<AppBoxDesign.DesignNode>());

    public static OnNewNode(result: AppBoxDesign.NewNodeResult) {
        //TODO:
    }
}
