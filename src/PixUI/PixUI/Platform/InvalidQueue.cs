using System;
using System.Collections.Generic;

namespace PixUI
{
    /// <summary>
    /// Widget重新布局后向上所影响的Widget及区域
    /// </summary>
    [TSNoInitializer]
    public sealed class AffectsByRelayout
    {
        internal static readonly AffectsByRelayout Default = new();

        public Widget Widget = null!;
        public float OldX = 0;
        public float OldY = 0;
        public float OldW = 0;
        public float OldH = 0;

        private AffectsByRelayout() { }

        /// <summary>
        /// 计算受影响的Widget的dirty area(新旧的union), 注意相对于上级
        /// </summary>
        public IDirtyArea GetDirtyArea()
        {
            //TODO: 考虑Root返回null或现有Bounds
            return new RepaintArea(
                new Rect(Math.Min(OldX, Widget.X),
                    Math.Min(OldY, Widget.Y),
                    Math.Max(OldX + OldW, Widget.X + Widget.W),
                    Math.Max(OldY + OldH, Widget.Y + Widget.H))
            );
        }
    }

    public enum InvalidAction
    {
        Repaint,
        Relayout,
    }

    internal sealed class InvalidWidget
    {
        internal Widget Widget = null!;
        internal InvalidAction Action;
        internal int Level;
        internal bool RelayoutOnly = false;

        /// <summary>
        /// 用于局部重绘的对象,null表示全部重绘
        /// </summary>
        internal IDirtyArea? Area;

        internal InvalidWidget() { } //Need for web now, TODO:use TSRecordAttribute
    }

    /// <summary>
    /// Dirty widget queue, One UIWindow has two queue.
    /// </summary>
    [TSNoInitializer]
    internal sealed class InvalidQueue
    {
        #region ====Static====

        /// <summary>
        /// Add invalid widget to queue
        /// 只允许UI thread 添加，动画控制器只向ui thread递交修改状态请求
        /// </summary>
        /// <returns>false=widget is not mounted and can't add to queue</returns>
        internal static bool Add(Widget widget, InvalidAction action, IDirtyArea? item)
        {
            //暂在这里判断Widget是否已挂载
            if (!widget.IsMounted) return false;

            //根据Widget所在的画布加入相应的队列
            var root = widget.Root!;
            if (root is Overlay)
            {
                //When used for overlay, only Relayout invalid add to queue.
                if (action == InvalidAction.Relayout)
                    root.Window.OverlayInvalidQueue.AddInternal(widget, action, item);
            }
            else
            {
                root.Window.WidgetsInvalidQueue.AddInternal(widget, action, item);
            }

            if (!root.Window.HasPostInvalidateEvent)
            {
                root.Window.HasPostInvalidateEvent = true;
                UIApplication.Current.PostInvalidateEvent();
            }

            return true;
        }

        #endregion

        private readonly List<InvalidWidget> _queue = new List<InvalidWidget>(32);

        internal bool IsEmpty => _queue.Count == 0;

        /// <summary>
        /// Add dirty widget to queue.
        /// </summary>
        /// <returns>true=the first item added to queue</returns>
        private void AddInternal(Widget widget, InvalidAction action, IDirtyArea? item)
        {
            //先尝试合并入现有项
            var level = GetLevelToTop(widget);
            var insertPos = 0; // -1 mean has merged to exist.
            var relayoutOnly = false;

            foreach (var exist in _queue)
            {
                if (exist.Level > level) break; //TODO:判断新项是否现存项的任意上级，是则尝试合并

                // check is same widget
                if (ReferenceEquals(exist.Widget, widget))
                {
                    if (exist.Action < action)
                        exist.Action = action;
                    if (exist.Action == InvalidAction.Repaint && action == InvalidAction.Repaint)
                    {
                        if (item == null)
                            exist.Area = null;
                        exist.Area?.Merge(item);
                    }

                    insertPos = -1;
                    break;
                }

                // check is any parent of current
                if (exist.Widget.IsAnyParentOf(widget))
                {
                    if (exist.Action == InvalidAction.Relayout ||
                        (exist.Action == InvalidAction.Repaint && action == InvalidAction.Repaint))
                    {
                        insertPos = -1;
                        break;
                    }

                    //上级要求重绘，子级要求重新布局的情况，尽可能标记当前项为RelayoutOnly
                    relayoutOnly = true;
                    exist.Area = null; //TODO:合并脏区域
                }

                insertPos++;
            }

            if (insertPos < 0) return;

            //在同一上级的子级内排序,eg: 同一Stack内的两个widget同时需要刷新，但要控制重绘顺序
            if (widget.Parent != null)
            {
                for (var i = insertPos - 1; i >= 0; i--)
                {
                    var exist = _queue[i];
                    if (exist.Level < level) break;
                    //same level now, check parent is same
                    if (!ReferenceEquals(exist.Widget.Parent, widget.Parent)) continue;
                    //compare index of same parent
                    var existIndex = widget.Parent.IndexOfChild(exist.Widget);
                    var curIndex = widget.Parent.IndexOfChild(widget);
                    if (curIndex > existIndex) break;
                    insertPos = i;
                }
            }

            // insert to invalid queue.
            //TODO:use object pool for InvalidWidget
            var target = new InvalidWidget
            {
                Widget = widget, Action = action, Level = level, Area = item,
                RelayoutOnly = relayoutOnly
            };
            _queue.Insert(insertPos, target);
        }

