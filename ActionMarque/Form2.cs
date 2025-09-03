using System;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace ActionMarque
{
    public partial class Form2 : Form
    {
        private readonly Chart chart = new Chart { Dock = DockStyle.Fill };

        public Form2()
        {
            InitializeComponent();

            Controls.Add(chart);

            var area = new ChartArea("main");
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisX.IntervalType = DateTimeIntervalType.Years;
            area.AxisX.Interval = 1;
            area.AxisX.LabelStyle.Format = "yyyy";
            area.AxisY.MajorGrid.LineColor = System.Drawing.Color.Gainsboro;
            area.AxisY.LabelStyle.Format = "#,0";
            chart.ChartAreas.Add(area);

            if (!chart.Legends.Any())
            {
                var legend = new Legend
                {
                    Docking = Docking.Right,
                    Alignment = StringAlignment.Near
                };
                chart.Legends.Add(legend);
            }

            Shown += async (_, __) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var symbols = new[] { "AAPL", "TSLA", "NKE" };
            var colors = new[] { Color.Red, Color.Green, Color.Blue };

            using (var http = new HttpClient())
            {
                for (int i = 0; i < symbols.Length; i++)
                {
                    var url = $"https://api.marketstack.com/v1/eod?access_key=006fe7d63a19548a4c9f4a6beaa755ba&symbols={Uri.EscapeDataString(symbols[i])}&date_from=2019-01-01&date_to=2025-12-31&limit=500";
                    var json = await http.GetStringAsync(url);

                    var obj = Newtonsoft.Json.Linq.JObject.Parse(json);
                    var points = (obj["data"] ?? new Newtonsoft.Json.Linq.JArray())
                        .Select(t => new
                        {
                            Date = (DateTime?)t["date"] ?? default(DateTime),
                            Close = (double?)t["close"] ?? 0d
                        })
                        .Where(p => p.Date != default(DateTime))
                        .OrderBy(p => p.Date)
                        .ToList();

                    var series = chart.Series.FirstOrDefault(s => s.Name == symbols[i]);
                    if (series == null)
                    {
                        series = new Series(symbols[i])
                        {
                            ChartType = SeriesChartType.Line,
                            BorderWidth = 3,
                            Color = colors[i],
                            ChartArea = "main"
                        };
                        chart.Series.Add(series);
                    }
                    series.Points.Clear();
                    foreach (var p in points)
                        series.Points.AddXY(p.Date, p.Close);
                }
            }

            SetFixedXAxisRange(chart, "main", 2019, 2025);
        }

        private void ApplyXAxisRange(Chart chart, string chartAreaName)
        {
            var allPoints = chart.Series
                .SelectMany(s => s.Points.Cast<DataPoint>())
                .ToList();
            if (!allPoints.Any()) return;

            var minOa = allPoints.Min(p => p.XValue);
            var maxOa = allPoints.Max(p => p.XValue);

            var area = chart.ChartAreas[chartAreaName];
            var minDt = new DateTime(DateTime.FromOADate(minOa).Year, 1, 1);
            var maxDt = new DateTime(DateTime.FromOADate(maxOa).Year, 12, 31);
            area.AxisX.Minimum = minDt.ToOADate();
            area.AxisX.Maximum = maxDt.ToOADate();
            area.AxisX.IntervalType = DateTimeIntervalType.Years;
            area.AxisX.Interval = 1;
            area.AxisX.LabelStyle.Format = "yyyy";
        }

        private void SetFixedXAxisRange(Chart chart, string chartAreaName, int minYear, int maxYear)
        {
            var area = chart.ChartAreas[chartAreaName];
            var minDt = new DateTime(minYear, 1, 1);
            var maxDt = new DateTime(maxYear, 12, 31);
            area.AxisX.Minimum = minDt.ToOADate();
            area.AxisX.Maximum = maxDt.ToOADate();
            area.AxisX.IntervalType = DateTimeIntervalType.Years;
            area.AxisX.Interval = 1;
            area.AxisX.LabelStyle.Format = "yyyy";
        }
    }
}


