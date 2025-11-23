using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using SmartCamping.Models;
using SmartCamping.Services;

namespace SmartCamping.Views
{
    public sealed partial class EventsForm : Form
    {
        //  Services 
        private readonly EventsService _svc;

        //  UI refs 
        private TableLayoutPanel _root = null!;
        private FlowLayoutPanel _flowLeft = null!;

        private PictureBox _picHero = null!;
        private Label _lblHeroTitle = null!;
        private Label _lblMeta = null!;
        private TextBox _txtDesc = null!;
        private ProgressBar _pbCap = null!;
        private Label _lblCap = null!;
        private Button _btnJoin = null!;

        private readonly List<CampEvent> _events = new();
        private CampEvent? _current;
        private readonly Dictionary<CampEvent, Panel> _cardIndex = new();

        //  Theme 
        private static readonly Color Teal = ColorTranslator.FromHtml("#167A65");
        private static readonly Color TealDark = ColorTranslator.FromHtml("#0F5B4B");
        private static readonly Color TealSoft = ColorTranslator.FromHtml("#1C8A73");
        private static readonly Color Paper = Color.FromArgb(250, 248, 244);
        private static readonly Color CardBg = Color.White;
        private static readonly Font TitleFont = new Font("Segoe UI", 18, FontStyle.Bold);

        public EventsForm(EventsService svc)
        {
            _svc = svc ?? throw new ArgumentNullException(nameof(svc));

            DoubleBuffered = true;
            Text = "Εκδηλώσεις";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(980, 620);
            Size = new Size(1100, 700);
            Font = new Font("Segoe UI", 10f);
            BackColor = Paper;

            BuildUi();
            TryHookChangeEvent();
            LoadAndRender();
        }

