using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace FoodOrderingClient
{
    public class ClientForm : Form
    {
        private ClientEngine _engine;
        private string _clientName;
        private List<MenuItem> _menu = new List<MenuItem>();
        private List<CartItem> _cart = new List<CartItem>();
        private string _selectedCategory = "All";

        private Dictionary<string, Color> _categoryColors = new Dictionary<string, Color>
        {
            { "Burgers",  Color.FromArgb(229, 57, 53)  },
            { "Wraps",    Color.FromArgb(251, 140, 0)  },
            { "Pizza",    Color.FromArgb(30, 136, 229) },
            { "Desi",     Color.FromArgb(67, 160, 71)  },
            { "Sides",    Color.FromArgb(142, 36, 170) },
            { "Drinks",   Color.FromArgb(0, 172, 193)  },
            { "Desserts", Color.FromArgb(216, 27, 96)  },
            { "All",      Color.FromArgb(55, 71, 79)   },
        };

        private Dictionary<string, string> _categoryEmoji = new Dictionary<string, string>
        {
            { "Burgers","🍔"},{"Wraps","🌯"},{"Pizza","🍕"},
            { "Desi","🍛"},{"Sides","🍟"},{"Drinks","🥤"},
            { "Desserts","🍰"},{"All","🍽️"},
        };

        private Panel _notifPanel, _headerPanel, _leftPanel, _rightCartPanel;
        private FlowLayoutPanel _menuGrid, _categoryFlow;
        private Label _notifLabel, _cartTotalLbl, _cartCountBadge;
        private Timer _notifTimer;
        private ListView _cartListView, _orderStatusListView;
        private RichTextBox _billBox, _logBox;
        private Button _placeOrderBtn, _clearCartBtn;
        private TabControl _bottomTabs;

        public ClientForm(ClientEngine engine, string name)
        {
            _engine = engine;
            _clientName = name;
            InitializeUI();
            HookEvents();
            _engine.RequestMenu();
        }

        private void HookEvents()
        {
            _engine.OnMenuReceived += items => SafeInvoke(() =>
            {
                _menu = items;
                BuildCategoryButtons();
                ShowMenuCards("All");
                AppendLog($"Menu loaded: {items.Count} items.");
            });

            _engine.OnOrderConfirmed += msg => SafeInvoke(() =>
            {
                ShowNotif("✅ " + msg, Color.FromArgb(56, 142, 60));
                AppendLog(msg);
                var parts = msg.Split(' ');
                string oid = parts.Length > 1 ? parts[1] : "---";
                _orderStatusListView.Items.Add(new ListViewItem(new[] { oid, DateTime.Now.ToString("HH:mm:ss"), "Pending" })
                { ForeColor = Color.White });
                _bottomTabs.SelectedIndex = 0;
            });

            _engine.OnOrderStatusUpdate += (oid, status) => SafeInvoke(() =>
            {
                foreach (ListViewItem lvi in _orderStatusListView.Items)
                    if (lvi.SubItems[0].Text == oid)
                    {
                        lvi.SubItems[2].Text = status;
                        lvi.ForeColor = status == "Delivered" ? Color.LightGreen :
                                        status == "Ready" ? Color.DeepSkyBlue : Color.Orange;
                    }
                Color c = status == "Ready" ? Color.FromArgb(25, 118, 210) :
                          status == "Delivered" ? Color.FromArgb(56, 142, 60) : Color.FromArgb(230, 81, 0);
                ShowNotif($"🔔 Order {oid}: {status}", c);
            });

            _engine.OnBillReceived += bill => SafeInvoke(() =>
            {
                _billBox.Text = bill;
                _bottomTabs.SelectedIndex = 2;
                ShowNotif("🧾 Bill ready! Check Bill tab.", Color.FromArgb(123, 31, 162));
            });

            _engine.OnRecommendationsReceived += items => SafeInvoke(() => HighlightRecommended(items));

            _engine.OnBroadcastReceived += msg => SafeInvoke(() =>
            {
                ShowNotif("📢 " + msg, Color.FromArgb(121, 85, 72));
                AppendLog($"[BROADCAST] {msg}");
            });

            _engine.OnDisconnected += () => SafeInvoke(() =>
            {
                ShowNotif("❌ Disconnected from server.", Color.DarkRed);
                _placeOrderBtn.Enabled = false;
            });

            _engine.OnLog += msg => SafeInvoke(() => AppendLog(msg));
        }

        private void SafeInvoke(Action a)
        {
            try { if (IsDisposed) return; this.Invoke(a); } catch { }
        }

        private void InitializeUI()
        {
            this.Text = $"🍔 QuickBite Kiosk — {_clientName}";
            this.Size = new Size(1280, 800);
            this.MinimumSize = new Size(1050, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(18, 18, 18);
            this.Font = new Font("Segoe UI", 9f);

            // NOTIF BAR
            _notifPanel = new Panel { Dock = DockStyle.Top, Height = 0, BackColor = Color.FromArgb(56, 142, 60) };
            _notifLabel = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter };
            _notifPanel.Controls.Add(_notifLabel);
            _notifTimer = new Timer { Interval = 4000 };
            _notifTimer.Tick += (s, e) => { _notifPanel.Height = 0; _notifTimer.Stop(); };

            // HEADER
            _headerPanel = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Color.FromArgb(200, 30, 30) };
            _headerPanel.Paint += PaintHeader;

            _cartCountBadge = new Label
            {
                Text = "0", Size = new Size(28, 28),
                BackColor = Color.White, ForeColor = Color.FromArgb(200, 30, 30),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _headerPanel.Controls.Add(_cartCountBadge);
            _headerPanel.Resize += (s, e) =>
            {
                _cartCountBadge.Location = new Point(_headerPanel.Width - 75, 8);
                _cartCountBadge.Region = MakeCircleRegion(14);
            };

            // BODY
            Panel body = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 18, 18) };

            // LEFT sidebar
            _leftPanel = new Panel { Dock = DockStyle.Left, Width = 125, BackColor = Color.FromArgb(22, 22, 22) };
            Label menuLbl = new Label
            {
                Text = "MENU", Dock = DockStyle.Top, Height = 35,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 150, 150),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            _categoryFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = true, BackColor = Color.Transparent,
                Padding = new Padding(6, 4, 6, 4)
            };
            _leftPanel.Controls.Add(_categoryFlow);
            _leftPanel.Controls.Add(menuLbl);

            // RIGHT cart
            _rightCartPanel = new Panel { Dock = DockStyle.Right, Width = 300, BackColor = Color.FromArgb(22, 22, 22) };
            BuildCartSection();

            // CENTER
            Panel center = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 18, 18) };

            _menuGrid = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, AutoScroll = true,
                BackColor = Color.Transparent, Padding = new Padding(8),
                WrapContents = true
            };

            // BOTTOM TABS
            _bottomTabs = new TabControl { Dock = DockStyle.Bottom, Height = 175 };
            StyleTabControl(_bottomTabs);

            TabPage ordTab = new TabPage("  📋 Orders  ") { BackColor = Color.FromArgb(25, 25, 25) };
            _orderStatusListView = new ListView
            {
                Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true,
                BackColor = Color.FromArgb(25, 25, 25), ForeColor = Color.White,
                BorderStyle = BorderStyle.None, GridLines = false
            };
            _orderStatusListView.Columns.Add("Order ID", 150);
            _orderStatusListView.Columns.Add("Time", 80);
            _orderStatusListView.Columns.Add("Status", 120);
            ordTab.Controls.Add(_orderStatusListView);

            TabPage logTab = new TabPage("  📟 Log  ") { BackColor = Color.FromArgb(15, 15, 15) };
            _logBox = new RichTextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                BackColor = Color.FromArgb(15, 15, 15), ForeColor = Color.LimeGreen,
                Font = new Font("Consolas", 8.5f), BorderStyle = BorderStyle.None
            };
            logTab.Controls.Add(_logBox);

            TabPage billTab = new TabPage("  🧾 Bill  ") { BackColor = Color.White };
            _billBox = new RichTextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                BackColor = Color.White, ForeColor = Color.Black,
                Font = new Font("Consolas", 10f), BorderStyle = BorderStyle.None,
                Text = "Your bill will appear here after delivery."
            };
            billTab.Controls.Add(_billBox);
            _bottomTabs.TabPages.AddRange(new[] { ordTab, logTab, billTab });

            center.Controls.Add(_menuGrid);
            center.Controls.Add(_bottomTabs);

            body.Controls.Add(center);
            body.Controls.Add(_rightCartPanel);
            body.Controls.Add(_leftPanel);

            this.Controls.Add(body);
            this.Controls.Add(_headerPanel);
            this.Controls.Add(_notifPanel);
        }

        private void StyleTabControl(TabControl tc)
        {
            tc.DrawMode = TabDrawMode.OwnerDrawFixed;
            tc.DrawItem += (s, e) =>
            {
                var g = e.Graphics;
                bool sel = e.Index == tc.SelectedIndex;
                g.FillRectangle(new SolidBrush(sel ? Color.FromArgb(40, 40, 40) : Color.FromArgb(25, 25, 25)), e.Bounds);
                g.DrawString(tc.TabPages[e.Index].Text, new Font("Segoe UI", 8.5f, sel ? FontStyle.Bold : FontStyle.Regular),
                    sel ? Brushes.White : new SolidBrush(Color.FromArgb(140, 140, 140)), e.Bounds,
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            };
        }

        private void BuildCartSection()
        {
            _rightCartPanel.Controls.Clear();

            // Header
            Panel ch = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(200, 30, 30) };
            ch.Paint += (s, e) => e.Graphics.DrawString("🛒  MY ORDER",
                new Font("Segoe UI", 12, FontStyle.Bold), Brushes.White, new PointF(10, 13));

            // Cart list
            _cartListView = new ListView
            {
                Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true,
                BackColor = Color.FromArgb(28, 28, 28), ForeColor = Color.White,
                BorderStyle = BorderStyle.None, GridLines = false, HeaderStyle = ColumnHeaderStyle.None
            };
            _cartListView.Columns.Add("Item", 148);
            _cartListView.Columns.Add("Qty", 32);
            _cartListView.Columns.Add("Rs.", 68);

            // Total
            Panel totPanel = new Panel { Dock = DockStyle.Bottom, Height = 44, BackColor = Color.FromArgb(35, 35, 35) };
            _cartTotalLbl = new Label
            {
                Text = "Total:  Rs. 0",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 213, 79),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter
            };
            totPanel.Controls.Add(_cartTotalLbl);

            // Buttons
            Panel btnPnl = new Panel { Dock = DockStyle.Bottom, Height = 96, BackColor = Color.FromArgb(22, 22, 22), Padding = new Padding(10, 8, 10, 8) };

            _placeOrderBtn = new Button
            {
                Text = "✅  PLACE ORDER",
                BackColor = Color.FromArgb(46, 125, 50), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Dock = DockStyle.Top, Height = 42, Cursor = Cursors.Hand
            };
            _placeOrderBtn.FlatAppearance.BorderSize = 0;
            _placeOrderBtn.Click += PlaceOrder;

            _clearCartBtn = new Button
            {
                Text = "🗑  Clear Cart",
                BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9),
                Dock = DockStyle.Bottom, Height = 30, Cursor = Cursors.Hand
            };
            _clearCartBtn.FlatAppearance.BorderSize = 0;
            _clearCartBtn.Click += (s, e) => { _cart.Clear(); RefreshCart(); };

            btnPnl.Controls.Add(_placeOrderBtn);
            btnPnl.Controls.Add(_clearCartBtn);

            _rightCartPanel.Controls.Add(_cartListView);
            _rightCartPanel.Controls.Add(btnPnl);
            _rightCartPanel.Controls.Add(totPanel);
            _rightCartPanel.Controls.Add(ch);
        }

        private void BuildCategoryButtons()
        {
            _categoryFlow.Controls.Clear();
            var cats = new List<string> { "All" };
            cats.AddRange(_menu.Select(m => m.Category).Distinct().OrderBy(c => c));

            foreach (var cat in cats)
            {
                bool active = cat == _selectedCategory;
                Color col = GetCatColor(cat);

                var btn = new Button
                {
                    Text = $"{GetEmoji(cat)}\n{cat}",
                    Width = 108, Height = 72,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.5f, active ? FontStyle.Bold : FontStyle.Regular),
                    ForeColor = Color.White,
                    BackColor = active ? col : Color.FromArgb(40, 40, 40),
                    Cursor = Cursors.Hand,
                    Tag = cat,
                    Margin = new Padding(0, 0, 0, 5),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                btn.FlatAppearance.BorderSize = active ? 0 : 0;
                btn.Click += (s, e) =>
                {
                    _selectedCategory = btn.Tag.ToString();
                    BuildCategoryButtons();
                    ShowMenuCards(_selectedCategory);
                };
                _categoryFlow.Controls.Add(btn);
            }
        }

        private void ShowMenuCards(string category)
        {
            _menuGrid.Controls.Clear();
            var items = category == "All" ? _menu : _menu.Where(m => m.Category == category).ToList();
            foreach (var item in items)
                _menuGrid.Controls.Add(MakeCard(item));
        }

        private Panel MakeCard(MenuItem item)
        {
            Color col = GetCatColor(item.Category);

            var card = new Panel
            {
                Width = 178, Height = 200,
                Margin = new Padding(6),
                BackColor = Color.FromArgb(32, 32, 32),
                Cursor = Cursors.Hand,
                Tag = item
            };

            // Top colored area with emoji
            var top = new Panel { Dock = DockStyle.Top, Height = 95, BackColor = col, Tag = item };
            top.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // Subtle circle bg
                e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(40, 255, 255, 255)), 35, 10, 75, 75);
                e.Graphics.DrawString(GetEmoji(item.Category),
                    new Font("Segoe UI Emoji", 26), Brushes.White, new PointF(50, 20));
            };

            // Info area
            var info = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(8, 6, 8, 4) };

            var nameLbl = new Label
            {
                Text = item.Name,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top, Height = 36,
                TextAlign = ContentAlignment.TopLeft, AutoEllipsis = true
            };

            var priceLbl = new Label
            {
                Text = $"Rs. {item.Price:F0}",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 213, 79),
                Dock = DockStyle.Top, Height = 22,
                TextAlign = ContentAlignment.TopLeft
            };

            var addBtn = new Button
            {
                Text = "+ Add to Order",
                BackColor = col, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Dock = DockStyle.Bottom, Height = 28,
                Cursor = Cursors.Hand
            };
            addBtn.FlatAppearance.BorderSize = 0;
            addBtn.Click += (s, e) => AddToCart(item);

            info.Controls.Add(addBtn);
            info.Controls.Add(priceLbl);
            info.Controls.Add(nameLbl);

            card.Controls.Add(info);
            card.Controls.Add(top);

            // Click whole card
            foreach (Control c in new Control[] { card, top, nameLbl })
                c.Click += (s, e) => AddToCart(item);

            // Hover
            card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(48, 48, 48);
            card.MouseLeave += (s, e) => card.BackColor = Color.FromArgb(32, 32, 32);
            top.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(48, 48, 48);
            top.MouseLeave += (s, e) => card.BackColor = Color.FromArgb(32, 32, 32);

            return card;
        }

        private void HighlightRecommended(List<string> names)
        {
            foreach (Control c in _menuGrid.Controls)
            {
                if (c is Panel card && card.Tag is MenuItem item && names.Contains(item.Name))
                {
                    if (!card.Controls.OfType<Label>().Any(l => l.Text.Contains("AI")))
                    {
                        var badge = new Label
                        {
                            Text = "⭐ AI Pick",
                            Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                            BackColor = Color.FromArgb(255, 213, 79),
                            ForeColor = Color.FromArgb(50, 30, 0),
                            Size = new Size(60, 17),
                            Location = new Point(3, 3),
                            TextAlign = ContentAlignment.MiddleCenter
                        };
                        card.Controls.Add(badge);
                        badge.BringToFront();
                    }
                }
            }
        }

        private void AddToCart(MenuItem item)
        {
            var ex = _cart.FirstOrDefault(c => c.Item.Id == item.Id);
            if (ex != null) ex.Quantity++;
            else _cart.Add(new CartItem { Item = item, Quantity = 1 });
            RefreshCart();
            ShowNotif($"✅ {item.Name} added to order!", GetCatColor(item.Category));
            AppendLog($"Added: {item.Name}");
        }

        private void RefreshCart()
        {
            _cartListView.Items.Clear();
            double total = 0;
            foreach (var c in _cart)
            {
                var lvi = new ListViewItem(new[] { c.Item.Name, $"x{c.Quantity}", $"Rs.{c.Subtotal:F0}" })
                { ForeColor = Color.White, BackColor = Color.FromArgb(33, 33, 33) };
                _cartListView.Items.Add(lvi);
                total += c.Subtotal;
            }
            _cartTotalLbl.Text = $"Total:  Rs. {total:F0}";
            _cartCountBadge.Text = _cart.Sum(c => c.Quantity).ToString();
            _headerPanel.Invalidate();
        }

        private void PlaceOrder(object sender, EventArgs e)
        {
            if (_cart.Count == 0) { ShowNotif("⚠ Cart is empty! Tap an item to add.", Color.FromArgb(230, 81, 0)); return; }
            double total = _cart.Sum(c => c.Subtotal);
            string details = string.Join("\n", _cart.Select(c => $"  • {c.Item.Name} x{c.Quantity}  —  Rs.{c.Subtotal:F0}"));
            var res = MessageBox.Show($"Confirm your order?\n\n{details}\n\n💰 Total: Rs.{total:F0}",
                "Place Order", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                _engine.SendOrder(_cart);
                _cart.Clear();
                RefreshCart();
                AppendLog("Order sent! Waiting for confirmation...");
            }
        }

        private void PaintHeader(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var bg = new LinearGradientBrush(new Rectangle(0, 0, _headerPanel.Width, _headerPanel.Height),
                Color.FromArgb(211, 47, 47), Color.FromArgb(140, 20, 20), 0f);
            g.FillRectangle(bg, 0, 0, _headerPanel.Width, _headerPanel.Height);
            g.FillEllipse(new SolidBrush(Color.FromArgb(50, 255, 255, 255)), 12, 10, 50, 50);
            g.DrawString("🍔", new Font("Segoe UI Emoji", 20), Brushes.White, new PointF(15, 14));
            g.DrawString("QuickBite Kiosk", new Font("Segoe UI", 17, FontStyle.Bold), Brushes.White, new PointF(72, 9));
            g.DrawString($"Welcome, {_clientName}!  |  Tap any item to add to your order",
                new Font("Segoe UI", 9), new SolidBrush(Color.FromArgb(210, 255, 255, 255)), new PointF(74, 42));
            // Cart icon
            int cx = _headerPanel.Width - 115;
            g.DrawString("🛒", new Font("Segoe UI Emoji", 20), Brushes.White, new PointF(cx, 16));
        }

        private void ShowNotif(string msg, Color color)
        {
            _notifLabel.Text = msg;
            _notifPanel.BackColor = color;
            _notifPanel.Height = 36;
            _notifTimer.Stop();
            _notifTimer.Start();
        }

        private void AppendLog(string msg)
        {
            if (_logBox.IsDisposed) return;
            _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            _logBox.ScrollToCaret();
        }

        private Color GetCatColor(string cat) =>
            _categoryColors.ContainsKey(cat) ? _categoryColors[cat] : Color.FromArgb(80, 80, 80);

        private string GetEmoji(string cat) =>
            _categoryEmoji.ContainsKey(cat) ? _categoryEmoji[cat] : "🍽️";

        private Region MakeCircleRegion(int r)
        {
            var path = new GraphicsPath();
            path.AddEllipse(0, 0, r * 2, r * 2);
            return new Region(path);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _engine?.Disconnect();
            base.OnFormClosing(e);
        }
    }
}
