using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SmartCamping.Models;

namespace SmartCamping.Views
{
    public partial class TarpPlacementForm : Form
    {
        private class Tarp
        {
            public PointF Center;
            public SizeF Size;
            public float AngleDeg;
            public bool Hit(Point p) => PointInRotatedRect(p, Center, Size, AngleDeg);
            public bool HitRotateHandle(Point p)
            {
                var h = RotateHandle(Center, Size, AngleDeg);
                return Distance(p, h) <= 10;
            }
        }

        private readonly List<Tarp> _tarps = new();
        private readonly float _windDirDeg;
        private readonly float _wind01;
        private Image? _bg;
        private Panel _canvas = null!;
        private Label _lblInfo = null!, _lblAdvice = null!, _lblScore = null!;
        private Button _btnDeploy = null!, _btnAuto = null!, _btnAdd = null!, _btnClear = null!;
        private bool _addMode = false;

        private Tarp? _active;
        private Point _dragStart;
        private PointF _startCenter;
        private bool _rotating;

        public TarpPlacementResult? Result { get; private set; }

        public TarpPlacementForm(float? windDirDeg = null, float? windStrength01 = null)
        {
            var rnd = new Random();
            _windDirDeg = windDirDeg ?? rnd.Next(0, 360);
            _wind01 = windStrength01 ?? (float)rnd.NextDouble();

            Text = "Προστατευτικά Πανιά – Τοποθέτηση";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1000, 680);
            Size = new Size(1100, 720);
            DoubleBuffered = true;

            BuildUI();
            LoadBackground();
            UpdatePanels();
        }

