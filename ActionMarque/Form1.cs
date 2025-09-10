using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing.Drawing2D;

namespace ActionMarque
{
    public partial class Form1 : Form
    {
        private Chart chart;
        private Panel rightPanel;
        // Labels de statistiques supprimés pour design simplifié
        private Panel brandListPanel;
        private TextBox txtValues;
        private Button btnAdd;
        private Button btnDelete;
        private List<Color> colorPalette = new List<Color> { Color.DodgerBlue, Color.Orange, Color.LimeGreen, Color.Gold, Color.Violet, Color.Tomato, Color.Cyan, Color.Silver, Color.Khaki, Color.Magenta };
        private int colorIndex = 0;
        private const int baseYear = 2019;
        private bool suppressItemCheckEvents = false;
        private List<BrandItem> brands = new List<BrandItem>();
        private AlphaVantageService apiService;
        private const string API_KEY = "5D-P4JJKCO-K378T";

        private class BrandItem
        {
            public string Name { get; set; }
            public string DisplayText { get; set; }
            public bool IsVisible { get; set; } = true;
            public double[] Values { get; set; }
            public Color Color { get; set; }
            public string Status { get; set; } = "neutral"; // positive, negative, neutral
            public override string ToString() => DisplayText;
        }

        public Form1()
        {
            apiService = new AlphaVantageService(API_KEY);
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "Vente Marques";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.Black;

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };
            this.Controls.Add(mainPanel);

            // Ajouter d'abord le panneau de droite
            rightPanel = new Panel
            {
                Dock = DockStyle.Right, // Dock à droite au lieu de Fill
                Width = 300, // Largeur fixe
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(15),
                Visible = true
            };
            mainPanel.Controls.Add(rightPanel);

            // Puis le panneau de gauche
            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill, // Fill pour prendre l'espace restant
                BackColor = Color.Black,
                Padding = new Padding(20)
            };
            mainPanel.Controls.Add(leftPanel);
            
            System.Diagnostics.Debug.WriteLine($"RightPanel créé. Size: {rightPanel.Size}, location: {rightPanel.Location}");

            SetupChart(leftPanel);
            SetupControls();

