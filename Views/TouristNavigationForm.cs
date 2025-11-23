using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SmartCamping.Views
{
    public partial class TouristNavigationForm : Form
    {
        private PictureBox _mapBox = null!;
        private Label _infoLabel = null!;
        private Bitmap _mapImage = null!;
        private Bitmap _mapWithPins = null!;

        // προκαθορισμένα σημεία
        private readonly Dictionary<string, (float u, float v, string msg)> POIS =
            new()
            {
                { " Σκηνές", (0.27f, 0.34f,
                    "Περιοχή κατασκήνωσης · ήσυχη ζώνη με σκιά και κοντά στις υποδομές.\nΙδανικό σημείο για οικογένειες και χαλάρωση.") },
                { " Reception", (0.18f, 0.48f,
                    "Υποδοχή/Πληροφορίες · εδώ κάνεις check-in, βρίσκεις χάρτες και ρωτάς για τις δραστηριότητες.\nΑνοιχτή όλη την ημέρα με προσωπικό πρόθυμο να βοηθήσει.") },
                { " WC / Ντους", (0.32f, 0.42f,
                    "WC/Ντουζ · καθαροί χώροι υγιεινής. \nΒρίσκονται κεντρικά ώστε να είναι εύκολα προσβάσιμοι από όλες τις ζώνες.") },
                { " Μονοπάτια", (0.62f, 0.28f,
                    "Μονοπάτια πεζοπορίας · ξεκινούν από το δάσος και καταλήγουν σε σημεία με θέα.\nΚατάλληλα για περίπατο, φωτογραφίες και άσκηση στη φύση.") },
                { " Μπαρ", (0.78f, 0.70f,
                    "Beach bar · δροσερά ποτά, σνακ και μουσική το βράδυ.\nΣυχνά διοργανώνονται θεματικά πάρτι με ζωντανή μουσική.") },
                { "🏖Παραλία", (0.50f, 0.82f,
                    "Παραλία · οργανωμένος χώρος με ξαπλώστρες και ομπρέλες.\nΙδανική για κολύμπι, ηλιοθεραπεία και θαλάσσια σπορ.") },
                { "🌊 Ποτάμι", (0.12f, 0.60f,
                    "Ποτάμι · ρέει κατά μήκος του κάμπινγκ, προσφέρει δροσιά και όμορφο τοπίο.\nΙδανικό για περίπατο στη φύση, παρατήρηση πουλιών και ξεκούραση.") }
            };

        public TouristNavigationForm()
        {
            Text = "Τουριστική Πλοήγηση";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1000, 700);
            Size = new Size(1100, 750);

            BuildUI();
            LoadMap();
        }

        private void BuildUI()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            Controls.Add(grid);

            //  Χάρτης 
            _mapBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.LightGray
            };
            grid.Controls.Add(_mapBox, 0, 0);

            
            var rightPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            rightPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // Κουμπιά
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Info Label
            grid.Controls.Add(rightPanel, 1, 0);

            var stack = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(5)
            };
            rightPanel.Controls.Add(stack, 0, 0);

            // Δυναμική δημιουργία κουμπιών
            foreach (var kv in POIS)
                stack.Controls.Add(MakeActionButton(kv.Key, kv.Value.msg, kv.Value.u, kv.Value.v));

            // Περιοχή πληροφοριών 
            _infoLabel = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11f),
                Text = "Επίλεξε κατηγορία για να εμφανιστεί σημείο στον χάρτη.",
                Padding = new Padding(8),
                AutoSize = false,
                TextAlign = ContentAlignment.TopLeft
            };
            _infoLabel.MaximumSize = new Size(0, 0); // χωρίς περιορισμ
            rightPanel.Controls.Add(_infoLabel, 0, 1);
        }

        private Button MakeActionButton(string text, string message, float u, float v)
        {
            var btn = new Button
            {
                Text = text,
                Width = 190,
                Height = 42,
                BackColor = Color.Teal,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 11f),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(3, 6, 3, 0)
            };
            btn.FlatAppearance.BorderSize = 0;

            btn.Click += (s, e) =>
            {
                _infoLabel.Text = message;
                DrawMarkerByPercent(u, v);
            };

            return btn;
        }

        private void DrawMarkerByPercent(float u, float v)
        {
            if (_mapImage == null) return;

            int px = (int)Math.Round(u * _mapImage.Width);
            int py = (int)Math.Round(v * _mapImage.Height);

            _mapWithPins = (Bitmap)_mapImage.Clone();

            using (Graphics g = Graphics.FromImage(_mapWithPins))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int size = 14;
                var rect = new Rectangle(px - size / 2, py - size / 2, size, size);

                g.FillEllipse(Brushes.Red, rect);
                g.DrawEllipse(Pens.Black, rect);
            }

            _mapBox.Image = _mapWithPins;
        }

        private void LoadMap()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", "camp-map.png");
            if (!File.Exists(path))
            {
                MessageBox.Show("Δεν βρέθηκε ο χάρτης: " + path);
                return;
            }

            try
            {
                _mapImage = new Bitmap(path);
                _mapBox.Image = (Bitmap)_mapImage.Clone();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Αποτυχία φόρτωσης χάρτη: " + ex.Message);
            }
        }
    }
}
