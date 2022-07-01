namespace PixUI
{
    internal sealed class MenuController
    {
        internal readonly State<Color> TextColor = Colors.Black;
        
        public EdgeInsets ItemPadding { get; } = EdgeInsets.Only(8, 5, 8, 5);

        public float PopupItemHeight { get; } = 30;

        public Color BackgroundColor { get; set; } = new Color(200, 200, 200);

        public Color Color
        {
            set => TextColor.Value = value;
        }

        public Color HoverColor { get; set; } = Theme.AccentColor;

        public Color HoverTextColor { get; set; } = Colors.White;

        private PopupMenuStack? _popupMenuStack;

        internal void OnMenuItemHoverChanged(MenuItemWidget item, bool hover)
        {
            if (hover)
            {
                //尝试关闭之前打开的
                if (_popupMenuStack != null)
                {
                    if (_popupMenuStack.TryCloseSome(item))
                        return;
                }

                //尝试打开子菜单
                if (item.MenuItem.Type == MenuItemType.SubMenu)
                {
                    _popupMenuStack ??= new PopupMenuStack(item.Overlay!, CloseAll);

                    var popupMenu = new PopupMenu(item, item.Depth + 1, this);
                    var relativeToWinPt = item.LocalToWindow(0, 0);
                    // var relativeBounds =
                    //     Rect.FromLTWH(relativeToWinPt.X, relativeToWinPt.Y, item.W, item.H);
                    //TODO:计算弹出位置
                    if (item.Parent is PopupMenu)
                        popupMenu.SetPosition(relativeToWinPt.X + item.W, relativeToWinPt.Y);
                    else
                        popupMenu.SetPosition(relativeToWinPt.X, relativeToWinPt.Y + item.H);
                    popupMenu.Layout(item.Root!.Window.Width, item.Root.Window.Height);

                    _popupMenuStack.Add(popupMenu);
                }

                //如果没有打开的子菜单，移除整个PopupStack
                if (_popupMenuStack != null && !_popupMenuStack.HasChild)
                    CloseAll();
            }
        }

        internal void CloseAll()
        {
            _popupMenuStack?.Hide();
            _popupMenuStack = null;
        }
    }
}