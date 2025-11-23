using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SmartCamping.Models;
using SmartCamping.Services;
using System.Drawing.Drawing2D;

namespace SmartCamping.Views
{
    public sealed partial class NavigationForm : Form
    {
        private readonly NavigationService _svc;
        private readonly MapCanvas _canvas;

        // UI
        private RadioButton _rbS1, _rbS2, _rbS3;
        private ComboBox _cbPref;
        private CheckBox _chkRoutes, _chkPins, _chkHazards;
        private Label _lblEta;
        private ListBox _lstGuidance;
        private Button _btnStart, _btnClose;

        // Θέμα
        private static readonly Color Teal = NavTheme.Teal;
        private static readonly Color TealDark = NavTheme.TealDark;
        private static readonly Color Olive = NavTheme.Olive;
        private static readonly Color GreyGreen = NavTheme.GreyGreen;
        private static readonly Color Cream = NavTheme.Cream;
        private static readonly Color Ink = NavTheme.Ink;

        public NavigationForm(NavigationService svc)
        {
            _svc = svc;

            Text = "Καταφύγιο & Πλοήγηση";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(980, 620);
            Size = new Size(1040, 660);
            Font = new Font("Segoe UI", 10f);
            BackColor = Color.White;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(14) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            Controls.Add(root);

            // Αριστερά: Χάρτης
            var grpMap = new GroupBox { Text = "Χάρτης", Dock = DockStyle.Fill, Padding = new Padding(8) };
            grpMap.ForeColor = Ink;
            _canvas = new MapCanvas(_svc);
            grpMap.Controls.Add(_canvas);
            root.Controls.Add(grpMap, 0, 0);

            // Δεξιά: Έλεγχος/Οδηγίες
            var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(4) };
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // κάρτα επιλογών
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // εκτίμηση
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // οδηγίες
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // κουμπιά
            root.Controls.Add(right, 1, 0);

            // Κάρτα επιλογών
            var card = new Panel { Dock = DockStyle.Top, Padding = new Padding(10, 8, 10, 8), BackColor = Cream, AutoSize = true };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(TealDark, 1);
                var r = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawRectangle(pen, r);
            };
            right.Controls.Add(card);

