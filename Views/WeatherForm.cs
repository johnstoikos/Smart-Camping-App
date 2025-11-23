using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using SmartCamping.Models;
using SmartCamping.Services;

namespace SmartCamping.Views
{
    public sealed partial class WeatherForm : Form
    {
        private readonly WeatherService _svc;
        private EventHandler<WeatherState>? _subscription;

        // UI
        private Label _lblTemp, _lblHum, _lblWind, _lblCond;
        private Label _lblAdvice;
        private Button _btnOpenTarps, _btnClose;
        private SparkPanel _sparkTemp, _sparkHum, _sparkWind;
        private CompassPanel _compass;

        // Theme
        private static readonly Color Teal = WeatherTheme.Teal;
        private static readonly Color TealDark = WeatherTheme.TealDark;
        private static readonly Color Olive = WeatherTheme.Olive;
        private static readonly Color GreyGreen = WeatherTheme.GreyGreen;
        private static readonly Color Cream = WeatherTheme.Cream;
        private static readonly Color Ink = WeatherTheme.Ink;

        public WeatherForm(WeatherService svc)
        {
            _svc = svc;

            Text = "Καιρός – Παρακολούθηση & Προσαρμογή";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(980, 620);
            Size = new Size(1080, 680);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 10f);
            BackColor = Color.White;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(16)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
            Controls.Add(root);

