using System.Threading.Tasks;
using AppBoxCore;
using LiveCharts;
using LiveChartsCore;
using PixUI.Dynamic;

namespace AppBoxClient.Dynamic;

public sealed class ColumnSeriesSettings : CartesianSeriesSettings
{
    public override string Type => "Column";

    public override CartesianSeriesSettings Clone()
    {
        return new ColumnSeriesSettings()
        {
            DataSet = DataSet, Field = Field, Name = Name
        };
    }

    public override async Task<ISeries> Build(IDynamicView dynamicView)
    {
        var res = new ColumnSeries<DynamicEntity>()
        {
            Name = Name ?? Field,
            Values = (DynamicDataSet?)(await dynamicView.GetDataSet(DataSet)),
            Mapping = (obj, point) =>
            {
                var v = obj[Field].ToDouble();
                if (v.HasValue)
                {
                    point.PrimaryValue = v.Value;
                    point.SecondaryValue = point.Context.Entity.EntityIndex;
                }
            }
        };
        return res;
    }
}