using static System.Collections.Specialized.BitVector32;
using System.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace ActionMarque
{
    public partial class Form1 : Form
    {
        private Chart chart;
        private Panel rightPanel;
        private Panel brandListPanel;
        private TextBox txtValues;
        private Button btnAdd;
        private Button btnDelete;
        private ComboBox cmbSort;
        private Label lblStats;
        private Button btnFilter;

        private List<Color> colorPalette = new List<Color>
        {
            Color.DodgerBlue, Color.Orange, Color.LimeGreen, Color.Gold,
            Color.Violet, Color.Tomato, Color.Cyan, Color.Silver,
            Color.Khaki, Color.Magenta
        };
        private int colorIndex = 0;
        private const int baseYear = 2019;
        private bool suppressItemCheckEvents = false;

        private List<BrandItem> brands = new List<BrandItem>();
        private TwelveDataService apiService;
        private const string API_KEY = "6ae579e4a04d419088432ae3bbe455ee";

        // dictionnaire FullName -> Abbreviation (ticker)
        private Dictionary<string, string> brandMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private class BrandItem
        {
            public string Name { get; set; }
            public string DisplayText { get; set; }
            public bool IsVisible { get; set; } = true;
            public double[] Values { get; set; }
            public Color Color { get; set; }
            public string Status { get; set; } = "neutral";
            public override string ToString() => DisplayText ?? Name;
        }

        public Form1()
        {
            apiService = new TwelveDataService(API_KEY);
            SetupUI();
            LoadBrandDictionary("marques.txt");
        }

        private void LoadBrandDictionary(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show($"Le fichier {filePath} est introuvable. Le dictionnaire est vide.");
                return;
            }

            foreach (var line in File.ReadAllLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("="))
                    continue;

                var parts = line.Split('=');
                if (parts.Length == 2)
                {
                    string fullName = parts[0].Trim();
                    string symbol = parts[1].Trim();
                    if (!brandMap.ContainsKey(fullName))
                        brandMap[fullName] = symbol;
                }
            }
        }

        private void SetupUI()
        {
            this.Text = "Vente Marques";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.Black;

            var mainPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };
            this.Controls.Add(mainPanel);

            rightPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 300,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(15),
                Visible = true
            };
            mainPanel.Controls.Add(rightPanel);

            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                Padding = new Padding(20)
            };
            mainPanel.Controls.Add(leftPanel);

            SetupChart(leftPanel);
            SetupControls();
            LoadData();
        }

        private void SetupChart(Panel parent)
        {
            chart = new Chart { Dock = DockStyle.Fill, BackColor = Color.Black };
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

            lblStats = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(20, 20, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Padding = new Padding(10, 0, 0, 0),
                Text = ""
            };
            parent.Controls.Add(lblStats);
            parent.Controls.SetChildIndex(lblStats, 0);

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

            btnAdd = new Button
            {
                Text = File.Exists("add.png") ? "" : "+",
                Location = new Point(20, yPos),
                Size = new Size(40, 40),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Cursor = Cursors.Hand,
                BackgroundImageLayout = ImageLayout.Stretch
            };
            if (File.Exists("add.png"))
                btnAdd.BackgroundImage = Image.FromFile("add.png");
            btnAdd.Click += BtnAdd_Click;
            rightPanel.Controls.Add(btnAdd);

            // Filter button (image)
            btnFilter = new Button
            {
                Size = new Size(40, 40),
                Location = new Point(70, yPos),
                BackgroundImageLayout = ImageLayout.Stretch,
                Cursor = Cursors.Hand
            };
            if (File.Exists("filter.png"))
                btnFilter.BackgroundImage = Image.FromFile("filter.png");
            rightPanel.Controls.Add(btnFilter);

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

            // Sorting ComboBox
            cmbSort = new ComboBox
            {
                Location = new Point(20, yPos),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbSort.Items.AddRange(new[] { "A → Z", "Z → A", "Montante", "Chute" });
            cmbSort.SelectedIndex = 0;
            cmbSort.SelectedIndexChanged += (s, e) => RefreshBrandList();
            rightPanel.Controls.Add(cmbSort);

            yPos += 40;

            var addSection = new Panel
            {
                Location = new Point(20, yPos),
                Size = new Size(260, 100),
                BackColor = Color.FromArgb(20, 20, 20),
                BorderStyle = BorderStyle.None
            };
            rightPanel.Controls.Add(addSection);

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

            btnDelete = new Button
            {
                Text = File.Exists("remove.png") ? "" : "×",
                Location = new Point(140, 30),
                Size = new Size(30, 25),
                BackColor = Color.FromArgb(215, 58, 73),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand,
                BackgroundImageLayout = ImageLayout.Stretch
            };
            if (File.Exists("remove.png"))
                btnDelete.BackgroundImage = Image.FromFile("remove.png");
            btnDelete.Click += BtnDelete_Click;
            addSection.Controls.Add(btnDelete);
        }

        private async void LoadData()
        {
            brands.Clear();
            brandListPanel.Controls.Clear();

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
                var symbols = brandMap.Values.ToArray();
                var brandData = await apiService.GetMultipleStocksDataAsync(symbols);

                if (brandData.Count > 0)
                {
                    foreach (var data in brandData)
                    {
                        if (data.DataPoints.Count > 0)
                        {
                            var prices = data.DataPoints.Select(dp => dp.Price).ToArray();
                            string displayName = brandMap.FirstOrDefault(x => x.Value == data.Name).Key ?? data.Name;
                            AddBrandSeries(displayName, prices);
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

            var status = ComputeStatus(values);
            var brandItem = new BrandItem
            {
                Name = name,
                Values = values,
                Color = seriesColor,
                Status = status,
                IsVisible = true,
                DisplayText = name
            };

            brands.RemoveAll(b => b.Name == name);
            brands.Add(brandItem);
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

            IEnumerable<BrandItem> sorted = brands;
            string selectedSort = cmbSort.SelectedItem?.ToString() ?? "A → Z";

            switch (selectedSort)
            {
                case "A → Z":
                    sorted = brands.OrderBy(b => b.Name);
                    break;
                case "Z → A":
                    sorted = brands.OrderByDescending(b => b.Name);
                    break;
                case "Montante":
                    sorted = brands.OrderByDescending(b => b.Values.Last() - b.Values.First());
                    break;
                case "Chute":
                    sorted = brands.OrderBy(b => b.Values.Last() - b.Values.First());
                    break;
            }

            int yPos = 10;
            foreach (var brand in sorted)
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

            var statusCircle = new Panel
            {
                Location = new Point(10, 14),
                Size = new Size(8, 8),
                BackColor = GetStatusColor(brand.Status),
                BorderStyle = BorderStyle.None
            };
            panel.Controls.Add(statusCircle);

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

            panel.Click += (s, e) => SelectBrand(brand);
            nameLabel.Click += (s, e) => SelectBrand(brand);

            return panel;
        }

        private Color GetStatusColor(string status)
        {
            switch (status)
            {
                case "positive":
                    return Color.FromArgb(0, 200, 0);
                case "negative":
                    return Color.FromArgb(200, 0, 0);
                default:
                    return Color.FromArgb(150, 150, 150);
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
            VleLine(brand.Name);
        }

        private async void BtnAdd_Click(object sender, EventArgs e)
        {
            txtValues.Visible = true;
            txtValues.Focus();
            txtValues.Text = "";
            txtValues.BringToFront();
        }

        private async void AddBrandFromTextBox(string input)
        {
            try
            {
                string symbole = input;
                string displayName = input;

                if (brandMap.ContainsKey(input))
                {
                    symbole = brandMap[input];
                    displayName = input;
                }

                var data = await apiService.GetStockDataAsync(symbole);
                if (data.Count > 0)
                {
                    var prices = data.Select(dp => dp.Price).ToArray();
                    AddBrandSeries(displayName, prices);
                }
                else
                {
                    var parsed = ParseValues(txtValues.Text);
                    if (parsed.Length > 0)
                        AddBrandSeries(displayName, parsed);
                }

                RefreshBrandList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des données pour {input}: {ex.Message}");
            }
        }

        private void VleLine(string line)
        {
            var series = chart.Series.FindByName(line);
            if (series == null || series.Points.Count == 0)
            {
                lblStats.Text = "";
                return;
            }

            var values = series.Points.Select(p => p.YValues[0]).ToList();
            double min = values.Min();
            double max = values.Max();
            double avg = values.Average();

            lblStats.Text = $"Min: {min:F2}   Max: {max:F2}   Avg: {avg:F2}";
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            var name = (txtValues.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name)) return;

            brands.RemoveAll(b => b.Name == name);

            var series = chart.Series.FindByName(name);
            if (series != null)
            {
                chart.Series.Remove(series);
            }

            RefreshBrandList();
            txtValues.Text = "";
        }

        private static double[] ParseValues(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return Array.Empty<double>();
            var parts = input.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<double>();
            foreach (var p in parts)
            {
                if (double.TryParse(p.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double v))
                {
                    list.Add(v);
                }
            }
            return list.ToArray();
        }
    }
}
