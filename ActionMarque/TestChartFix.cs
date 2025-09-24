using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace ActionMarque
{
    public class TestChartFix
    {
        public static void TestChartExtension()
        {
            // Créer un formulaire de test
            var form = new Form
            {
                Text = "Test Chart Extension",
                Size = new Size(800, 600),
                StartPosition = FormStartPosition.CenterScreen
            };

            // Créer un graphique
            var chart = new Chart { Dock = DockStyle.Fill, BackColor = Color.Black };
            var chartArea = new ChartArea("main")
            {
                BackColor = Color.Black,
                BorderColor = Color.White,
                BorderWidth = 1
            };

            // Configuration de l'axe X
            chartArea.AxisX.MajorGrid.Enabled = false;
            chartArea.AxisX.LabelStyle.ForeColor = Color.White;
            chartArea.AxisX.LineColor = Color.White;
            chartArea.AxisX.Title = "Période (24/09/2020 - 23/09/2025)";
            chartArea.AxisX.TitleForeColor = Color.White;
            chartArea.AxisX.IntervalType = DateTimeIntervalType.Days;
            chartArea.AxisX.Interval = 365; // Afficher une étiquette par an
            chartArea.AxisX.LabelStyle.Format = "yyyy";

            chartArea.AxisY.MajorGrid.LineColor = Color.Gray;
            chartArea.AxisY.LabelStyle.ForeColor = Color.White;
            chartArea.AxisY.LineColor = Color.White;
            chartArea.AxisY.Title = "Prix ($)";
            chartArea.AxisY.TitleForeColor = Color.White;
            chartArea.AxisY.LabelStyle.Format = "#,0";

            // Définir les limites de l'axe X
            var minDate = new DateTime(2020, 9, 24);
            var maxDate = new DateTime(2025, 9, 23);
            chartArea.AxisX.Minimum = minDate.ToOADate();
            chartArea.AxisX.Maximum = maxDate.ToOADate();

            chart.ChartAreas.Add(chartArea);

            // Créer des données de test qui vont jusqu'en 2025
            var testData = new List<StockDataPoint>
            {
                new StockDataPoint { Date = new DateTime(2020, 9, 24), Price = 100.0 },
                new StockDataPoint { Date = new DateTime(2021, 1, 1), Price = 120.0 },
                new StockDataPoint { Date = new DateTime(2022, 1, 1), Price = 110.0 },
                new StockDataPoint { Date = new DateTime(2023, 1, 1), Price = 130.0 },
                new StockDataPoint { Date = new DateTime(2024, 1, 1), Price = 140.0 },
                new StockDataPoint { Date = new DateTime(2025, 9, 23), Price = 150.0 } // Point final
            };

            // Créer une série de test
            var series = new Series("Test AAPL")
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 3,
                Color = Color.DodgerBlue,
                ChartArea = "main",
                XValueType = ChartValueType.DateTime,
                Enabled = true
            };

            // Ajouter les points
            foreach (var point in testData)
            {
                series.Points.AddXY(point.Date, point.Price);
                Console.WriteLine($"Point ajouté: {point.Date:dd/MM/yyyy} = {point.Price:F2}");
            }

            chart.Series.Add(series);

            // Forcer la mise à jour
            chart.Invalidate();
            chartArea.AxisX.ScaleView.ZoomReset();

            form.Controls.Add(chart);
            form.ShowDialog();
        }
    }
}
