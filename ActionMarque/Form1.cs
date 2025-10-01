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
        private Label lblStats;
        private Button btnFilter;
        private int currentSortIndex = 0; // Index du tri actuel (0=A→Z, 1=Z→A, 2=Montante, 3=Chute)

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
        private double currentZoomLevel = 1.0; // Niveau de zoom actuel (1.0 = vue complète)
        private DataGranularity currentGranularity = DataGranularity.Yearly; // Granularité actuelle des données
        private bool _isZooming = false; // Protection contre les appels multiples
        private bool _isReloading = false; // Protection contre les rechargements multiples
        private DateTime _lastZoomTime = DateTime.MinValue; // Dernier zoom pour éviter les appels trop rapides
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
                Width = 200, // Largeur légèrement augmentée pour un meilleur espacement
                BackColor = Color.FromArgb(28, 28, 30), // Couleur plus moderne (gris foncé)
                Padding = new Padding(12),
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
            
            // Ajuster la position et la taille du graphique pour éviter le débordement
            chartArea.Position.X = 5; // Marge gauche
            chartArea.Position.Y = 5; // Marge haute
            chartArea.Position.Width = 85; // 85% de la largeur disponible (réduire pour éviter le débordement)
            chartArea.Position.Height = 90; // 90% de la hauteur disponible

            // Configuration de l'axe X avec affichage des mois et années
            chartArea.AxisX.MajorGrid.Enabled = false;
            chartArea.AxisX.LabelStyle.ForeColor = Color.White;
            chartArea.AxisX.LineColor = Color.White;
            chartArea.AxisX.Title = "Période (24/09/2020 - 23/09/2025)";
            chartArea.AxisX.TitleForeColor = Color.White;
            chartArea.AxisX.IntervalType = DateTimeIntervalType.Months;
            chartArea.AxisX.Interval = 1; // Afficher chaque mois
            chartArea.AxisX.LabelStyle.Format = "MMM"; // Format court des mois (Jan, Fév, Mar, etc.)
            chartArea.AxisX.LabelStyle.Angle = -45; // Incliner les labels pour éviter le chevauchement

            chartArea.AxisY.MajorGrid.LineColor = Color.Gray;
            chartArea.AxisY.LabelStyle.ForeColor = Color.White;
            chartArea.AxisY.LineColor = Color.White;
            chartArea.AxisY.Title = "Prix ($)";
            chartArea.AxisY.TitleForeColor = Color.White;
            chartArea.AxisY.LabelStyle.Format = "#,0";
            
            // Définir les limites de l'axe X (copiée du test qui fonctionne)
            var minDate = new DateTime(2020, 9, 24);
            var maxDate = new DateTime(2025, 9, 23);
            chartArea.AxisX.Minimum = minDate.ToOADate();
            chartArea.AxisX.Maximum = maxDate.ToOADate();
            
            // Forcer l'affichage de toute la plage
            chartArea.AxisX.ScaleView.Zoomable = false;
            chartArea.AxisY.ScaleView.Zoomable = false;
            
            // Ajouter des marges pour éviter le débordement
            chartArea.InnerPlotPosition.X = 10; // Marge gauche
            chartArea.InnerPlotPosition.Y = 10; // Marge haute
            chartArea.InnerPlotPosition.Width = 80; // Largeur réduite
            chartArea.InnerPlotPosition.Height = 80; // Hauteur réduite
            
            // Configuration simple de l'axe X avec mois et années
            SetupSimpleAxisLabels(chartArea);

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
            
            // Ajouter le zoom à la molette de souris
            chart.MouseWheel += Chart_MouseWheel;
        }

        private void SetupControls()
        {
            int yPos = 20;

            var titleLabel = new Label
            {
                Text = "MARQUES",
                ForeColor = Color.FromArgb(0, 122, 255), // Bleu moderne
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(15, yPos),
                AutoSize = true
            };
            rightPanel.Controls.Add(titleLabel);

            yPos += 45;

            btnAdd = new Button
            {
                Text = File.Exists("add.png") ? "" : "+",
                Location = new Point(15, yPos),
                Size = new Size(42, 42),
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Cursor = Cursors.Hand,
                BackgroundImageLayout = ImageLayout.Stretch
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.FlatAppearance.MouseOverBackColor = Color.FromArgb(10, 132, 255);
            btnAdd.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 102, 204);
            if (File.Exists("add.png"))
                btnAdd.BackgroundImage = Image.FromFile("add.png");
            btnAdd.Click += BtnAdd_Click;
            rightPanel.Controls.Add(btnAdd);

            // Filter button (image)
            btnFilter = new Button
            {
                Size = new Size(42, 42),
                Location = new Point(65, yPos),
                BackgroundImageLayout = ImageLayout.Stretch,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(52, 52, 54),
                FlatStyle = FlatStyle.Flat
            };
            btnFilter.FlatAppearance.BorderSize = 0;
            btnFilter.FlatAppearance.MouseOverBackColor = Color.FromArgb(72, 72, 74);
            btnFilter.FlatAppearance.MouseDownBackColor = Color.FromArgb(42, 42, 44);
            if (File.Exists("filter.png"))
                btnFilter.BackgroundImage = Image.FromFile("filter.png");
            btnFilter.Click += BtnFilter_Click;
            rightPanel.Controls.Add(btnFilter);

            // Bouton de réinitialisation du zoom
            var btnResetZoom = new Button
            {
                Text = "🔍",
                Size = new Size(42, 42),
                Location = new Point(115, yPos),
                BackColor = Color.FromArgb(52, 52, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btnResetZoom.FlatAppearance.BorderSize = 0;
            btnResetZoom.FlatAppearance.MouseOverBackColor = Color.FromArgb(72, 72, 74);
            btnResetZoom.FlatAppearance.MouseDownBackColor = Color.FromArgb(42, 42, 44);
            btnResetZoom.Click += (s, e) => ResetZoom();
            rightPanel.Controls.Add(btnResetZoom);
            
            // Ajouter un ToolTip séparé
            var toolTip = new ToolTip();
            toolTip.SetToolTip(btnResetZoom, "Réinitialiser le zoom");

            yPos += 50;

            // Label pour afficher le niveau de zoom actuel
            var lblGranularity = new Label
            {
                Text = "Vue complète",
                Location = new Point(15, yPos),
                Size = new Size(170, 18),
                ForeColor = Color.FromArgb(142, 142, 147),
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                Name = "lblGranularity"
            };
            rightPanel.Controls.Add(lblGranularity);

            yPos += 25;

            brandListPanel = new Panel
            {
                Location = new Point(10, yPos),
                Size = new Size(178, 380), // Augmenter la hauteur
                BackColor = Color.FromArgb(44, 44, 46), // Fond légèrement plus clair que le panel parent
                AutoScroll = true,
                BorderStyle = BorderStyle.None
            };
            rightPanel.Controls.Add(brandListPanel);

            yPos += 380;

            var addSection = new Panel
            {
                Location = new Point(10, yPos),
                Size = new Size(178, 90),
                BackColor = Color.FromArgb(44, 44, 46),
                BorderStyle = BorderStyle.None
            };
            rightPanel.Controls.Add(addSection);

            var lblValues = new Label
            {
                Text = "Symbole boursier",
                ForeColor = Color.FromArgb(142, 142, 147),
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(10, 8),
                AutoSize = true
            };
            addSection.Controls.Add(lblValues);

            txtValues = new TextBox
            {
                Location = new Point(10, 30),
                Size = new Size(130, 28),
                BackColor = Color.FromArgb(58, 58, 60),
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
                Location = new Point(145, 30),
                Size = new Size(28, 28),
                BackColor = Color.FromArgb(255, 59, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Cursor = Cursors.Hand,
                BackgroundImageLayout = ImageLayout.Stretch
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 79, 68);
            btnDelete.FlatAppearance.MouseDownBackColor = Color.FromArgb(235, 39, 28);
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
                            AddBrandSeriesWithDates(displayName, prices, data.DataPoints);
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
            AddBrandSeriesWithDates(name, values, null, fixedColor);
        }
        
        private void AddBrandSeriesWithDates(string name, double[] values, List<StockDataPoint> dataPoints = null, Color? fixedColor = null)
        {
            var seriesColor = fixedColor ?? colorPalette[colorIndex++ % colorPalette.Count];
            
            try
        {
            var existing = chart.Series.FindByName(name);
            if (existing != null)
            {
                chart.Series.Remove(existing);
            }
                
            var s = new Series(name)
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 3,
                Color = seriesColor,
                ChartArea = "main",
                XValueType = ChartValueType.DateTime,
                Enabled = true
            };

                if (dataPoints != null && dataPoints.Count > 0)
                {
                    // Utiliser la logique simple qui fonctionne du test
                    foreach (var point in dataPoints)
                    {
                        s.Points.AddXY(point.Date, point.Price);
                        System.Diagnostics.Debug.WriteLine($"Point ajouté: {point.Date:dd/MM/yyyy} = {point.Price:F2}");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Série {name} ajoutée avec {s.Points.Count} points (dates réelles)");
                }
                else
                {
                    // Fallback : utiliser les dates par défaut (années)
            for (int i = 0; i < values.Length; i++)
            {
                        var pointDate = new DateTime(baseYear + i, 1, 1);
                            s.Points.AddXY(pointDate, values[i]);
                    }
                    System.Diagnostics.Debug.WriteLine($"Série {name} ajoutée avec {s.Points.Count} points (dates par défaut)");
                }
                
                // Ajouter la série
            chart.Series.Add(s);
                
                // Forcer la mise à jour (copiée du test qui fonctionne)
                chart.Invalidate();
                chart.Refresh();
                
                // S'assurer que l'axe X affiche toute la plage
                var chartArea = chart.ChartAreas[0];
                var minDate = new DateTime(2020, 9, 24);
                var maxDate = new DateTime(2025, 9, 23);
                chartArea.AxisX.Minimum = minDate.ToOADate();
                chartArea.AxisX.Maximum = maxDate.ToOADate();
                chartArea.AxisX.ScaleView.ZoomReset();
                
                // Reconfigurer les labels personnalisés
                SetupSimpleAxisLabels(chartArea);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans AddBrandSeries pour {name}: {ex.Message}");
            }

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
            
            // Pour les données réelles d'actions, comparons les valeurs récentes
            // Si on a plus de 10 points, comparons les 25% les plus récents avec les 25% les plus anciens
            if (values.Count >= 4)
            {
                int quarterSize = Math.Max(1, values.Count / 4);
                
                // Moyenne du premier quart (anciennes valeurs)
                double oldAvg = 0;
                for (int i = 0; i < quarterSize; i++)
                {
                    oldAvg += values[i];
                }
                oldAvg /= quarterSize;
                
                // Moyenne du dernier quart (valeurs récentes)
                double newAvg = 0;
                for (int i = values.Count - quarterSize; i < values.Count; i++)
                {
                    newAvg += values[i];
                }
                newAvg /= quarterSize;
                
                var diff = newAvg - oldAvg;
                var percentChange = Math.Abs(diff) / oldAvg * 100;
                
                // Seuil de 5% pour éviter les changements trop petits
                if (percentChange > 5)
                {
                    if (diff > 0) return "positive";
                    if (diff < 0) return "negative";
                }
            }
            else
            {
                // Pour peu de données, comparaison simple
            var diff = values[values.Count - 1] - values[0];
                if (Math.Abs(diff) > 0.01) // Seuil minimal
                {
            if (diff > 0) return "positive";
            if (diff < 0) return "negative";
                }
            }
            
            return "neutral";
        }

        private void RefreshBrandList()
        {
            brandListPanel.Controls.Clear();

            IEnumerable<BrandItem> sorted = brands;

            switch (currentSortIndex)
            {
                case 0: // A → Z
                    sorted = brands.OrderBy(b => b.Name);
                    break;
                case 1: // Z → A
                    sorted = brands.OrderByDescending(b => b.Name);
                    break;
                case 2: // Montante
                    sorted = brands.OrderByDescending(b => b.Values.Last() - b.Values.First());
                    break;
                case 3: // Chute
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
                Location = new Point(5, yPos),
                Size = new Size(165, 42),
                BackColor = Color.FromArgb(58, 58, 60),
                BorderStyle = BorderStyle.None,
                Cursor = Cursors.Hand
            };

            // Effet hover
            panel.MouseEnter += (s, e) => panel.BackColor = Color.FromArgb(72, 72, 74);
            panel.MouseLeave += (s, e) => panel.BackColor = Color.FromArgb(58, 58, 60);

            var statusCircle = new Panel
            {
                Location = new Point(12, 16),
                Size = new Size(10, 10),
                BackColor = GetStatusColor(brand.Status),
                BorderStyle = BorderStyle.None
            };
            panel.Controls.Add(statusCircle);

            var nameLabel = new Label
            {
                Text = brand.Name,
                Location = new Point(30, 12),
                Size = new Size(95, 18),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                BackColor = Color.Transparent
            };
            nameLabel.MouseEnter += (s, e) => panel.BackColor = Color.FromArgb(72, 72, 74);
            nameLabel.MouseLeave += (s, e) => panel.BackColor = Color.FromArgb(58, 58, 60);
            panel.Controls.Add(nameLabel);

            var checkBox = new CheckBox
            {
                Location = new Point(135, 13),
                Size = new Size(18, 18),
                Checked = brand.IsVisible,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(0, 122, 255)
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
                    return Color.FromArgb(52, 199, 89); // Vert moderne iOS
                case "negative":
                    return Color.FromArgb(255, 59, 48); // Rouge moderne iOS
                default:
                    return Color.FromArgb(142, 142, 147); // Gris moderne iOS
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

        private bool _filtersVisible = false;
        private Panel _filterPanel = null;

        private void BtnFilter_Click(object sender, EventArgs e)
        {
            if (_filtersVisible)
            {
                // Masquer les filtres
                if (_filterPanel != null)
                {
                    rightPanel.Controls.Remove(_filterPanel);
                    _filterPanel.Dispose();
                    _filterPanel = null;
                }
                _filtersVisible = false;
            }
            else
            {
                // Afficher les filtres
                ShowFilterOptions();
                _filtersVisible = true;
            }
        }

        private void ShowFilterOptions()
        {
            if (_filterPanel != null) return;

            _filterPanel = new Panel
            {
                Size = new Size(178, 310),
                Location = new Point(10, 190),
                BackColor = Color.FromArgb(58, 58, 60),
                BorderStyle = BorderStyle.None,
                AutoScroll = true
            };

            int yPos = 10;

            var lblTitle = new Label
            {
                Text = "FILTRES",
                Location = new Point(12, yPos),
                Size = new Size(155, 20),
                ForeColor = Color.FromArgb(0, 122, 255),
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            _filterPanel.Controls.Add(lblTitle);
            yPos += 35;

            // Section Plage d'années
            var lblYearRange = new Label
            {
                Text = "Période",
                Location = new Point(12, yPos),
                Size = new Size(155, 18),
                ForeColor = Color.FromArgb(142, 142, 147),
                Font = new Font("Segoe UI", 8, FontStyle.Regular)
            };
            _filterPanel.Controls.Add(lblYearRange);
            yPos += 22;

            // Année de début
            var lblFrom = new Label
            {
                Text = "De",
                Location = new Point(12, yPos),
                Size = new Size(25, 18),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };
            _filterPanel.Controls.Add(lblFrom);

            var numYearFrom = new NumericUpDown
            {
                Location = new Point(38, yPos - 2),
                Size = new Size(55, 22),
                Minimum = 2020,
                Maximum = 2025,
                Value = 2020,
                BackColor = Color.FromArgb(44, 44, 46),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9)
            };
            _filterPanel.Controls.Add(numYearFrom);

            var lblTo = new Label
            {
                Text = "à",
                Location = new Point(98, yPos),
                Size = new Size(15, 18),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };
            _filterPanel.Controls.Add(lblTo);

            var numYearTo = new NumericUpDown
            {
                Location = new Point(115, yPos - 2),
                Size = new Size(55, 22),
                Minimum = 2020,
                Maximum = 2025,
                Value = 2025,
                BackColor = Color.FromArgb(44, 44, 46),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9)
            };
            _filterPanel.Controls.Add(numYearTo);
            yPos += 28;

            // Bouton appliquer filtre années
            var btnApplyYearFilter = new Button
            {
                Text = "Appliquer",
                Location = new Point(12, yPos),
                Size = new Size(155, 28),
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };
            btnApplyYearFilter.FlatAppearance.BorderSize = 0;
            btnApplyYearFilter.FlatAppearance.MouseOverBackColor = Color.FromArgb(10, 132, 255);
            btnApplyYearFilter.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 102, 204);
            btnApplyYearFilter.Click += (s, e) => ApplyYearFilter((int)numYearFrom.Value, (int)numYearTo.Value);
            _filterPanel.Controls.Add(btnApplyYearFilter);
            yPos += 38;

            // Section Tri
            var lblSort = new Label
            {
                Text = "Tri",
                Location = new Point(12, yPos),
                Size = new Size(155, 18),
                ForeColor = Color.FromArgb(142, 142, 147),
                Font = new Font("Segoe UI", 8, FontStyle.Regular)
            };
            _filterPanel.Controls.Add(lblSort);
            yPos += 22;

            // ComboBox de tri dans le filtre
            var cmbFilterSort = new ComboBox
            {
                Location = new Point(12, yPos),
                Size = new Size(155, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(44, 44, 46),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            cmbFilterSort.Items.AddRange(new[] { "A → Z", "Z → A", "Montante", "Chute" });
            cmbFilterSort.SelectedIndex = currentSortIndex;
            cmbFilterSort.SelectedIndexChanged += (s, e) => {
                currentSortIndex = cmbFilterSort.SelectedIndex;
                RefreshBrandList();
            };
            _filterPanel.Controls.Add(cmbFilterSort);
            yPos += 35;

            // Section Affichage
            var lblDisplay = new Label
            {
                Text = "Affichage",
                Location = new Point(12, yPos),
                Size = new Size(155, 18),
                ForeColor = Color.FromArgb(142, 142, 147),
                Font = new Font("Segoe UI", 8, FontStyle.Regular)
            };
            _filterPanel.Controls.Add(lblDisplay);
            yPos += 22;

            // Bouton pour masquer toutes les marques
            var btnHideAll = new Button
            {
                Text = "Masquer tout",
                Location = new Point(12, yPos),
                Size = new Size(75, 28),
                BackColor = Color.FromArgb(72, 72, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8)
            };
            btnHideAll.FlatAppearance.BorderSize = 0;
            btnHideAll.FlatAppearance.MouseOverBackColor = Color.FromArgb(92, 92, 94);
            btnHideAll.Click += (s, e) => HideAllBrands();
            _filterPanel.Controls.Add(btnHideAll);

            // Bouton pour afficher toutes les marques
            var btnShowAll = new Button
            {
                Text = "Afficher tout",
                Location = new Point(92, yPos),
                Size = new Size(75, 28),
                BackColor = Color.FromArgb(72, 72, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8)
            };
            btnShowAll.FlatAppearance.BorderSize = 0;
            btnShowAll.FlatAppearance.MouseOverBackColor = Color.FromArgb(92, 92, 94);
            btnShowAll.Click += (s, e) => ShowAllBrands();
            _filterPanel.Controls.Add(btnShowAll);
            yPos += 35;

            // Bouton fermer
            var btnClose = new Button
            {
                Text = "Fermer",
                Location = new Point(12, yPos),
                Size = new Size(155, 28),
                BackColor = Color.FromArgb(255, 59, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 79, 68);
            btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(235, 39, 28);
            btnClose.Click += (s, e) => {
                rightPanel.Controls.Remove(_filterPanel);
                _filterPanel.Dispose();
                _filterPanel = null;
                _filtersVisible = false;
            };
            _filterPanel.Controls.Add(btnClose);

            rightPanel.Controls.Add(_filterPanel);
            _filterPanel.BringToFront();
        }

        private void ToggleAllBrands(bool showAll)
        {
            foreach (var brand in brands)
            {
                brand.IsVisible = showAll;
                var series = chart.Series.FindByName(brand.Name);
                if (series != null)
                {
                    series.Enabled = showAll;
                }
            }
            RefreshBrandList();
        }

        private void HideAllBrands()
        {
            foreach (var brand in brands)
            {
                brand.IsVisible = false;
                var series = chart.Series.FindByName(brand.Name);
                if (series != null)
                {
                    series.Enabled = false;
                }
            }
            RefreshBrandList();
        }

        private void ShowAllBrands()
        {
            foreach (var brand in brands)
            {
                brand.IsVisible = true;
                var series = chart.Series.FindByName(brand.Name);
                if (series != null)
                {
                    series.Enabled = true;
                }
            }
            RefreshBrandList();
        }

        private void ApplyYearFilter(int yearFrom, int yearTo)
        {
            try
            {
                var chartArea = chart.ChartAreas[0];
                
                // Convertir les années en dates OADate
                var minDate = new DateTime(yearFrom, 1, 1);
                var maxDate = new DateTime(yearTo, 12, 31);
                
                // Appliquer le filtre de zoom sur la plage d'années
                chartArea.AxisX.ScaleView.Zoom(minDate.ToOADate(), maxDate.ToOADate());
                
                // Mettre à jour le titre
                var yearRange = yearFrom == yearTo ? yearFrom.ToString() : $"{yearFrom}-{yearTo}";
                UpdateChartTitle($"Vente Marques - Période: {yearRange}");
                
                // Forcer la mise à jour
                chart.Invalidate();
                chart.Refresh();
                
                System.Diagnostics.Debug.WriteLine($"Filtre appliqué: {yearFrom} à {yearTo}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'application du filtre: {ex.Message}");
                MessageBox.Show($"Erreur lors de l'application du filtre: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private bool _isAddingBrand = false;

        private async void AddBrandFromTextBox(string input)
        {
            // Éviter les ajouts multiples simultanés
            if (_isAddingBrand)
            {
                System.Diagnostics.Debug.WriteLine($"AddBrandFromTextBox ignoré pour {input} - ajout en cours");
                return;
            }
            
            _isAddingBrand = true;
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"Début ajout marque: {input}");
                
                string symbole = input;
                string displayName = input;

                if (brandMap.ContainsKey(input))
                {
                    symbole = brandMap[input];
                    displayName = input;
                }

                System.Diagnostics.Debug.WriteLine($"Recherche données pour symbole: {symbole}");
                var data = await apiService.GetStockDataAsync(symbole);
                System.Diagnostics.Debug.WriteLine($"Données reçues: {data.Count} points");
                
                if (data.Count > 0)
                {
                    // Ajouter le symbole au dictionnaire pour le rechargement futur
                    if (!brandMap.ContainsKey(displayName))
                    {
                        brandMap[displayName] = symbole;
                        System.Diagnostics.Debug.WriteLine($"Symbole ajouté au dictionnaire: {displayName} -> {symbole}");
                    }
                    
                    // Ajouter directement au graphique avec les vraies données de l'API
                    var prices = data.Select(dp => dp.Price).ToArray();
                    AddBrandSeriesWithDates(displayName, prices, data);
                    
                    
                    System.Diagnostics.Debug.WriteLine($"Marque ajoutée: {displayName} avec {prices.Length} points");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Aucune donnée API, utilisation des valeurs manuelles");
                    var parsed = ParseValues(input);
                    if (parsed.Length > 0)
                    {
                        AddBrandSeries(displayName, parsed);
                        brands.Add(new BrandItem { Name = displayName, Values = parsed });
                        
                    }
                }

                RefreshBrandList();
                
                
                System.Diagnostics.Debug.WriteLine($"Ajout marque terminé: {input}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur ajout marque {input}: {ex.Message}");
                MessageBox.Show($"Erreur lors du chargement des données pour {input}: {ex.Message}");
            }
            finally
            {
                _isAddingBrand = false;
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

            // Supprimer directement de la liste et du graphique
            brands.RemoveAll(b => b.Name == name);

            var seriesToRemove = chart.Series.Cast<Series>().Where(s => s.Name == name).ToList();
            foreach (var series in seriesToRemove)
            {
                chart.Series.Remove(series);
            }
            
            System.Diagnostics.Debug.WriteLine($"Marque supprimée: {name}");

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

        private bool _isResetting = false;
        
        /// <summary>
        /// Gestionnaire du zoom avec la molette de souris avec changement de granularité
        /// </summary>
        private async void Chart_MouseWheel(object sender, MouseEventArgs e)
        {
            // Éviter les appels multiples simultanés
            if (_isZooming) 
            {
                System.Diagnostics.Debug.WriteLine("Zoom ignoré - déjà en cours");
                return;
            }
            
            _isZooming = true;
            
            try
            {
                var chartArea = chart.ChartAreas[0];
                
                // Calculer le facteur de zoom (positif = zoom in, négatif = zoom out)
                double zoomFactor = e.Delta > 0 ? 0.8 : 1.25; // 20% de zoom in/out
                
                // Mettre à jour le niveau de zoom
                currentZoomLevel *= zoomFactor;
                
                // Limiter le zoom (entre 0.5x et 5x pour éviter les bugs)
                currentZoomLevel = Math.Max(0.5, Math.Min(5.0, currentZoomLevel));
                
                // Déterminer la granularité selon le niveau de zoom
                DataGranularity newGranularity = DetermineGranularity(currentZoomLevel);
                
                // Si la granularité a changé, recharger les données
                if (newGranularity != currentGranularity)
                {
                    System.Diagnostics.Debug.WriteLine($"Changement de granularité: {currentGranularity} → {newGranularity}");
                    currentGranularity = newGranularity;
                    
                    // Recharger toutes les marques avec la nouvelle granularité
                    await ReloadAllBrandsWithNewGranularity();
                }
                
                // Appliquer le zoom
                if (currentZoomLevel <= 1.0)
                {
                    // Zoom out ou vue complète - afficher toute la plage
                    chartArea.AxisX.ScaleView.ZoomReset();
                    chartArea.AxisY.ScaleView.ZoomReset();
                }
                else
                {
                    // Zoom in - calculer la nouvelle plage de vue
                    double totalRange = chartArea.AxisX.Maximum - chartArea.AxisX.Minimum;
                    double newRange = totalRange / currentZoomLevel;
                    double center = (chartArea.AxisX.Maximum + chartArea.AxisX.Minimum) / 2;
                    double newMin = center - newRange / 2;
                    double newMax = center + newRange / 2;
                    
                    // S'assurer que nous restons dans les limites
                    if (newMin < chartArea.AxisX.Minimum)
                    {
                        newMax += (chartArea.AxisX.Minimum - newMin);
                        newMin = chartArea.AxisX.Minimum;
                    }
                    if (newMax > chartArea.AxisX.Maximum)
                    {
                        newMin -= (newMax - chartArea.AxisX.Maximum);
                        newMax = chartArea.AxisX.Maximum;
                    }
                    
                    // Appliquer le zoom
                    chartArea.AxisX.ScaleView.Zoom(newMin, newMax);
                }
                
                // Ajuster le formatage de l'axe X selon la granularité
                AdjustAxisFormattingForGranularity(newGranularity);
                
                // Forcer la mise à jour
                chart.Invalidate();
                chart.Refresh();
                
                // Mettre à jour l'affichage du zoom
                UpdateZoomDisplay();
                
                System.Diagnostics.Debug.WriteLine($"Zoom molette: facteur={zoomFactor:F2}, niveau={currentZoomLevel:F2}, granularité={currentGranularity}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur zoom molette: {ex.Message}");
            }
            finally
            {
                _isZooming = false;
            }
        }
        
        /// <summary>
        /// Applique un niveau de zoom spécifique au graphique avec rechargement adaptatif des données
        /// Système stable : Années (1.0x) → Mois (1.1x+)
        /// </summary>
        private async void ApplyZoomLevel(double zoomLevel)
        {
            await ApplyZoomLevelInternal(zoomLevel, null);
        }
        
        /// <summary>
        /// Applique un niveau de zoom sur le point visé par la souris (comme ScottPlot)
        /// </summary>
        private async void ApplyZoomLevelAtCursor(double zoomLevel, Point cursorLocation)
        {
            await ApplyZoomLevelInternal(zoomLevel, cursorLocation);
        }
        
        /// <summary>
        /// Logique interne de zoom avec support du curseur
        /// </summary>
        private async Task ApplyZoomLevelInternal(double zoomLevel, Point? cursorLocation)
        {
            try
            {
                var chartArea = chart.ChartAreas[0];
                
                // Déterminer la granularité appropriée selon le niveau de zoom
                DataGranularity newGranularity = DetermineGranularity(zoomLevel);
                
                // Si la granularité a changé, recharger les données
                if (newGranularity != currentGranularity)
                {
                    System.Diagnostics.Debug.WriteLine($"Changement de granularité: {currentGranularity} → {newGranularity}");
                    currentGranularity = newGranularity;
                    
                    // Attendre que le rechargement se termine
                    await ReloadAllBrandsWithNewGranularity();
                    
                    // Attendre un peu pour s'assurer que tout est chargé
                    await Task.Delay(100);
                    
                    System.Diagnostics.Debug.WriteLine($"Rechargement terminé, granularité actuelle: {currentGranularity}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Granularité inchangée: {currentGranularity}");
                }
                
                // TOUJOURS définir les limites de l'axe X en premier selon les règles spécifiées
                var minDate = new DateTime(2020, 9, 24); // 24/09/2020 pour le début
                var maxDate = new DateTime(2025, 9, 23); // 23/09/2025 pour la fin
                
                chartArea.AxisX.Minimum = minDate.ToOADate();
                chartArea.AxisX.Maximum = maxDate.ToOADate();
                
                if (zoomLevel <= 1.0)
                {
                    // Zoom out ou vue complète - afficher toute la plage
                    try
                    {
                        chartArea.AxisX.ScaleView.ZoomReset();
                        chartArea.AxisY.ScaleView.ZoomReset();
                        
                        // Forcer le recalcul des axes
                        chart.Invalidate();
                        
                        System.Diagnostics.Debug.WriteLine($"Zoom reset appliqué - limites: {minDate:yyyy} à {maxDate:yyyy}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erreur lors du zoom reset: {ex.Message}");
                    }
                }
                else
                {
                    // Zoom in - calculer la nouvelle plage de vue
                    try
                    {
                        double totalRange = chartArea.AxisX.Maximum - chartArea.AxisX.Minimum;
                        double newRange = totalRange / zoomLevel;
                        
                        double center;
                        if (cursorLocation.HasValue)
                        {
                            // Zoom sur le point visé par la souris (comme ScottPlot)
                            double cursorX = cursorLocation.Value.X;
                            
                            // Convertir la position X de la souris en coordonnées du graphique
                            double chartLeft = chartArea.Position.X;
                            double chartWidth = chartArea.Position.Width;
                            double relativeX = (cursorX - chartLeft) / chartWidth;
                            
                            // S'assurer que relativeX est dans la plage [0, 1]
                            relativeX = Math.Max(0, Math.Min(1, relativeX));
                            
                            center = chartArea.AxisX.Minimum + (totalRange * relativeX);
                            System.Diagnostics.Debug.WriteLine($"Zoom sur curseur: X={cursorX}, chartLeft={chartLeft}, chartWidth={chartWidth}, relative={relativeX:F2}, center={center:F2}");
                        }
                        else
                        {
                            // Zoom sur le centre du graphique
                            center = (chartArea.AxisX.Maximum + chartArea.AxisX.Minimum) / 2;
                            System.Diagnostics.Debug.WriteLine($"Zoom sur centre: {center:F2}");
                        }
                        
                        double newMin = center - newRange / 2;
                        double newMax = center + newRange / 2;
                        
                        // S'assurer que nous restons dans les limites
                        if (newMin < chartArea.AxisX.Minimum)
                        {
                            newMax += (chartArea.AxisX.Minimum - newMin);
                            newMin = chartArea.AxisX.Minimum;
                        }
                        if (newMax > chartArea.AxisX.Maximum)
                        {
                            newMin -= (newMax - chartArea.AxisX.Maximum);
                            newMax = chartArea.AxisX.Maximum;
                        }
                        
                        // Appliquer le zoom
                        chartArea.AxisX.ScaleView.Zoom(newMin, newMax);
                        System.Diagnostics.Debug.WriteLine($"Zoom appliqué: {newMin:F2} à {newMax:F2}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erreur lors du zoom: {ex.Message}");
                    }
                }
                
                // Ajuster le formatage de l'axe X selon la granularité
                AdjustAxisFormattingForGranularity(newGranularity);
                
                // Mettre à jour le titre avec le niveau de zoom et la granularité
                string granularityText = GetGranularityDisplayName(newGranularity);
                UpdateChartTitle($"Vente Marques - Zoom {currentZoomLevel:F1}x ({granularityText})");
                
                // Mettre à jour le label de zoom
                UpdateZoomDisplay();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur ApplyZoomLevel: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Met à jour le titre du graphique
        /// </summary>
        private void UpdateChartTitle(string newTitle)
        {
            try
            {
                if (chart.Titles.Count > 0)
                {
                    chart.Titles[0].Text = newTitle;
                }
                else
                {
                    chart.Titles.Add(newTitle);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur UpdateChartTitle: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gestionnaire pour prévenir les crashes lors du slide
        /// </summary>
        private void Chart_AxisViewChanged(object sender, ViewEventArgs e)
        {
            try
            {
                var chartArea = chart.ChartAreas[0];
                var viewMin = chartArea.AxisX.ScaleView.ViewMinimum;
                var viewMax = chartArea.AxisX.ScaleView.ViewMaximum;
                
                // Vérifier si les valeurs sont dans les limites raisonnables
                if (viewMin < 0 || viewMax < viewMin || 
                    (viewMax - viewMin) < 1 ||
                    viewMin < chartArea.AxisX.Minimum ||
                    viewMax > chartArea.AxisX.Maximum)
                {
                    System.Diagnostics.Debug.WriteLine($"Vue invalide détectée: Min={viewMin}, Max={viewMax} - réinitialisation");
                    
                    // Réinitialiser la vue à des valeurs sûres
                    chartArea.AxisX.ScaleView.ZoomReset();
                    chartArea.AxisY.ScaleView.ZoomReset();
                    chart.Invalidate();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans Chart_AxisViewChanged: {ex.Message}");
                // En cas d'erreur, réinitialiser complètement la vue
                try
                {
                    var chartArea = chart.ChartAreas[0];
                    chartArea.AxisX.ScaleView.ZoomReset();
                    chartArea.AxisY.ScaleView.ZoomReset();
                    chart.Invalidate();
                }
                catch
                {
                    // Ignorer les erreurs de récupération
                }
            }
        }
        
        /// <summary>
        /// Réinitialise le zoom du graphique à la vue complète (version simplifiée)
        /// </summary>
        private void ResetZoom()
        {
            // Éviter les appels multiples simultanés
            if (_isResetting) 
            {
                System.Diagnostics.Debug.WriteLine("ResetZoom ignoré - déjà en cours");
                return;
            }
            
            _isResetting = true;
            
            try
            {
                System.Diagnostics.Debug.WriteLine("ResetZoom appelé");
                
                var chartArea = chart.ChartAreas[0];
                
                // Définir les limites de l'axe X selon les règles spécifiées
                var minDate = new DateTime(2020, 9, 24); // 24/09/2020 pour le début
                var maxDate = new DateTime(2025, 9, 23); // 23/09/2025 pour la fin
                
                chartArea.AxisX.Minimum = minDate.ToOADate();
                chartArea.AxisX.Maximum = maxDate.ToOADate();
                
                // Réinitialiser complètement la vue
                chartArea.AxisX.ScaleView.ZoomReset();
                chartArea.AxisY.ScaleView.ZoomReset();
                
                // Forcer le recalcul des axes
                chart.Invalidate();
                chart.Refresh();
                
                // Reconfigurer les labels personnalisés
                SetupSimpleAxisLabels(chartArea);
                
                // Réinitialiser le niveau de zoom à 1.0 (vue complète)
                currentZoomLevel = 1.0;
                currentGranularity = DataGranularity.Yearly; // Retour aux données annuelles
                
                // Mettre à jour l'affichage du zoom
                UpdateZoomDisplay();
                
                System.Diagnostics.Debug.WriteLine("ResetZoom terminé");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la réinitialisation du zoom: {ex.Message}");
            }
            finally
            {
                _isResetting = false;
            }
        }

        /// <summary>
        /// Détermine la granularité appropriée selon le niveau de zoom
        /// </summary>
        private DataGranularity DetermineGranularity(double zoomLevel)
        {
            if (zoomLevel <= 1.0)
            {
                return DataGranularity.Yearly; // Vue complète = données annuelles
            }
            else if (zoomLevel > 1.0 && zoomLevel <= 2.0)
            {
                return DataGranularity.Monthly; // Zoom moyen = données mensuelles
            }
            else
            {
                return DataGranularity.Daily; // Zoom fort = données journalières
            }
        }
        
        /// <summary>
        /// Obtient le nom d'affichage de la granularité
        /// </summary>
        private string GetGranularityDisplayName(DataGranularity granularity)
        {
            switch (granularity)
            {
                case DataGranularity.Yearly: return "Années";
                case DataGranularity.Monthly: return "Mois";
                case DataGranularity.Daily: return "Jours";
                case DataGranularity.Weekly: return "Semaines";
                case DataGranularity.Intraday: return "Heures";
                default: return "Données";
            }
        }
        
        /// <summary>
        /// Recharge toutes les marques avec la nouvelle granularité
        /// </summary>
        private async Task ReloadAllBrandsWithNewGranularity()
        {
            // Éviter les rechargements multiples simultanés
            if (_isReloading)
            {
                System.Diagnostics.Debug.WriteLine("Rechargement ignoré - déjà en cours");
                return;
            }
            
            _isReloading = true;
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"Rechargement de {brands.Count} marques avec granularité {currentGranularity}");
                
                // Sauvegarder la liste des marques actuelles AVANT de vider
                var brandsToReload = brands.ToList();
                System.Diagnostics.Debug.WriteLine($"Marques à recharger: {string.Join(", ", brandsToReload.Select(b => b.Name))}");
                
                // Vider le graphique et la liste
                chart.Series.Clear();
                brands.Clear();
                
                // Attendre un peu pour s'assurer que le nettoyage est terminé
                await Task.Delay(50);
                
                // Recharger chaque marque avec la nouvelle granularité
                foreach (var brand in brandsToReload)
                {
                    System.Diagnostics.Debug.WriteLine($"Traitement de la marque: {brand.Name}");
                    System.Diagnostics.Debug.WriteLine($"brandMap contient {brand.Name}: {brandMap.ContainsKey(brand.Name)}");
                    
                    if (brandMap.ContainsKey(brand.Name))
                    {
                        // Marque avec symbole API - recharger depuis l'API
                        var symbol = brandMap[brand.Name];
                        System.Diagnostics.Debug.WriteLine($"Rechargement API de {brand.Name} ({symbol}) avec granularité {currentGranularity}");
                        
                        try
                        {
                            // Limiter à 1 an pour les données mensuelles pour éviter la surcharge
                            int yearsToLoad = (currentGranularity == DataGranularity.Monthly) ? 1 : 5;
                            var data = await apiService.GetStockDataAsync(symbol, yearsToLoad, currentGranularity);
                            System.Diagnostics.Debug.WriteLine($"Données reçues pour {brand.Name}: {data?.Count ?? 0} points");
                            
                            if (data != null && data.Count > 0)
                            {
                                var prices = data.Select(dp => dp.Price).ToArray();
                                
                                // Vérifier qu'il n'y a pas déjà une série avec ce nom
                                var existingSeries = chart.Series.FindByName(brand.Name);
                                if (existingSeries != null)
                                {
                                    chart.Series.Remove(existingSeries);
                                    System.Diagnostics.Debug.WriteLine($"Série existante {brand.Name} supprimée");
                                }
                                
                                // Passer les vraies dates des données pour un affichage correct
                                AddBrandSeriesWithDates(brand.Name, prices, data, brand.Color);
                                
                                // Recréer le BrandItem
                                var status = ComputeStatus(prices);
                                var brandItem = new BrandItem
                                {
                                    Name = brand.Name,
                                    Values = prices,
                                    Color = brand.Color, // Garder la même couleur
                                    Status = status,
                                    IsVisible = true,
                                    DisplayText = brand.Name
                                };
                                
                                brands.Add(brandItem);
                                System.Diagnostics.Debug.WriteLine($"Marque {brand.Name} rechargée avec {prices.Length} points");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"Aucune donnée reçue pour {brand.Name}, conservation des anciennes données");
                                AddBrandSeries(brand.Name, brand.Values, brand.Color);
                                brands.Add(brand);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Erreur lors du rechargement de {brand.Name}: {ex.Message}");
                            // En cas d'erreur, garder les anciennes données
                            AddBrandSeries(brand.Name, brand.Values, brand.Color);
                            brands.Add(brand);
                        }
                    }
                    else
                    {
                        // Marque manuelle - garder les données existantes mais ajuster l'affichage
                        System.Diagnostics.Debug.WriteLine($"Marque manuelle {brand.Name} - conservation des données existantes");
                        
                        AddBrandSeries(brand.Name, brand.Values, brand.Color);
                        brands.Add(brand);
                    }
                }
                
                // Nettoyer les doublons dans la liste des marques
                CleanDuplicateBrands();
                
                // Mettre à jour l'affichage
                RefreshBrandList();
                System.Diagnostics.Debug.WriteLine($"Rechargement terminé: {brands.Count} marques");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du rechargement: {ex.Message}");
            }
            finally
            {
                _isReloading = false;
            }
        }
        
        /// <summary>
        /// Ajuste le formatage de l'axe X selon la granularité
        /// </summary>
        private void AdjustAxisFormattingForGranularity(DataGranularity granularity)
        {
            try
            {
                var chartArea = chart.ChartAreas[0];
                var axisX = chartArea.AxisX;
                
                switch (granularity)
                {
                    case DataGranularity.Yearly:
                        // Configuration simple pour afficher les années uniquement
                        axisX.LabelStyle.Format = "yyyy";
                        axisX.Interval = 1;
                        axisX.IntervalType = DateTimeIntervalType.Years;
                        axisX.LabelStyle.Angle = 0;
                        axisX.CustomLabels.Clear(); // Nettoyer les labels personnalisés
                        
                        // Forcer l'affichage de toutes les années
                        axisX.IntervalOffset = 0;
                        axisX.MajorGrid.Enabled = true;
                        axisX.MinorGrid.Enabled = false;
                        System.Diagnostics.Debug.WriteLine("Configuration YEARLY appliquée");
                        break;
                        
                    case DataGranularity.Monthly:
                        // Configuration pour afficher les mois par tranches de 12 mois (années)
                        axisX.LabelStyle.Format = "MMM yyyy"; // Format mois + année
                        axisX.Interval = 12; // 12 mois = 1 année
                        axisX.IntervalType = DateTimeIntervalType.Months;
                        axisX.LabelStyle.Angle = -45;
                        axisX.IntervalOffset = 0;
                        axisX.CustomLabels.Clear();
                        
                        // Forcer l'affichage des mois par tranches de 12 mois
                        axisX.MajorGrid.Enabled = true;
                        axisX.MinorGrid.Enabled = false;
                        
                        // Ajuster l'espacement des labels
                        axisX.LabelStyle.Interval = 0; // Pas d'intervalle supplémentaire
                        axisX.LabelStyle.IntervalOffset = 0;
                        
                        System.Diagnostics.Debug.WriteLine("Configuration MONTHLY appliquée - Interval: " + axisX.Interval + " mois, Type: " + axisX.IntervalType);
                        break;
                        
                    case DataGranularity.Daily:
                        // Configuration simple pour afficher les mois
                        axisX.LabelStyle.Format = "MMM yyyy";
                        axisX.Interval = 1;
                        axisX.IntervalType = DateTimeIntervalType.Months;
                        axisX.LabelStyle.Angle = -45;
                        axisX.IntervalOffset = 0;
                        axisX.CustomLabels.Clear();
                        
                        // Forcer l'affichage des mois
                        axisX.MajorGrid.Enabled = true;
                        axisX.MinorGrid.Enabled = false;
                        
                        // S'assurer que les labels sont activés
                        axisX.LabelStyle.Enabled = true;
                        
                        // Forcer la mise à jour de l'axe
                        axisX.Interval = 1;
                        axisX.IntervalType = DateTimeIntervalType.Months;
                        
                        System.Diagnostics.Debug.WriteLine("Configuration DAILY appliquée - Mois simples");
                        break;
                        
                    default:
                        axisX.LabelStyle.Format = "dd/MM/yyyy";
                        axisX.Interval = 1;
                        axisX.IntervalType = DateTimeIntervalType.Days;
                        axisX.LabelStyle.Angle = 0;
                        break;
                }
                
                // Forcer le recalcul de l'axe
                chart.Invalidate();
                chart.Refresh();
                
                System.Diagnostics.Debug.WriteLine($"Formatage de l'axe X ajusté pour: {granularity}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'ajustement du formatage: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Nettoie les doublons dans la liste des marques
        /// </summary>
        private void CleanDuplicateBrands()
        {
            try
            {
                var originalCount = brands.Count;
                
                // Supprimer les doublons basés sur le nom
                var uniqueBrands = brands
                    .GroupBy(b => b.Name)
                    .Select(g => g.First()) // Prendre le premier de chaque groupe
                    .ToList();
                
                brands.Clear();
                brands.AddRange(uniqueBrands);
                
                if (originalCount != brands.Count)
                {
                    System.Diagnostics.Debug.WriteLine($"Doublons supprimés: {originalCount} → {brands.Count} marques");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du nettoyage des doublons: {ex.Message}");
            }
        }
        
        
        /// <summary>
        /// Obtient la plage de dates visible sur le graphique
        /// </summary>
        private (DateTime Start, DateTime End)? GetVisibleDateRange(ChartArea chartArea)
        {
            try
            {
                var axisX = chartArea.AxisX;
                
                // Si on est en vue complète (pas de zoom)
                if (!axisX.ScaleView.IsZoomed)
                {
                    return (DateTime.FromOADate(axisX.Minimum), DateTime.FromOADate(axisX.Maximum));
                }
                
                // Si on est zoomé, utiliser la plage de zoom
                return (DateTime.FromOADate(axisX.ScaleView.ViewMinimum), DateTime.FromOADate(axisX.ScaleView.ViewMaximum));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'obtention de la plage visible: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Configure l'axe X de manière simple avec années uniquement en vue complète
        /// </summary>
        private void SetupSimpleAxisLabels(ChartArea chartArea)
        {
            try
            {
                // Configuration pour afficher les années uniquement en vue complète
                chartArea.AxisX.Interval = 1;
                chartArea.AxisX.IntervalType = DateTimeIntervalType.Years;
                chartArea.AxisX.LabelStyle.Format = "yyyy"; // Format année uniquement
                chartArea.AxisX.LabelStyle.Angle = 0; // Pas d'angle pour les années
                chartArea.AxisX.LabelStyle.Enabled = true;
                chartArea.AxisX.IntervalOffset = 0;
                
                // S'assurer que l'axe affiche toutes les années
                chartArea.AxisX.Minimum = new DateTime(2020, 1, 1).ToOADate();
                chartArea.AxisX.Maximum = new DateTime(2025, 12, 31).ToOADate();
                
                System.Diagnostics.Debug.WriteLine("Configuration simple de l'axe X appliquée (années uniquement)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la configuration de l'axe X: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Configure les labels personnalisés de l'axe X avec mois et années
        /// </summary>
        private void SetupCustomAxisLabels(ChartArea chartArea)
        {
            try
            {
                // Nettoyer les labels existants
                chartArea.AxisX.CustomLabels.Clear();
                
                // Configuration de base pour afficher les mois
                chartArea.AxisX.Interval = 1;
                chartArea.AxisX.IntervalType = DateTimeIntervalType.Months;
                chartArea.AxisX.LabelStyle.Format = "MMM";
                chartArea.AxisX.LabelStyle.Angle = -45;
                chartArea.AxisX.LabelStyle.Enabled = true; // S'assurer que les labels sont activés
                
                // Ajouter des labels personnalisés pour les années (en plus des mois)
                var years = new[] { 2020, 2021, 2022, 2023, 2024, 2025 };
                
                foreach (var year in years)
                {
                    // Label pour le 1er janvier de chaque année
                    var yearStart = new DateTime(year, 1, 1);
                    var customLabel = new CustomLabel();
                    customLabel.FromPosition = yearStart.ToOADate() - 15; // Étendre un peu la zone
                    customLabel.ToPosition = yearStart.ToOADate() + 15;
                    customLabel.Text = year.ToString();
                    customLabel.LabelMark = LabelMarkStyle.LineSideMark;
                    customLabel.ForeColor = Color.Yellow; // Couleur différente pour les années
                    customLabel.RowIndex = 1; // Mettre les années sur une ligne séparée
                    chartArea.AxisX.CustomLabels.Add(customLabel);
                }
                
                System.Diagnostics.Debug.WriteLine("Labels personnalisés configurés pour l'axe X");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la configuration des labels: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Met à jour l'affichage du niveau de zoom actuel avec granularité
        /// </summary>
        private void UpdateZoomDisplay()
        {
            try
            {
                var lblGranularity = rightPanel.Controls.Find("lblGranularity", false).FirstOrDefault() as Label;
                if (lblGranularity != null)
                {
                    string granularityText = GetGranularityDisplayName(currentGranularity);
                    string zoomText;
                    if (currentZoomLevel <= 1.0)
                    {
                        zoomText = "Vue complète";
                    }
                    else
                    {
                        zoomText = $"Zoom {currentZoomLevel:F1}x • {granularityText}";
                    }
                    lblGranularity.Text = zoomText;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la mise à jour du zoom: {ex.Message}");
            }
        }
    }
}
