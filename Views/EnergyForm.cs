using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SmartCamping.Models;
using SmartCamping.Services;
using System.Drawing.Drawing2D;

namespace SmartCamping.Views
{
    public partial class EnergyForm : Form
    {
        private readonly EnergyService _svc;

        private EnergyCanvas _canvas;
        private Label _lblBatt, _lblPv, _lblLoad, _lblNet, _lblEst, _lblLastAction;
        private CheckBox _chkAutosave;
        private ComboBox _cbAcMode;
        private TrackBar _tbSetpoint;
        private Button _btnAcToggle, _btnSaveNow;
        private FlowLayoutPanel _devicesPanel;

        private bool _updating;
        private readonly Stopwatch _throttle = Stopwatch.StartNew();

        private static readonly Color PvYellow = ColorTranslator.FromHtml("#E9D85D");
        private static readonly Color BattGreen = ColorTranslator.FromHtml("#3FCF7A");
        private static readonly Color NetPos = ColorTranslator.FromHtml("#21A87A");
        private static readonly Color NetNeg = ColorTranslator.FromHtml("#C85B4A");
        private static readonly Color LoadPanel = Color.FromArgb(200, 28, 31, 34); // ημιδιαφανές
        private static readonly Color GlowBg = Color.FromArgb(18, 22, 28);      // fallback bg

        public EnergyForm(EnergyService svc)
        {
            _svc = svc;
            BuildUi();
            LoadFromState(_svc.State);
            _svc.StateChanged += OnServiceStateChanged;
        }

