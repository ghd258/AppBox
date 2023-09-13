using System;
using System.Collections.Generic;
using System.Linq;
using AppBoxClient.Dynamic;
using AppBoxCore;
using PixUI;
using PixUI.Dynamic;
using PixUI.Dynamic.Design;

namespace AppBoxDesign.PropertyEditor;

internal abstract class SeriesEditor<T> : SingleChildWidget where T : CartesianSeriesSettings
{
    protected SeriesEditor(State<T> state,
        DataGridController<CartesianSeriesSettings> dataGridController,
        DesignController designController)
    {
        _dataGridController = dataGridController;
        _designController = designController;

        var dataset = new RxProxy<string?>(() => state.Value.DataSet, v => state.Value.DataSet = v ?? string.Empty);
        dataset.Listen(OnDataSetChanged);
        var yField = new RxProxy<string?>(() => state.Value.Field, v => state.Value.Field = v ?? string.Empty);
        yField.Listen(v => RefreshCurrentRow());

        // ReSharper disable once VirtualMemberCallInConstructor
        var extProps = GetExtProps(state).ToArray();

        state.Listen(_ =>
        {
            dataset.NotifyValueChanged();
            yField.NotifyValueChanged();
            foreach (var prop in extProps)
            {
                prop.Item2.NotifyValueChanged();
            }
        });

        var allDataSet = designController.GetAllDataSet().Select(s => s.Name).ToArray();
        var formItems = new List<FormItem>
        {
            new("DataSet", new Select<string>(dataset) { Options = allDataSet }),
            new("YField", new Select<string>(yField) { Ref = _yFieldRef })
        };
        formItems.AddRange(extProps.Select(prop => new FormItem(prop.Item1, prop.Item3)));

        Child = new Form
        {
            LabelWidth = 90,
            Children = formItems,
        };

        OnDataSetChanged(dataset.Value);
    }

    private readonly DesignController _designController;
    private readonly DataGridController<CartesianSeriesSettings> _dataGridController;
    private readonly WidgetRef<Select<string>> _yFieldRef = new();

    protected virtual IEnumerable<ValueTuple<string, State, Widget>> GetExtProps(State<T> state)
    {
        yield break;
    }

    private async void OnDataSetChanged(string? dsName)
    {
        if (string.IsNullOrEmpty(dsName)) return;

        var dsState = _designController.FindState(dsName);
        var dsSettings = dsState!.Value as IDynamicDataSetStateValue;
        if (dsSettings == null) return;

        var ds = await dsSettings.GetRuntimeDataSet() as DynamicDataSet;
        if (ds == null) return;

        var numbers = ds.Fields.Where(f => f.IsNumber).Select(f => f.Name).ToArray();
        //var numbersAndDates = ds.Fields.Where(f => f.IsNumber || f.IsDateTime).Select(f => f.Name).ToArray();
        _yFieldRef.Widget!.Options = numbers;
    }

    private void RefreshCurrentRow() //TODO:待DataGrid实现绑定单元格状态后移除
    {
        _dataGridController.RefreshCurrentRow();
    }
}