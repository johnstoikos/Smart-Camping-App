using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SmartCamping.Models; 

namespace SmartCamping.Views
{
    public partial class PegPlacementForm : Form
    {
        //  ΕΔΑΦΟΣ 
        public enum SoilKind { Firm, LooseSand, Unknown }

        //  ΤΥΠΟΙ ΠΑΣΑΛΩΝ 
        private enum PegKind { Aluminum, Steel, SandAnchor, GroundScrew }

        private PegKind CurrentPegKind => cbPeg.SelectedIndex switch
        {
            0 => PegKind.Aluminum,
            1 => PegKind.Steel,
            2 => PegKind.SandAnchor,
            3 => PegKind.GroundScrew,
            _ => PegKind.Steel
        };

        //  ΔΕΔΟΜΕΝΑ ΣΗΜΕΙΟΥ
        private readonly SiteSelectionResult _site;
        private readonly SoilKind _soil;
        private readonly float _wind01;

        //  UI 
        private Panel canvas = null!;
        private TrackBar tbAngle = null!, tbPressure = null!, tbTension = null!;
        private ComboBox cbPeg = null!;
        private Label lblAngle = null!, lblPressure = null!, lblTension = null!, lblScore = null!, lblTips = null!;

        public PegPlacementConfig? Result { get; private set; }

        public PegPlacementForm(SiteSelectionResult site)
        {
            _site = site;
            _soil = site.ZoneName switch
            {
                "Sand" => SoilKind.LooseSand,
                "Forest" => SoilKind.Firm,
                "Land" => SoilKind.Firm,
                _ => SoilKind.Unknown
            };
            _wind01 = Math.Max(0f, Math.Min(1f, site.Wind));

            InitializeComponent(); 
            DoubleBuffered = true;
            BuildUI();
            EvaluateAndRedraw();
        }

