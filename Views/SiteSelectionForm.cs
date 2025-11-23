using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace SmartCamping.Views
{
    public class SiteSelectionResult
    {
        public Point PixelPoint { get; set; }
        public PointF NormalizedPoint01 { get; set; } 
        public float Sun { get; set; }        
        public float Humidity { get; set; }   
        public float Wind { get; set; }       
        public float Stability { get; set; }  
        public string Advice { get; set; } = "";
        public string ZoneName { get; set; } = "";
    }

    public partial class SiteSelectionForm : Form
    {
        //  UI
        private Panel header = null!;
        private Panel side = null!;
        private Panel map = null!;
        private Label lPos = null!, lSun = null!, lHum = null!, lWind = null!, lStable = null!, lAdvice = null!;
        private Button btnAccept = null!, btnCancel = null!;

        //  Background 
        private Image? _mapBg;

        //  Κατάσταση 
        private Point? _selected;
        private bool _showGrid = false;
        private float _lastNX, _lastNY;

        private readonly List<RectangleF> _forbiddenRects01 = new();

        public SiteSelectionResult Result { get; private set; } = new SiteSelectionResult();

        private enum EnvKind { Water, Road, Sand, Forest, Land }

        public SiteSelectionForm()
        {
            InitializeComponent();
            Text = "Στήσιμο Σκηνής – Επιλογή Σημείου";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1100, 680);
            MinimumSize = new Size(950, 600);
            DoubleBuffered = true;
            KeyPreview = true;

            BuildLayout();
            LoadBackground();
            BuildForbiddenAreas();  

            KeyDown += SiteSelectionForm_KeyDown;
        }

        //  Layout 
        private void BuildLayout()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            header = new Panel { Dock = DockStyle.Fill, BackColor = ColorTranslator.FromHtml("#FFF3E0"), Padding = new Padding(12) };
            root.Controls.Add(header, 0, 0);
            root.SetColumnSpan(header, 2);

            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoScroll = true };
            header.Controls.Add(flow);

            lPos = Chip("Σημείο", "—");
            lSun = Chip("Ήλιος", "—");
            lHum = Chip("Υγρασία", "—");
            lWind = Chip("Άνεμος", "—");
            lStable = Chip("Σταθερότητα", "—");
            lAdvice = Chip("Πρόταση", "—");
            flow.Controls.AddRange(new Control[] { lPos, lSun, lHum, lWind, lStable, lAdvice });

            map = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            map.Paint += Map_Paint;
            map.MouseMove += Map_MouseMove;
            map.MouseClick += Map_MouseClick;
            root.Controls.Add(map, 0, 1);

            side = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };
            root.Controls.Add(side, 1, 1);

            btnAccept = new Button { Text = "Αποδοχή Σημείου", Dock = DockStyle.Top, Height = 44, Enabled = false };
            btnCancel = new Button { Text = "Άκυρο", Dock = DockStyle.Top, Height = 40, Margin = new Padding(0, 10, 0, 0) };
            btnAccept.Click += BtnAccept_Click;
            btnCancel.Click += (s, e) => Close();

            var help = new Label
            {
                Dock = DockStyle.Top,
                Height = 195,
                Padding = new Padding(0, 8, 0, 0),
                TextAlign = ContentAlignment.TopLeft,
                Text = "Πώς δουλεύει:\n" +
                       "• Κούνα το ποντίκι πάνω στον χάρτη – βλέπεις Ήλιο/Υγρασία/Άνεμο/Σταθερότητα.\n" +
                       "• Κλικ για επιλογή σημείου (αν είναι επιτρεπτό).\n" +
                       "• G: εμφάνιση/απόκρυψη grid με (nX,nY).  Space: αντιγραφή (nX,nY).\n" +
                       "• Θάλασσα (μπλε): ΑΠΑΓΟΡΕΥΕΤΑΙ.  Δρόμος (γκρι): ΑΠΑΓΟΡΕΥΕΤΑΙ.\n" +
                       "• Άμμος: επιτρέπεται αλλά ζητά επιβεβαίωση (μη ιδανικές συνθήκες)."
            };

            side.Controls.Add(btnCancel);
            side.Controls.Add(btnAccept);
            side.Controls.Add(help);
        }

        private Label Chip(string title, string value) => new Label
        {
            AutoSize = true,
            Margin = new Padding(8, 6, 8, 6),
            Padding = new Padding(14, 8, 14, 8),
            BackColor = Color.White,
            ForeColor = ColorTranslator.FromHtml("#203028"),
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            Text = $"{title}: {value}"
        };

        private void LoadBackground()
        {
            try
            {
                var bgPath = Path.Combine(Application.StartupPath, "Assets", "camp-map.png");
                if (File.Exists(bgPath))
                {
                    _mapBg = Image.FromFile(bgPath);
                    map.BackgroundImage = _mapBg;
                    map.BackgroundImageLayout = ImageLayout.Stretch;
                }
            }
            catch { /* ignore */ }
        }

        //  Απαγορευμένα (μαγαζιά/μπαρ) 
        private void BuildForbiddenAreas()
        {
            _forbiddenRects01.Clear();

            
            AddForbiddenRect01(0.66f, 0.60f, 0.80f, 0.78f);
        }

        private void AddForbiddenRect01(float x1, float y1, float x2, float y2)
        {
            float l = Math.Min(x1, x2), r = Math.Max(x1, x2);
            float t = Math.Min(y1, y2), b = Math.Max(y1, y2);
            _forbiddenRects01.Add(new RectangleF(l, t, r - l, b - t));
        }

        private bool IsInForbiddenNormalized(PointF p01)
        {
            foreach (var r in _forbiddenRects01)
                if (p01.X >= r.Left && p01.X <= r.Right && p01.Y >= r.Top && p01.Y <= r.Bottom)
                    return true;
            return false;
        }

        //  Χρώμα & Ανίχνευση 
        private static void RgbToHsv(Color c, out double h, out double s, out double v)
        {
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            v = max;
            double d = max - min;
            s = max == 0 ? 0 : d / max;
            if (d == 0) { h = 0; return; }
            if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
            else if (max == g) h = (b - r) / d + 2;
            else h = (r - g) / d + 4;
            h *= 60.0;
        }

        private static int ClampInt(int v, int mn, int mx) => v < mn ? mn : (v > mx ? mx : v);

        // Αναγνώριση θάλασσας 
        private static bool IsWaterColor(Color c)
        {
            RgbToHsv(c, out double h, out double s, out double v);
            bool hsvBlue = (h >= 150 && h <= 265) && (s >= 0.08) && (v >= 0.15);
            bool rgbBlueDominant = (c.B >= c.G + 18 && c.B >= c.R + 18) || (c.B > 180 && c.G < 190 && c.R < 190);
            bool cyanLowSat = (h >= 150 && h <= 195) && (s >= 0.04) && (v >= 0.55);
            return hsvBlue || rgbBlueDominant || cyanLowSat;
        }

        // Αναγνώριση δρόμου 
        private static bool IsRoadColor(Color c)
        {
            RgbToHsv(c, out double h, out double s, out double v);
            int maxc = Math.Max(c.R, Math.Max(c.G, c.B));
            int minc = Math.Min(c.R, Math.Min(c.G, c.B));
            int spread = maxc - minc;       
            bool lowSatGray = (s <= 0.12) && (spread <= 22); // σχεδόν γκρι
            bool midBrightness = (v >= 0.28 && v <= 0.95);   
            return lowSatGray && midBrightness;
        }

        private bool SampleFraction(Point p, Func<Color, bool> predicate, int radius, out double frac)
        {
            frac = 0;
            if (!(_mapBg is Bitmap bmp)) return false;

            int W = Math.Max(map.ClientSize.Width, 1);
            int H = Math.Max(map.ClientSize.Height, 1);
            int imgW = bmp.Width, imgH = bmp.Height;

            int ix = (int)Math.Round(p.X * (imgW / (double)W));
            int iy = (int)Math.Round(p.Y * (imgH / (double)H));

            int tot = 0, hits = 0;
            for (int dy = -radius; dy <= radius; dy++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int sx = ClampInt(ix + dx, 0, imgW - 1);
                    int sy = ClampInt(iy + dy, 0, imgH - 1);
                    if (predicate(bmp.GetPixel(sx, sy))) hits++;
                    tot++;
                }
            if (tot > 0) frac = (double)hits / tot;
            return true;
        }

        //  Μετρήσεις βάσει κατηγορίας 
        private static float Clamp01(float v) => v < 0 ? 0 : (v > 1 ? 1 : v);

        private void GetMetricsAt(Point p, out float sun, out float humidity, out float wind, out float stability, out string advice, out EnvKind kind)
        {
            int W = Math.Max(map.ClientSize.Width, 1);
            int H = Math.Max(map.ClientSize.Height, 1);
            float nx = Clamp01(p.X / (float)W);
            float ny = Clamp01(p.Y / (float)H);

            // 1) ΘΑΛΑΣΣΑ;
            if (SampleFraction(p, IsWaterColor, radius: 4, out double waterFrac) && waterFrac > 0.30)
            {
                kind = EnvKind.Water;
                sun = 0.4f; humidity = 1.0f; wind = 0.6f; stability = 0.0f;
                advice = "Απαγορεύεται: Θάλασσα ⛔";
                return;
            }

            // 2) ΔΡΟΜΟΣ;
            if (SampleFraction(p, IsRoadColor, radius: 3, out double roadFrac) && roadFrac > 0.35)
            {
                kind = EnvKind.Road;
                sun = 0.5f; humidity = 0.5f; wind = 0.4f; stability = 0.0f; // δεν έχει σημασία, είναι απαγορευμένο
                advice = "Απαγορεύεται: Δρόμος ⛔";
                return;
            }

            // 3) Όχι νερό/δρόμος
            if (!(_mapBg is Bitmap bmp))
            {
                kind = EnvKind.Land;
                sun = 0.6f; humidity = 0.3f; wind = 0.3f; stability = 0.85f;
                advice = "Ιδανικό σημείο ✅";
                return;
            }
            int imgW = bmp.Width, imgH = bmp.Height;
            int ix = ClampInt((int)Math.Round(p.X * (imgW / (double)W)), 0, imgW - 1);
            int iy = ClampInt((int)Math.Round(p.Y * (imgH / (double)H)), 0, imgH - 1);
            Color col = bmp.GetPixel(ix, iy);

            // ΜΟΝΟ για Sand/Forest/Land 
            RgbToHsv(col, out double h, out double s, out double v);
            if (v > 0.55 && h >= 20 && h <= 65) kind = EnvKind.Sand;
            else if (s > 0.15 && h >= 60 && h <= 170) kind = EnvKind.Forest;
            else kind = EnvKind.Land;

            float micro = 0.02f * (float)(Math.Sin(9 * nx + 7 * ny) + Math.Cos(5 * nx - 6 * ny)) * 0.5f;

            switch (kind)
            {
                case EnvKind.Forest:
                    sun = Clamp01(0.35f + micro);
                    humidity = Clamp01(0.22f + micro);
                    wind = Clamp01(0.25f + micro * 0.5f);
                    stability = Math.Max(0.80f, Clamp01(0.86f + micro));
                    advice = "Ιδανικό σημείο ✅ (στεριά/δέντρα)";
                    break;

                case EnvKind.Sand:
                    sun = Clamp01(0.90f + micro);
                    humidity = Clamp01(0.78f + micro);
                    wind = Clamp01(0.40f + micro * 0.5f);
                    stability = Math.Min(0.40f, Clamp01(0.30f + micro));
                    advice = "Αποδεκτό σημείο ⚠️ (άμμος: χαμηλή σταθερότητα, υψηλή υγρασία)";
                    break;

                default: // Land
                    sun = Clamp01(0.55f + micro);
                    humidity = Clamp01(0.28f + micro);
                    wind = Clamp01(0.30f + micro * 0.5f);
                    stability = Math.Max(0.80f, Clamp01(0.82f + micro));
                    advice = "Ιδανικό σημείο ✅ (στεριά)";
                    break;
            }
        }

        //  Ζωγραφική & Αλληλεπίδραση 
        private void Map_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_showGrid)
            {
                using var pen = new Pen(Color.FromArgb(70, Color.Black)) { DashStyle = DashStyle.Dot };
                for (int i = 1; i < 10; i++)
                {
                    int x = (int)(i * 0.1f * map.ClientSize.Width);
                    int y = (int)(i * 0.1f * map.ClientSize.Height);
                    g.DrawLine(pen, x, 0, x, map.ClientSize.Height);
                    g.DrawLine(pen, 0, y, map.ClientSize.Width, y);
                    using var f = new Font("Segoe UI", 8f);
                    g.DrawString($"{i / 10f:0.0}", f, Brushes.Black, x + 2, 2);
                    g.DrawString($"{i / 10f:0.0}", f, Brushes.Black, 2, y + 2);
                }
            }

            if (_selected.HasValue)
            {
                var p = _selected.Value;
                using var br = new SolidBrush(Color.FromArgb(220, 56, 142, 60));
                using var pen = new Pen(Color.DarkGreen, 3);
                g.FillEllipse(br, p.X - 8, p.Y - 8, 16, 16);
                g.DrawEllipse(pen, p.X - 8, p.Y - 8, 16, 16);
            }
        }

        private void Map_MouseMove(object? sender, MouseEventArgs e)
        {
            GetMetricsAt(e.Location, out var sun, out var hum, out var wind, out var stab, out var msg, out var kind);

            float nx = e.X / Math.Max(1f, map.ClientSize.Width);
            float ny = e.Y / Math.Max(1f, map.ClientSize.Height);
            _lastNX = nx; _lastNY = ny;

            var p01 = new PointF(nx, ny);
            if (IsInForbiddenNormalized(p01)) msg = "Απαγορεύεται εδώ (Καταστήματα/Μπαρ) ⛔";
            if (kind == EnvKind.Road) msg = "Απαγορεύεται εδώ (Δρόμος) ⛔";
            if (kind == EnvKind.Water) msg = "Απαγορεύεται εδώ (Θάλασσα) ⛔";

            lPos.Text = $"Σημείο: {e.X}, {e.Y}  (nX={nx:0.00}, nY={ny:0.00})";
            lSun.Text = $"Ήλιος: {(int)(sun * 100)}%";
            lHum.Text = $"Υγρασία: {(int)(hum * 100)}%";
            lWind.Text = $"Άνεμος: {(int)(wind * 100)}%";
            lStable.Text = $"Σταθερότητα: {(int)(stab * 100)}%";
            lAdvice.Text = $"Πρόταση: {msg}";
        }

        private void Map_MouseClick(object? sender, MouseEventArgs e)
        {
            float nx = e.X / Math.Max(1f, map.ClientSize.Width);
            float ny = e.Y / Math.Max(1f, map.ClientSize.Height);
            var p01 = new PointF(nx, ny);

            // 1) Απαγορευμένη περιοχή (μαγαζιά/μπαρ)
            if (IsInForbiddenNormalized(p01))
            {
                _selected = null;
                btnAccept.Enabled = false;
                lAdvice.Text = "Πρόταση: Απαγορεύεται εδώ (Καταστήματα/Μπαρ) ⛔";
                System.Media.SystemSounds.Exclamation.Play();
                map.Invalidate();
                return;
            }

            // 2) Μετρικές & κατηγορία
            GetMetricsAt(e.Location, out var sun, out var hum, out var wind, out var stab, out var msg, out var kind);

            // ΘΑΛΑΣΣΑ ή ΔΡΟΜΟΣ: απαγόρευση
            if (kind == EnvKind.Water || kind == EnvKind.Road)
            {
                _selected = null;
                btnAccept.Enabled = false;
                lAdvice.Text = $"Πρόταση: {msg}";
                System.Media.SystemSounds.Exclamation.Play();
                map.Invalidate();
                return;
            }

            // ΑΜΜΟΣ: επιβεβαίωση
            if (kind == EnvKind.Sand)
            {
                string details = $"Ήλιος {(int)(sun * 100)}% • Υγρασία {(int)(hum * 100)}% • " +
                                 $"Άνεμος {(int)(wind * 100)}% • Σταθερότητα {(int)(stab * 100)}%";
                var confirm = MessageBox.Show(
                    "Είσαι σίγουρος/η ότι θέλεις να στήσεις εδώ;\n" +
                    "Η άμμος έχει υψηλή υγρασία και χαμηλή σταθερότητα.\n\n" +
                    details + "\n\nΣυνέχεια;",
                    "Μη ιδανικές συνθήκες",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2
                );
                if (confirm != DialogResult.Yes)
                {
                    _selected = null;
                    btnAccept.Enabled = false;
                    lAdvice.Text = "Πρόταση: Προτίμησε πράσινες/στεριανές περιοχές για καλύτερη σταθερότητα.";
                    map.Invalidate();
                    return;
                }
            }

            // Επιτρεπτό (στεριά ή άμμος με επιβεβαίωση)
            _selected = e.Location;
            btnAccept.Enabled = true;

            Result = new SiteSelectionResult
            {
                PixelPoint = e.Location,
                NormalizedPoint01 = p01,
                Sun = sun,
                Humidity = hum,
                Wind = wind,
                Stability = stab,
                Advice = msg,
                ZoneName = kind.ToString()
            };

            map.Invalidate();
        }

        private void BtnAccept_Click(object? sender, EventArgs e)
        {
            if (!_selected.HasValue) return;

            float nx = _selected.Value.X / Math.Max(1f, map.ClientSize.Width);
            float ny = _selected.Value.Y / Math.Max(1f, map.ClientSize.Height);
            var p01 = new PointF(nx, ny);

            // Διπλός έλεγχος: απαγορευμένη ζώνη
            if (IsInForbiddenNormalized(p01))
            {
                MessageBox.Show("Απαγορεύεται: Καταστήματα/Μπαρ", "Δεν επιτρέπεται",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnAccept.Enabled = false;
                return;
            }

            // Και για θάλασσα/δρόμο
            GetMetricsAt(_selected.Value, out _, out _, out _, out _, out var msg, out var kind);
            if (kind == EnvKind.Water || kind == EnvKind.Road)
            {
                MessageBox.Show(msg, "Δεν επιτρέπεται",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnAccept.Enabled = false;
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void SiteSelectionForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.G)
            {
                _showGrid = !_showGrid;
                map.Invalidate();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Space)
            {
                try { Clipboard.SetText($"new({_lastNX:0.00}f, {_lastNY:0.00}f)"); } catch { }
                e.Handled = true;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _mapBg?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