        // UI
        private void BuildUi()
        {
            _root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10),
                BackColor = Color.Transparent
            };
            _root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360)); // left list
            _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // right details
            Controls.Add(_root);

            var leftBorder = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(6, 8, 6, 8)
            };
            var leftScroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            _flowLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            leftScroll.Controls.Add(_flowLeft);
            leftBorder.Controls.Add(leftScroll);
            _root.Controls.Add(leftBorder, 0, 0);

            //  right: details 
            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // hero
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // meta
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // description
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // join
            _root.Controls.Add(right, 1, 0);

           
            var hero = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White,
                Padding = new Padding(8),
                Margin = new Padding(0, 0, 0, 10),
                Height = 360 // πιο άνετο ύψος για να χωράει η εικόνα
            };
            hero.RowStyles.Add(new RowStyle(SizeType.Absolute, 260)); // εικόνα μεγάλη
            hero.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // τίτλος

            _picHero = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom, // κρατάει αναλογίες
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };
            _lblHeroTitle = new Label
            {
                Text = "Επιλογή εκδήλωσης",
                AutoSize = true,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = TitleFont,
                ForeColor = TealDark,
                Padding = new Padding(0, 8, 0, 0)
            };

            hero.Controls.Add(_picHero, 0, 0);
            hero.Controls.Add(_lblHeroTitle, 0, 1);
            right.Controls.Add(hero, 0, 0);


            var meta = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                RowCount = 2,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 8)
            };
            meta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            meta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            meta.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));

            _lblMeta = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 2, 0, 6)
            };
            _lblCap = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 2, 0, 6)
            };
            _pbCap = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Height = 12
            };

            meta.Controls.Add(_lblMeta, 0, 0);
            meta.SetColumnSpan(_lblMeta, 2);
            meta.Controls.Add(_lblCap, 2, 0);
            meta.Controls.Add(_pbCap, 2, 1);
            right.Controls.Add(meta, 0, 1);

            // description
            _txtDesc = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };


            right.Controls.Add(_txtDesc, 0, 2);

            // join bar
            var joinBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 0)
            };
            _btnJoin = MakeSolid("Συμμετοχή");
            _btnJoin.Click += (_, __) => JoinSelected();
            joinBar.Controls.Add(_btnJoin);
            right.Controls.Add(joinBar, 0, 3);
        }

        // Data + 
        private void LoadAndRender()
        {
            _events.Clear();

            //  Πάρε ό,τι δίνει το service 
            _events.AddRange(GetEventsFromService().OrderBy(e => e.Start));

            if (_events.Count == 0)
            {
                TrySeedFromService();
                _events.AddRange(GetEventsFromService().OrderBy(e => e.Start));
            }

            if (_events.Count == 0)
            {
                _events.AddRange(BuildDemoEvents());
            }

            BuildCards();

            if (_events.Count > 0)
                ShowDetails(_events[0]);
            else
                _txtDesc.Text = "Δεν υπάρχουν διαθέσιμες εκδηλώσεις αυτή τη στιγμή.";
        }

        private IEnumerable<CampEvent> GetEventsFromService()
        {
            var t = _svc.GetType();
            object? raw = t.GetProperty("Events", BindingFlags.Public | BindingFlags.Instance)
                         ?.GetValue(_svc);

            if (raw == null)
            {
                var m = t.GetMethod("GetEvents") ?? t.GetMethod("All") ?? t.GetMethod("AllEvents");
                if (m != null) raw = m.Invoke(_svc, null);
            }

            if (raw is IEnumerable en)
            {
                foreach (var item in en)
                    if (item is CampEvent ce) yield return ce;
            }
        }

        private void BuildCards()
        {
            _cardIndex.Clear();
            _flowLeft.SuspendLayout();
            _flowLeft.Controls.Clear();

            foreach (var e in _events)
            {
                var p = MakeCard(e);
                _cardIndex[e] = p;
                _flowLeft.Controls.Add(p);
            }

            _flowLeft.ResumeLayout();
        }

        private Panel MakeCard(CampEvent e)
        {
            var card = new Panel
            {
                Width = 320,
                Height = 110,
                BackColor = CardBg,
                Margin = new Padding(6, 6, 6, 10),
                Padding = new Padding(10),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };

            // image
            var pic = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black,
                Width = 80,
                Height = 80,
                Left = 10,
                Top = 14
            };
            var img = TryLoadImageFor(e);
            if (img != null) pic.Image = img;

            // title
            var lblTitle = new Label
            {
                Text = e.Title,
                AutoSize = false,
                Left = 100,
                Top = 10,
                Width = 180,
                Height = 24,
                Font = new Font(Font, FontStyle.Bold)
            };

            // when
            var lblWhen = new Label
            {
                Text = $"{GreekDay(e.Start)} {e.Start:dd/MM} {e.Start:HH:mm}–{e.End:HH:mm}",
                AutoSize = false,
                Left = 100,
                Top = 36,
                Width = 190,
                Height = 22,
                ForeColor = Color.DimGray
            };

            int cap = Math.Max(1, AsInt(e.Capacity));
            int reg = Math.Max(0, AsInt(e.Registered));
            double ratio = Math.Min(1.0, reg / (double)cap);

            var pill = new Panel
            {
                Width = 72,
                Height = 26,
                Top = card.Height - 32,           // στο κάτω μέρος
                Left = card.Width - 16 - 72,      // δεξιά
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,

                BackColor = Color.White,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                
            };

            // περίγραμμα
            pill.Paint += (_, pe) =>
            {
                using var pen = new Pen(Teal, 1);
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                pe.Graphics.DrawRectangle(pen, 0, 0, pill.Width - 1, pill.Height - 1);
            };

            // πράσινο progress 
            var fill = new Panel
            {
                Dock = DockStyle.Left,
                Width = (int)Math.Round((pill.Width - 2) * ratio),
                BackColor = Teal
            };

            var lblPill = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                ForeColor = Color.Black,
                Font = new Font(Font.FontFamily, 9f, FontStyle.Bold),
                Text = $"{reg}/{cap}",
                Bounds = new Rectangle(0, 0, pill.Width, pill.Height) // πιάνει όλο το pill
            };

            pill.Resize += (_, __) =>
            {
                fill.Width = (int)Math.Round((pill.Width - 2) * ratio);
                lblPill.Bounds = new Rectangle(0, 0, pill.Width, pill.Height);
            };

            pill.Controls.Add(fill);
            pill.Controls.Add(lblPill);
            lblPill.BringToFront();

         



            pill.Controls.SetChildIndex(lblPill, 0);
            var tip = new ToolTip();
            tip.SetToolTip(pill, $"{reg}/{cap} ({Math.Round(ratio * 100)}%)");

            // assemble
            card.Controls.Add(pic);
            card.Controls.Add(lblTitle);
            card.Controls.Add(lblWhen);
            card.Controls.Add(pill);

            // selection behavior
            void Select(object? s, EventArgs a)
            {
                ShowDetails(e);
                Highlight(card);
            }
            

            card.Click += Select;
            foreach (Control c in card.Controls) c.Click += Select;

            return card;

            void Highlight(Panel p)
            {
                foreach (var kv in _cardIndex) kv.Value.BackColor = CardBg;
                p.BackColor = Color.FromArgb(245, 252, 248);
            }
        }

        private void ShowDetails(CampEvent e)
        {
            _current = e;

            _lblHeroTitle.Text = e.Title;
            _lblHeroTitle.ForeColor = TealDark;

            var img = TryLoadImageFor(e);
            if (_picHero.Image != null)
            {
                var old = _picHero.Image;
                _picHero.Image = null;
                try { old.Dispose(); } catch { /* ignore */ }
            }
            _picHero.Image = img;                
            _picHero.SizeMode = PictureBoxSizeMode.Zoom;
            _picHero.Invalidate();

            _lblMeta.Text = $"Ημερομηνία/Ώρα: {GreekDay(e.Start)} {e.Start:dd/MM} {e.Start:HH:mm} – {e.End:HH:mm}    Τοποθεσία: {e.Location}";
            _txtDesc.Text = e.Description ?? "";

            UpdateCapacityUI(e);
            _btnJoin.Enabled = AsInt(e.Registered) < AsInt(e.Capacity);
        }

        private void UpdateCapacityUI(CampEvent e)
        {
            int reg = AsInt(e.GetType().GetProperty("Registered")?.GetValue(e));
            int cap = AsInt(e.GetType().GetProperty("Capacity")?.GetValue(e));

            reg = Math.Max(0, reg);
            cap = Math.Max(1, cap);

            _lblCap.Text = $"Διαθεσιμότητα: {reg}/{cap}";
            _pbCap.Maximum = cap;
            _pbCap.Value = Math.Min(reg, cap);
        }

        // Actions
        private void JoinSelected()
        {
            if (_current == null) return;

            try
            {
                var t = _svc.GetType();
                var m = t.GetMethod("Join", new[] { typeof(Guid) }) ??
                        t.GetMethod("JoinEvent", new[] { typeof(Guid) }) ??
                        t.GetMethod("Join", new[] { typeof(CampEvent) }) ??
                        t.GetMethod("JoinEvent", new[] { typeof(CampEvent) });

                if (m != null)
                {
                    var arg = m.GetParameters()[0].ParameterType == typeof(Guid)
                              ? (object)_current.Id
                              : (object)_current;
                    m.Invoke(_svc, new[] { arg });
                }
                else
                {
                    if (AsInt(_current.Registered) < AsInt(_current.Capacity))
                        _current.Registered++;
                }

                LoadAndRender();
                var again = _events.FirstOrDefault(x => x.Id == _current.Id) ?? _events.FirstOrDefault();
                if (again != null) ShowDetails(again);
            }
            catch
            {
                if (AsInt(_current.Registered) < AsInt(_current.Capacity))
                    _current.Registered++;
                UpdateCapacityUI(_current);
            }
        }

        // Helpers
        private static int AsInt(object? v)
        {
            if (v == null) return 0;
            try { return Convert.ToInt32(v); }
            catch { return 0; }
        }

        private static string GreekDay(DateTime d)
        {
            string[] days = { "Κυρ", "Δευ", "Τρι", "Τετ", "Πεμ", "Παρ", "Σαβ" };
            return days[(int)d.DayOfWeek];
        }

        private Image? TryLoadImageFor(CampEvent e)
        {
            try
            {
                string? file = null;
                var t = e.GetType();
                file = t.GetProperty("ImagePath")?.GetValue(e) as string
                    ?? t.GetProperty("ImageFile")?.GetValue(e) as string;

                file ??= MapTitleToFile(e.Title);
                if (string.IsNullOrWhiteSpace(file)) return null;

                var baseDir = AppContext.BaseDirectory;
                var path = Path.Combine(baseDir, "Assets", file);
                if (!File.Exists(path)) return null;

                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    return new Bitmap(fs);
            }
            catch
            {
                return null;
            }
        }

        private static string? MapTitleToFile(string title)
        {
            var t = (title ?? "").ToLowerInvariant();

            if (t.Contains("yoga")) return "sunset_yoga.jpg";
            if (t.Contains("movie")) return "movie_night.jpg";
            if (t.Contains("volley")) return "beach_volley.jpg";
            if (t.Contains("bbq")) return "bbq.jpg";
            if (t.Contains("stargaz")) return "stargazing.jpg";
            if (t.Contains("πεζοπορ") || t.Contains("φαράγ"))
                return "canyon.jpg";

            return null;
        }

        private Button MakeSolid(string text)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = true,
                BackColor = Teal,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Padding = new Padding(14, 8, 14, 8),
                Margin = new Padding(6),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.MouseEnter += (_, __) => b.BackColor = TealSoft;
            b.MouseLeave += (_, __) => b.BackColor = Teal;
            return b;
        }

        private void TrySeedFromService()
        {
            var t = _svc.GetType();
            var names = new[] { "FillDemo", "Seed", "SeedDemo", "EnsureDemo", "EnsureSeed", "Init", "Initialize" };
            foreach (var n in names)
            {
                var m = t.GetMethod(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m != null && m.GetParameters().Length == 0)
                {
                    try { m.Invoke(_svc, null); } catch { /* ignore */ }
                    break;
                }
            }
        }

        private IEnumerable<CampEvent> BuildDemoEvents()
        {
            DateTime d0 = DateTime.Today;
            return new[]
            {
                new CampEvent { Id = Guid.NewGuid(), Title = "Sunset Yoga",            Start = d0.AddDays(1).AddHours(18), End = d0.AddDays(1).AddHours(19), Location = "Ακτή A1", Capacity = 25, Registered = 8,  Description = "Χαλαρωτική απογευματινή πρακτική με θέα το ηλιοβασίλεμα.", ImageFile = "sunset_yoga.jpg" },
                new CampEvent { Id = Guid.NewGuid(), Title = "Movie Night",            Start = d0.AddDays(1).AddHours(21), End = d0.AddDays(1).AddHours(23), Location = "Κεντρική Σκηνή", Capacity = 60, Registered = 12, Description = "Υπαίθρια προβολή με popcorn.", ImageFile = "movie_night.jpg" },
                new CampEvent { Id = Guid.NewGuid(), Title = "Beach Volleyball 4x4",   Start = d0.AddDays(2).AddHours(16), End = d0.AddDays(2).AddHours(19), Location = "Γήπεδο Άμμου", Capacity = 32, Registered = 10, Description = "Φιλικό τουρνουά 4x4.", ImageFile = "beach_volley.jpg" },
                new CampEvent { Id = Guid.NewGuid(), Title = "BBQ Night",              Start = d0.AddDays(2).AddHours(20), End = d0.AddDays(2).AddHours(23), Location = "Πλατεία", Capacity = 80, Registered = 30, Description = "Ψησταριές, μουσική και καλή παρέα.", ImageFile = "bbq.jpg" },
                new CampEvent { Id = Guid.NewGuid(), Title = "Πεζοπορία στο φαράγγι",  Start = d0.AddDays(3).AddHours(9),  End = d0.AddDays(3).AddHours(12), Location = "Σημείο Αφετηρίας Β", Capacity = 30, Registered = 5,  Description = "Διαδρομή μέτριας δυσκολίας. Απαραίτητα νερό και καπέλο.", ImageFile = "canyon.jpg" },
                new CampEvent { Id = Guid.NewGuid(), Title = "Stargazing",             Start = d0.AddDays(3).AddHours(22), End = d0.AddDays(3).AddHours(23), Location = "Λόφος", Capacity = 40, Registered = 7,  Description = "Παρατήρηση νυχτερινού ουρανού με τηλεσκόπιο.", ImageFile = "stargazing.jpg" },
            };
        }

        private void TryHookChangeEvent()
        {
            var ev = _svc.GetType().GetEvent("EventsChanged");
            if (ev == null) return;

            if (ev.EventHandlerType == typeof(EventHandler))
            {
                EventHandler h = (_, __) => SafeUi(LoadAndRender);
                ev.AddEventHandler(_svc, h);
            }
        }

        private void SafeUi(Action a)
        {
            if (!IsHandleCreated) return;
            if (InvokeRequired) BeginInvoke(a);
            else a();
        }
    }
}
