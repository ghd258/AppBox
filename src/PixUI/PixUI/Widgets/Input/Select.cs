using System;
using System.Collections.Generic;

namespace PixUI
{
    /// <summary>
    /// 弹出选择列表，仅支持单选
    /// </summary>
    public sealed class Select<T> : InputBase<Widget>
    {
        public Select(State<T?> value, bool filterable = false)
            : base(filterable
                ? new EditableText(value.AsStateOfString())
                : new SelectText(value.AsStateOfString()))
        {
            _selectedValue = value;

            SuffixWidget = new ExpandIcon(new FloatTween(0, 1).Animate(_expandAnimation));

            if (_editor is IMouseRegion mouseRegion)
                mouseRegion.MouseRegion.PointerTap += OnEditorTap;
            if (_editor is IFocusable focusable)
                focusable.FocusNode.FocusChanged += OnFocusChanged;
        }

        private readonly State<T?> _selectedValue;
        private readonly ListPopupItemBuilder<T>? _optionBuilder;
        private readonly OptionalAnimationController _expandAnimation = new();
        private ListPopup<T>? _listPopup;
        private bool _showing;

        public T[] Options { get; set; } = Array.Empty<T>();

        public override State<bool>? Readonly
        {
            get
            {
                if (_editor is EditableText editableText) return editableText.Readonly;
                return ((SelectText)_editor).Readonly;
            }
            set
            {
                if (_editor is EditableText editableText) editableText.Readonly = value;
                else ((SelectText)_editor).Readonly = value;
            }
        }

        private void OnFocusChanged(bool focused)
        {
            if (!focused)
                HidePopup();
        }

        private void OnEditorTap(PointerEvent e)
        {
            if (_showing) HidePopup();
            else ShowPopup();
        }

        private void ShowPopup()
        {
            if (_showing || Options.Length == 0) return;
            _showing = true;

            var optionBuilder =
                _optionBuilder ??
                ((data, index, isHover, isSelected) =>
                {
                    var color = RxComputed<Color>.Make(
                        isSelected, v => v ? Colors.White : Colors.Black);
                    return new Text(data?.ToString() ?? "") { TextColor = color };
                });
            _listPopup =
                new ListPopup<T>(Overlay!, optionBuilder, W, Theme.DefaultFontSize + 2);
            _listPopup.DataSource = new List<T>(Options);
            _listPopup.OnSelectionChanged = OnSelectionChanged;
            _listPopup.Show(this, null, Popup.DefaultTransitionBuilder);
        }

        private void HidePopup()
        {
            if (!_showing) return;
            _showing = false;

            _listPopup?.Hide();
            // _listPopup?.Dispose();
            // _listPopup = null;
        }

        private void OnSelectionChanged(T? data)
        {
            _showing = false;
            _selectedValue.Value = data;
        }
    }

    internal sealed class SelectText : TextBase, IMouseRegion, IFocusable
    {
        public SelectText(State<string> text) : base(text)
        {
            MouseRegion = new MouseRegion();
            FocusNode = new FocusNode();
        }

        public MouseRegion MouseRegion { get; }
        public FocusNode FocusNode { get; }

        private State<bool>? _readonly;

        public State<bool>? Readonly
        {
            get => _readonly;
            set => _readonly = Rebind(_readonly, value, BindingOptions.None);
        }

        protected override bool ForceHeight => true;

        public override void Layout(float availableWidth, float availableHeight)
        {
            var width = CacheAndCheckAssignWidth(availableWidth);
            var height = CacheAndCheckAssignHeight(availableHeight);

            BuildParagraph(Text.Value, width);

            var fontHeight = (FontSize?.Value ?? Theme.DefaultFontSize) + 4;
            SetSize(width, Math.Min(height, fontHeight));
        }

        public override void Paint(Canvas canvas, IDirtyArea? area = null)
        {
            if (Text.Value.Length == 0) return;
            canvas.DrawParagraph(CachedParagraph!, 0, 2 /*offset*/);
        }
    }
}