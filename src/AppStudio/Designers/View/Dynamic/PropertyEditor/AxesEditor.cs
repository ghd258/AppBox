using System.Collections.Generic;
using System.Linq;
using AppBoxClient.Dynamic;
using AppBoxCore;
using PixUI;
using PixUI.Dynamic;
using PixUI.Dynamic.Design;

namespace AppBoxDesign.PropertyEditor;

internal sealed class AxesEditor : SingleChildWidget
{
    public AxesEditor(State<AxisSettings?> state,
        DataGridController<AxisSettings> dataGridController,
        DesignController designController)
    {
        _dataGridController = dataGridController;
        _designController = designController;

        var name = new RxProxy<string>(() => state.Value?.Name ?? string.Empty, v =>
        {
            if (state.Value != null) state.Value.Name = v;
        });
        var dataset = new RxProxy<string?>(() => state.Value?.DataSet, v =>
        {
            if (state.Value != null) state.Value.DataSet = v;
        });
        var labels = new RxProxy<string?>(() => state.Value?.Labels, v =>
        {
            if (state.Value != null) state.Value.Labels = v;
        });
        var textSize = new RxProxy<double?>(() => state.Value?.TextSize, v =>
        {
            if (state.Value != null) state.Value.TextSize = v;
        });
        var minStep = new RxProxy<double?>(() => state.Value?.MinStep, v =>
        {
            if (state.Value != null) state.Value.MinStep = v;
        });
        var forceMinStep = new RxProxy<bool>(() => state.Value?.ForceStepToMin ?? false, v =>
        {
            if (state.Value != null) state.Value.ForceStepToMin = v;
        });

        dataset.Listen(OnDataSetChanged);
        labels.Listen(v => RefreshCurrentRow());
        state.Listen(_ =>
        {
            name.NotifyValueChanged();
            dataset.NotifyValueChanged();
            labels.NotifyValueChanged();
            textSize.NotifyValueChanged();
            minStep.NotifyValueChanged();
            forceMinStep.NotifyValueChanged();
        });

        var allDataSet = designController.GetAllDataSet().Select(s => s.Name).ToArray();
        var formItems = new List<FormItem>
        {
            new("Name", new TextInput(name)),
            new("DataSet", new Select<string>(dataset) { Options = allDataSet }),
            new("Labels", new Select<string>(labels) { Ref = _labelsRef }),
            new("TextSize", new NumberInput<double>(textSize)),
            new("MinStep", new NumberInput<double>(minStep)),
            new("ForceMinStep", new Checkbox(forceMinStep)),
        };

        Child = new Form
        {
            LabelWidth = 100,
            Children = formItems,
        };

        OnDataSetChanged(dataset.Value);
    }

    private readonly DesignController _designController;
    private readonly DataGridController<AxisSettings> _dataGridController;
    private readonly WidgetRef<Select<string>> _labelsRef = new();

    private async void OnDataSetChanged(string? dsName)
    {
        if (string.IsNullOrEmpty(dsName)) return;

        var dsState = _designController.FindState(dsName);
        var dsSettings = dsState!.Value as IDynamicDataSetStateValue;
        if (dsSettings == null) return;

        var ds = await dsSettings.GetRuntimeDataSet() as DynamicDataSet;
        if (ds == null) return;

        var strings = ds.Fields.Where(f => f.IsString).Select(f => f.Name).ToArray();
        _labelsRef.Widget!.Options = strings;
    }

    private void RefreshCurrentRow() //TODO:待DataGrid实现绑定单元格状态后移除
    {
        _dataGridController.RefreshCurrentRow();
    }
}