using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SmartCamping.Models;
using SmartCamping.Services;

namespace SmartCamping.Views
{
    public sealed partial class OrdersForm : Form
    {
        
        private const string BackgroundFilename = "photo-1606787366850-de6330128bfc.jpg";

        //  SERVICES 
        private readonly OrdersService _svc;
        private readonly EventsService? _events;

        // Header
        private Button _btnEvents = null!;

        private ComboBox _cbPeriod = null!;
        private ComboBox _cbCategory = null!;
        private NumericUpDown _numQty = null!;
        private TextBox _txtTent = null!;
        private Button _btnAdd = null!;
        private ListView _lvMenu = null!;

        // Orders 
        private ListView _lvOrders = null!;
        private Button _btnPay = null!;
        private Button _btnCharge = null!;
        private Button _btnClear = null!;
        private ListBox _lstChat = null!;
        private TextBox _txtMsg = null!;
        private Button _btnSend = null!;

        //  THEME 
        private static readonly Color Teal = ColorTranslator.FromHtml("#167A65");
        private static readonly Color TealDark = ColorTranslator.FromHtml("#0F5B4B");
        private static readonly Color TealSoft = ColorTranslator.FromHtml("#1C8A73");
        private static readonly Color Paper = Color.FromArgb(248, 252, 250);

        private static readonly Color Accent = ColorTranslator.FromHtml("#22D3EE"); // cyan-400
        private static readonly Color AccentHover = ColorTranslator.FromHtml("#06B6D4"); // cyan-500

        public OrdersForm(OrdersService svc, EventsService? eventsSvc = null)
        {
            _svc = svc ?? throw new ArgumentNullException(nameof(svc));
            _events = eventsSvc;

            DoubleBuffered = true;

            BuildUi();
            ApplyBackground();         
            Wire();
            RebuildMenu();
            RebuildOrders();

            try { _svc.OrdersChanged += () => SafeUi(RebuildOrders); } catch { }
        }

