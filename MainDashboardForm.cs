using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SmartCamping.Views;
using SmartCamping.Models;
using SmartCamping.Services;

namespace SmartCamping
{
    public partial class MainDashboardForm : Form
    {
        private SiteSelectionResult _lastSite;
        private Panel _topSpacer;

        private readonly OrdersService _ordersService = new OrdersService();
        private readonly EventsService _eventsService = new EventsService();

        private static readonly Color TealDark = ColorTranslator.FromHtml("#0F5B4B");
        private static readonly Color Teal = ColorTranslator.FromHtml("#167A65");
        private static readonly Color TealHover = ColorTranslator.FromHtml("#1C8A73");
        private static readonly Color Cream = ColorTranslator.FromHtml("#FFF3E0");
        private static readonly Color AppBg = ColorTranslator.FromHtml("#E9F4EE");
        private static readonly Color TextMain = ColorTranslator.FromHtml("#203028");
        private static readonly Color White = Color.White;

        private Panel panelMenu, panelContent, panelStatus;
        private PictureBox picture;
        private Label lblTemp, lblWind, lblBatt, lblPv;

        // Υπηρεσίες
        private readonly LightingService _lightingService = new LightingService();
        private readonly EnergyService _energyService;
        private readonly NavigationService _navService = new NavigationService();
        private readonly WeatherService _weatherService = new WeatherService();

        // Tooltips
        private ToolTip _tooltips;

        public MainDashboardForm()
        {
            InitializeComponent();
            BuildForm();
            BuildLayout();
            BuildStatusBar();
            BuildHelpButton();

            _tooltips = new ToolTip();
            BuildMenu();

            UpdateStatus("21°C", "5 km/h", "78%", "65 W");

            // Badge τίτλου για φωτισμό
            _lightingService.StateChanged += (s, st) => BeginInvoke(new Action(() => UpdateLightingBadge(st)));
            UpdateLightingBadge(_lightingService.State);

            // Energy service
            _energyService = new EnergyService(_lightingService);

            //  ενημέρωση status bar
            _energyService.StateChanged += (s, st) => BeginInvoke(new Action(() =>
            {
                lblBatt.Text = $"{st.BatteryPercent}%";
                lblPv.Text = $"{st.PvPowerW} W";
            }));
        }

        private void BuildForm()
        {
            Text = "Έξυπνο Camping – Πίνακας Ελέγχου";
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            MinimumSize = new Size(1100, 700);
            Size = new Size(1220, 760);
            BackColor = AppBg;
            Font = new Font("Segoe UI", 10f);
            ForeColor = TextMain;
            DoubleBuffered = true;
        }

        private void BuildLayout()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(0, 6, 0, 0),
                Padding = Padding.Empty
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(grid);

            panelMenu = new Panel
            {
                BackColor = TealDark,
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 14, 16, 14)
            };
            grid.Controls.Add(panelMenu, 0, 0);
            grid.SetRowSpan(panelMenu, 2);