        //  UI 
        private void BuildUI()
        {
            Text = "Καθοδήγηση Πασάλων";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(920, 620);
            Size = new Size(980, 640);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            // header
            var header = new Panel { Dock = DockStyle.Fill, BackColor = ColorTranslator.FromHtml("#FFF3E0"), Padding = new Padding(12) };
            header.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 12.5f),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = $"Έδαφος: {SoilLabel(_soil)}   •   Άνεμος: {(int)(_wind01 * 100)}%   •   Σημείο: ({(int)(_site.NormalizedPoint01.X * 100)}%, {(int)(_site.NormalizedPoint01.Y * 100)}%)"
            });
            root.Controls.Add(header, 0, 0);
            root.SetColumnSpan(header, 2);

            // canvas
            canvas = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            canvas.Paint += Canvas_Paint;
            root.Controls.Add(canvas, 0, 1);

            // right panel
            var right = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
            root.Controls.Add(right, 1, 1);

            void AddTop(Control c, int topMargin = 10, int height = 0)
            {
                c.Dock = DockStyle.Top;
                if (height > 0) c.Height = height;
                var m = c.Margin; m.Top = topMargin; c.Margin = m;
                right.Controls.Add(c);
            }

            var pegRow = new FlowLayoutPanel { Height = 40, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            pegRow.Controls.Add(new Label { Text = "Τύπος πασάλου:", AutoSize = true, Margin = new Padding(0, 10, 8, 0) });

            cbPeg = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
            cbPeg.Items.AddRange(new[] { "Αλουμινίου", "Ατσαλένιος", "Sand Anchor (για άμμο)", "Βίδα εδάφους" });
            cbPeg.SelectedIndex = _soil == SoilKind.LooseSand ? 2 : 1; // default per soil
            cbPeg.SelectedIndexChanged += (_, __) =>
            {
                tbAngle.Value = RecommendedAngle(_soil, CurrentPegKind);
                tbPressure.Value = RecommendedPressure(_soil, CurrentPegKind);
                tbTension.Value = RecommendedTension(_soil, CurrentPegKind, _wind01);
                EvaluateAndRedraw();
            };
            pegRow.Controls.Add(cbPeg);

            // sliders και labels
            lblAngle = new Label { Text = "Γωνία: —°", AutoSize = false, Height = 22, TextAlign = ContentAlignment.MiddleLeft };
            tbAngle = new TrackBar { Minimum = 20, Maximum = 75, TickFrequency = 5 };

            lblPressure = new Label { Text = "Πίεση/Βάθος: —%", AutoSize = false, Height = 22, TextAlign = ContentAlignment.MiddleLeft };
            tbPressure = new TrackBar { Minimum = 0, Maximum = 100, TickFrequency = 10 };

            lblTension = new Label { Text = "Τάση σχοινιού: —%", AutoSize = false, Height = 22, TextAlign = ContentAlignment.MiddleLeft };
            tbTension = new TrackBar { Minimum = 0, Maximum = 100, TickFrequency = 10 };

            // αρχικές τιμές ανά soil και peg
            tbAngle.Value = RecommendedAngle(_soil, CurrentPegKind);
            tbPressure.Value = RecommendedPressure(_soil, CurrentPegKind);
            tbTension.Value = RecommendedTension(_soil, CurrentPegKind, _wind01);

            tbAngle.Scroll += (_, __) => EvaluateAndRedraw();
            tbPressure.Scroll += (_, __) => EvaluateAndRedraw();
            tbTension.Scroll += (_, __) => EvaluateAndRedraw();

            lblScore = new Label { AutoSize = false, Height = 56, Font = new Font("Segoe UI Semibold", 18f), TextAlign = ContentAlignment.MiddleCenter };
            lblTips = new Label { AutoSize = false, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10.5f) };

            right.SuspendLayout();
            AddTop(lblTips, 10);                 
            AddTop(lblScore, 10, 56);
            AddTop(tbTension, 0, 45);
            AddTop(lblTension, 12, 22);
            AddTop(tbPressure, 0, 45);
            AddTop(lblPressure, 12, 22);
            AddTop(tbAngle, 0, 45);
            AddTop(lblAngle, 12, 22);
            AddTop(pegRow, 0, 40);
            right.ResumeLayout();

            // bottom buttons
            var bottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 6, 12, 8)
            };
            var btnOk = new Button { Text = "Αποδοχή", Width = 120, Height = 34 };
            var btnCancel = new Button { Text = "Άκυρο", Width = 120, Height = 34 };
            var btnAuto = new Button { Text = "Αυτόματη ρύθμιση", Width = 160, Height = 34 };

            btnOk.Click += (s, e) =>
            {
                Result = new PegPlacementConfig
                {
                    Angle = tbAngle.Value,
                    Pressure = tbPressure.Value,
                    Tension = tbTension.Value,
                    PegType = cbPeg.SelectedItem?.ToString() ?? "",
                    Score = ParseScoreFromLabel(lblScore.Text)
                };
                DialogResult = DialogResult.OK;
                Close();
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            btnAuto.Click += (s, e) =>
            {
                tbAngle.Value = RecommendedAngle(_soil, CurrentPegKind);
                tbPressure.Value = RecommendedPressure(_soil, CurrentPegKind);
                tbTension.Value = RecommendedTension(_soil, CurrentPegKind, _wind01);
                EvaluateAndRedraw();
            };

            bottom.Controls.Add(btnOk);
            bottom.Controls.Add(btnCancel);
            bottom.Controls.Add(btnAuto);
            Controls.Add(bottom);
        }

        private static string SoilLabel(SoilKind sk) => sk switch
        {
            SoilKind.Firm => "Στερεό/Δάσος",
            SoilKind.LooseSand => "Άμμος/Χαλαρό",
            _ => "Άγνωστο"
        };

        private int RecommendedAngle(SoilKind sk, PegKind pk) => (sk, pk) switch
        {
            (SoilKind.Firm, PegKind.Aluminum) => 45,
            (SoilKind.Firm, PegKind.Steel) => 40,
            (SoilKind.Firm, PegKind.GroundScrew) => 35,
            (SoilKind.Firm, PegKind.SandAnchor) => 42,

            (SoilKind.LooseSand, PegKind.SandAnchor) => 30,
            (SoilKind.LooseSand, PegKind.Aluminum) => 35,
            (SoilKind.LooseSand, PegKind.Steel) => 33,
            (SoilKind.LooseSand, PegKind.GroundScrew) => 32,
            _ => 45
        };

        private int RecommendedPressure(SoilKind sk, PegKind pk) => (sk, pk) switch
        {
            (SoilKind.Firm, PegKind.Aluminum) => 55,
            (SoilKind.Firm, PegKind.Steel) => 65,
            (SoilKind.Firm, PegKind.GroundScrew) => 45,
            (SoilKind.Firm, PegKind.SandAnchor) => 60,

            (SoilKind.LooseSand, PegKind.SandAnchor) => 85,
            (SoilKind.LooseSand, PegKind.Aluminum) => 75,
            (SoilKind.LooseSand, PegKind.Steel) => 80,
            (SoilKind.LooseSand, PegKind.GroundScrew) => 70,
            _ => 60
        };

        private int RecommendedTension(SoilKind sk, PegKind pk, float wind01)
        {
            int baseVal = (sk, pk) switch
            {
                (SoilKind.Firm, PegKind.Aluminum) => 45,
                (SoilKind.Firm, PegKind.Steel) => 50,
                (SoilKind.Firm, PegKind.GroundScrew) => 55,
                (SoilKind.Firm, PegKind.SandAnchor) => 48,

                (SoilKind.LooseSand, PegKind.SandAnchor) => 55,
                (SoilKind.LooseSand, PegKind.Aluminum) => 55,
                (SoilKind.LooseSand, PegKind.Steel) => 55,
                (SoilKind.LooseSand, PegKind.GroundScrew) => 60,
                _ => 50
            };
            return Math.Max(0, Math.Min(100, baseVal + (int)(wind01 * 20)));
        }

        private void EvaluateAndRedraw()
        {
            int angle = tbAngle.Value;
            int pressure = tbPressure.Value;
            int tension = tbTension.Value;

            lblAngle.Text = $"Γωνία: {angle}°";
            lblPressure.Text = $"Πίεση/Βάθος: {pressure}%";
            lblTension.Text = $"Τάση σχοινιού: {tension}%";

            var pk = CurrentPegKind;
            var recA = RecommendedAngle(_soil, pk);
            var recP = RecommendedPressure(_soil, pk);
            var recT = RecommendedTension(_soil, pk, _wind01);

            float aTol = pk switch { PegKind.GroundScrew => 8f, PegKind.SandAnchor => 10f, _ => 12f };
            int pTol = pk == PegKind.GroundScrew ? 12 : 15;
            int tTol = 15;

            float angleScore = 100f * Math.Max(0, 1f - Math.Abs(angle - recA) / aTol);
            float pressureScore = 100f * Math.Max(0, 1f - Math.Abs(pressure - recP) / (float)pTol);
            float tensionScore = 100f * Math.Max(0, 1f - Math.Abs(tension - recT) / (float)tTol);

            float typeAdj = 0f;
            if (_soil == SoilKind.LooseSand && pk == PegKind.SandAnchor) typeAdj += 8f;
            if (_soil == SoilKind.Firm && (pk == PegKind.Steel || pk == PegKind.GroundScrew)) typeAdj += 6f;
            if (_soil == SoilKind.Firm && pk == PegKind.SandAnchor) typeAdj -= 10f;

            float windPenalty = _wind01 * (Math.Abs(angle - recA) / aTol * 15f + Math.Abs(tension - recT) / (float)tTol * 15f);

            float total = 0.45f * angleScore + 0.35f * pressureScore + 0.20f * tensionScore - windPenalty + typeAdj;
            total = Math.Max(0, Math.Min(100, total));

            string status; Color color;
            if (total >= 80) { status = "Εξαιρετικό"; color = Color.SeaGreen; }
            else if (total >= 60) { status = "Καλό"; color = Color.OliveDrab; }
            else if (total >= 40) { status = "Οριακό"; color = Color.DarkOrange; }
            else { status = "Λάθος"; color = Color.Firebrick; }

            lblScore.ForeColor = color;
            lblScore.Text = $"Σκορ: {(int)total} – {status}";
            lblTips.Text = BuildTips(_soil, angle, recA, pressure, recP, tension, recT, pk);

            canvas.Invalidate();
        }

        private string BuildTips(SoilKind sk, int angle, int recA, int p, int recP, int t, int recT, PegKind pk)
        {
            var s = "Συμβουλές:\n";
            if (Math.Abs(angle - recA) > 6) s += $"• Ρύθμισε γωνία προς ~{recA}°.\n";
            if (Math.Abs(p - recP) > 10) s += $"• Βάλε πίεση/βάθος κοντά στο {recP}%.\n";
            if (Math.Abs(t - recT) > 10) s += $"• Ρύθμισε τάση κοντά στο {recT}%.\n";
            if (sk == SoilKind.LooseSand && pk != PegKind.SandAnchor) s += "• Σε άμμο προτίμησε Sand Anchor ή θαμμένο «deadman».\n";
            if (sk == SoilKind.Firm && pk == PegKind.Aluminum) s += "• Σε σκληρό έδαφος προτίμησε ατσαλένιο ή βίδα εδάφους.\n";
            s += "• Προσανατόλισε τον πάσσαλο μακριά από τη σκηνή.\n";
            return s;
        }

        private int ParseScoreFromLabel(string txt)
        {
            foreach (var token in txt.Split(' '))
                if (int.TryParse(token, out var n)) return Math.Max(0, Math.Min(100, n));
            return 0;
        }

        private void Canvas_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            var r = canvas.ClientRectangle;

            // έδαφος
            int groundY = (int)(r.Height * 0.65);
            using (var grPen = new Pen(ColorTranslator.FromHtml("#8BC34A"), 14))
                g.DrawLine(grPen, 30, groundY, r.Width - 30, groundY);

            // πάσσαλος
            int angle = tbAngle.Value;
            double rad = angle * Math.PI / 180.0;
            Point basePt = new Point((int)(r.Width * 0.35), groundY);
            int pegLen = (int)(r.Height * 0.35);
            Point tip = new Point(
                basePt.X + (int)(Math.Cos(rad) * pegLen),
                basePt.Y - (int)(Math.Sin(rad) * pegLen));

            // οδηγός recommended
            int recA = RecommendedAngle(_soil, CurrentPegKind);
            double recRad = recA * Math.PI / 180.0;
            using (var guide = new Pen(Color.Gray, 2) { DashStyle = DashStyle.Dash })
            {
                int len = (int)(r.Height * 0.32);
                Point recTip = new Point(
                    basePt.X + (int)(Math.Cos(recRad) * len),
                    basePt.Y - (int)(Math.Sin(recRad) * len));
                g.DrawLine(guide, basePt, recTip);
            }

            Color pegColor = lblScore.ForeColor;
            using (var pen = new Pen(pegColor, 8)) { pen.EndCap = LineCap.Round; pen.StartCap = LineCap.Round; g.DrawLine(pen, basePt, tip); }

            // σχοινί
            var ropePen = new Pen(Color.SaddleBrown, 3) { DashStyle = DashStyle.Dot };
            Point tentTop = new Point((int)(r.Width * 0.55), (int)(groundY - r.Height * 0.28));
            g.DrawLine(ropePen, tip, tentTop);

            // πάνω-αριστερά info
            using var f = new Font("Segoe UI", 10.5f);
            g.DrawString($"Γωνία {tbAngle.Value}°  |  Πίεση {tbPressure.Value}%  |  Τάση {tbTension.Value}%", f, Brushes.Black, new PointF(20, 20));
        }
    }
}
