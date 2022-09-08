using AppBoxClient;
using PixUI;

namespace AppBoxDesign
{
    internal sealed class DesignTreePad : View
    {
        private readonly State<string> _searchKey = "";
        private bool _hasLoadTree = false;

        public DesignTreePad()
        {
            Child = new Column
            {
                Children = new Widget[]
                {
                    new Input(_searchKey) { Prefix = new Icon(Icons.Filled.Search) },
                    new TreeView<DesignNodeVO>(DesignStore.TreeController),
                }
            };
        }

        internal static void BuildTreeNode(DesignNodeVO data, TreeNode<DesignNodeVO> node)
        {
            node.Icon = new Icon(GetIconForNode(data));
            node.Label = new Text(data.Label);
            node.IsLeaf = data.Type == DesignNodeType.ModelNode ||
                          data.Type == DesignNodeType.DataStoreNode;
            node.IsExpanded = data.Type == DesignNodeType.DataStoreRootNode ||
                              data.Type == DesignNodeType.ApplicationRoot ||
                              data.Type == DesignNodeType.ApplicationNode;
        }

        private static IconData GetIconForNode(DesignNodeVO data)
        {
            switch (data.Type)
            {
                case DesignNodeType.DataStoreNode: return Icons.Filled.Storage;
                case DesignNodeType.ApplicationNode: return Icons.Filled.Widgets;
                case DesignNodeType.ModelNode:
                    return IconUtil.GetIconForModelType(((ModelNodeVO)data).ModelType);
                default: return Icons.Filled.Folder;
            }
        }

        protected override void OnMounted()
        {
            base.OnMounted();

            LoadDesignTree();
        }

        private async void LoadDesignTree()
        {
            if (_hasLoadTree) return;
            _hasLoadTree = true;

            DesignStore.TreeController.IsLoading = true;
            try
            {
                var res = await Channel.Invoke<DesignTreeVO>("sys.DesignService.LoadDesignTree");
                DesignStore.TreeController.DataSource = res!.RootNodes;
            }
            catch (System.Exception ex)
            {
                Notification.Error($"Can't load design tree: {ex.Message}");
            }
            finally
            {
                DesignStore.TreeController.IsLoading = false;
            }
        }
    }
}