        private static int GetLevelToTop(Widget widget)
        {
            var level = 0;
            Widget cur = widget;
            while (cur.Parent != null)
            {
                level++;
                cur = cur.Parent;
            }

            return level;
        }

        /// <summary>
        /// Only for Widgets Tree
        /// </summary>
        internal void RenderFrame(PaintContext context)
        {
            var hasRelayout = false;

            foreach (var item in _queue)
            {
                if (item.Action == InvalidAction.Relayout)
                {
                    hasRelayout = true;
                    var affects = AffectsByRelayout.Default;
                    RelayoutWidget(item.Widget, affects);
                    if (!item.RelayoutOnly)
                    {
                        //注意: 以下重绘的是受影响Widget的上级，除非本身是根节点
                        RepaintWidget(context, affects.Widget.Parent ?? affects.Widget,
                            affects.GetDirtyArea());
                    }
                }
                else
                {
                    RepaintWidget(context, item.Widget, item.Area);
                }
            }

            // clear items
            _queue.Clear();

            // 通知重新进行HitTest TODO:确认布局影响，eg:Input重布局没有改变大小，则不需要重新HitTest
            if (hasRelayout)
                context.Window.RunNewHitTest();
        }

        /// <summary>
        /// Only for overlay
        /// </summary>
        internal void RelayoutAll()
        {
            foreach (var item in _queue)
            {
                if (item.Action == InvalidAction.Relayout)
                {
                    var affects = AffectsByRelayout.Default;
                    RelayoutWidget(item.Widget, affects);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            // clear items
            _queue.Clear();
        }

        private static void RelayoutWidget(Widget widget, AffectsByRelayout affects)
        {
            //先初始化受影响的Widget(必须因为可能重新布局后没有改变大小)
            affects.Widget = widget;
            affects.OldX = widget.X;
            affects.OldY = widget.Y;
            affects.OldW = widget.W;
            affects.OldH = widget.H;

            //再重新布局并尝试通知上级
            widget.Layout(widget.CachedAvailableWidth, widget.CachedAvailableHeight);
            widget.TryNotifyParentIfSizeChanged(affects.OldW, affects.OldH, affects);
        }

        private static void RepaintWidget(PaintContext ctx, Widget widget, IDirtyArea? dirtyArea)
        {
            Console.WriteLine($"Repaint: {widget} rect={dirtyArea?.GetRect()}");
            var canvas = ctx.Canvas;

            //TODO:不能简单判断是否不透明，例如上级有OpacityWidget or ImageFilter等
            //Find first opaque (self or parent) to clear dirty area
            if (widget.IsOpaque) //self is opaque
            {
                var pt2Win = widget.LocalToWindow(0, 0);
                canvas.Translate(pt2Win.X, pt2Win.Y);
                widget.Paint(canvas, dirtyArea);
                canvas.Translate(-pt2Win.X, -pt2Win.Y);
                return;
            }

            //向上查找不透明的父级组件
            Widget? opaque = null;
            Widget current = widget;
            float dx = 0; //当前需要重绘的Widget相对于不透明的上级的坐标偏移X
            float dy = 0; //当前需要重绘的Widget相对于不透明的上级的坐标偏移Y
            while (current.Parent != null)
            {
                dx += current.X;
                dy += current.Y;

                current = current.Parent;
                if (current.IsOpaque)
                {
                    opaque = current;
                    break;
                }
            }

            opaque ??= current; //没找到暂指向Window's RootWidget

            //计算脏区域(重绘的Widget相对于Opaque Widget)
            var dirtyRect = dirtyArea?.GetRect();
            var dirtyX = dx + (dirtyRect?.Left ?? 0);
            var dirtyY = dy + (dirtyRect?.Top ?? 0);
            var dirtyW = dirtyRect?.Width ?? widget.W;
            var dirtyH = dirtyRect?.Height ?? widget.H;
            var dirtyChildRect = Rect.FromLTWH(dirtyX, dirtyY, dirtyW, dirtyH);

            //裁剪脏区域并开始绘制
            canvas.Save();
            var opaque2Win = opaque.LocalToWindow(0, 0);
            canvas.Translate(opaque2Win.X, opaque2Win.Y);
            canvas.ClipRect(dirtyChildRect, ClipOp.Intersect, false); //TODO: Root不用
            //判断是否RootWidget且非不透明，是则清空画布脏区域
            if (ReferenceEquals(opaque, ctx.Window.RootWidget) && !opaque.IsOpaque)
                canvas.Clear(ctx.Window.BackgroundColor);
            opaque.Paint(canvas, new RepaintArea(dirtyChildRect));
            canvas.Restore();
        }
    }
}