            var row1 = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 2, 0, 4) };
            _rbS1 = new RadioButton { Text = "S1  Forest Ridge", AutoSize = true, Font = new Font("Segoe UI", 9f), Checked = _svc.State.Destination.Id == "S1" };
            _rbS2 = new RadioButton { Text = "S2  North Gate", AutoSize = true, Font = new Font("Segoe UI", 9f), Checked = _svc.State.Destination.Id == "S2" };
            _rbS3 = new RadioButton { Text = "S3  East Dunes", AutoSize = true, Font = new Font("Segoe UI", 9f), Checked = _svc.State.Destination.Id == "S3" };
            row1.Controls.AddRange(new Control[] { _rbS1, _rbS2, _rbS3 });

            var row2 = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 2, 0, 4) };
            row2.Controls.Add(new Label { Text = "Προτίμηση:", AutoSize = true, Margin = new Padding(0, 4, 6, 0), Font = new Font("Segoe UI Semibold", 9f) });
            _cbPref = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, Font = new Font("Segoe UI", 9f) };
            _cbPref.Items.AddRange(Enum.GetNames(typeof(RoutePreference)));
            _cbPref.SelectedItem = _svc.State.Preference.ToString();
            row2.Controls.Add(_cbPref);

            var row3 = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 2, 0, 2) };
            _chkRoutes = new CheckBox { Text = "Διαδρομές", Checked = _svc.State.ShowRoutes, AutoSize = true, Font = new Font("Segoe UI", 9f) };
            _chkPins = new CheckBox { Text = "Καταφύγια", Checked = _svc.State.ShowPins, AutoSize = true, Font = new Font("Segoe UI", 9f) };
            _chkHazards = new CheckBox { Text = "Κίνδυνοι", Checked = _svc.State.ShowHazards, AutoSize = true, Font = new Font("Segoe UI", 9f) };
            row3.Controls.AddRange(new Control[] { _chkRoutes, _chkPins, _chkHazards });

            card.Controls.Add(row3);
            card.Controls.Add(row2);
            card.Controls.Add(row1);

            // Απόσταση/Χρόνος
            var etaPanel = new Panel { Dock = DockStyle.Top, Padding = new Padding(0, 6, 0, 6) };
            _lblEta = new Label { Text = "—", AutoSize = true, Font = new Font("Segoe UI Semibold", 10f), ForeColor = Ink, Dock = DockStyle.Left };
            etaPanel.Controls.Add(_lblEta);
            right.Controls.Add(etaPanel);

            // Λίστα οδηγιών
            _lstGuidance = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, Font = new Font("Segoe UI", 9f), ItemHeight = 17 };
            right.Controls.Add(_lstGuidance);

            // Κουμπιά
            var btns = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
            _btnClose = new Button { Text = "Κλείσιμο", AutoSize = true, DialogResult = DialogResult.OK, Font = new Font("Segoe UI", 9f) };
            _btnStart = new Button { Text = "Έναρξη Πλοήγησης", AutoSize = true, BackColor = Teal, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9f) };
            _btnStart.FlatAppearance.BorderSize = 0;
            btns.Controls.Add(_btnClose);
            btns.Controls.Add(_btnStart);
            right.Controls.Add(btns);

            // Events
            _rbS1.CheckedChanged += (s, e) => { if (_rbS1.Checked) _svc.SetDestination("S1", _canvas.MapPixelSize); };
            _rbS2.CheckedChanged += (s, e) => { if (_rbS2.Checked) _svc.SetDestination("S2", _canvas.MapPixelSize); };
            _rbS3.CheckedChanged += (s, e) => { if (_rbS3.Checked) _svc.SetDestination("S3", _canvas.MapPixelSize); };

            _cbPref.SelectedIndexChanged += (s, e) =>
            {
                if (_cbPref.SelectedItem is string name && Enum.TryParse<RoutePreference>(name, out var pref))
                    _svc.SetPreference(pref, _canvas.MapPixelSize);
            };

            _chkRoutes.CheckedChanged += (s, e) => _svc.ToggleOverlays(showRoutes: _chkRoutes.Checked);
            _chkPins.CheckedChanged += (s, e) => _svc.ToggleOverlays(showPins: _chkPins.Checked);
            _chkHazards.CheckedChanged += (s, e) => _svc.ToggleOverlays(showHazards: _chkHazards.Checked);

            _btnStart.Click += (s, e) =>
            {
                MessageBox.Show("Πλοήγηση ξεκίνησε!\n\n(Σενάριο προσομοίωσης: τα βήματα εμφανίζονται στη λίστα.)",
                                "Navigation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // Sync UI όταν αλλάζει το state
            _svc.StateChanged += (s, st) => BeginInvoke(new Action(() =>
            {
                _lblEta.Text =
                    $"Απόσταση ~ {st.EstimatedDistanceM / 1000.0:0.00} km   •   " +
                    $"Χρόνος ~ {st.EstimatedTime:mm\\:ss}";
                _lstGuidance.Items.Clear();
                _lstGuidance.Items.AddRange(st.Guidance);
                _canvas.Invalidate();
            }));

            // Πρώτο refresh
            _lblEta.Text = $"Απόσταση ~ {_svc.State.EstimatedDistanceM / 1000.0:0.00} km   •   " +
                           $"Χρόνος ~ {_svc.State.EstimatedTime:mm\\:ss}";
            _lstGuidance.Items.AddRange(_svc.State.Guidance);
        }

        private sealed class MapCanvas : Panel
        {
            private readonly NavigationService _svc;
            private Image? _baseMap, _overlayRoutes, _overlayPins, _overlayHazards, _fallbackAnnotated;

            public Size MapPixelSize => _baseMap?.Size ?? _fallbackAnnotated?.Size ?? new Size(1600, 1067);

            public MapCanvas(NavigationService svc)
            {
                _svc = svc;
                Dock = DockStyle.Fill;
                DoubleBuffered = true;
                BackColor = Color.Black;
                LoadImages();
                Resize += (s, e) => _svc.RescalePath(MapPixelSize);
            }

            private void LoadImages()
            {
                string assets = Path.Combine(Application.StartupPath, "Assets");
                try
                {
                    string baseMap = Path.Combine(assets, "camp-map.png");
                    if (File.Exists(baseMap)) _baseMap = Image.FromFile(baseMap);

                    string r = Path.Combine(assets, "overlay_routes_v3_1600.png");
                    string h = Path.Combine(assets, "overlay_hazards_v3_1600.png");
                    string p = Path.Combine(assets, "overlay_pins_v3_1600.png");
                    if (File.Exists(r)) _overlayRoutes = Image.FromFile(r);
                    if (File.Exists(h)) _overlayHazards = Image.FromFile(h);
                    if (File.Exists(p)) _overlayPins = Image.FromFile(p);

                    string fall1 = Path.Combine(assets, "camp-map_annotated_v3_1024.jpg");
                    string fall2 = Path.Combine(assets, "camp-map_nav_ready_fixS3_1024.jpg");
                    string chosen = File.Exists(fall1) ? fall1 : (File.Exists(fall2) ? fall2 : "");
                    if (!string.IsNullOrEmpty(chosen))
                        _fallbackAnnotated = Image.FromFile(chosen);
                }
                catch { }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                if (_baseMap != null)
                    DrawImageCover(g, _baseMap, ClientRectangle);
                else if (_fallbackAnnotated != null)
                    DrawImageCover(g, _fallbackAnnotated, ClientRectangle);

                if (_svc.State.ShowHazards && _overlayHazards != null)
                    DrawImageCover(g, _overlayHazards, ClientRectangle);
                if (_svc.State.ShowRoutes && _overlayRoutes != null)
                    DrawImageCover(g, _overlayRoutes, ClientRectangle);
                if (_svc.State.ShowPins && _overlayPins != null)
                    DrawImageCover(g, _overlayPins, ClientRectangle);

                if (_svc.State.ActivePathPixels.Count > 1)
                {
                    using var path = new GraphicsPath();
                    path.AddLines(ScalePathToClient(_svc.State.ActivePathPixels).ToArray());

                    using var glowPen = new Pen(Color.FromArgb(90, NavTheme.Teal), 18f)
                    { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
                    g.DrawPath(glowPen, path);

                    using var corePen = new Pen(NavTheme.Teal, 6f)
                    { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
                    g.DrawPath(corePen, path);
                }
            }

            private static void DrawImageCover(Graphics g, Image img, Rectangle dst)
            {
                float arImg = (float)img.Width / img.Height;
                float arDst = (float)dst.Width / dst.Height;

                Rectangle draw;
                if (arImg > arDst)
                {
                    int h = dst.Height;
                    int w = (int)(h * arImg);
                    int x = dst.X + (dst.Width - w) / 2;
                    draw = new Rectangle(x, dst.Y, w, h);
                }
                else
                {
                    int w = dst.Width;
                    int h = (int)(w / arImg);
                    int y = dst.Y + (dst.Height - h) / 2;
                    draw = new Rectangle(dst.X, y, w, h);
                }
                g.DrawImage(img, draw);
            }

            private System.Collections.Generic.List<PointF> ScalePathToClient(System.Collections.Generic.List<PointF> pxPath)
            {
                var map = MapPixelSize;
                float arImg = (float)map.Width / map.Height;
                float arDst = (float)Width / Height;

                Rectangle draw;
                if (arImg > arDst)
                {
                    int h = Height;
                    int w = (int)(h * arImg);
                    int x = (Width - w) / 2;
                    draw = new Rectangle(x, 0, w, h);
                }
                else
                {
                    int w = Width;
                    int h = (int)(w / arImg);
                    int y = (Height - h) / 2;
                    draw = new Rectangle(0, y, w, h);
                }

                float sx = (float)draw.Width / map.Width;
                float sy = (float)draw.Height / map.Height;

                return pxPath.Select(p => new PointF(draw.X + p.X * sx, draw.Y + p.Y * sy)).ToList();
            }
        }
    }
}