            panelStatus = new Panel
            {
                BackColor = Cream,
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 14, 16, 10)
            };
            grid.Controls.Add(panelStatus, 1, 0);

            panelContent = new Panel
            {
                BackColor = White,
                Dock = DockStyle.Fill,
                Padding = Padding.Empty
            };
            grid.Controls.Add(panelContent, 1, 1);

            picture = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            panelContent.Controls.Add(picture);

            try
            {
                string path = Path.Combine(Application.StartupPath, "Assets", "main_camping.jpg");
                if (File.Exists(path)) picture.Image = Image.FromFile(path);
            }
            catch { }
        }

        private void BuildStatusBar()
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panelStatus.Controls.Add(tbl);

            tbl.Controls.Add(MetricTitle("Θερμοκρασία"), 0, 0);
            tbl.Controls.Add(MetricTitle("Άνεμος"), 1, 0);
            tbl.Controls.Add(MetricTitle("Μπαταρία"), 2, 0);
            tbl.Controls.Add(MetricTitle("PV"), 3, 0);

            lblTemp = MetricValue();
            lblWind = MetricValue();
            lblBatt = MetricValue();
            lblPv = MetricValue();

            tbl.Controls.Add(lblTemp, 0, 1);
            tbl.Controls.Add(lblWind, 1, 1);
            tbl.Controls.Add(lblBatt, 2, 1);
            tbl.Controls.Add(lblPv, 3, 1);
        }

        private Label MetricTitle(string text) => new Label
        {
            Text = text + ":",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 2),
            Font = new Font("Segoe UI Semibold", 12f),
            ForeColor = TextMain,
            TextAlign = ContentAlignment.BottomCenter
        };

        private Label MetricValue() => new Label
        {
            Text = "—",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 0),
            Font = new Font("Segoe UI Semibold", 20f),
            ForeColor = TextMain,
            TextAlign = ContentAlignment.TopCenter
        };

        private void UpdateStatus(string temp, string wind, string batt, string pv)
        {
            lblTemp.Text = temp;
            lblWind.Text = wind;
            lblBatt.Text = batt;
            lblPv.Text = pv;
        }

        private void BuildMenu()
        {
            var title = new Label
            {
                Text = "Camping",
                Dock = DockStyle.Top,
                Height = 44,
                Font = new Font("Segoe UI Semibold", 20f),
                ForeColor = White,
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelMenu.Controls.Add(title);

            var stack = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 10, 0, 0),
                BackColor = TealDark,
                AutoScroll = true
            };
            panelMenu.Controls.Add(stack);
            title.BringToFront();
            stack.BringToFront();

            stack.Controls.Add(MenuButton("Στήσιμο Σκηνής", () =>
            {
                using var f = new SiteSelectionForm();
                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    _lastSite = f.Result;
                    MessageBox.Show(
                        "Σημείο αποθηκεύτηκε!\n" +
                        $"Ήλιος: {(int)(_lastSite.Sun * 100)}% | Υγρασία: {(int)(_lastSite.Humidity * 100)}%\n" +
                        $"Άνεμος: {(int)(_lastSite.Wind * 100)}% | Σταθερότητα: {(int)(_lastSite.Stability * 100)}%\n" +
                        $"Πρόταση: {_lastSite.Advice}", "OK");
                }
            }));

            stack.Controls.Add(MenuButton("Τοποθέτηση Πασάλων", () =>
            {
                if (_lastSite == null)
                {
                    MessageBox.Show("Πρώτα επίλεξε σημείο στησίματος (Στήσιμο Σκηνής).",
                                    "Δεν υπάρχει σημείο", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var f = new PegPlacementForm(_lastSite);
                if (f.ShowDialog(this) == DialogResult.OK && f.Result != null)
                {
                    var r = f.Result;
                    MessageBox.Show(
                        $"Ρυθμίσεις πασάλων:\n" +
                        $"- Γωνία: {r.Angle}°\n- Πίεση: {r.Pressure}%\n- Τάση: {r.Tension}%\n- Τύπος: {r.PegType}\n" +
                        $"- Σκορ: {r.Score}/100",
                        "Αποθηκεύτηκαν");
                }
            }));

            stack.Controls.Add(MenuButton("Προστατευτικά Πανιά", () =>
            {
                float? wind01 = null;
                if (_lastSite != null)
                    wind01 = (float)Math.Max(0, Math.Min(1, _lastSite.Wind));

                using var f = new TarpPlacementForm(240f, wind01);
                if (f.ShowDialog(this) == DialogResult.OK && f.Result != null)
                {
                    MessageBox.Show(
                        $"Deploy {f.Result.Items.Count} πανιών.\n" +
                        $"Άνεμος: {(int)(f.Result.WindStrength01 * 100)}% @ {f.Result.WindDirDeg:0}°\n\n" +
                        f.Result.Advice,
                        "Εκτέλεση");
                }
            }));

            stack.Controls.Add(MenuButton("Φωτισμός", () =>
            {
                using var f = new LightingForm(_lightingService);
                f.ShowDialog(this);
            }));

            stack.Controls.Add(MenuButton("Ενέργεια", () =>
            {
                using var f = new EnergyForm(_energyService);
                f.ShowDialog(this);
            }));

            stack.Controls.Add(MenuButton("Καιρός", () =>
            {
                using var f = new WeatherForm(_weatherService);
                f.ShowDialog(this);
            }));

            stack.Controls.Add(MenuButton("Καταφύγιο\nΠλοήγηση", () =>
            {
                using var f = new NavigationForm(_navService);
                f.ShowDialog(this);
            }));

            stack.Controls.Add(MenuButton("Τουριστική\nΠλοήγηση", () =>
            {
                using var f = new TouristNavigationForm();
                f.ShowDialog(this);
            }));

            stack.Controls.Add(MenuButton("Παραγγελίες &\nΕκδηλώσεις", () =>
            {
                using var f = new OrdersForm(_ordersService, _eventsService);
                f.ShowDialog(this);
            }));
        }

        private Control MenuButton(string text, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Width = 230,
                Height = 58,
                Margin = new Padding(0, 10, 0, 0),
                BackColor = Teal,
                ForeColor = White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 12.5f),
                TextAlign = ContentAlignment.MiddleCenter,
                UseCompatibleTextRendering = true
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => onClick();
            btn.MouseEnter += (s, e) => btn.BackColor = TealHover;
            btn.MouseLeave += (s, e) => btn.BackColor = Teal;
            return btn;
        }

        private void UpdateLightingBadge(LightingState st)
        {
            string mode = st.Effect.ToString();
            string onoff = st.IsOn ? "ON" : "OFF";
            Text = $"Έξυπνο Camping – Πίνακας Ελέγχου  |  Φως: {onoff}  {st.Brightness}%  {mode}";
        }

        private void BuildHelpButton()
        {
            var helpBtn = new Button
            {
                Text = "?",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Size = new Size(36, 36),
                BackColor = Color.LightGray,
                FlatStyle = FlatStyle.Flat
            };
            helpBtn.FlatAppearance.BorderSize = 0;
            helpBtn.Click += (s, e) => ShowQuickTips();

            this.Controls.Add(helpBtn);
            helpBtn.BringToFront();

            // όταν αλλάζει μέγεθος το form, να μένει πάντα δεξιά πάνω
            this.Resize += (s, e) =>
            {
                helpBtn.Location = new Point(this.ClientSize.Width - helpBtn.Width - 10, 10);
                helpBtn.BringToFront();
            };

            // αρχική θέση
            helpBtn.Location = new Point(this.ClientSize.Width - helpBtn.Width - 10, 10);
        }


        private void ShowQuickTips()
        {
            var tips =
                "Γρήγορες Οδηγίες:\n" +
                "• Site Selection: σύρε δείκτη στον χάρτη και έλεγξε Sun/Humidity/Wind/Stability.\n" +
                "• Pegs: ρύθμισε γωνία/πίεση/τάση μέχρι να γίνει πράσινο το score.\n" +
                "• Tarps: drag/rotate τα πανιά. Σε ισχυρό άνεμο χρησιμοποίησε «Auto».\n" +
                "• Lighting: ένταση/χρώμα ή presets (Night/Reading/Pulse).\n" +
                "• Energy: δες Battery/PV/Load και άναψε Energy-Save. Ρύθμισε A/C.\n" +
                "• Navigation: επίλεξε καταφύγιο ή POI και δες ETA/απόσταση.\n" +
                "• Orders: επίλεξε γεύματα ανά ώρα και παρακολούθησε status.\n" +
                "• Events: δήλωσε συμμετοχή και πάρε ειδοποιήσεις αλλαγών.";
            MessageBox.Show(tips, "Quick Tips", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _energyService.Dispose();
            _lightingService.Dispose();
            base.OnFormClosed(e);
        }
    }
}
