using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SmartCamping.Models;
using SmartCamping.Services;

namespace SmartCamping.Views
{
    public partial class LightingForm : Form
    {
        private readonly LightingService _svc;

        private GlowPreview _preview;
        private TrackBar _tbBrightness;
        private Button _btnColor, _btnApply, _btnToggle, _btnClose;
        private ComboBox _cbEffect, _cbPresets;
        private CheckBox _chkAutoNight, _chkLive;
        private Label _lblB, _lblMode;
        private Panel _swatch;
        private Color _currentColor;

        private bool _updatingUi;
        private readonly Stopwatch _throttle = Stopwatch.StartNew(); 

        public LightingForm(LightingService svc)
        {
            _svc = svc;
            InitializeComponent(); // Designer
            BuildUi();             
            LoadFromState(_svc.State);

            _svc.StateChanged += OnServiceStateChanged;
        }

        private void BuildUi()
        {
            Font = new Font("Segoe UI", 10f);
            Text = "Ρύθμιση Φωτισμού";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(680, 430);
            Size = new Size(720, 460);
            DoubleBuffered = true;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(14)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            var grpPrev = new GroupBox { Text = "Προεπισκόπηση", Dock = DockStyle.Fill, Padding = new Padding(10) };
            _preview = new GlowPreview { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 22, 28), MinimumSize = new Size(260, 200) };
            grpPrev.Controls.Add(_preview);
            root.Controls.Add(grpPrev, 0, 0);

            // RIGHT CONTROLS
            var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            for (int i = 0; i < 7; i++) right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(right, 1, 0);

            // Brightness
            _lblB = new Label { Text = "Ένταση: 60%", AutoSize = true, Margin = new Padding(0, 6, 0, 0) };
            _tbBrightness = new TrackBar { Minimum = 0, Maximum = 100, TickFrequency = 10, Value = 60, Dock = DockStyle.Fill };
            _tbBrightness.ValueChanged += (s, e) =>
            {
                if (_updatingUi) return; 
                _lblB.Text = $"Ένταση: {_tbBrightness.Value}%";
                UpdatePreview();
                if (_chkLive.Checked) ApplyFromUi(false);
            };
            right.Controls.Add(_lblB, 0, 0);
            right.Controls.Add(_tbBrightness, 1, 0);