        private void BuildUI()
        {
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(grid);

            var header = new Panel { Dock = DockStyle.Fill, BackColor = ColorTranslator.FromHtml("#FFF3E0"), Padding = new Padding(12) };
            _lblInfo = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 12f), TextAlign = ContentAlignment.MiddleLeft };
            header.Controls.Add(_lblInfo);
            grid.Controls.Add(header, 0, 0);
            grid.SetColumnSpan(header, 2);
            _lblInfo.Text = $"Άνεμος: {(int)(_wind01 * 100)}%  |  Διεύθυνση: {_windDirDeg:0}°  •  Σύρε πανιά, στρίψε με τροχό/λαβή.";

            _canvas = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };
            _canvas.Paint += Canvas_Paint;
            _canvas.MouseDown += Canvas_MouseDown;
            _canvas.MouseMove += Canvas_MouseMove;
            _canvas.MouseUp += Canvas_MouseUp;
            _canvas.MouseWheel += Canvas_MouseWheel;
            grid.Controls.Add(_canvas, 0, 1);

            var right = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14) };
            grid.Controls.Add(right, 1, 1);

            _btnAdd = new Button { Text = "Προσθήκη πανιού", Height = 38, Dock = DockStyle.Top };
            _btnAdd.Click += (_, __) => { _addMode = true; Cursor = Cursors.Cross; };
            right.Controls.Add(_btnAdd);

            _btnAuto = new Button { Text = " Αυτόματη τοποθέτηση", Height = 38, Dock = DockStyle.Top, Margin = new Padding(0, 10, 0, 0) };
            _btnAuto.Click += (_, __) => { AutoPlace(); _canvas.Invalidate(); UpdatePanels(); };
            right.Controls.Add(_btnAuto);

            _btnClear = new Button { Text = " Καθαρισμός", Height = 38, Dock = DockStyle.Top, Margin = new Padding(0, 10, 0, 0) };
            _btnClear.Click += (_, __) => { _tarps.Clear(); _canvas.Invalidate(); UpdatePanels(); };
            right.Controls.Add(_btnClear);

            _lblScore = new Label { Text = "Σκορ κάλυψης: —", Height = 40, Dock = DockStyle.Top, Font = new Font("Segoe UI Semibold", 14f) };
            right.Controls.Add(_lblScore);

            _lblAdvice = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5f),
                Text = "Σύρε πανιά (drag & rotate). Δεξί κλικ πάνω στο πανί για διαγραφή.\nΤροχός ποντικιού για περιστροφή."
            };
            right.Controls.Add(_lblAdvice);

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 50, Padding = new Padding(0, 6, 0, 6) };
            _btnDeploy = new Button { Text = "Deploy", Width = 140, Height = 36 };
            var btnCancel = new Button { Text = "Άκυρο", Width = 120, Height = 36 };

            _btnDeploy.Click += (_, __) =>
            {
                Result = new TarpPlacementResult { WindDirDeg = _windDirDeg, WindStrength01 = _wind01, Advice = _lblAdvice.Text };
                foreach (var t in _tarps)
                    Result.Items.Add(new TarpPlacement { Center = t.Center, Size = t.Size, AngleDeg = t.AngleDeg });
                DialogResult = DialogResult.OK;
                Close();
            };
            btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

            bottom.Controls.Add(_btnDeploy);
            bottom.Controls.Add(btnCancel);
            right.Controls.Add(bottom);
        }

        private void LoadBackground()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", "tarp_image.jpeg");

            if (!File.Exists(path))
            {
                MessageBox.Show("Δεν βρήκα την εικόνα: " + path);
                return;
            }

            try
            {
                _bg = Image.FromFile(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Αποτυχία φόρτωσης εικόνας: " + ex.Message);
            }
        }

        //  Drawing 
        private void Canvas_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_bg != null)
            {
                var panelRect = _canvas.ClientRectangle;
                g.DrawImage(_bg, panelRect);
            }

            DrawWind(g);

            var tentRect = TentRect();
            using var tentPen = new Pen(Color.FromArgb(120, Color.DarkOliveGreen), 3);
            g.DrawRectangle(tentPen, tentRect.X, tentRect.Y, tentRect.Width, tentRect.Height);

            foreach (var t in _tarps) DrawTarp(g, t);
        }

        private void DrawWind(Graphics g)
        {
            var r = _canvas.ClientRectangle;
            var center = new PointF(r.Width - 140, 50);
            var len = 70f;
            float dirRad = Deg2Rad(_windDirDeg);
            var end = new PointF(center.X + (float)Math.Cos(dirRad) * len, center.Y + (float)Math.Sin(dirRad) * len);
            using var pen = new Pen(Color.FromArgb(200, 20, 120, 200), 5) { EndCap = LineCap.ArrowAnchor };
            g.DrawLine(pen, center, end);
            using var f = new Font("Segoe UI", 9f);
            g.DrawString("Άνεμος", f, Brushes.Black, center.X - 18, center.Y + 10);
        }

        private void DrawTarp(Graphics g, Tarp t)
        {
            var pts = RotRectPoints(t.Center, t.Size, t.AngleDeg);
            using var fill = new SolidBrush(Color.FromArgb(110, 30, 144, 255));
            using var pen = new Pen(Color.FromArgb(170, 20, 80, 200), 2);
            g.FillPolygon(fill, pts);
            g.DrawPolygon(pen, pts);

            var handle = RotateHandle(t.Center, t.Size, t.AngleDeg);
            using var hb = new SolidBrush(Color.FromArgb(220, 255, 215, 0));
            g.FillEllipse(hb, handle.X - 6, handle.Y - 6, 12, 12);
            g.DrawEllipse(Pens.Gray, handle.X - 6, handle.Y - 6, 12, 12);
        }

        //  Interaction 
        private void Canvas_MouseDown(object? sender, MouseEventArgs e)
        {
            if (_addMode)
            {
                _addMode = false; Cursor = Cursors.Default;
                float normal = (_windDirDeg + 90f) % 360f;
                _tarps.Add(new Tarp { Center = e.Location, Size = new SizeF(180, 80), AngleDeg = normal });
                _canvas.Invalidate(); UpdatePanels(); return;
            }

            if (e.Button == MouseButtons.Right)
            {
                var hit = _tarps.LastOrDefault(t => t.Hit(e.Location));
                if (hit != null) { _tarps.Remove(hit); _canvas.Invalidate(); UpdatePanels(); }
                return;
            }

            foreach (var t in _tarps.AsEnumerable().Reverse())
            {
                if (t.HitRotateHandle(e.Location)) { _active = t; _rotating = true; _dragStart = e.Location; break; }
                if (t.Hit(e.Location)) { _active = t; _rotating = false; _dragStart = e.Location; _startCenter = t.Center; break; }
            }
        }

        private void Canvas_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_active == null) return;

            if (_rotating)
            {
                var v = new PointF(e.X - _active.Center.X, e.Y - _active.Center.Y);
                _active.AngleDeg = (float)(Math.Atan2(v.Y, v.X) * 180.0 / Math.PI);
            }
            else
            {
                _active.Center = new PointF(_startCenter.X + (e.X - _dragStart.X), _startCenter.Y + (e.Y - _dragStart.Y));
            }

            _canvas.Invalidate(); UpdatePanels();
        }

        private void Canvas_MouseUp(object? sender, MouseEventArgs e) { _active = null; _rotating = false; }

        private void Canvas_MouseWheel(object? sender, MouseEventArgs e)
        {
            var hit = _tarps.LastOrDefault(t => t.Hit(e.Location));
            if (hit == null) return;
            hit.AngleDeg = (hit.AngleDeg + (e.Delta > 0 ? 5 : -5)) % 360f;
            _canvas.Invalidate(); UpdatePanels();
        }

        // ------- Auto placement -------
        private void AutoPlace()
        {
            _tarps.Clear();
            var tent = TentRect();
            float normal = (_windDirDeg + 90f) % 360f;
            var pos = new PointF(tent.Left - 40, tent.Top + tent.Height / 2f);
            _tarps.Add(new Tarp { Center = pos, Size = new SizeF(220, 90), AngleDeg = normal });
        }

        private void UpdatePanels()
        {
            float score = ComputeCoverageScore();
            Color c = score >= 80 ? Color.SeaGreen : score >= 60 ? Color.OliveDrab : score >= 40 ? Color.DarkOrange : Color.Firebrick;
            _lblScore.ForeColor = c;
            _lblScore.Text = $"Σκορ κάλυψης: {(int)score}";

            string tip = "";
            if (_wind01 > 0.6f && _tarps.Count == 0) tip += "• Ισχυρός άνεμος – βάλε 1-2 πανιά.\n";
            if (score < 60 && _tarps.Count > 0) tip += "• Κάνε τα πανιά κάθετα στην κατεύθυνση του ανέμου.\n";
            if (_tarps.Count > 2) tip += "• Πολλά πανιά = πιθανοί παλμοί. Προτίμησε 1–2 σωστά τοποθετημένα.\n";
            if (string.IsNullOrWhiteSpace(tip)) tip = "Έτοιμο! Πάτα Deploy.";

            _lblAdvice.Text = tip;
        }

        private float ComputeCoverageScore()
        {
            if (_tarps.Count == 0) return 0f;
            return Math.Min(100f, _tarps.Count * 50f);
        }

        //  Helpers 
        private RectangleF TentRect()
        {
            var r = _canvas.ClientRectangle;
            float w = r.Width * 0.20f, h = r.Height * 0.25f;
            float x = r.Width * 0.55f, y = r.Height * 0.65f;
            return new RectangleF(x, y, w, h);
        }

        private static float Deg2Rad(float d) => (float)(Math.PI / 180.0 * d);
        private static float Distance(PointF a, PointF b) => (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        private static PointF[] RotRectPoints(PointF center, SizeF size, float angleDeg)
        {
            var half = new SizeF(size.Width / 2f, size.Height / 2f);
            var pts = new[]
            {
                new PointF(-half.Width, -half.Height),
                new PointF( half.Width, -half.Height),
                new PointF( half.Width,  half.Height),
                new PointF(-half.Width,  half.Height),
            };
            double a = angleDeg * Math.PI / 180.0; float cos = (float)Math.Cos(a), sin = (float)Math.Sin(a);
            for (int i = 0; i < pts.Length; i++)
                pts[i] = new PointF(center.X + pts[i].X * cos - pts[i].Y * sin,
                                    center.Y + pts[i].X * sin + pts[i].Y * cos);
            return pts;
        }

        private static bool PointInRotatedRect(Point p, PointF center, SizeF size, float angleDeg)
        {
            double a = -angleDeg * Math.PI / 180.0;
            float cos = (float)Math.Cos(a), sin = (float)Math.Sin(a);
            var rel = new PointF(p.X - center.X, p.Y - center.Y);
            var xr = rel.X * cos - rel.Y * sin;
            var yr = rel.X * sin + rel.Y * cos;
            return Math.Abs(xr) <= size.Width / 2f && Math.Abs(yr) <= size.Height / 2f;
        }

        private static PointF RotateHandle(PointF center, SizeF size, float angleDeg)
        {
            float r = size.Height / 2f + 18f;
            double a = angleDeg * Math.PI / 180.0 - Math.PI / 2.0;
            return new PointF(center.X + (float)Math.Cos(a) * r, center.Y + (float)Math.Sin(a) * r);
        }
    }
}
