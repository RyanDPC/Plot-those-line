using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace ActionMarque
{
    public partial class Form3 : Form
    {
        private Chart chart;
        private Panel rightPanel;
        private Label lblMinValue;
        private Label lblMaxValue;
        private Label lblAverageValue;
        private ListBox brandList;

        public Form3()
        {
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "Vente Marques";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.Black;

            // Panel principal
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };
            this.Controls.Add(mainPanel);

            // Panel gauche pour le graphique
            var leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 700,
                BackColor = Color.Black,
                Padding = new Padding(20)
            };
            mainPanel.Controls.Add(leftPanel);

            // Panel droit pour les contrôles
            rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                Padding = new Padding(20)
            };
            mainPanel.Controls.Add(rightPanel);

            // Configuration du graphique
            SetupChart(leftPanel);

            // Configuration des contrôles
            SetupControls();

            // Charger les données
            LoadData();
        }

        private void SetupChart(Panel parent)
        {
            chart = new Chart
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };

            // Zone de graphique
            var chartArea = new ChartArea("main")
            {
                BackColor = Color.Black,
                BorderColor = Color.White,
                BorderWidth = 1
            };

            // Configuration des axes
            chartArea.AxisX.MajorGrid.Enabled = false;
            chartArea.AxisX.LabelStyle.ForeColor = Color.White;
            chartArea.AxisX.LineColor = Color.White;
            chartArea.AxisX.Title = "Années";
            chartArea.AxisX.TitleForeColor = Color.White;
            chartArea.AxisX.IntervalType = DateTimeIntervalType.Years;
            chartArea.AxisX.Interval = 1;
            chartArea.AxisX.LabelStyle.Format = "yyyy";

            chartArea.AxisY.MajorGrid.LineColor = Color.Gray;
            chartArea.AxisY.LabelStyle.ForeColor = Color.White;
            chartArea.AxisY.LineColor = Color.White;
            chartArea.AxisY.Title = "Prix ($)";
            chartArea.AxisY.TitleForeColor = Color.White;
            chartArea.AxisY.LabelStyle.Format = "#,0";

            chart.ChartAreas.Add(chartArea);

            // Légende
            var legend = new Legend
            {
                Docking = Docking.Bottom,
                Alignment = StringAlignment.Center,
                ForeColor = Color.White,
                BackColor = Color.Black
            };
            chart.Legends.Add(legend);

            // Titre
            var title = new Title("Vente Marques")
            {
                ForeColor = Color.White,
                Font = new Font("Arial", 14, FontStyle.Bold)
            };
            chart.Titles.Add(title);

            parent.Controls.Add(chart);
        }

        private void SetupControls()
        {
            int yPos = 20;
            int spacing = 30;

            // Titre
            var titleLabel = new Label
            {
                Text = "Marques",
                ForeColor = Color.White,
                Font = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(0, yPos),
                AutoSize = true
            };
            rightPanel.Controls.Add(titleLabel);

            yPos += 50;

            // Liste des marques
            brandList = new ListBox
            {
                Location = new Point(0, yPos),
                Size = new Size(250, 200),
                BackColor = Color.Black,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Arial", 10)
            };
            rightPanel.Controls.Add(brandList);

            yPos += 220;

            // Statistiques
            var lblMin = new Label
            {
                Text = "Minimum :",
                ForeColor = Color.White,
                Font = new Font("Arial", 10),
                Location = new Point(0, yPos),
                AutoSize = true
            };
            rightPanel.Controls.Add(lblMin);

            lblMinValue = new Label
            {
                Text = "",
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new Point(80, yPos),
                AutoSize = true
            };
            rightPanel.Controls.Add(lblMinValue);

            var lblMax = new Label
            {
                Text = "Maximum :",
                ForeColor = Color.White,
                Font = new Font("Arial", 10),
                Location = new Point(0, yPos + spacing),
                AutoSize = true
            };
            rightPanel.Controls.Add(lblMax);

            lblMaxValue = new Label
            {
                Text = "",
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new Point(80, yPos + spacing),
                AutoSize = true
            };
            rightPanel.Controls.Add(lblMaxValue);

            var lblAverage = new Label
            {
                Text = "Moyenne :",
                ForeColor = Color.White,
                Font = new Font("Arial", 10),
                Location = new Point(0, yPos + spacing * 2),
                AutoSize = true
            };
            rightPanel.Controls.Add(lblAverage);

            lblAverageValue = new Label
            {
                Text = "",
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new Point(80, yPos + spacing * 2),
                AutoSize = true
            };
            rightPanel.Controls.Add(lblAverageValue);
        }

        private void LoadData()
        {
            // Ajouter les marques à la liste
            var brands = new[] { "Apple", "Tesla", "Nike", "Samsung", "Celio", "Pull & Bear", "Ferrari", "Rolex", "Coca Cola", "Pepsi" };
            foreach (var brand in brands)
            {
                brandList.Items.Add($"● {brand}");
            }

            // Données pour le graphique
            var allValues = new List<double>();

            // Apple (AAPL)
            var appleSeries = new Series("AAPL")
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 3,
                Color = Color.Blue,
                ChartArea = "main",
                XValueType = ChartValueType.DateTime
            };
            appleSeries.Points.AddXY(new DateTime(2019, 1, 1), 150);
            appleSeries.Points.AddXY(new DateTime(2020, 1, 1), 200);
            appleSeries.Points.AddXY(new DateTime(2021, 1, 1), 130);
            appleSeries.Points.AddXY(new DateTime(2022, 1, 1), 180);
            appleSeries.Points.AddXY(new DateTime(2023, 1, 1), 190);
            chart.Series.Add(appleSeries);
            allValues.AddRange(new[] { 150, 200, 130, 180, 190 });

            // Tesla (TSLA)
            var teslaSeries = new Series("TSLA")
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 3,
                Color = Color.Orange,
                ChartArea = "main",
                XValueType = ChartValueType.DateTime
            };
            teslaSeries.Points.AddXY(new DateTime(2019, 1, 1), 60);
            teslaSeries.Points.AddXY(new DateTime(2020, 1, 1), 200);
            teslaSeries.Points.AddXY(new DateTime(2021, 1, 1), 400);
            teslaSeries.Points.AddXY(new DateTime(2022, 1, 1), 200);
            teslaSeries.Points.AddXY(new DateTime(2023, 1, 1), 250);
            chart.Series.Add(teslaSeries);
            allValues.AddRange(new[] { 60, 200, 400, 200, 250 });

            // Nike (NKE)
            var nikeSeries = new Series("NKE")
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 3,
                Color = Color.Green,
                ChartArea = "main",
                XValueType = ChartValueType.DateTime
            };
            nikeSeries.Points.AddXY(new DateTime(2019, 1, 1), 80);
            nikeSeries.Points.AddXY(new DateTime(2020, 1, 1), 100);
            nikeSeries.Points.AddXY(new DateTime(2021, 1, 1), 120);
            nikeSeries.Points.AddXY(new DateTime(2022, 1, 1), 110);
            nikeSeries.Points.AddXY(new DateTime(2023, 1, 1), 130);
            chart.Series.Add(nikeSeries);
            allValues.AddRange(new[] { 80, 100, 120, 110, 130 });

            // Calculer et afficher les statistiques
            if (allValues.Any())
            {
                var min = allValues.Min();
                var max = allValues.Max();
                var average = allValues.Average();

                lblMinValue.Text = min.ToString("N2");
                lblMaxValue.Text = max.ToString("N2");
                lblAverageValue.Text = average.ToString("N2");
            }

            // Recalculer les axes
            chart.ChartAreas["main"].RecalculateAxesScale();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1000, 600);
            this.Name = "Form3";
            this.Text = "Vente Marques";
            this.ResumeLayout(false);
        }
    }
}