        private void BuildUi()
        {
            Text = "Διαχείριση Ενέργειας";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(880, 520);
            Size = new Size(940, 560);
            Font = new Font("Segoe UI", 10f);
            DoubleBuffered = true;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(14)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            var grpLeft = new GroupBox { Text = "Σκηνικό  Ροές Ενέργειας", Dock = DockStyle.Fill, Padding = new Padding(10) };
            _canvas = new EnergyCanvas { Dock = DockStyle.Fill };
            grpLeft.Controls.Add(_canvas);
            root.Controls.Add(grpLeft, 0, 0);

            // εξιά: πίνακας ελέγχου
            var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            for (int i = 0; i < 9; i++) right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(right, 1, 0);

            _lblBatt = AddRow(right, "Μπαταρία:", out var _);
            _lblPv = AddRow(right, "PV:", out _);
            _lblLoad = AddRow(right, "Κατανάλωση:", out _);
            _lblNet = AddRow(right, "Ισοζύγιο:", out _);
            _lblEst = AddRow(right, "Αυτονομία:", out _);

            _chkAutosave = new CheckBox { Text = "Αυτόματη εξοικονόμηση (≤20%)", AutoSize = true, Checked = true, Margin = new Padding(0, 10, 0, 0) };
            _chkAutosave.CheckedChanged += (s, e) => { if (_updating) return; _svc.SetAutoSave(_chkAutosave.Checked); };
            right.Controls.Add(_chkAutosave, 0, 5);
            right.SetColumnSpan(_chkAutosave, 2);

            // A/C
            var lblMode = new Label { Text = "A/C Mode:", AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            _cbAcMode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            _cbAcMode.Items.AddRange(Enum.GetNames(typeof(AcMode)));
            _cbAcMode.SelectedIndex = 0;
            _cbAcMode.SelectedIndexChanged += (s, e) => ApplyAc();
            right.Controls.Add(lblMode, 0, 6);
            right.Controls.Add(_cbAcMode, 1, 6);

            var lblSet = new Label { Text = "Setpoint (°C):", AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            _tbSetpoint = new TrackBar { Minimum = 16, Maximum = 30, TickFrequency = 1, Value = 24, Dock = DockStyle.Fill };
            _tbSetpoint.ValueChanged += (s, e) => ApplyAc();
            right.Controls.Add(lblSet, 0, 7);
            right.Controls.Add(_tbSetpoint, 1, 7);

            _btnAcToggle = new Button { Text = "A/C ON/OFF", AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
            _btnAcToggle.Click += (s, e) => _svc.SetAc(!_svc.State.AcOn, (AcMode)_cbAcMode.SelectedIndex, _tbSetpoint.Value);
            right.Controls.Add(_btnAcToggle, 0, 8);
            right.SetColumnSpan(_btnAcToggle, 2);

            // Συσκευές
            var grpDev = new GroupBox { Text = "Συσκευές", Dock = DockStyle.Fill, Padding = new Padding(8) };
            _devicesPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoScroll = true, WrapContents = false };
            grpDev.Controls.Add(_devicesPanel);
            root.Controls.Add(grpDev, 0, 1);
            root.SetColumnSpan(grpDev, 2);

            // Κουμπί Εξοικονόμηση τώρα
            _btnSaveNow = new Button { Text = "Εξοικονόμηση τώρα", AutoSize = true, Margin = new Padding(0, 6, 0, 8) };
            _btnSaveNow.Click += (s, e) => _svc.ApplySavingNow();
            right.Controls.Add(_btnSaveNow);
            right.SetColumnSpan(_btnSaveNow, 2);

            _lblLastAction = new Label { AutoSize = true, ForeColor = SystemColors.GrayText };
            right.Controls.Add(_lblLastAction);
            right.SetColumnSpan(_lblLastAction, 2);
        }

        private static Label AddRow(TableLayoutPanel tbl, string title, out Label titleLabel)
        {
            titleLabel = new Label { Text = title, AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
            var valueLabel = new Label { Text = "—", AutoSize = true, Font = new Font("Segoe UI Semibold", 10f), Margin = new Padding(0, 8, 0, 0) };
            tbl.Controls.Add(titleLabel);
            tbl.Controls.Add(valueLabel);
            return valueLabel;
        }

        private void RebuildDevices(EnergyState st)
        {
            _devicesPanel.SuspendLayout();
            _devicesPanel.Controls.Clear();
            foreach (var d in st.Devices)
            {
                var cb = new CheckBox { Text = $"{d.Name} ({d.PowerW}W)", Checked = d.IsOn, AutoSize = true };
                cb.CheckedChanged += (s, e) => { if (_updating) return; _svc.ToggleDevice(d.Name, cb.Checked); };
                _devicesPanel.Controls.Add(cb);
            }
            _devicesPanel.ResumeLayout();
        }

        private void ApplyAc()
        {
            if (_updating) return;
            var mode = (AcMode)Enum.GetValues(typeof(AcMode)).GetValue(_cbAcMode.SelectedIndex)!;
            _svc.SetAc(true, mode, _tbSetpoint.Value);
        }

        private void OnServiceStateChanged(object? sender, EnergyState st)
        {
            if (_throttle.ElapsedMilliseconds < 120) return;
            _throttle.Restart();

            if (!IsHandleCreated) return;
            BeginInvoke(new Action(() => LoadFromState(st)));
        }

        private void LoadFromState(EnergyState st)
        {
            _updating = true;

            _lblBatt.Text = $"{st.BatteryPercent}%";
            _lblPv.Text = $"{st.PvPowerW} W";
            _lblLoad.Text = $"{st.LoadPowerW} W";
            _lblNet.Text = $"{(st.NetPowerW >= 0 ? "+" : "")}{st.NetPowerW} W";
            _lblNet.ForeColor = st.NetPowerW >= 0 ? NetPos : NetNeg;   // χρώμα ανάλογα με το πρόσημο
            _lblEst.Text = double.IsInfinity(st.EstHoursRemaining) ? "∞" : $"{st.EstHoursRemaining:0.0} h";
            _chkAutosave.Checked = st.AutoSave;

            _btnAcToggle.Text = st.AcOn ? "A/C: Απενεργοποίηση" : "A/C: Ενεργοποίηση";
            if (_cbAcMode.Items.Contains(st.AcMode.ToString()))
                _cbAcMode.SelectedItem = st.AcMode.ToString();
            _tbSetpoint.Value = Math.Max(_tbSetpoint.Minimum, Math.Min(_tbSetpoint.Maximum, st.AcSetpointC));

            _lblLastAction.Text = st.LastAction ?? "";

            _canvas.UpdateFromState(st);

            if (_devicesPanel.Controls.Count != st.Devices.Count) RebuildDevices(st);

            _updating = false;
        }

        // εικόνα
        private sealed class EnergyCanvas : Panel
        {
            private Image? _bg;
            private EnergyState _st = new EnergyState();

            public EnergyCanvas()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer, true);
                UpdateStyles();

                try
                {
                    // Θα δοκιμάσει 3 ονόματα για να μη χρειάζεται να μετονομάσεις το αρχείο
                    var assets = Path.Combine(Application.StartupPath, "Assets");
                    var candidates = new[]
                    {
                        "energy_forest_solar.jpg",
                        "energy_forest_solar_v2.jpg",
                        "energy_forest_solar_v2 (1).jpg"
                    };
                    foreach (var name in candidates)
                    {
                        var p = Path.Combine(assets, name);
                        if (File.Exists(p)) { _bg = Image.FromFile(p); break; }
                    }
                }
                catch { /* ignore */ }

                BackColor = GlowBg; // σκούρο fallback
            }

            public void UpdateFromState(EnergyState s)
            {
                _st = s.Clone();
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // background
                if (_bg != null) g.DrawImage(_bg, ClientRectangle);
                else using (var br = new SolidBrush(GlowBg)) g.FillRectangle(br, ClientRectangle);

                using (var overlay = new SolidBrush(Color.FromArgb(24, 15, 91, 75)))
                    g.FillRectangle(overlay, ClientRectangle);

                // ηλιακό πάνελ (πάνω αριστερά)
                var panelRect = new Rectangle(24, 24, 150, 90);
                DrawPvPanel(g, panelRect);

                // μπαταρία (κάτω αριστερά)
                var battRect = new Rectangle(24, Height - 120, 200, 40);
                DrawBattery(g, battRect, _st.BatteryPercent);

                // box συσκευών/ φορτίο (κάτω δεξιά)
                var loadRect = new Rectangle(Width - 240, Height - 120, 200, 80);
                DrawLoadBox(g, loadRect, _st.LoadPowerW);

                DrawArrows(g, panelRect, battRect, loadRect, _st);
            }

            private static void DrawPvPanel(Graphics g, Rectangle r)
            {
                using var frame = new SolidBrush(Color.FromArgb(35, 45, 55));
                using var grid = new Pen(Color.FromArgb(116, 165, 191), 2);
                using var sky = new SolidBrush(ColorTranslator.FromHtml("#135E80"));

                var face = new Rectangle(r.X + 6, r.Y + 6, r.Width - 12, r.Height - 12);
                g.FillRectangle(frame, r);
                g.FillRectangle(sky, face);
                for (int i = 1; i < 4; i++)
                    g.DrawLine(grid, face.Left, face.Top + i * face.Height / 4, face.Right, face.Top + i * face.Height / 4);
                for (int i = 1; i < 6; i++)
                    g.DrawLine(grid, face.Left + i * face.Width / 6, face.Top, face.Left + i * face.Width / 6, face.Bottom);
            }

            private static void DrawBattery(Graphics g, Rectangle r, int percent)
            {
                using var frame = new Pen(Color.FromArgb(40, 40, 40), 2);
                using var fill = new SolidBrush(BattGreen);  // πράσινο
                using var low = new SolidBrush(NetNeg);     // κόκκινο για low
                using var txt = new SolidBrush(Color.White);

                g.DrawRectangle(frame, r);
                var cap = new Rectangle(r.Right + 2, r.Y + r.Height / 4, 8, r.Height / 2);
                g.FillRectangle(Brushes.Gray, cap);
                g.DrawRectangle(frame, cap);

                int w = (int)(r.Width * Math.Clamp(percent / 100.0, 0, 1));
                var fillRect = new Rectangle(r.X + 1, r.Y + 1, Math.Max(0, w - 2), r.Height - 2);
                g.FillRectangle(percent <= 20 ? low : fill, fillRect);
                g.DrawString($"{percent}%", new Font("Segoe UI", 9, FontStyle.Bold), txt, r.X + 6, r.Y - 18);
            }

            private static void DrawLoadBox(Graphics g, Rectangle r, int loadW)
            {
                using var br = new SolidBrush(LoadPanel);
                using var pen = new Pen(Color.FromArgb(90, 90, 90), 1);
                using var txt = new SolidBrush(Color.White);
                g.FillRectangle(br, r);
                g.DrawRectangle(pen, r);
                g.DrawString($"Load: {loadW} W", new Font("Segoe UI", 9, FontStyle.Bold), txt, r.X + 8, r.Y + 8);
            }

            private static void DrawArrows(Graphics g, Rectangle pv, Rectangle batt, Rectangle load, EnergyState st)
            {
                void ArrowGlow(Point a, Point b, Color c)
                {
                    using var glow = new Pen(Color.FromArgb(70, c), 9) { StartCap = LineCap.Round, EndCap = LineCap.ArrowAnchor };
                    using var pen = new Pen(c, 5) { StartCap = LineCap.Round, EndCap = LineCap.ArrowAnchor };
                    g.DrawLine(glow, a, b);
                    g.DrawLine(pen, a, b);
                }

                var pvMid = new Point(pv.Right, pv.Top + pv.Height / 2);
                var battMid = new Point(batt.Left, batt.Top + batt.Height / 2);
                var loadMid = new Point(load.Left, load.Top + 20);

                ArrowGlow(new Point(pvMid.X, pvMid.Y - 10), new Point(battMid.X, battMid.Y - 10), PvYellow);
                ArrowGlow(new Point(pvMid.X, pvMid.Y + 10), new Point(loadMid.X, loadMid.Y), PvYellow);

                if (st.NetPowerW < 0)
                    ArrowGlow(new Point(battMid.X + 20, battMid.Y + 12), new Point(loadMid.X + 60, loadMid.Y + 20), BattGreen);
            }
        }
    }
}
