using System.Threading.Tasks;
using AppBoxCore;
using LiveCharts;
using LiveChartsCore;
using PixUI.Dynamic;

namespace AppBoxClient.Dynamic;

public sealed class LineSeriesSettings : CartesianSeriesSettings
{
    public override string Type => "Line";

    public double? Smoothness { get; set; }

    public bool Fill { get; set; } = true;

    public override CartesianSeriesSettings Clone()
    {
        return new LineSeriesSettings()
        {
            DataSet = DataSet, Field = Field, Name = Name, Smoothness = Smoothness, Fill = Fill
        };
    }

    public override async Task<ISeries> Build(IDynamicView dynamicView)
    {
        var res = new LineSeries<DynamicEntity>
        {
            Name = Name ?? Field,
            Values = (DynamicDataSet?)(await dynamicView.GetDataSet(DataSet)),
            LineSmoothness = Smoothness ?? 0.65f,
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

        if (Smoothness.HasValue) res.LineSmoothness = Smoothness.Value;
        if (!Fill) res.Fill = null;

        return res;
    }
}