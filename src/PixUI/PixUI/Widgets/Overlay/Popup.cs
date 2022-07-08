using System;

namespace PixUI
{
    public delegate Widget PopupTransitionBuilder(Animation<double> animation, Widget child,
        Offset? origin);

    public abstract class Popup : Widget, IEventHook
    {
        protected Popup(Overlay overlay)
        {
            Owner = overlay;
        }

        internal readonly Overlay Owner;
        internal readonly FocusManager FocusManager = new();
        private PopupTransitionWrap? _transition;
        private PopupProxy? _proxy;
        internal AnimationController? AnimationController => _transition?.AnimationController;

        /// <summary>
        /// 默认的沿Y缩放的打开动画
        /// </summary>
        public static readonly PopupTransitionBuilder DefaultTransitionBuilder =
            (animation, child, origin) => new ScaleYTransition(animation, origin)
            {
                Child = child
            };

        public void UpdatePosition(float x, float y)
        {
            SetPosition(x, y);
            Invalidate(InvalidAction.Repaint);
        }

        public void Show(Widget? relativeTo = null,
            Offset? relativeOffset = null, PopupTransitionBuilder? transitionBuilder = null)
        {
            Widget target = this;

            //先计算显示位置
            Offset? origin = null;
            var winX = 0f;
            var winY = 0f;
            if (relativeTo != null)
            {
                var winPt = relativeTo.LocalToWindow(0, 0);
                var offsetX = relativeOffset?.Dx ?? 0;
                var offsetY = relativeOffset?.Dy ?? 0;

                _proxy = new PopupProxy(this); //构建占位并计算布局
                target = _proxy;
                var popupHeight = H;
                //暂简单支持向下或向上弹出
                if (winPt.Y + relativeTo.H + offsetY + popupHeight > Owner.Window.Height)
                {
                    //向上弹出
                    winX = winPt.X + offsetX;
                    winY = winPt.Y - offsetY - popupHeight;
                    origin = new Offset(0, popupHeight);
                }
                else
                {
                    //向下弹出
                    winX = winPt.X + offsetX;
                    winY = winPt.Y + relativeTo.H + offsetY;
                    //origin = new Offset(0, 0);
                }
            }

            if (transitionBuilder != null)
            {
                _proxy ??= new PopupProxy(this);
                _transition = new PopupTransitionWrap(Owner, _proxy, origin, transitionBuilder);
                _transition.Forward();
                target = _transition;
            }

            if (relativeTo != null)
                target.SetPosition(winX, winY);
            Owner.Window.EventHookManager.Add(this);
            Owner.Window.FocusManagerStack.Push(FocusManager);
            Owner.Show(target);
        }

        public void Hide( /*TODO:TransitionBuilder*/)
        {
            Owner.Window.EventHookManager.Remove(this);
            Owner.Window.FocusManagerStack.Remove(FocusManager);
            if (_transition != null)
            {
                _transition.Reverse();
            }
            else if (_proxy != null)
            {
                Owner.Remove(_proxy);
                _proxy = null;
            }
            else
            {
                Owner.Remove(this);
            }
        }

        public virtual EventPreviewResult PreviewEvent(EventType type, object? e)
        {
            return EventPreviewResult.NotProcessed;
        }
    }

    internal sealed class PopupTransitionWrap : SingleChildWidget
    {
        internal PopupTransitionWrap(Overlay overlay, PopupProxy proxy, Offset? origin,
            PopupTransitionBuilder transitionBuilder)
        {
            _overlay = overlay;
            AnimationController = new AnimationController(100);
            AnimationController.StatusChanged += OnAnimationStateChanged;

            Child = transitionBuilder(AnimationController, proxy, origin);
        }

        internal readonly AnimationController AnimationController;
        private readonly Overlay _overlay;

        internal void Forward() => AnimationController.Forward();

        internal void Reverse() => AnimationController.Reverse();

        private void OnAnimationStateChanged(AnimationStatus status)
        {
            if (status == AnimationStatus.Dismissed)
                _overlay.Remove(this);
        }

        public override void Dispose()
        {
            AnimationController.Dispose();
            base.Dispose();
        }
    }

    /// <summary>
    /// 相当于Popup的占位，布局时不用再计算Popup
    /// </summary>
    internal sealed class PopupProxy : Widget
    {
        internal PopupProxy(Popup popup)
        {
            //直接布局方便计算显示位置，后续不用再计算
            popup.Layout(popup.Owner.Window.Width, popup.Owner.Window.Height);

            _popup = popup;
            _popup.Parent = this;
        }

        private readonly Popup _popup;

        public override void VisitChildren(Func<Widget, bool> action)
            => action(_popup);

        public override void Layout(float availableWidth, float availableHeight)
        {
            //popup已经布局过,只需要设置自身大小等于popup的大小
            SetSize(_popup.W, _popup.H);
        }

        protected override void OnUnmounted()
        {
            _popup.Parent = null;
            base.OnUnmounted();
        }
    }

    internal sealed class ScaleYTransition : Transform //TODO: 整合
    {
        public ScaleYTransition(Animation<double> animation, Offset? origin = null)
            : base(Matrix4.CreateScale(1, (float)(animation.Value), 1), origin)
        {
            _animation = animation;
            _animation.ValueChanged += OnAnimationValueChanged;
        }

        private readonly Animation<double> _animation;

        private void OnAnimationValueChanged() =>
            SetTransform(Matrix4.CreateScale(1, (float)_animation.Value, 1));
    }
}