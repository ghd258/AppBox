using System.Text.Json.Serialization;
using AppBoxCore;
using PixUI;
using PixUI.Dynamic;

namespace AppBoxClient.Dynamic;

public sealed class DynamicTable : SingleChildWidget, IDataSetBinder
{
    public DynamicTable()
    {
        Child = new DataGrid<DynamicEntity>(Controller);
    }

    private string? _dataset;
    private TableColumnSettings[]? _columns;
    private TableFooterCell[]? _footer;
    private TableStyles? _styles;
    [JsonIgnore] private IDynamicContext? _dynamicContext;

    [JsonIgnore] internal DataGridController<DynamicEntity> Controller { get; } = new();

    /// <summary>
    /// 绑定的数据集名称
    /// </summary>
    public string? DataSet
    {
        get => _dataset;
        set
        {
            //设计时改变了重置并取消监听数据集变更
            if (IsMounted && !string.IsNullOrEmpty(_dataset))
            {
                _columns = null;
                _footer = null;
                _dynamicContext?.UnbindToDataSet(this, _dataset);
            }

            _dataset = value;

            if (IsMounted)
                Fetch();
        }
    }

    public TableColumnSettings[]? Columns
    {
        get => _columns;
        set
        {
            _columns = value;
            OnColumnsChanged();
        }
    }

    public TableFooterCell[]? Footer
    {
        get => _footer;
        set
        {
            _footer = value;
            OnFooterChanged();
        }
    }

    public TableStyles? Styles
    {
        get => _styles;
        set
        {
            _styles = value;
            Controller.Theme = _styles == null ? DataGridTheme.Default : _styles.ToRuntimeStyles();
        }
    }

    private void OnColumnsChanged()
    {
        Controller.Columns.Clear();
        if (_columns != null)
        {
            foreach (var column in _columns)
            {
                Controller.Columns.Add(column.BuildColumn());
            }
        }
    }

    private void OnFooterChanged()
    {
        Controller.DataGrid.FooterCells = null;
        if (_footer is { Length: > 0 })
        {
            var cells = new DataGridFooterCell[_footer.Length];
            for (var i = 0; i < cells.Length; i++)
            {
                cells[i] = _footer[i].Build(this);
            }

            Controller.DataGrid.FooterCells = cells;
        }
    }

    protected override void OnMounted()
    {
        base.OnMounted();
        //监听目标数据集变更
        _dynamicContext = FindParent(w => w is IDynamicContext) as IDynamicContext;
        _dynamicContext?.BindToDataSet(this, _dataset);
        //填充数据集
        Fetch();
    }

    protected override void OnUnmounted()
    {
        //取消监听数据集变更
        _dynamicContext?.UnbindToDataSet(this, _dataset);
        base.OnUnmounted();
    }

    private async void Fetch()
    {
        if (string.IsNullOrEmpty(DataSet))
        {
            Controller.DataSource = null;
            return;
        }

        if (_dynamicContext == null) return;

        var ds = (DynamicDataSet?)await _dynamicContext.GetDataSet(DataSet);
        Controller.DataSource = ds;
    }

    public override void Paint(Canvas canvas, IDirtyArea? area = null)
    {
        if (_columns == null || _columns.Length == 0)
        {
            var rect = Rect.FromLTWH(0, 0, W, H);
            var borderColor = DataGridTheme.Default.BorderColor;

            DataGridPainter.PaintCellBorder(canvas, rect, borderColor);
            using var ph = DataGridPainter.BuildCellParagraph(rect, CellStyle.AlignCenter(),
                Controller.Theme.DefaultRowCellStyle, "No Columns for Table", 1);
            DataGridPainter.PaintCellParagraph(canvas, rect, DataGridTheme.Default.DefaultRowCellStyle, ph);
            return;
        }

        base.Paint(canvas, area);
    }

    #region ====IDataSetBinder====

    void IDataSetBinder.OnDataSetValueChanged() => Fetch();

    #endregion
}