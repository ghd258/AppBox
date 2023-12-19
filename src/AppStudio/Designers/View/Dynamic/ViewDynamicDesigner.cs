using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AppBoxClient;
using AppBoxClient.Dynamic;
using AppBoxClient.Dynamic.Events;
using AppBoxDesign.EventEditors;
using AppBoxDesign.PropertyEditors;
using PixUI;
using PixUI.Dynamic;
using PixUI.Dynamic.Design;

namespace AppBoxDesign;

internal sealed class ViewDynamicDesigner : View, IModelDesigner
{
    static ViewDynamicDesigner()
    {
        if (DesignSettings.GetDataSetStateEditor != null) return;

        // 初始化一些动态视图设计时的委托
        DesignSettings.GetEventEditor = (element, meta) => new EventEditDialog(element, meta);
        DesignSettings.GetDataSetStateEditor = (c, s) => new DataSetStateEditDialog(c, s);
        DesignSettings.MakeDataSetState = () => new DynamicDataSetState();
        DesignSettings.GetValueStateEditor = state => new ValueStateEditDialog(state);
        DesignSettings.MakeValueState = () => new DynamicValueState();
        // 初始化其他属性编辑器
        PropertyEditor.RegisterClassValueEditor<string, DataSetPropEditor>(false, "DataSetSelect");
        PropertyEditor.RegisterClassValueEditor<CartesianSeriesSettings[], CartesianSeriesPropEditor>(true);
        PropertyEditor.RegisterClassValueEditor<ChartAxisSettings[], AxesPropEditor>(true);
        PropertyEditor.RegisterClassValueEditor<PieSeriesSettings, PieSeriesPropEditor>(true);
        PropertyEditor.RegisterClassValueEditor<TableColumnSettings[], TableColumnsPropEditor>(true);
        PropertyEditor.RegisterClassValueEditor<TableFooterCell[], TableFooterPropEditor>(true);
        PropertyEditor.RegisterClassValueEditor<TableStyles, TableStylesPropEditor>(true);
        // 初始化其他事件编辑器
        EventEditor.Register(nameof(FetchDataSet), (e, m, a) => new FetchDataSetEditor(e, m, a));
    }

    public ViewDynamicDesigner(ModelNodeVO modelNode)
    {
        ModelNode = modelNode;
        _toolboxPad = new Toolbox(_designController);
        _outlinePad = new DynamicOutlinePad(_designController);

        Child = new Column
        {
            Children =
            {
                // CommandBar
                new Container
                {
                    Height = 40,
                    Padding = EdgeInsets.All(5),
                    FillColor = Colors.Gray,
                    Child = new Row(VerticalAlignment.Middle, 5f)
                    {
                        Children =
                        {
                            new Button("Add") { OnTap = OnAdd },
                            new Button("Remove") { OnTap = OnRemove },
                            new Button("Background") { OnTap = OnSetBackground },
                        }
                    }
                },
                // Designer
                new Row
                {
                    Children =
                    {
                        new Expanded { Child = new DesignCanvas(_designController) },
                        new Container { Width = 260, Child = new PropertyPanel(_designController) }
                    }
                },
            }
        };
    }

    private readonly DesignController _designController = new();
    private readonly Toolbox _toolboxPad;
    private readonly DynamicOutlinePad _outlinePad;
    private bool _hasLoadSourceCode = false;

    public ModelNodeVO ModelNode { get; }

    protected override void OnMounted()
    {
        base.OnMounted();
        TryLoadSourceCode();
    }

    private async void TryLoadSourceCode()
    {
        if (_hasLoadSourceCode) return;
        _hasLoadSourceCode = true;

        if (await DynamicInitiator.TryInitAsync())
            _toolboxPad.Rebuild();

        //TODO:直接获取utf8 bytes
        try
        {
            var srcCode = await Channel.Invoke<string>("sys.DesignService.OpenCodeModel",
                new object[] { ModelNode.Id });
            if (srcCode != null)
            {
                var jsonData = Encoding.UTF8.GetBytes(srcCode);
                _designController.Load(jsonData);
            }
        }
        catch (Exception e)
        {
            Notification.Error($"无法加载动态视图: {e.Message}");
        }
    }

    public Task SaveAsync()
    {
        //TODO:直接传输utf8 bytes
        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms);
        _designController.Write(writer);
        writer.Flush();
        var json = Encoding.UTF8.GetString(ms.ToArray());

        return Channel.Invoke("sys.DesignService.SaveModel", new object?[] { ModelNode.Id, json });
    }

    public Task RefreshAsync()
    {
        throw new System.NotImplementedException();
    }

    public void GotoDefinition(ReferenceVO reference)
    {
        throw new System.NotImplementedException();
    }

    public Widget? GetOutlinePad() => _outlinePad;

    public Widget? GetToolboxPad() => _toolboxPad;

    private void OnAdd(PointerEvent e)
    {
        if (_designController.FirstSelected == null) return;

        var meta = _designController.CurrentToolboxItem;
        if (meta == null) return;

        var active = _designController.FirstSelected!;
        active.OnDrop(meta);
    }

    private void OnRemove(PointerEvent e)
    {
        var cmd = new DeleteElementsCommand();
        cmd.Run(_designController);
    }

    private async void OnSetBackground(PointerEvent e)
    {
        var dlg = new BackgroundDialog();
        var dlgResult = await dlg.ShowAsync();
        if (dlgResult != DialogResult.OK) return;

        var bg = dlg.GetBackground();
        _designController.Background = bg;
    }
}