            // Color
            var lblColor = new Label { Text = "Χρώμα:", AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            var colorRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = false };
            _swatch = new Panel { Width = 28, Height = 20, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 2, 6, 0) };
            _btnColor = new Button { Text = "Επιλογή…", AutoSize = true };
            _btnColor.Click += (s, e) =>
            {
                using var dlg = new ColorDialog { FullOpen = true, Color = _currentColor };
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _currentColor = dlg.Color;
                    _swatch.BackColor = _currentColor;
                    UpdatePreview();
                    if (_chkLive.Checked) ApplyFromUi(false);
                }
            };
            colorRow.Controls.Add(_swatch);
            colorRow.Controls.Add(_btnColor);
            right.Controls.Add(lblColor, 0, 1);
            right.Controls.Add(colorRow, 1, 1);

            // Presets
            var lblPreset = new Label { Text = "Προεπιλογή:", AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            _cbPresets = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            _cbPresets.Items.AddRange(new object[] { "Custom", "Night Light", "Reading", "Party (Pulse)", "Rainbow (Cycle)" });
            _cbPresets.SelectedIndex = 0;
            _cbPresets.SelectedIndexChanged += (s, e) =>
            {
                if (_updatingUi) return; // ΝΕΟ
                switch (_cbPresets.SelectedIndex)
                {
                    case 1:
                        _tbBrightness.Value = Math.Min(_tbBrightness.Value, 35);
                        _currentColor = Color.FromArgb(255, 255, 196, 140);
                        _cbEffect.SelectedItem = "NightLight";
                        break;
                    case 2:
                        _tbBrightness.Value = Math.Max(_tbBrightness.Value, 70);
                        _currentColor = Color.FromArgb(255, 230, 240, 255);
                        _cbEffect.SelectedItem = "Reading";
                        break;
                    case 3:
                        _cbEffect.SelectedItem = "Pulse";
                        break;
                    case 4:
                        _cbEffect.SelectedItem = "ColorCycle";
                        break;
                }
                _swatch.BackColor = _currentColor;
                UpdatePreview();
                if (_chkLive.Checked) ApplyFromUi(false);
            };
            right.Controls.Add(lblPreset, 0, 2);
            right.Controls.Add(_cbPresets, 1, 2);

            // Effect
            var lblEffect = new Label { Text = "Εφέ:", AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            _cbEffect = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            _cbEffect.Items.AddRange(new object[] { "Static", "NightLight", "Reading", "Pulse", "ColorCycle" });
            _cbEffect.SelectedIndex = 0;
            _cbEffect.SelectedIndexChanged += (s, e) =>
            {
                if (_updatingUi) return;
                UpdatePreview();
                if (_chkLive.Checked) ApplyFromUi(false);
            };
            right.Controls.Add(lblEffect, 0, 3);
            right.Controls.Add(_cbEffect, 1, 3);

            // Auto Night
            _chkAutoNight = new CheckBox { Text = "Αυτόματη ρύθμιση για νύχτα (21:00–06:00)", Checked = true, AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            right.Controls.Add(_chkAutoNight, 0, 4);
            right.SetColumnSpan(_chkAutoNight, 2);

            // Live preview
            _chkLive = new CheckBox { Text = "Άμεση προεπισκόπηση", Checked = true, AutoSize = true, Margin = new Padding(0, 0, 0, 0) };
            right.Controls.Add(_chkLive, 0, 5);
            right.SetColumnSpan(_chkLive, 2);

            // Mode label
            _lblMode = new Label { Text = "", AutoSize = true, ForeColor = SystemColors.GrayText, Margin = new Padding(0, 6, 0, 0) };
            right.Controls.Add(_lblMode, 0, 6);
            right.SetColumnSpan(_lblMode, 2);

            // Buttons bottom
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 8, 0, 0),
                AutoSize = true
            };
            _btnApply = new Button { Text = "Εφαρμογή", AutoSize = true };
            _btnApply.Click += (s, e) => ApplyFromUi(true);
            _btnToggle = new Button { AutoSize = true };
            _btnToggle.Click += (s, e) => _svc.Toggle(!_svc.State.IsOn);
            _btnClose = new Button { Text = "Κλείσιμο", AutoSize = true, DialogResult = DialogResult.OK };

            buttons.Controls.Add(_btnClose);
            buttons.Controls.Add(_btnApply);
            buttons.Controls.Add(_btnToggle);

            root.Controls.Add(buttons, 0, 1);
            root.SetColumnSpan(buttons, 2);

            var tips = new ToolTip();
            tips.SetToolTip(_tbBrightness, "Ρύθμιση έντασης (0–100%)");
            tips.SetToolTip(_btnColor, "Επιλογή χρώματος φωτισμού");
            tips.SetToolTip(_cbEffect, "Επιλέξτε εφέ: Static, NightLight, Reading, Pulse, ColorCycle");
            tips.SetToolTip(_chkAutoNight, "Αυτόματη προσαρμογή χαμηλού, ζεστού φωτός τις βραδινές ώρες");
            tips.SetToolTip(_chkLive, "Εφαρμόζει τις αλλαγές αμέσως, χωρίς να πατήσετε 'Εφαρμογή'");

            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Space) { _svc.Toggle(!_svc.State.IsOn); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.Enter) { ApplyFromUi(true); e.Handled = true; }
            };

            AcceptButton = _btnApply;
        }

        private void OnServiceStateChanged(object? sender, LightingState st)
        {
            if (!IsHandleCreated) return;

            if (_throttle.ElapsedMilliseconds < 33) return;

            BeginInvoke(new Action(() =>
            {
                _updatingUi = true;

                int b = Math.Max(0, Math.Min(100, st.Brightness));
                if (_tbBrightness.Value != b) _tbBrightness.Value = b;

                if (_currentColor.ToArgb() != st.Color.ToArgb())
                {
                    _currentColor = st.Color;
                    if (_swatch != null) _swatch.BackColor = _currentColor;
                }

                _btnToggle.Text = st.IsOn ? "Απενεργοποίηση" : "Ενεργοποίηση";
                Text = st.IsOn ? "Ρύθμιση Φωτισμού (ON)" : "Ρύθμιση Φωτισμού (OFF)";

                _updatingUi = false;

                UpdatePreview();
                _throttle.Restart();
            }));
        }

        private void ApplyFromUi(bool userInitiated)
        {
            var st = _svc.State.Clone();
            st.Brightness = _tbBrightness.Value;
            st.Color = _currentColor;
            st.AutoNight = _chkAutoNight.Checked;
            st.Effect = (LightingEffect)Enum.Parse(typeof(LightingEffect), _cbEffect.SelectedItem!.ToString()!);
            st.IsOn = true;
            _svc.Apply(st);
        }

        private void LoadFromState(LightingState st)
        {
            _updatingUi = true;

            _tbBrightness.Value = Math.Max(0, Math.Min(100, st.Brightness));
            _chkAutoNight.Checked = st.AutoNight;
            _currentColor = st.Color;
            if (_swatch != null) _swatch.BackColor = _currentColor;

            var name = st.Effect.ToString();
            if (_cbEffect.Items.Contains(name)) _cbEffect.SelectedItem = name; else _cbEffect.SelectedIndex = 0;

            _cbPresets.SelectedIndex =
                st.Effect == LightingEffect.NightLight ? 1 :
                st.Effect == LightingEffect.Reading ? 2 :
                st.Effect == LightingEffect.Pulse ? 3 :
                st.Effect == LightingEffect.ColorCycle ? 4 : 0;

            _btnToggle.Text = st.IsOn ? "Απενεργοποίηση" : "Ενεργοποίηση";
            Text = st.IsOn ? "Ρύθμιση Φωτισμού (ON)" : "Ρύθμιση Φωτισμού (OFF)";

            _updatingUi = false;

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            _preview?.SetColorAndBrightness(_currentColor, _tbBrightness.Value / 100.0);
            _lblB.Text = $"Ένταση: {_tbBrightness.Value}%";
            _lblMode.Text = $"Εφέ: {(_cbEffect?.SelectedItem ?? "Static")}  •  Χρώμα: {_currentColor.R},{_currentColor.G},{_currentColor.B}";
        }

        private sealed class GlowPreview : Panel
        {
            public Color Color { get; private set; } = Color.FromArgb(255, 255, 244, 230);
            public double Brightness01 { get; private set; } = 0.6;

            public GlowPreview()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer, true);
                UpdateStyles();
            }

            public void SetColorAndBrightness(Color c, double b)
            {
                Color = c;
                Brightness01 = Math.Max(0.0, Math.Min(1.0, b));
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(BackColor);

                var center = new PointF(Width / 2f, Height / 2f);
                float radius = Math.Min(Width, Height) * 0.40f;
                var baseColor = System.Drawing.Color.FromArgb((int)(255 * Math.Max(0.1, Brightness01)), Color);

                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    path.AddEllipse(center.X - radius, center.Y - radius, 2 * radius, 2 * radius);
                    using var brush = new System.Drawing.Drawing2D.PathGradientBrush(path)
                    {
                        CenterColor = baseColor,
                        SurroundColors = new[] { System.Drawing.Color.FromArgb(0, baseColor) }
                    };
                    g.FillRectangle(brush, ClientRectangle);
                }

                using var pen = new Pen(System.Drawing.Color.FromArgb(80, 255, 255, 255));
                g.DrawEllipse(pen, center.X - radius, center.Y - radius, 2 * radius, 2 * radius);
            }
        }
    }
}
