using System.Collections.Generic;

namespace PixUI.Test.Mac
{
    public sealed class DemoTreeView : View
    {
        private readonly List<TreeData> _treeDataSource = new()
        {
            new TreeData
            {
                Icon = Icons.Filled.Cloud, Text = "Cloud", Children = new List<TreeData>
                {
                    new TreeData { Icon = Icons.Filled.Train, Text = "Train" },
                    new TreeData { Icon = Icons.Filled.AirplanemodeOn, Text = "AirPlane" },
                }
            },
            new TreeData
            {
                Icon = Icons.Filled.BeachAccess, Text = "Beach",
                Children = new List<TreeData>
                {
                    new TreeData
                    {
                        Icon = Icons.Filled.Cake, Text = "Cake", Children = new List<TreeData>
                        {
                            new TreeData { Icon = Icons.Filled.Apple, Text = "Apple" },
                            new TreeData { Icon = Icons.Filled.Adobe, Text = "Adobe" },
                        }
                    },
                    new TreeData { Icon = Icons.Filled.Camera, Text = "Camera" },
                }
            },
            new TreeData { Icon = Icons.Filled.Sunny, Text = "Sunny" }
        };

        private readonly TreeController<TreeData> _treeController;

        public DemoTreeView()
        {
            _treeController = new TreeController<TreeData>(BuildTreeNode, d => d.Children!);
            _treeController.DataSource = _treeDataSource;

            Child = new Container()
            {
                Padding = EdgeInsets.All(20),
                Child = new Column()
                {
                    Children = new Widget[]
                    {
                        new Row(VerticalAlignment.Middle, 20)
                        {
                            Children = new Widget[]
                            {
                                new Button("Insert") { OnTap = OnInsert },
                                new Button("Remove") { OnTap = OnRemove },
                            }
                        },
                        new Expanded()
                        {
                            Child = new TreeView<TreeData>(_treeController)
                                { Color = new Color(0xFFDCDCDC) }
                        }
                    }
                }
            };
        }

        private void BuildTreeNode(TreeData data, TreeNode<TreeData> node)
        {
            node.Icon = new Icon(data.Icon);
            node.Label = new Text(data.Text);
            node.IsLeaf = data.Children == null;
            node.IsExpanded = data.Text == "Cloud";
        }

        private void OnInsert(PointerEvent e)
        {
            var parentNode = _treeController.FindNode(t => t.Text == "Cake");
            var childNode = _treeController.InsertNode(
                new TreeData() { Icon = Icons.Filled.Start, Text = "AppBox" }, parentNode, 1);
            _treeController.ExpandTo(childNode);
            _treeController.SelectNode(childNode);
        }

        private void OnRemove(PointerEvent e)
        {
            var node = _treeController.FindNode(t => t.Text == "AppBox");
            if (node != null)
                _treeController.RemoveNode(node);
        }
    }

    internal struct TreeData
    {
        public IconData Icon;
        public string Text;
        public List<TreeData>? Children;
    }
}