            //  πάνω αριστερά: μετρητές 
            var cardNow = Card("Τρέχουσες συνθήκες");
            cardNow.Padding = new Padding(14);

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(0, 0, 0, 4)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));

            _lblTemp = BigValue("—°C");
            _lblHum = BigValue("—%");
            _lblWind = BigValue("— km/h");
            _lblCond = new Label
            {
                Text = "—",
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 13f),
                ForeColor = Ink,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(6, 2, 0, 2)
            };

            grid.Controls.Add(MetricTitle("Θερμοκρασία"), 0, 0); grid.Controls.Add(_lblTemp, 1, 0);
            grid.Controls.Add(MetricTitle("Υγρασία"), 0, 1); grid.Controls.Add(_lblHum, 1, 1);
            grid.Controls.Add(MetricTitle("Άνεμος"), 0, 2); grid.Controls.Add(_lblWind, 1, 2);

            cardNow.Controls.Add(grid);
            root.Controls.Add(cardNow, 0, 0);

            //  πάνω δεξιά: banner, πυξίδα 
            var cardAdvice = Card("Πρόταση προσαρμογής");
            cardAdvice.Padding = new Padding(14);

            _lblAdvice = new Label
            {
                Text = "—",
                AutoSize = true,
                Dock = DockStyle.Top,
                ForeColor = Ink,
                Padding = new Padding(10),
                Margin = new Padding(0, 4, 0, 8),
                BackColor = Color.Transparent
            };

            _btnOpenTarps = new Button
            {
                Text = "Άνοιγμα Προστατευτικών Πανιών",
                AutoSize = true,
                BackColor = Teal,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 4, 0, 0),
                Padding = new Padding(10, 6, 10, 6)
            };
            _btnOpenTarps.FlatAppearance.BorderSize = 0;
            _btnOpenTarps.Click += (s, e) =>
            {
                using var f = new TarpPlacementForm(_svc.WindDirDeg, _svc.WindStrength01);
                f.ShowDialog(this);
            };

            cardAdvice.Controls.Add(_btnOpenTarps);
            cardAdvice.Controls.Add(_lblAdvice);

            var rightStack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            rightStack.RowStyles.Clear();
            rightStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 100)); 
            rightStack.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); 

            rightStack.Controls.Add(cardAdvice, 0, 0);

            var cardCompass = Card("Άνεμος – Πυξίδα");
            cardCompass.Padding = new Padding(12);
            _compass = new CompassPanel
            {
                Dock = DockStyle.Fill,
                MinimumSize = new Size(0, 260) 
            };
            cardCompass.Controls.Add(_compass);

            rightStack.Controls.Add(cardCompass, 0, 1);
            root.Controls.Add(rightStack, 1, 0);

            //  κάτω αριστερά: Ιστορικό 
            var cardSparks = Card("Ιστορικό (τελευταία λεπτά)");
            cardSparks.Padding = new Padding(12);

            var sparx = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0),
                AutoScroll = true
            };
            sparx.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            sparx.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            sparx.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));

            _sparkTemp = new SparkPanel { Dock = DockStyle.Fill, Title = "Θερμοκρασία (°C)", Line = Teal };
            _sparkHum = new SparkPanel { Dock = DockStyle.Fill, Title = "Υγρασία (%)", Line = Olive };
            _sparkWind = new SparkPanel { Dock = DockStyle.Fill, Title = "Άνεμος (km/h)", Line = GreyGreen };

            sparx.Controls.Add(_sparkTemp, 0, 0);
            sparx.Controls.Add(_sparkHum, 0, 1);
            sparx.Controls.Add(_sparkWind, 0, 2);

            cardSparks.Controls.Add(sparx);
            root.Controls.Add(cardSparks, 0, 1);

            var btns = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
            _btnClose = new Button { Text = "Κλείσιμο", AutoSize = true, Padding = new Padding(10, 6, 10, 6) };
            _btnClose.Click += (s, e) => Close();
            btns.Controls.Add(_btnClose);
            root.Controls.Add(btns, 1, 1);

            _subscription = (s, st) =>
            {
                if (!IsHandleCreated || IsDisposed) return;
                try { BeginInvoke((Action)(() => LoadFromState(st))); } catch { /* closing */ }
            };
            _svc.StateChanged += _subscription;

            FormClosed += (s, e) =>
            {
                if (_subscription != null) _svc.StateChanged -= _subscription;
                _subscription = null;
            };

            LoadFromState(_svc.State);
        }

        private GroupBox Card(string title)
        {
            var grp = new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill,
                ForeColor = Ink,
                BackColor = Cream,
                Padding = new Padding(10),
                Margin = new Padding(6)
            };
            grp.Paint += (s, e) =>
            {
                using var pen = new Pen(TealDark, 1);
                var r = grp.ClientRectangle; r.Inflate(-1, -1);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawRectangle(pen, r);
            };
            return grp;
        }

        private Label MetricTitle(string text) => new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = Ink,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 6, 8, 6)
        };

        private Label BigValue(string t) => new Label
        {
            Text = t,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 28f),
            ForeColor = Ink,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, 2, 0, 2)
        };

        private void LoadFromState(WeatherState st)
        {
            _lblTemp.Text = $"{st.Now.TempC:0.#}°C";
            _lblHum.Text = $"{st.Now.HumidityPct}%";
            _lblWind.Text = $"{st.Now.WindKmh:0.#} km/h";
            _lblCond.Text = st.Now.Condition switch
            {
                WeatherCondition.Clear => "Αίθριος",
                WeatherCondition.Cloudy => "Συννεφιά",
                WeatherCondition.Windy => "Άνεμος",
                WeatherCondition.Rain => "Βροχή",
                WeatherCondition.Storm => "Καταιγίδα",
                _ => "—"
            };

            var adv = _svc.CurrentAdvice();
            _lblAdvice.Text = adv ?? "Δεν απαιτείται δράση προς το παρόν.";
            _lblAdvice.BackColor = adv switch
            {
                null => Color.Transparent,
                _ when st.Now.Condition == WeatherCondition.Storm => Color.FromArgb(28, 214, 108, 92),
                _ when st.Now.Condition == WeatherCondition.Rain => Color.FromArgb(26, 79, 163, 209),
                _ when st.Now.WindKmh >= 18 => Color.FromArgb(22, Teal.R, Teal.G, Teal.B),
                _ => Color.FromArgb(18, 0, 0, 0)
            };

            var hist = st.History.ToArray();
            _sparkTemp.UpdateValues(hist.Select(h => (float)h.TempC).ToArray());
            _sparkHum.UpdateValues(hist.Select(h => (float)h.HumidityPct).ToArray());
            _sparkWind.UpdateValues(hist.Select(h => (float)h.WindKmh).ToArray());

            _compass.UpdateWind((float)st.Now.WindDirDeg, (float)st.Now.WindKmh);
        }

        private sealed class SparkPanel : Panel
        {
            public string Title { get; set; } = "";
            public Color Line { get; set; } = Color.DimGray;
            private float[] _values = Array.Empty<float>();

            public SparkPanel()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
                BackColor = Color.White;
                Margin = new Padding(4);
                MinimumSize = new Size(0, 110);
            }

            public void UpdateValues(float[] v) { _values = v ?? Array.Empty<float>(); Invalidate(); }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (var title = new SolidBrush(WeatherTheme.Ink))
                    g.DrawString(Title, new Font("Segoe UI Semibold", 10f), title, 8, 4);

                if (_values.Length < 2) return;

                var r = ClientRectangle; r.Inflate(-10, -18);

                using (var grid = new Pen(Color.FromArgb(45, 0, 0, 0), 1))
                {
                    for (int i = 1; i <= 4; i++)
                    {
                        float y = r.Top + i * r.Height / 5f;
                        g.DrawLine(grid, r.Left, y, r.Right, y);
                    }
                }

                float min = _values.Min(), max = _values.Max();
                if (Math.Abs(max - min) < 0.0001f) max = min + 1;

                PointF Map(int i, float val)
                {
                    float x = r.Left + i * (r.Width - 1f) / (_values.Length - 1f);
                    float y = r.Bottom - (val - min) / (max - min) * (r.Height - 1f);
                    return new PointF(x, y);
                }

                using var pGlow = new Pen(Color.FromArgb(90, Line), 8f) { LineJoin = LineJoin.Round };
                using var pCore = new Pen(Line, 2.4f) { LineJoin = LineJoin.Round };
                var pts = Enumerable.Range(0, _values.Length).Select(i => Map(i, _values[i])).ToArray();
                g.DrawLines(pGlow, pts);
                g.DrawLines(pCore, pts);

                var last = pts[^1];
                using var bubble = new SolidBrush(Color.White);
                using var outline = new Pen(Line, 2);
                g.FillEllipse(bubble, last.X - 14, last.Y - 10, 28, 20);
                g.DrawEllipse(outline, last.X - 14, last.Y - 10, 28, 20);
                using var valBrush = new SolidBrush(WeatherTheme.Ink);
                g.DrawString($"{_values[^1]:0.#}", new Font("Segoe UI", 8f, FontStyle.Bold), valBrush, last.X - 12, last.Y - 8);
            }
        }

        private sealed class CompassPanel : Panel
        {
            private float _deg = 0f;  
            private float _wind = 0f;  

            public CompassPanel()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
                BackColor = Color.White;
                Margin = new Padding(0);
                MinimumSize = new Size(0, 260);
            }

            public void UpdateWind(float dirDeg, float windKmh) { _deg = dirDeg; _wind = windKmh; Invalidate(); }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                const int infoH = 18;
                const int pad = 6;

                using var infoBrush = new SolidBrush(WeatherTheme.Ink);
                var info = $"Διεύθυνση: {(_deg + 360) % 360:0}°   •   Ταχύτητα: {_wind:0.#} km/h";
                g.DrawString(info, new Font("Segoe UI", 9f), infoBrush, pad, 2);

                float areaW = Width - pad * 2;
                float areaH = Height - infoH - pad * 2;

                float safe = 10f;
                float maxR = Math.Min(areaW, areaH) / 2f - safe;
                float R = Math.Max(20f, Math.Min(maxR, 0.27f * Math.Min(areaW, areaH)));

                float cx = Width / 2f;
                float cy = infoH + pad + R + 6f;

                using var ring = new Pen(WeatherTheme.TealDark, 2);
                g.DrawEllipse(ring, cx - R, cy - R, R * 2, R * 2);

                using var lbl = new SolidBrush(WeatherTheme.Ink);
                var f = new Font("Segoe UI Semibold", 10f);
                g.DrawString("N", f, lbl, cx - 6, cy - R - 12);
                g.DrawString("S", f, lbl, cx - 6, cy + R + 6);
                g.DrawString("W", f, lbl, cx - R - 20, cy - 8);
                g.DrawString("E", f, lbl, cx + R + 8, cy - 8);

                double rad = (_deg - 90) * Math.PI / 180.0;
                var tip = new PointF(cx + (float)Math.Cos(rad) * (R * 0.92f),
                                     cy + (float)Math.Sin(rad) * (R * 0.92f));
                using var glow = new Pen(Color.FromArgb(90, WeatherTheme.Teal), 10) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                using var pen = new Pen(WeatherTheme.Teal, 4) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLine(glow, cx, cy, tip.X, tip.Y);
                g.DrawLine(pen, cx, cy, tip.X, tip.Y);
            }
        }
    }
}
