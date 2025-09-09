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
        private Dictionary<string, List<double>> brandNameToValues = new Dictionary<string, List<double>>();
        private Dictionary<string, Color> brandStatusColor = new Dictionary<string, Color>();
        private readonly List<DateTime> years = new List<DateTime>
        {
            new DateTime(2019,1,1),
            new DateTime(2020,1,1),
            new DateTime(2021,1,1),
            new DateTime(2022,1,1),
            new DateTime(2023,1,1)
        };
        private readonly Random random = new Random();

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

            // En-tête avec boutons + / -
            var headerPanel = new Panel
            {
                Location = new Point(0, yPos),
                Size = new Size(260, 36),
                BackColor = Color.Black
            };
            rightPanel.Controls.Add(headerPanel);

            var titleLabel = new Label
            {
                Text = "Marques",
                ForeColor = Color.White,
                Font = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(0, 8),
                AutoSize = true
            };
            headerPanel.Controls.Add(titleLabel);

            var btnAdd = new Button
            {
                Text = "+",
                ForeColor = Color.Black,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(28, 28),
                Location = new Point(170, 4)
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += (s, e) => OnAddBrand();
            headerPanel.Controls.Add(btnAdd);

            var btnRemove = new Button
            {
                Text = "-",
                ForeColor = Color.Black,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(28, 28),
                Location = new Point(204, 4)
            };
            btnRemove.FlatAppearance.BorderSize = 0;
            btnRemove.Click += (s, e) => OnRemoveSelectedBrands();
            headerPanel.Controls.Add(btnRemove);

            yPos += 50;

            // Liste des marques (cochable, owner-draw pour point coloré)
            brandList = new CheckedListBox
            {
                Location = new Point(0, yPos),
                Size = new Size(260, 250),
                BackColor = Color.Black,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Arial", 10),
                CheckOnClick = true,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 22
            };
            brandList.DrawItem += BrandList_DrawItem;
            brandList.ItemCheck += BrandList_ItemCheck;
            rightPanel.Controls.Add(brandList);

            yPos += 270;

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
                Location = new Point(90, yPos),
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
                Location = new Point(90, yPos + spacing),
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
                Location = new Point(90, yPos + spacing * 2),
                AutoSize = true
            };
            rightPanel.Controls.Add(lblAverageValue);
        }

        private void LoadData()
        {
            // Préparer les données de base pour plusieurs marques
            brandNameToValues["Apple"] = new List<double> { 150, 200, 130, 180, 190 };
            brandNameToValues["Tesla"] = new List<double> { 60, 200, 400, 200, 250 };
            brandNameToValues["Nike"] = new List<double> { 80, 100, 120, 110, 130 };
            brandNameToValues["Samsung"] = GenerateRandomValues(80, 220, drift: 10);
            brandNameToValues["Celio"] = GenerateRandomValues(20, 90, drift: -2);
            brandNameToValues["Pull & Bear"] = GenerateRandomValues(30, 120, drift: 3);
            brandNameToValues["Ferrari"] = GenerateRandomValues(100, 400, drift: 15);
            brandNameToValues["Rolex"] = GenerateRandomValues(200, 500, drift: 8);
            brandNameToValues["Coca Cola"] = GenerateRandomValues(60, 140, drift: 1);
            brandNameToValues["Pepsi"] = GenerateRandomValues(55, 135, drift: 1);

            // Mettre à jour les couleurs de statut en fonction de la tendance
            foreach (var kvp in brandNameToValues)
            {
                brandStatusColor[kvp.Key] = GetStatusColor(kvp.Value);
            }

            // Remplir la liste: premières marques cochées (affichées)
            AddBrandToList("Apple", isChecked: true);
            AddBrandToList("Tesla", isChecked: true);
            AddBrandToList("Nike", isChecked: true);

            // Les autres non cochées par défaut
            var others = new[] { "Samsung", "Celio", "Pull & Bear", "Ferrari", "Rolex", "Coca Cola", "Pepsi" };
            foreach (var b in others)
            {
                AddBrandToList(b, isChecked: false);
            }

            // Dessiner les séries selon les cases cochées
            RenderChartFromCheckedBrands();
        }

        private void AddBrandToList(string brand, bool isChecked)
        {
            if (!brandNameToValues.ContainsKey(brand)) return;
            int index = brandList.Items.Add(brand, isChecked);
            // Stocker la couleur de statut si manquante
            if (!brandStatusColor.ContainsKey(brand))
            {
                brandStatusColor[brand] = GetStatusColor(brandNameToValues[brand]);
            }
        }

        private List<double> GenerateRandomValues(int min, int max, int drift)
        {
            var list = new List<double>();
            double value = random.Next(min, max);
            for (int i = 0; i < years.Count; i++)
            {
                // petite dérive et bruit
                value += drift + random.Next(-15, 16);
                value = Math.Max(min, Math.Min(max, value));
                list.Add(Math.Round(value, 0));
            }
            return list;
        }

        private Color GetStatusColor(List<double> values)
        {
            if (values == null || values.Count < 2) return Color.Gray;
            double last = values[values.Count - 1];
            double prev = values[values.Count - 2];
            if (last > prev) return Color.LimeGreen;
            if (last < prev) return Color.Red;
            return Color.Gray;
        }

        private void RenderChartFromCheckedBrands()
        {
            chart.Series.Clear();

            // Palette de couleurs de ligne
            var lineColors = new[] { Color.DodgerBlue, Color.Orange, Color.LimeGreen, Color.MediumVioletRed, Color.Gold, Color.MediumTurquoise, Color.Plum, Color.Tomato, Color.Sienna, Color.SkyBlue };
            int colorIndex = 0;

            var visibleValues = new List<double>();

            foreach (var item in brandList.CheckedItems)
            {
                string brand = item.ToString();
                if (!brandNameToValues.TryGetValue(brand, out var values)) continue;

                var series = new Series(brand)
                {
                    ChartType = SeriesChartType.Line,
                    BorderWidth = 3,
                    Color = lineColors[colorIndex % lineColors.Length],
                    ChartArea = "main",
                    XValueType = ChartValueType.DateTime
                };

                for (int i = 0; i < years.Count && i < values.Count; i++)
                {
                    series.Points.AddXY(years[i], values[i]);
                    visibleValues.Add(values[i]);
                }

                chart.Series.Add(series);
                colorIndex++;
            }

            // Statistiques basées sur les séries visibles
            if (visibleValues.Any())
            {
                lblMinValue.Text = visibleValues.Min().ToString("N0");
                lblMaxValue.Text = visibleValues.Max().ToString("N0");
                lblAverageValue.Text = visibleValues.Average().ToString("N0");
            }
            else
            {
                lblMinValue.Text = "-";
                lblMaxValue.Text = "-";
                lblAverageValue.Text = "-";
            }

            chart.ChartAreas["main"].RecalculateAxesScale();
            brandList.Invalidate();
        }

        private void BrandList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // Retarder le rendu jusqu'après la mise à jour de l'état
            this.BeginInvoke((Action)(() => RenderChartFromCheckedBrands()));
        }

        private void BrandList_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= brandList.Items.Count) return;

            e.DrawBackground();
            string brand = brandList.Items[e.Index].ToString();
            bool isChecked = brandList.GetItemChecked(e.Index);

            // Couleurs
            Color textColor = Color.White;
            Color statusColor = brandStatusColor.ContainsKey(brand) ? brandStatusColor[brand] : Color.Gray;

            // Dessiner point coloré
            int dotSize = 10;
            var dotRect = new Rectangle(e.Bounds.X + 4, e.Bounds.Y + (e.Bounds.Height - dotSize) / 2, dotSize, dotSize);
            using (var brush = new SolidBrush(statusColor))
            {
                e.Graphics.FillEllipse(brush, dotRect);
            }

            // Dessiner le nom
            using (var brushText = new SolidBrush(textColor))
            {
                e.Graphics.DrawString(brand, e.Font, brushText, e.Bounds.X + 20, e.Bounds.Y + 3);
            }

            // Indication visuelle pour élément coché
            if (isChecked)
            {
                using (var pen = new Pen(Color.FromArgb(80, 255, 255, 255)))
                {
                    e.Graphics.DrawRectangle(pen, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
                }
            }

            e.DrawFocusRectangle();
        }

        private void OnAddBrand()
        {
            string name = PromptForText("Nom de la marque:", "Ajouter une marque");
            if (string.IsNullOrWhiteSpace(name)) return;

            if (brandNameToValues.ContainsKey(name))
            {
                MessageBox.Show("Cette marque existe déjà.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Générer des valeurs plausibles
            var values = GenerateRandomValues(50, 300, drift: random.Next(-5, 12));
            brandNameToValues[name] = values;
            brandStatusColor[name] = GetStatusColor(values);

            int idx = brandList.Items.Add(name, true);
            brandList.SelectedIndex = idx;
            RenderChartFromCheckedBrands();
        }

        private void OnRemoveSelectedBrands()
        {
            var indices = brandList.SelectedIndices;
            if (indices == null || indices.Count == 0)
            {
                MessageBox.Show("Sélectionnez au moins une marque à supprimer.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Supprimer en partant de la fin
            for (int i = indices.Count - 1; i >= 0; i--)
            {
                int idx = indices[i];
                string brand = brandList.Items[idx].ToString();
                brandList.Items.RemoveAt(idx);
                brandNameToValues.Remove(brand);
                brandStatusColor.Remove(brand);
            }

            RenderChartFromCheckedBrands();
        }

        private string PromptForText(string text, string caption)
        {
            var form = new Form
            {
                Width = 360,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White
            };

            var lbl = new Label { Left = 10, Top = 15, Text = text, AutoSize = true };
            var box = new TextBox { Left = 10, Top = 40, Width = 320 };
            var ok = new Button { Text = "OK", Left = 170, Width = 70, Top = 70, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "Annuler", Left = 250, Width = 80, Top = 70, DialogResult = DialogResult.Cancel };
            form.Controls.Add(lbl);
            form.Controls.Add(box);
            form.Controls.Add(ok);
            form.Controls.Add(cancel);
            form.AcceptButton = ok;
            form.CancelButton = cancel;

            return form.ShowDialog(this) == DialogResult.OK ? box.Text?.Trim() : null;
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