        //  UI 
        private void BuildUi()
        {
            Text = "Παραγγελίες & Εκδηλώσεις";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1100, 680);
            Size = new Size(1180, 720);
            Font = new Font("Segoe UI", 10f);
            BackColor = Paper;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10),
                BackColor = Color.Transparent
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));         // header
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 250f));   // catalog 
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));    // orders + chat
            Controls.Add(root);

            //  Header 
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = Color.Transparent
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var lblTitle = new Label
            {
                Text = "Παραγγελίες  Εκδηλώσεις",
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                ForeColor = TealDark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(2, 4, 0, 8)
            };

            _btnEvents = MakeAccent("Εκδηλώσεις…"); 

            header.Controls.Add(lblTitle, 0, 0);
            header.Controls.Add(_btnEvents, 1, 0);
            root.Controls.Add(header, 0, 0);

            var catalog = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White,
                Padding = new Padding(8)
            };
            catalog.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // φίλτρα
            catalog.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // λίστα ειδών
            root.Controls.Add(MakeGroup("Κατάλογος προϊόντων", catalog), 0, 1);

            // filters row
            var filters = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 10,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // "Ζώνη:"
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // "Κατηγορία:"
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // "Ποσότητα:"
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // "Σκηνή:"
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // "Προσθήκη"

            filters.Controls.Add(MakeLabel("Ζώνη:"), 0, 0);
            _cbPeriod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
            _cbPeriod.Items.AddRange(Enum.GetNames(typeof(DayPeriod)));
            if (_cbPeriod.Items.Count > 0) _cbPeriod.SelectedIndex = 0;
            filters.Controls.Add(_cbPeriod, 1, 0);

            filters.Controls.Add(MakeLabel("Κατηγορία:"), 2, 0);
            _cbCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };
            _cbCategory.DisplayMember = "Text";
            _cbCategory.ValueMember = "Value";
            _cbCategory.DataSource = new[]
            {
                new { Text = "(Όλες)",  Value = (MenuCategory?)null },
                new { Text = "Καφές",   Value = (MenuCategory?)MenuCategory.Coffee },
                new { Text = "Ποτά",    Value = (MenuCategory?)MenuCategory.Drink },
                new { Text = "Γεύματα", Value = (MenuCategory?)MenuCategory.Meal },
                new { Text = "Γλυκά",   Value = (MenuCategory?)MenuCategory.Dessert },
                new { Text = "Σνακ",    Value = (MenuCategory?)MenuCategory.Snack },
            }.ToList();
            filters.Controls.Add(_cbCategory, 3, 0);

            filters.Controls.Add(MakeLabel("Ποσότητα:"), 4, 0);
            _numQty = new NumericUpDown { Minimum = 1, Maximum = 20, Value = 1, Width = 70 };
            filters.Controls.Add(_numQty, 5, 0);

            filters.Controls.Add(MakeLabel("Σκηνή:"), 6, 0);
            _txtTent = new TextBox { Text = "A1", Width = 80 };
            filters.Controls.Add(_txtTent, 7, 0);

            filters.Controls.Add(new Label { AutoSize = true }, 8, 0); // filler
            _btnAdd = MakeSolid("Προσθήκη");
            filters.Controls.Add(_btnAdd, 9, 0);

            catalog.Controls.Add(filters, 0, 0);

            // menu list
            _lvMenu = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false
            };
            _lvMenu.Columns.Add("Είδος", 520);
            _lvMenu.Columns.Add("Τιμή", 120, HorizontalAlignment.Right);
            catalog.Controls.Add(_lvMenu, 0, 1);

            //  Bottom: Orders
            var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.Transparent };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54)); // orders
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46)); // chat
            root.Controls.Add(bottom, 0, 2);

            // left: orders
            var ordersPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = Color.Transparent };
            ordersPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            ordersPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _lvOrders = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false
            };
            _lvOrders.Columns.Add("#", 70);
            _lvOrders.Columns.Add("Σκηνή", 100);
            _lvOrders.Columns.Add("Κατάσταση", 160);
            _lvOrders.Columns.Add("Σύνολο", 100, HorizontalAlignment.Right);

            ordersPanel.Controls.Add(MakeGroup("Ενεργές παραγγελίες", _lvOrders), 0, 0);

            var barOrders = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Padding = new Padding(6, 6, 6, 0),
                BackColor = Color.Transparent
            };
            _btnPay = MakeSolid("Πληρωμή");
            _btnCharge = MakeSolid("Χρέωση σκηνής");
            _btnClear = MakeSolid("Καθαρισμός");

            barOrders.Controls.AddRange(new Control[] { _btnPay, _btnCharge, _btnClear });
            ordersPanel.Controls.Add(barOrders, 0, 1);

            bottom.Controls.Add(ordersPanel, 0, 0);

            // right: chat
            var chatPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = Color.Transparent };
            chatPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            chatPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _lstChat = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
            chatPanel.Controls.Add(MakeGroup("Συνομιλία", _lstChat), 0, 0);

            var chatBar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(6, 6, 6, 0), BackColor = Color.Transparent };
            chatBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            chatBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            _txtMsg = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Γράψε μήνυμα…" };
            _btnSend = MakeSolid("Αποστολή");
            chatBar.Controls.Add(_txtMsg, 0, 0);
            chatBar.Controls.Add(_btnSend, 1, 0);
            chatPanel.Controls.Add(chatBar, 0, 1);

            bottom.Controls.Add(chatPanel, 1, 0);
        }

        private void Wire()
        {
            _cbPeriod.SelectedIndexChanged += (_, __) => RebuildMenu();
            _cbCategory.SelectedIndexChanged += (_, __) => RebuildMenu();

            _lvMenu.DoubleClick += (_, __) => AddSelectedMenuItem();
            _btnAdd.Click += (_, __) => AddSelectedMenuItem();

            _btnPay.Click += (_, __) => PaySelected();
            _btnCharge.Click += (_, __) => ChargeSelected();

            _btnClear.Click += (_, __) =>
            {
                var id = CurrentOrderId;
                if (id != null && MessageBox.Show(
                        "Να αφαιρεθούν όλα τα είδη από την επιλεγμένη παραγγελία;",
                        "Καθαρισμός", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _svc.ClearOrder(id.Value);
                }
            };

            _lvOrders.SelectedIndexChanged += (_, __) => UpdateButtons();

            _btnSend.Click += (_, __) => SendMessage();
            _txtMsg.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { SendMessage(); e.SuppressKeyPress = true; } };

            _btnEvents.Click += (_, __) => OpenEvents();
        }

        private void RebuildMenu()
        {
            var period = DayPeriod.Anytime;
            if (_cbPeriod.SelectedItem is string p && Enum.TryParse(p, out DayPeriod dp))
                period = dp;

            var catValue = _cbCategory.SelectedValue as MenuCategory?;
            var items = _svc.FilterMenu(period, catValue).ToList();

            _lvMenu.BeginUpdate();
            _lvMenu.Items.Clear();
            foreach (var it in items)
            {
                var li = new ListViewItem(it.Name) { Tag = it };
                li.SubItems.Add(it.Price.ToString("0.00") + " €");
                _lvMenu.Items.Add(li);
            }
            _lvMenu.EndUpdate();

            if (_lvMenu.Items.Count > 0) _lvMenu.Items[0].Selected = true;
        }

        private void AddSelectedMenuItem()
        {
            if (_lvMenu.SelectedItems.Count == 0)
            {
                MessageBox.Show("Διάλεξε ένα είδος από τον κατάλογο.", "Μενού");
                return;
            }

            if (_lvMenu.SelectedItems[0].Tag is not MenuItem mi) return;

            var qty = (int)_numQty.Value;
            var tent = string.IsNullOrWhiteSpace(_txtTent.Text) ? "A1" : _txtTent.Text.Trim();

            var orderId = CurrentOrderId ?? _svc.StartOrGetOpenOrder(tent).Id;
            _svc.AddItem(orderId, mi, qty);

            RebuildOrders();
            _lstChat.Items.Add($"[Staff] Προστέθηκε: {qty}× {mi.Name} ({mi.Price:0.00}€)");
        }

        private void RebuildOrders()
        {
            _lvOrders.BeginUpdate();
            _lvOrders.Items.Clear();

            foreach (var o in _svc.Orders)
            {
                var it = new ListViewItem(o.ShortId) { Tag = o.Id };
                it.SubItems.Add(o.Tent ?? "-");
                it.SubItems.Add(o.Status.ToString());
                it.SubItems.Add(o.Total.ToString("0.00") + " €");
                _lvOrders.Items.Add(it);
            }

            _lvOrders.EndUpdate();

            if (_lvOrders.Items.Count > 0 && _lvOrders.SelectedItems.Count == 0)
                _lvOrders.Items[0].Selected = true;

            UpdateButtons();
        }

        private void UpdateButtons()
        {
            var has = CurrentOrderId != null;
            _btnPay.Enabled = has;
            _btnCharge.Enabled = has;
            _btnClear.Enabled = has;
        }

        private Guid? CurrentOrderId
        {
            get
            {
                if (_lvOrders.SelectedItems.Count == 0) return null;
                return (Guid)_lvOrders.SelectedItems[0].Tag;
            }
        }

        private void PaySelected()
        {
            var id = CurrentOrderId;
            if (id == null) return;

            _svc.PayOrder(id.Value);
            RebuildOrders();
            _lstChat.Items.Add("[Staff] Η παραγγελία εξοφλήθηκε.");
        }

        private void ChargeSelected()
        {
            var id = CurrentOrderId;
            if (id == null) return;

            _svc.ChargeToTent(id.Value);
            RebuildOrders();
            _lstChat.Items.Add("[Staff] Η παραγγελία χρεώθηκε στη σκηνή.");
        }

        private void OpenEvents()
        {
            if (_events == null)
            {
                MessageBox.Show("Δεν έχει οριστεί υπηρεσία εκδηλώσεων.", "Εκδηλώσεις");
                return;
            }
            using var f = new EventsForm(_events);
            f.ShowDialog(this);
        }

        private void SendMessage()
        {
            var txt = _txtMsg.Text.Trim();
            if (string.IsNullOrEmpty(txt)) return;

            _lstChat.Items.Add($"[Εσύ] {txt}");
            _txtMsg.Clear();
        }

        //  Helpers 
        private static GroupBox MakeGroup(string title, Control inner)
        {
            var gb = new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                ForeColor = TealDark,
                BackColor = Color.White
            };
            inner.Dock = DockStyle.Fill;
            gb.Controls.Add(inner);
            return gb;
        }

        private static Label MakeLabel(string text) => new Label
        {
            Text = text,
            AutoSize = true,
            Margin = new Padding(0, 8, 8, 0)
        };

        private Button MakeSolid(string text)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = true,
                BackColor = Teal,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Padding = new Padding(12, 6, 12, 6),
                Margin = new Padding(6),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.MouseEnter += (_, __) => b.BackColor = TealSoft;
            b.MouseLeave += (_, __) => b.BackColor = Teal;
            return b;
        }

        private Button MakeAccent(string text)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = true,
                BackColor = Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Padding = new Padding(14, 8, 14, 8),
                Margin = new Padding(6),
                Cursor = Cursors.Hand,
                Font = new Font(Font, FontStyle.Bold)
            };
            b.FlatAppearance.BorderSize = 0;
            b.MouseEnter += (_, __) => b.BackColor = AccentHover;
            b.MouseLeave += (_, __) => b.BackColor = Accent;
            return b;
        }

        private void SafeUi(Action a)
        {
            if (!IsHandleCreated) return;
            if (InvokeRequired) BeginInvoke(a);
            else a();
        }

        private void ApplyBackground()
        {
            try
            {
                var exe = AppContext.BaseDirectory;
                var path = System.IO.Path.Combine(exe, "Assets", BackgroundFilename);

                if (System.IO.File.Exists(path))
                {
                    BackgroundImage = Image.FromFile(path);
                    BackgroundImageLayout = ImageLayout.Stretch;

                    foreach (Control c in Controls)
                        SetTransparentIfContainer(c);
                }
                else
                {
                    BackgroundImage = null;
                }
            }
            catch
            {
                BackgroundImage = null;
            }
        }

        private static void SetTransparentIfContainer(Control c)
        {
            if (c is Panel || c is TableLayoutPanel || c is FlowLayoutPanel)
                c.BackColor = Color.Transparent;

            foreach (Control k in c.Controls)
                SetTransparentIfContainer(k);
        }
    }
}
