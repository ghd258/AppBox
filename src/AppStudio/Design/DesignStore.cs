using System.Collections.Generic;
using PixUI;

namespace AppBoxDesign
{
    /// <summary>
    /// 设计时全局状态
    /// </summary>
    public static class DesignStore
    {
        /// <summary>
        /// 当前选择的侧边栏
        /// </summary>
        public static readonly State<SidePadType> ActiveSidePad = SidePadType.DesignTree;

        /// <summary>
        /// 模型树控制器
        /// </summary>
        internal static readonly TreeController<DesignNode> TreeController =
            new TreeController<DesignNode>(DesignTreePad.BuildTreeNode, n => n.Children!);

        /// <summary>
        /// 设计器控制器
        /// </summary>
        internal static readonly TabController<DesignNode> DesignerController =
            new TabController<DesignNode>(new List<DesignNode>());

        private static DataGridController<CodeProblem>? _problemsController;

        /// <summary>
        /// 问题列表控制器
        /// </summary>
        internal static DataGridController<CodeProblem> ProblemsController
        {
            get
            {
                //延迟实始化 for web
                _problemsController ??= new DataGridController<CodeProblem>(
                    new List<DataGridColumn<CodeProblem>>()
                    {
                        //new DataGridTextColumn<CodeProblem>("Model", p => p.Model, ColumnWidth.Fixed(150)),
                        new DataGridTextColumn<CodeProblem>("Position", p => p.Position,
                            ColumnWidth.Fixed(180)),
                        new DataGridTextColumn<CodeProblem>("Message", p => p.Message),
                    });
                return _problemsController;
            }
        }

        /// <summary>
        /// 新建成功返回后刷新模型根节点或添加新建的节点
        /// </summary>
        internal static void OnNewNode(NewNodeResult result)
        {
            //TODO:result.RootNodeId !=null 重新刷新模型根节点，因为可能被其他开发者改变过目录结构

            var parentNode = TreeController.FindNode(n =>
                n.Type == result.ParentNodeType && n.Id == result.ParentNodeId)!;
            var newNode = TreeController.InsertNode(result.NewNode, parentNode, result.InsertIndex);
            TreeController.ExpandTo(newNode);
            TreeController.SelectNode(newNode);
        }

        internal static void UpdateProblems(ModelNode modelNode, IList<CodeProblem> problems)
        {
            //TODO:暂简单实现
            ProblemsController.DataSource = problems;
        }
    }
}