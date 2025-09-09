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
        private CheckedListBox brandList;
        private TextBox txtName;
        private TextBox txtValues;
        private Button btnAdd;
        private Button btnRemove;
        private List<Color> colorPalette = new List<Color> { Color.DodgerBlue, Color.Orange, Color.LimeGreen, Color.Gold, Color.Violet, Color.Tomato, Color.Cyan, Color.Silver, Color.Khaki, Color.Magenta };
        private int colorIndex = 0;
        private const int baseYear = 2019;

        private class BrandItem
        {
            public string Name { get; set; }
            public string DisplayText { get; set; }
            public override string ToString() => DisplayText;
        }

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

            // Liste des marques (cochable pour afficher/masquer)
            brandList = new CheckedListBox
            {
                Location = new Point(0, yPos),
                Size = new Size(250, 200),
                BackColor = Color.Black,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Arial", 10),
                CheckOnClick = true
            };
            brandList.ItemCheck += BrandList_ItemCheck;
            rightPanel.Controls.Add(brandList);

            yPos += 220;

            // Champs simples pour ajouter/supprimer
            var lblNom = new Label
            {
                Text = "Nom :",
                ForeColor = Color.White,
                Font = new Font("Arial", 10),
                Location = new Point(0, yPos),
                AutoSize = true
            };
            rightPanel.Controls.Add(lblNom);

            txtName = new TextBox
            {
                Location = new Point(60, yPos - 2),
                Size = new Size(190, 22),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
            };
            rightPanel.Controls.Add(txtName);

            yPos += spacing;

            var lblVals = new Label
            {
                Text = "Valeurs (2019→) :",
                ForeColor = Color.White,
                Font = new Font("Arial", 10),
                Location = new Point(0, yPos),
                AutoSize = true
            };
            rightPanel.Controls.Add(lblVals);

            txtValues = new TextBox
            {
                Location = new Point(120, yPos - 2),
                Size = new Size(130, 22),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                Text = "100,120,140"
            };
            rightPanel.Controls.Add(txtValues);

            yPos += spacing;

            btnAdd = new Button
            {
                Text = "+ Ajouter",
                Location = new Point(0, yPos - 4),
                Size = new Size(120, 26),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAdd.Click += BtnAdd_Click;
            rightPanel.Controls.Add(btnAdd);

            btnRemove = new Button
            {
                Text = "- Supprimer",
                Location = new Point(130, yPos - 4),
                Size = new Size(120, 26),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRemove.Click += BtnRemove_Click;
            rightPanel.Controls.Add(btnRemove);

            // Statistiques
            var lblMin = new Label
            {
                Text = "Minimum :",
                ForeColor = Color.White,
                Font = new Font("Arial", 10),
                Location = new Point(0, yPos + spacing),
                AutoSize = true
            };
            rightPanel.Controls.Add(lblMin);

            lblMinValue = new Label
            {
                Text = "",
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new Point(80, yPos + spacing),
                AutoSize = true
            };
            rightPanel.Controls.Add(lblMinValue);

            var lblMax = new Label
            {
                Text = "Maximum :",
                ForeColor = Color.White,
                Font = new Font("Arial", 10),
                Location = new Point(0, yPos + spacing * 2),
                AutoSize = true
            };
            rightPanel.Controls.Add(lblMax);

            lblMaxValue = new Label
            {
                Text = "",
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new Point(80, yPos + spacing * 2),
                AutoSize = true
            };
            rightPanel.Controls.Add(lblMaxValue);

            var lblAverage = new Label
            {
                Text = "Moyenne :",
                ForeColor = Color.White,
                Font = new Font("Arial", 10),
                Location = new Point(0, yPos + spacing * 3),
                AutoSize = true
            };
            rightPanel.Controls.Add(lblAverage);

            lblAverageValue = new Label
            {
                Text = "",
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new Point(80, yPos + spacing * 3),
                AutoSize = true
            };
            rightPanel.Controls.Add(lblAverageValue);
        }

        private void LoadData()
        {
            brandList.Items.Clear();

            // Données simples de départ
            AddBrandSeries("Apple", new[] { 150.0, 200.0, 130.0, 180.0, 190.0 });
            AddBrandSeries("Tesla", new[] { 60.0, 200.0, 400.0, 200.0, 250.0 });
            AddBrandSeries("Nike", new[] { 80.0, 100.0, 120.0, 110.0, 130.0 });

            UpdateStatsAndAxes();
        }

        private void AddBrandSeries(string name, double[] values, Color? fixedColor = null)
        {
            // Supprimer série existante si nécessaire
            var existing = chart.Series.FindByName(name);
            if (existing != null)
            {
                chart.Series.Remove(existing);
            }

            var seriesColor = fixedColor ?? colorPalette[colorIndex++ % colorPalette.Count];
            var s = new Series(name)
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 3,
                Color = seriesColor,
                ChartArea = "main",
                XValueType = ChartValueType.DateTime,
                Enabled = true
            };

            for (int i = 0; i < values.Length; i++)
            {
                s.Points.AddXY(new DateTime(baseYear + i, 1, 1), values[i]);
            }
            chart.Series.Add(s);

            // Mettre à jour/ajouter l'élément dans la liste cochable
            RemoveBrandItemIfExists(name);
            var emoji = ComputeTrendEmoji(values);
            var item = new BrandItem { Name = name, DisplayText = emoji + " " + name };
            brandList.Items.Add(item, true);
        }

        private void RemoveBrandItemIfExists(string name)
        {
            for (int i = brandList.Items.Count - 1; i >= 0; i--)
            {
                var it = brandList.Items[i] as BrandItem;
                if (it != null && it.Name == name)
                {
                    brandList.Items.RemoveAt(i);
                }
            }
        }

        private static string ComputeTrendEmoji(IReadOnlyList<double> values)
        {
            if (values.Count == 0) return "⚪";
            var diff = values[values.Count - 1] - values[0];
            if (diff > 0) return "🟢";
            if (diff < 0) return "🔴";
            return "⚪";
        }

        private void BrandList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // Afficher/masquer série
            var item = brandList.Items[e.Index] as BrandItem;
            if (item != null)
            {
                var series = chart.Series.FindByName(item.Name);
                if (series != null)
                {
                    series.Enabled = (e.NewValue == CheckState.Checked);
                }
            }
            // Recalculer après que l'état ait changé (post event)
            this.BeginInvoke(new Action(UpdateStatsAndAxes));
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            var name = (txtName.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name)) return;

            var parsed = ParseValues(txtValues.Text);
            if (parsed.Length == 0) return;

            AddBrandSeries(name, parsed);
            UpdateStatsAndAxes();
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            if (brandList.SelectedItem is BrandItem item)
            {
                var s = chart.Series.FindByName(item.Name);
                if (s != null) chart.Series.Remove(s);
                brandList.Items.Remove(item);
                UpdateStatsAndAxes();
            }
        }

        private static double[] ParseValues(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return Array.Empty<double>();
            var parts = input.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<double>();
            foreach (var p in parts)
            {
                if (double.TryParse(p.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v))
                {
                    list.Add(v);
                }
            }
            return list.ToArray();
        }

        private void UpdateStatsAndAxes()
        {
            var values = new List<double>();
            foreach (var s in chart.Series)
            {
                if (!s.Enabled) continue;
                foreach (var dp in s.Points)
                {
                    values.Add(dp.YValues[0]);
                }
            }

            if (values.Count > 0)
            {
                lblMinValue.Text = values.Min().ToString("N2");
                lblMaxValue.Text = values.Max().ToString("N2");
                lblAverageValue.Text = values.Average().ToString("N2");
            }
            else
            {
                lblMinValue.Text = "—";
                lblMaxValue.Text = "—";
                lblAverageValue.Text = "—";
            }

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