using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows.Forms;

namespace FoodOrderingServer
{
    public class ServerForm : Form
    {
        private ServerEngine _server;
        private List<MenuItem> _menu;

        private Dictionary<string, Color> _catColors = new Dictionary<string, Color>
        {
            {"Burgers",Color.FromArgb(229,57,53)},{"Wraps",Color.FromArgb(251,140,0)},
            {"Pizza",Color.FromArgb(30,136,229)},{"Desi",Color.FromArgb(67,160,71)},
            {"Sides",Color.FromArgb(142,36,170)},{"Drinks",Color.FromArgb(0,172,193)},
            {"Desserts",Color.FromArgb(216,27,96)},
        };
        private Dictionary<string, string> _catEmoji = new Dictionary<string, string>
        {
            {"Burgers","🍔"},{"Wraps","🌯"},{"Pizza","🍕"},
            {"Desi","🍛"},{"Sides","🍟"},{"Drinks","🥤"},{"Desserts","🍰"},
        };

        // Controls
        private Panel _headerPanel, _notifPanel;
        private Label _notifLbl, _statusLbl, _clientCountLbl, _totalOrdersLbl, _queueLbl;
        private Button _startBtn, _stopBtn, _broadcastBtn;
        private TextBox _broadcastTxt;
        private NumericUpDown _portNum;
        private TabControl _mainTabs;
        private FlowLayoutPanel _menuGrid;
        private ListView _clientList, _orderList;
        private RichTextBox _logBox;
        private Timer _notifTimer;

        public ServerForm()
        {
            InitializeMenu();
            InitializeUI();
        }

        private void InitializeMenu()
        {
            _menu = new List<MenuItem>
            {
                new MenuItem{Id=1, Name="Zinger Burger",      Category="Burgers",  Price=550,  Description="Crispy chicken burger",     OrderCount=15},
                new MenuItem{Id=2, Name="Double Smash Burger",Category="Burgers",  Price=750,  Description="Double patty smash burger", OrderCount=8 },
                new MenuItem{Id=3, Name="Chicken Shawarma",   Category="Wraps",    Price=350,  Description="Grilled chicken wrap",      OrderCount=20},
                new MenuItem{Id=4, Name="Beef Shawarma",      Category="Wraps",    Price=450,  Description="Spicy beef wrap",           OrderCount=12},
                new MenuItem{Id=5, Name="Pepperoni Pizza",    Category="Pizza",    Price=1200, Description="12-inch pepperoni",         OrderCount=18},
                new MenuItem{Id=6, Name="Margarita Pizza",    Category="Pizza",    Price=950,  Description="Classic cheese pizza",      OrderCount=10},
                new MenuItem{Id=7, Name="Chicken Karahi",     Category="Desi",     Price=850,  Description="Pakistani style karahi",    OrderCount=25},
                new MenuItem{Id=8, Name="Biryani (Half)",     Category="Desi",     Price=400,  Description="Chicken biryani",           OrderCount=30},
                new MenuItem{Id=9, Name="Fries (Large)",      Category="Sides",    Price=250,  Description="Crispy salted fries",       OrderCount=35},
                new MenuItem{Id=10,Name="Coleslaw",           Category="Sides",    Price=150,  Description="Creamy coleslaw",           OrderCount=9 },
                new MenuItem{Id=11,Name="Coca Cola (500ml)",  Category="Drinks",   Price=100,  Description="Chilled coke",              OrderCount=40},
                new MenuItem{Id=12,Name="Mango Juice",        Category="Drinks",   Price=120,  Description="Fresh mango juice",         OrderCount=22},
                new MenuItem{Id=13,Name="Chocolate Shake",    Category="Desserts", Price=300,  Description="Thick chocolate milkshake", OrderCount=14},
                new MenuItem{Id=14,Name="Brownie",            Category="Desserts", Price=200,  Description="Warm chocolate brownie",    OrderCount=7 },
            };
        }

