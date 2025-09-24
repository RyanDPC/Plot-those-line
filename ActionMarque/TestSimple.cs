using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace ActionMarque
{
    public class TestSimple
    {
        public static void TestChartDisplay()
        {
            var form = new Form
            {
                Text = "Test Simple - Extension jusqu'en 2025",
                Size = new Size(1000, 600),
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.Black
            };

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
            chartArea.AxisX.Interval = 365;
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

            // Créer des données de test qui vont clairement jusqu'en 2025
            var testData = new List<(DateTime date, double price)>
            {
                (new DateTime(2020, 9, 24), 100.0),
                (new DateTime(2021, 1, 1), 120.0),
                (new DateTime(2022, 1, 1), 110.0),
                (new DateTime(2023, 1, 1), 130.0),
                (new DateTime(2024, 1, 1), 140.0),
                (new DateTime(2024, 6, 1), 150.0),
                (new DateTime(2024, 12, 1), 160.0),
                (new DateTime(2025, 6, 1), 170.0),
                (new DateTime(2025, 9, 23), 180.0) // Point final
            };

            // Créer une série de test
            var series = new Series("Test Extension 2025")
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 3,
                Color = Color.DodgerBlue,
                ChartArea = "main",
                XValueType = ChartValueType.DateTime,
                Enabled = true
            };

            // Ajouter les points
            foreach (var (date, price) in testData)
            {
                series.Points.AddXY(date, price);
                Console.WriteLine($"Point ajouté: {date:dd/MM/yyyy} = {price:F2}");
            }

            chart.Series.Add(series);

            // Forcer la mise à jour
            chart.Invalidate();
            chart.Refresh();
            chartArea.AxisX.ScaleView.ZoomReset();

            // Ajouter un bouton pour forcer la mise à jour
            var btnUpdate = new Button
            {
                Text = "Forcer Mise à Jour",
                Location = new Point(10, 10),
                Size = new Size(150, 30),
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White
            };
            btnUpdate.Click += (s, e) =>
            {
                chartArea.AxisX.Minimum = minDate.ToOADate();
                chartArea.AxisX.Maximum = maxDate.ToOADate();
                chartArea.AxisX.ScaleView.ZoomReset();
                chart.Invalidate();
                chart.Refresh();
                Console.WriteLine("Mise à jour forcée de l'axe X");
            };

            form.Controls.Add(btnUpdate);
            form.Controls.Add(chart);
            form.ShowDialog();
        }
    }
}
