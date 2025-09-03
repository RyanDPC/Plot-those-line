using System;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using ActionMarque.Interface.Api.Clients;

namespace ActionMarque
{
    public partial class Form1 : Form
    {
        private readonly Chart chart = new Chart() { Dock = DockStyle.Fill };
        private readonly Button btnMarket = new Button() { Text = "Ouvrir Marketstack", Dock = DockStyle.Top, Height = 32 };

        public Form1()
        {
            InitializeComponent();
            Controls.Add(chart);
            Controls.Add(btnMarket);

            btnMarket.Click += (_, __) => new Form2 { StartPosition = FormStartPosition.CenterParent }.Show();

            var area = new ChartArea("main") { AxisX = { MajorGrid = { Enabled = false }, IntervalType = DateTimeIntervalType.Years, Interval = 1, LabelStyle = { Format = "yyyy" } }, AxisY = { MajorGrid = { LineColor = Color.Gainsboro }, LabelStyle = { Format = "#,0" } } };
            chart.ChartAreas.Add(area);

            chart.Legends.Add(new Legend { Docking = Docking.Right, Alignment = StringAlignment.Near });
            chart.Titles.Add("AlphaVantage");

            Shown += async (_, __) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var symbols = new[] { "AAPL", "TSLA", "NKE" };
            var colors = new[] { Color.Red, Color.Green, Color.Blue };

            var http = new HttpClient();
            var api = new AlphaVantageClient(http);

            symbols.Select((sym, i) => new { sym, color = colors[i] })
                   .ToList()
                   .ForEach(async x =>
                   {
                       try
                       {
                           var data = (await api.GetMonthlyCloseAsync(x.sym, "9B4ZAIY8PY127RW3"))
                                      .OrderBy(kv => kv.Key)
                                      .ToList();

                           if (!data.Any())
                           {
                               chart.Titles.Add($"{x.sym} data unavailable");
                               return;
                           }

                           var series = new Series(x.sym)
                           {
                               ChartType = SeriesChartType.Line,
                               BorderWidth = 3,
                               Color = x.color,
                               ChartArea = "main",
                               XValueType = ChartValueType.DateTime
                           };

                           data.ForEach(kv => series.Points.AddXY(kv.Key, kv.Value));
                           chart.Series.Add(series);
                       }
                       catch
                       {
                           chart.Titles.Add($"{x.sym} data unavailable");
                       }
                   });

            chart.ChartAreas["main"].RecalculateAxesScale();
        }
    }
}