        private void InitializeUI()
        {
            this.Text = "🍔 QuickBite — SERVER PANEL";
            this.Size = new Size(1200, 800);
            this.MinimumSize = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(18, 18, 18);
            this.Font = new Font("Segoe UI", 9f);

            // NOTIF
            _notifPanel = new Panel { Dock = DockStyle.Top, Height = 0, BackColor = Color.FromArgb(46, 125, 50) };
            _notifLbl = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter };
            _notifPanel.Controls.Add(_notifLbl);
            _notifTimer = new Timer { Interval = 4000 };
            _notifTimer.Tick += (s, e) => { _notifPanel.Height = 0; _notifTimer.Stop(); };

            // HEADER
            _headerPanel = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Color.FromArgb(180, 20, 20) };
            _headerPanel.Paint += PaintHeader;

            _statusLbl = new Label
            {
                Text = "● OFFLINE",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 150, 150),
                Size = new Size(100, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _headerPanel.Controls.Add(_statusLbl);
            _headerPanel.Resize += (s, e) => _statusLbl.Location = new Point(_headerPanel.Width - 115, 21);

            // STATS BAR
            Panel statsBar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(28, 28, 28) };
            _clientCountLbl = StatLabel("👤 Clients: 0", 10);
            _totalOrdersLbl = StatLabel("📋 Orders: 0", 200);
            _queueLbl = StatLabel("⏳ Queue: 0", 380);
            statsBar.Controls.AddRange(new Control[] { _clientCountLbl, _totalOrdersLbl, _queueLbl });