            LoadData();
        }

        private void SetupChart(Panel parent)
        {
            chart = new Chart
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };

            var chartArea = new ChartArea("main")
            {
                BackColor = Color.Black,
                BorderColor = Color.White,
                BorderWidth = 1
            };

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

            var legend = new Legend
            {
                Docking = Docking.Bottom,
                Alignment = StringAlignment.Center,
                ForeColor = Color.White,
                BackColor = Color.Black
            };
            chart.Legends.Add(legend);

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
            int yPos = 30;

            // Titre minimaliste
            var titleLabel = new Label
            {
                Text = "Marques",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(20, yPos),
                AutoSize = true
            };
            rightPanel.Controls.Add(titleLabel);
            
            yPos += 60;
            
            // Bouton d'ajout minimaliste
            btnAdd = new Button
            {
                Text = "+",
                Location = new Point(20, yPos),
                Size = new Size(40, 40),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAdd.Click += BtnAdd_Click;
            rightPanel.Controls.Add(btnAdd);
            
            yPos += 70;
            
            brandListPanel = new Panel
            {
                Location = new Point(20, yPos),
                Size = new Size(260, 300),
                BackColor = Color.Transparent,
                AutoScroll = true
            };
            rightPanel.Controls.Add(brandListPanel);
            
            yPos += 320;
            
            // Section d'ajout ultra simple
            var addSection = new Panel
            {
                Location = new Point(20, yPos),
                Size = new Size(260, 100),
                BackColor = Color.FromArgb(20, 20, 20),
                BorderStyle = BorderStyle.None
            };
            rightPanel.Controls.Add(addSection);
            
            // Champ symbole simple
            var lblValues = new Label
            {
                Text = "Symbole",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(10, 10),
                AutoSize = true
            };
            addSection.Controls.Add(lblValues);
            
            txtValues = new TextBox
            {
                Location = new Point(10, 30),
                Size = new Size(120, 25),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                Text = "AAPL",
                Visible = false,
            };
            addSection.Controls.Add(txtValues);
            txtValues.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    string marque = txtValues.Text.Trim();
                    if (!string.IsNullOrEmpty(marque))
                    {
                        AddBrandFromTextBox(marque);
                        txtValues.Visible = false;
                        txtValues.Text = "";
                    }
                }
            };
            // Bouton supprimer simple
            btnDelete = new Button
            {
                Text = "×",
                Location = new Point(140, 30),
                Size = new Size(30, 25),
                BackColor = Color.FromArgb(215, 58, 73),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
           
            btnDelete.Click += BtnDelete_Click;
            addSection.Controls.Add(btnDelete);
        }

        private Button CreateIconButton(string text, Point location, Color backColor)
        {
            return new Button
            {
                Text = text,
                Location = location,
                Size = new Size(25, 25),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
        }

        private async void LoadData()
        {
            brands.Clear();
            brandListPanel.Controls.Clear();

            // Afficher un message de chargement
            var loadingLabel = new Label
            {
                Text = "Chargement des données...",
                ForeColor = Color.Yellow,
                Font = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };
            brandListPanel.Controls.Add(loadingLabel);

            try
            {
                suppressItemCheckEvents = true;
                
                // Charger les données depuis l'API
                var symbols = new[] { "AAPL" }; // Apple, Tesla, Nike
                var brandData = await apiService.GetMultipleStocksDataAsync(symbols);

                if (brandData.Count > 0)
                {
                    foreach (var data in brandData)
                    {
                        if (data.DataPoints.Count > 0)
                        {
                            var prices = data.DataPoints.Select(dp => dp.Price).ToArray();
                            AddBrandSeries(data.Name, prices);
                        }
                    }
                }
                suppressItemCheckEvents = false;
                RefreshBrandList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur API: {ex.Message}");
                
                RefreshBrandList();
            }
            finally
            {
                // Supprimer le message de chargement
                brandListPanel.Controls.Remove(loadingLabel);
            }
        }

        private void AddBrandSeries(string name, double[] values, Color? fixedColor = null)
        {
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

            // Créer l'item de marque
            var status = ComputeStatus(values);
            var brandItem = new BrandItem 
            { 
                Name = name, 
                Values = values,
                Color = seriesColor,
                Status = status,
                IsVisible = true
            };
            
            RemoveBrandItemIfExists(name);
            brands.Add(brandItem);
        }

        private void RemoveBrandItemIfExists(string name)
        {
            brands.RemoveAll(b => b.Name == name);
        }

        private static string ComputeStatus(IReadOnlyList<double> values)
        {
            if (values.Count == 0) return "neutral";
            var diff = values[values.Count - 1] - values[0];
            if (diff > 0) return "positive";
            if (diff < 0) return "negative";
            return "neutral";
        }

        private void RefreshBrandList()
        {
            brandListPanel.Controls.Clear();
            
            int yPos = 10;
            foreach (var brand in brands)
            {
                var brandPanel = CreateBrandItemPanel(brand, yPos);
                brandListPanel.Controls.Add(brandPanel);
                yPos += 45;
            }
        }

        private Panel CreateBrandItemPanel(BrandItem brand, int yPos)
        {
            var panel = new Panel
            {
                Location = new Point(0, yPos),
                Size = new Size(240, 40),
                BackColor = Color.FromArgb(30, 30, 30),
                BorderStyle = BorderStyle.None,
                Cursor = Cursors.Hand
            };

            // Cercle de statut ultra simple
            var statusCircle = new Panel
            {
                Location = new Point(10, 14),
                Size = new Size(8, 8),
                BackColor = GetStatusColor(brand.Status),
                BorderStyle = BorderStyle.None
            };
            panel.Controls.Add(statusCircle);

            // Nom de la marque simple
            var nameLabel = new Label
            {
                Text = brand.Name,
                Location = new Point(25, 10),
                Size = new Size(150, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.Transparent
            };
            panel.Controls.Add(nameLabel);

            // Checkbox simple
            var checkBox = new CheckBox
            {
                Location = new Point(200, 10),
                Size = new Size(15, 15),
                Checked = brand.IsVisible,
                BackColor = Color.Transparent,
                ForeColor = Color.White
            };
            checkBox.CheckedChanged += (s, e) => ToggleBrandVisibility(brand, checkBox.Checked);
            panel.Controls.Add(checkBox);

            // Clic pour sélectionner
            panel.Click += (s, e) => SelectBrand(brand);
            nameLabel.Click += (s, e) => SelectBrand(brand);

            return panel;
        }

        private void DrawCircle(Graphics g, Rectangle rect, Color color)
        {
            using (var brush = new SolidBrush(color))
            {
                g.FillEllipse(brush, rect);
            }
        }

        private Color GetStatusColor(string status)
        {
            switch (status)
            {
                case "positive": return Color.FromArgb(0, 200, 0);
                case "negative": return Color.FromArgb(200, 0, 0);
                default: return Color.FromArgb(150, 150, 150);
            }
        }

        private void ToggleBrandVisibility(BrandItem brand, bool isVisible)
        {
            brand.IsVisible = isVisible;
            var series = chart.Series.FindByName(brand.Name);
            if (series != null)
            {
                series.Enabled = isVisible;
            }
        }

        private void SelectBrand(BrandItem brand)
        {
            txtValues.Visible = true;
            txtValues.Text = brand.Name;
            txtValues.Focus();
        }

        private async void BtnAdd_Click(object sender, EventArgs e)
        {
            txtValues.Visible = true;
            txtValues.Focus();
            txtValues.Text = "";
            txtValues.BringToFront();
        }
        private async void AddBrandFromTextBox(string symbole)
        {
            try
            {
                var data = await apiService.GetStockDataAsync(symbole);
                if (data.Count > 0)
                {
                    var prices = data.Select(dp => dp.Price).ToArray();
                    AddBrandSeries(symbole, prices);
                }
                else
                {
                    var parsed = ParseValues(txtValues.Text);
                    if (parsed.Length > 0)
                        AddBrandSeries(symbole, parsed);
                }

                RefreshBrandList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des données pour {symbole}: {ex.Message}");
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            var name = (txtValues.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name)) return;

            var parsed = ParseValues(txtValues.Text);
            if (parsed.Length == 0) return;

            // Supprimer l'ancienne série
            var existingSeries = chart.Series.FindByName(name);
            if (existingSeries != null)
            {
                chart.Series.Remove(existingSeries);
            }

            // Ajouter la nouvelle série
            AddBrandSeries(name, parsed);
            RefreshBrandList();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            var name = (txtValues.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name)) return;

            // Supprimer de la liste des marques
            brands.RemoveAll(b => b.Name == name);
            
            // Supprimer du graphique
            var series = chart.Series.FindByName(name);
            if (series != null) 
            {
                chart.Series.Remove(series);
            }

            RefreshBrandList();
            
            txtValues.Text = "";
            txtValues.Text = "100,120,140";
        }

        private async void BtnRefresh_Click(object sender, EventArgs e)
        {
            await Task.Run(() => LoadData());
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

    }
}


