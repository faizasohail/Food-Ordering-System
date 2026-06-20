using System;
using System.Drawing;
using System.Windows.Forms;

namespace FoodOrderingClient
{
    public class LoginForm : Form
    {
        private TextBox _nameTxt, _ipTxt, _portTxt;
        private Button _connectBtn;
        private Label _statusLbl;

        public LoginForm()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "Food Ordering System - Connect";
            this.Size = new Size(420, 480);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            // Logo/Header
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.FromArgb(211, 47, 47)
            };
            Label logo = new Label
            {
                Text = "🍔",
                Font = new Font("Segoe UI Emoji", 36),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 65
            };
            Label logoText = new Label
            {
                Text = "Food Ordering System",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            header.Controls.Add(logoText);
            header.Controls.Add(logo);

            // Form fields
            Panel content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(30, 20, 30, 20) };

            Label nameLbl = MakeLabel("Your Name:", 20);
            _nameTxt = MakeTextBox("e.g. Faiza", 45);

            Label ipLbl = MakeLabel("Server Laptop IP Address:", 100);
            _ipTxt = MakeTextBox("127.0.0.1", 125);
            Label ipHint = new Label
            {
                Text = "Same laptop: 127.0.0.1 | Other laptop: use IP shown in server log",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8),
                AutoSize = true,
                Top = 152, Left = 0
            };

            Label portLbl = MakeLabel("Port:", 180);
            _portTxt = MakeTextBox("8888", 205);

            _connectBtn = new Button
            {
                Text = "🔗  CONNECT TO SERVER",
                BackColor = Color.FromArgb(211, 47, 47),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Height = 45,
                Dock = DockStyle.Bottom,
                Cursor = Cursors.Hand
            };
            _connectBtn.Click += ConnectClick;

            _statusLbl = new Label
            {
                Text = "",
                ForeColor = Color.DarkRed,
                Font = new Font("Segoe UI", 9),
                AutoSize = false,
                Height = 25,
                Dock = DockStyle.Bottom,
                TextAlign = ContentAlignment.MiddleCenter
            };

            content.Controls.AddRange(new Control[] {
                nameLbl, _nameTxt, ipLbl, _ipTxt, ipHint, portLbl, _portTxt
            });

            this.Controls.Add(content);
            this.Controls.Add(_statusLbl);
            this.Controls.Add(_connectBtn);
            this.Controls.Add(header);
        }

        private Label MakeLabel(string text, int top)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = true,
                Top = top, Left = 0
            };
        }

        private TextBox MakeTextBox(string placeholder, int top)
        {
            return new TextBox
            {
                Text = placeholder,
                Font = new Font("Segoe UI", 11),
                Width = 330,
                Top = top, Left = 0,
                BorderStyle = BorderStyle.FixedSingle,
                Height = 30
            };
        }

        private void ConnectClick(object sender, EventArgs e)
        {
            string name = _nameTxt.Text.Trim();
            string ip = _ipTxt.Text.Trim();
            string portStr = _portTxt.Text.Trim();

            if (string.IsNullOrEmpty(name) || name == "e.g. Faiza")
            {
                _statusLbl.Text = "⚠ Please enter your name.";
                return;
            }

            if (string.IsNullOrEmpty(ip))
            {
                _statusLbl.Text = "⚠ Please enter server laptop IP address.";
                return;
            }

            if (!int.TryParse(portStr, out int port) || port < 1024 || port > 65535)
            {
                _statusLbl.Text = "⚠ Invalid port number. Use 1024 to 65535.";
                return;
            }

            _connectBtn.Enabled = false;
            _statusLbl.Text = "Connecting...";
            _statusLbl.ForeColor = Color.DarkBlue;

            ClientEngine engine = new ClientEngine();
            bool connected = engine.Connect(ip, port, name);

            if (connected)
            {
                _statusLbl.Text = "Connected!";
                _statusLbl.ForeColor = Color.Green;
                this.Hide();
                var mainForm = new ClientForm(engine, name);
                mainForm.FormClosed += (s, args) => this.Close();
                mainForm.Show();
            }
            else
            {
                _statusLbl.Text = $"❌ Could not connect: {engine.LastError}";
                _statusLbl.ForeColor = Color.DarkRed;
                _connectBtn.Enabled = true;
            }
        }
    }
}