            // CONTROL BAR
            Panel ctrlBar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(24, 24, 24), Padding = new Padding(10, 8, 10, 8) };

            Label portLbl = new Label { Text = "Port:", ForeColor = Color.Gray, AutoSize = true, Top = 17, Left = 8 };
            _portNum = new NumericUpDown { Minimum = 1024, Maximum = 65535, Value = 8888, Width = 75, Top = 13, Left = 45, BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White };

            _startBtn = CtrlButton("▶  START SERVER", Color.FromArgb(46, 125, 50), 145, 13, 130);
            _startBtn.Click += StartServer;

            _stopBtn = CtrlButton("■  STOP", Color.FromArgb(198, 40, 40), 90, 13, 280);
            _stopBtn.Enabled = false;
            _stopBtn.Click += StopServer;

            Label bcastLbl = new Label { Text = "Broadcast:", ForeColor = Color.Gray, AutoSize = true, Top = 17, Left = 395 };
            _broadcastTxt = new TextBox { Width = 290, Top = 13, Left = 478, BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            _broadcastBtn = CtrlButton("📢 Send All", Color.FromArgb(21, 101, 192), 90, 13, 776);
            _broadcastBtn.Enabled = false;
            _broadcastBtn.Click += BroadcastMsg;

            ctrlBar.Controls.AddRange(new Control[] { portLbl, _portNum, _startBtn, _stopBtn, bcastLbl, _broadcastTxt, _broadcastBtn });

            // MAIN TABS
            _mainTabs = new TabControl { Dock = DockStyle.Fill };
            StyleTabs(_mainTabs);

            // Tab 1: MENU DISPLAY (kiosk style)
            TabPage menuTab = new TabPage("  🍽️ Live Menu  ") { BackColor = Color.FromArgb(18, 18, 18) };
            _menuGrid = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, AutoScroll = true,
                BackColor = Color.Transparent, Padding = new Padding(10),
                WrapContents = true
            };
            BuildMenuCards();
            menuTab.Controls.Add(_menuGrid);

            // Tab 2: CLIENTS
            TabPage clientTab = new TabPage("  👥 Clients  ") { BackColor = Color.FromArgb(22, 22, 22) };
            _clientList = new ListView
            {
                Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true,
                BackColor = Color.FromArgb(28, 28, 28), ForeColor = Color.White, BorderStyle = BorderStyle.None
            };
            _clientList.Columns.Add("Client ID", 110);
            _clientList.Columns.Add("Name", 160);
            _clientList.Columns.Add("Connected At", 130);
            clientTab.Controls.Add(_clientList);

            // Tab 3: ORDERS
            TabPage orderTab = new TabPage("  📋 Live Orders  ") { BackColor = Color.FromArgb(22, 22, 22) };
            _orderList = new ListView
            {
                Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true,
                BackColor = Color.FromArgb(28, 28, 28), ForeColor = Color.White, BorderStyle = BorderStyle.None
            };
            _orderList.Columns.Add("Order ID", 130);
            _orderList.Columns.Add("Customer", 120);
            _orderList.Columns.Add("Items", 220);
            _orderList.Columns.Add("Total", 90);
            _orderList.Columns.Add("Status", 100);
            _orderList.Columns.Add("Time", 90);
            orderTab.Controls.Add(_orderList);

            // Tab 4: LOG
            TabPage logTab = new TabPage("  📟 Server Log  ") { BackColor = Color.FromArgb(12, 12, 12) };
            _logBox = new RichTextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                BackColor = Color.FromArgb(12, 12, 12), ForeColor = Color.LimeGreen,
                Font = new Font("Consolas", 9f), BorderStyle = BorderStyle.None
            };
            logTab.Controls.Add(_logBox);

            _mainTabs.TabPages.AddRange(new[] { menuTab, clientTab, orderTab, logTab });

            this.Controls.Add(_mainTabs);
            this.Controls.Add(statsBar);
            this.Controls.Add(ctrlBar);
            this.Controls.Add(_headerPanel);
            this.Controls.Add(_notifPanel);

            // Server engine
            _server = new ServerEngine(_menu);
            _server.OnLog += msg => SafeInvoke(() => AppendLog(msg));
            _server.OnClientConnected += (id, name) => SafeInvoke(() =>
            {
                _clientList.Items.Add(new ListViewItem(new[] { id, name, DateTime.Now.ToString("HH:mm:ss") }) { ForeColor = Color.White });
                ShowNotif($"✅ {name} connected!", Color.FromArgb(46, 125, 50));
                UpdateStats();
            });
            _server.OnClientDisconnected += id => SafeInvoke(() =>
            {
                foreach (ListViewItem lvi in _clientList.Items)
                    if (lvi.SubItems[0].Text == id) { _clientList.Items.Remove(lvi); break; }
                UpdateStats();
            });
            _server.OnOrderReceived += order => SafeInvoke(() =>
            {
                string items = string.Join(", ", order.Items.Select(i => $"{i.Item.Name}x{i.Quantity}"));
                var lvi = new ListViewItem(new[] { order.OrderId, order.ClientName, items, $"Rs.{order.TotalAmount:F0}", "Pending", order.OrderTime.ToString("HH:mm:ss") })
                { ForeColor = Color.White, BackColor = Color.FromArgb(40, 35, 20), Tag = order.OrderId };
                _orderList.Items.Add(lvi);
                ShowNotif($"📋 New order from {order.ClientName}! Rs.{order.TotalAmount:F0}", Color.FromArgb(21, 101, 192));
                UpdateStats();
                // Refresh card order counts
                BuildMenuCards();
            });
            _server.OnOrderStatusChanged += order => SafeInvoke(() =>
            {
                foreach (ListViewItem lvi in _orderList.Items)
                    if (lvi.Tag?.ToString() == order.OrderId)
                    {
                        lvi.SubItems[4].Text = order.Status.ToString();
                        lvi.BackColor = order.Status == OrderStatus.Delivered ? Color.FromArgb(20, 40, 20) :
                                        order.Status == OrderStatus.Ready ? Color.FromArgb(15, 30, 50) : Color.FromArgb(40, 30, 10);
                    }
                UpdateStats();
            });
            _server.OnClientCountChanged += n => SafeInvoke(() => _clientCountLbl.Text = $"👤 Clients: {n}");

            AppendLog("Server panel ready. Press START SERVER to begin.");
        }

        private void BuildMenuCards()
        {
            _menuGrid.Controls.Clear();
            foreach (var item in _menu)
            {
                Color col = _catColors.ContainsKey(item.Category) ? _catColors[item.Category] : Color.Gray;
                string emoji = _catEmoji.ContainsKey(item.Category) ? _catEmoji[item.Category] : "🍽️";

                var card = new Panel
                {
                    Width = 170, Height = 185,
                    Margin = new Padding(6),
                    BackColor = Color.FromArgb(32, 32, 32),
                    Cursor = Cursors.Default,
                    Tag = item
                };

                // Top colored area
                var top = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = col };
                top.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(40, 255, 255, 255)), 32, 8, 72, 72);
                    e.Graphics.DrawString(emoji, new Font("Segoe UI Emoji", 26), Brushes.White, new PointF(46, 18));
                    // Order count badge
                    string cnt = $"×{item.OrderCount}";
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(180, 0, 0, 0)), top.Width - 38, 4, 34, 18);
                    e.Graphics.DrawString(cnt, new Font("Segoe UI", 7.5f, FontStyle.Bold), Brushes.White, new PointF(top.Width - 36, 6));
                };

                var info = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(7, 5, 7, 4) };

                var nameLbl = new Label
                {
                    Text = item.Name,
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                    ForeColor = Color.White,
                    Dock = DockStyle.Top, Height = 34,
                    AutoEllipsis = true, TextAlign = ContentAlignment.TopLeft
                };

                var priceLbl = new Label
                {
                    Text = $"Rs. {item.Price:F0}",
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.FromArgb(255, 213, 79),
                    Dock = DockStyle.Top, Height = 22
                };

                var catLbl = new Label
                {
                    Text = item.Category,
                    Font = new Font("Segoe UI", 7.5f),
                    ForeColor = col,
                    Dock = DockStyle.Bottom, Height = 18,
                    TextAlign = ContentAlignment.BottomLeft
                };

                info.Controls.Add(catLbl);
                info.Controls.Add(priceLbl);
                info.Controls.Add(nameLbl);

                card.Controls.Add(info);
                card.Controls.Add(top);
                _menuGrid.Controls.Add(card);
            }
        }

        private void StyleTabs(TabControl tc)
        {
            tc.DrawMode = TabDrawMode.OwnerDrawFixed;
            tc.DrawItem += (s, e) =>
            {
                bool sel = e.Index == tc.SelectedIndex;
                e.Graphics.FillRectangle(new SolidBrush(sel ? Color.FromArgb(40, 40, 40) : Color.FromArgb(22, 22, 22)), e.Bounds);
                if (sel) e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(200, 30, 30)), new Rectangle(e.Bounds.X, e.Bounds.Bottom - 3, e.Bounds.Width, 3));
                e.Graphics.DrawString(tc.TabPages[e.Index].Text,
                    new Font("Segoe UI", 9f, sel ? FontStyle.Bold : FontStyle.Regular),
                    sel ? Brushes.White : new SolidBrush(Color.FromArgb(140, 140, 140)),
                    e.Bounds, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            };
        }

        private void PaintHeader(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var bg = new LinearGradientBrush(new Rectangle(0, 0, _headerPanel.Width, _headerPanel.Height),
                Color.FromArgb(200, 30, 30), Color.FromArgb(130, 15, 15), 0f);
            g.FillRectangle(bg, 0, 0, _headerPanel.Width, _headerPanel.Height);
            g.FillEllipse(new SolidBrush(Color.FromArgb(50, 255, 255, 255)), 12, 10, 50, 50);
            g.DrawString("🍔", new Font("Segoe UI Emoji", 20), Brushes.White, new PointF(15, 14));
            g.DrawString("QuickBite Kiosk", new Font("Segoe UI", 17, FontStyle.Bold), Brushes.White, new PointF(72, 9));
            g.DrawString("Server Control Panel  |  Network Programming Project — Faiza & Areeba",
                new Font("Segoe UI", 9), new SolidBrush(Color.FromArgb(200, 255, 255, 255)), new PointF(74, 43));
        }

        private Button CtrlButton(string text, Color col, int w, int top, int left)
        {
            var btn = new Button
            {
                Text = text, BackColor = col, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Width = w, Height = 32, Top = top, Left = left, Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Label StatLabel(string text, int left)
        {
            return new Label
            {
                Text = text, ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true, Top = 13, Left = left + 12
            };
        }

        private void StartServer(object sender, EventArgs e)
        {
            try
            {
                _server.Start((int)_portNum.Value);
                _startBtn.Enabled = false; _stopBtn.Enabled = true;
                _broadcastBtn.Enabled = true; _portNum.Enabled = false;
                _statusLbl.Text = "● ONLINE"; _statusLbl.ForeColor = Color.LightGreen;
                var localIps = GetLocalIPs();
                string ipText = localIps.Count > 0 ? string.Join(", ", localIps) : "IP not found";
                AppendLog($"[NETWORK] Server is listening on all network adapters, port {_portNum.Value}.");
                AppendLog($"[NETWORK] Give this IP to client laptops on the same WiFi/LAN: {ipText}");
                AppendLog("[NETWORK] Client laptops should use the shown IP and the same port. Example: 192.168.1.10 / 8888");
                ShowNotif($"✅ Server started! Client IP: {ipText}  Port: {_portNum.Value}", Color.FromArgb(46, 125, 50));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error"); }
        }

        private void StopServer(object sender, EventArgs e)
        {
            _server.Stop();
            _startBtn.Enabled = true; _stopBtn.Enabled = false;
            _broadcastBtn.Enabled = false; _portNum.Enabled = true;
            _statusLbl.Text = "● OFFLINE"; _statusLbl.ForeColor = Color.FromArgb(255, 150, 150);
        }

        private void BroadcastMsg(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_broadcastTxt.Text))
            {
                _server.BroadcastToAll($"BROADCAST:{_broadcastTxt.Text}\n");
                _broadcastTxt.Clear();
            }
        }

        private void UpdateStats()
        {
            var orders = _server.GetAllOrders();
            _totalOrdersLbl.Text = $"📋 Orders: {orders.Count}";
            _queueLbl.Text = $"⏳ Queue: {orders.Count(o => o.Status != OrderStatus.Delivered)}";
        }

        private void ShowNotif(string msg, Color col)
        {
            _notifLbl.Text = msg; _notifPanel.BackColor = col;
            _notifPanel.Height = 36; _notifTimer.Stop(); _notifTimer.Start();
        }

        private void AppendLog(string msg)
        {
            if (_logBox.IsDisposed) return;
            _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            _logBox.ScrollToCaret();
        }

        private void SafeInvoke(Action a) { try { if (!IsDisposed) this.Invoke(a); } catch { } }

        private List<string> GetLocalIPs()
        {
            var ips = new List<string>();
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(nic =>
                        nic.OperationalStatus == OperationalStatus.Up &&
                        nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel);

                foreach (var nic in interfaces)
                {
                    foreach (var addr in nic.GetIPProperties().UnicastAddresses)
                    {
                        var ip = addr.Address;
                        if (ip.AddressFamily != AddressFamily.InterNetwork)
                            continue;

                        string value = ip.ToString();
                        if (value.StartsWith("127.") || value.StartsWith("169.254."))
                            continue;

                        if (!ips.Contains(value))
                            ips.Add(value);
                    }
                }

                if (ips.Count == 0)
                {
                    foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                            ips.Add(ip.ToString());
                    }
                }
            }
            catch { }
            return ips;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _server?.Stop();
            base.OnFormClosing(e);
        }
    }
}
