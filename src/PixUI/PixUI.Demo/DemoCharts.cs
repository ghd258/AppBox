using System.Collections.Generic;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using PixLiveCharts;

namespace PixUI.Demo;

public sealed class DemoCharts : View
{
    private static float[] data1 = { 3, 2, 5, 6, 4, 1, 2 };
    private static float[] data2 = { 2, 1, 3, 5, 3, 4, 6 };

    private ISeries[] series =
    {
        new ColumnSeries<float> { Values = data1, },
        new LineSeries<float> { Values = data2, Fill = null },
    };

    private IEnumerable<ISeries> pieSeries = data1.AsLiveChartsPieSeries((value, s) =>
    {
        // here you can configure the series assigned to each value.
        s.Name = $"S{value}";
        s.DataLabelsPaint = new SolidColorPaint(new Color(30, 30, 30));
        s.DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer;
        //s.DataLabelsFormatter = p => $"{p.PrimaryValue} / {p.StackedValue!.Total} ({p.StackedValue.Share:P2})";
        s.DataLabelsFormatter = p => $"{p.StackedValue.Share:P2}";
    });

    private LabelVisual title = new LabelVisual()
    {
        Text = "My Chart Title",
        TextSize = 25,
        Padding = new Padding(15),
        Paint = new SolidColorPaint(Colors.Gray)
    };

    public DemoCharts()
    {
        Child = new Row
        {
            Children = new Widget[]
            {
                new Card
                {
                    Child = new CartesianChart
                    {
                        Series = series,
                        //Title = title,
                        Width = 400,
                        Height = 300,
                    }
                },
                new Card
                {
                    Child = new PieChart
                    {
                        Series = pieSeries,
                        LegendPosition = LegendPosition.Right,
                        Width = 600,
                        Height = 300,
                    }
                }
            }
        };
    }
}