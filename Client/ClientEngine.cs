using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FoodOrderingClient
{
    public class ClientEngine
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private bool _isConnected = false;
        private string _buffer = "";

        public string ClientId { get; private set; }
        public string ClientName { get; private set; }
        public string LastError { get; private set; }
        public bool IsConnected => _isConnected;

        public event Action<List<MenuItem>> OnMenuReceived;
        public event Action<string> OnOrderConfirmed;
        public event Action<string, string> OnOrderStatusUpdate;  // orderId, status
        public event Action<string> OnBillReceived;
        public event Action<List<string>> OnRecommendationsReceived;
        public event Action<string> OnBroadcastReceived;
        public event Action<string> OnLog;
        public event Action OnDisconnected;

        public bool Connect(string host, int port, string name)
        {
            try
            {
                LastError = "";
                _client = new TcpClient();
                var result = _client.BeginConnect(host, port, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5));
                if (!success)
                {
                    _client.Close();
                    throw new TimeoutException("Connection timed out. Check server IP, port, WiFi, and firewall.");
                }

                _client.EndConnect(result);
                _client.NoDelay = true;
                _stream = _client.GetStream();
                ClientName = name;
                _isConnected = true;

                // Send connection request
                SendMessage($"CONNECT:{name}");

                // Start receiving
                _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
                _receiveThread.Start();

                return true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                LastError = ex.Message;
                OnLog?.Invoke($"Connection failed: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
        }

        public void SendOrder(List<CartItem> cart)
        {
            if (!_isConnected) return;
            var parts = new List<string>();
            foreach (var item in cart)
                parts.Add($"{item.Item.Id}:{item.Quantity}");
            string msg = "ORDER:" + string.Join(",", parts);
            SendMessage(msg);
            OnLog?.Invoke($"Order sent: {msg}");
        }

        public void RequestMenu()
        {
            if (_isConnected) SendMessage("REFRESH_MENU");
        }

        private void SendMessage(string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                _stream.Write(data, 0, data.Length);
                _stream.Flush();
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Send error: {ex.Message}");
            }
        }

        private void ReceiveLoop()
        {
            byte[] buffer = new byte[8192];
            while (_isConnected)
            {
                try
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    _buffer += Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    ProcessBuffer();
                }
                catch
                {
                    break;
                }
            }
            _isConnected = false;
            OnDisconnected?.Invoke();
        }

        private void ProcessBuffer()
        {
            // Process line by line
            while (_buffer.Contains("\n"))
            {
                int idx = _buffer.IndexOf("\n");
                string line = _buffer.Substring(0, idx).Trim();
                _buffer = _buffer.Substring(idx + 1);

                if (string.IsNullOrEmpty(line)) continue;
                HandleMessage(line);
            }
        }

        private List<MenuItem> _tempMenu = new List<MenuItem>();
        private bool _collectingMenu = false;

        private void HandleMessage(string line)
        {
            OnLog?.Invoke($"[RECV] {line}");

            if (line.StartsWith("WELCOME:"))
            {
                var parts = line.Split(':');
                if (parts.Length >= 3) ClientId = parts[1];
                OnLog?.Invoke($"Connected! Your ID: {ClientId}");
            }
            else if (line == "MENU_START")
            {
                _tempMenu = new List<MenuItem>();
                _collectingMenu = true;
            }
            else if (line.StartsWith("MENU_ITEM:") && _collectingMenu)
            {
                var raw = line.Substring(10);
                var item = MenuItem.Parse(raw);
                if (item != null) _tempMenu.Add(item);
            }
            else if (line == "MENU_END")
            {
                _collectingMenu = false;
                OnMenuReceived?.Invoke(_tempMenu);
            }
            else if (line.StartsWith("ORDER_CONFIRMED:"))
            {
                var parts = line.Split(':');
                string info = parts.Length >= 3 ? $"Order {parts[1]} confirmed! Total: Rs.{parts[2]}" : line;
                OnOrderConfirmed?.Invoke(info);
            }
            else if (line.StartsWith("ORDER_STATUS:"))
            {
                var parts = line.Split(':');
                if (parts.Length >= 3)
                    OnOrderStatusUpdate?.Invoke(parts[1], parts[2]);
            }
            else if (line.StartsWith("BILL:"))
            {
                string bill = line.Substring(5).Replace("\\n", "\n");
                OnBillReceived?.Invoke(bill);
            }
            else if (line.StartsWith("RECOMMENDATIONS:"))
            {
                string data = line.Substring(16);
                var items = new List<string>(data.Split(','));
                OnRecommendationsReceived?.Invoke(items);
            }
            else if (line.StartsWith("BROADCAST:"))
            {
                OnBroadcastReceived?.Invoke(line.Substring(10));
            }
        }
    }
}
