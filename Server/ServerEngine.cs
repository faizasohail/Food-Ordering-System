using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

namespace FoodOrderingServer
{
    public class ServerEngine
    {
        private TcpListener _listener;
        private Thread _listenerThread;
        private bool _isRunning = false;
        private Dictionary<string, TcpClient> _connectedClients = new Dictionary<string, TcpClient>();
        private Queue<Order> _orderQueue = new Queue<Order>();
        private List<Order> _allOrders = new List<Order>();
        private List<MenuItem> _menu;
        private object _lock = new object();
        private Thread _orderProcessorThread;

        public event Action<string> OnLog;
        public event Action<string, string> OnClientConnected;    // clientId, clientName
        public event Action<string> OnClientDisconnected;
        public event Action<Order> OnOrderReceived;
        public event Action<Order> OnOrderStatusChanged;
        public event Action<int> OnClientCountChanged;

        public int Port { get; private set; } = 8888;
        public int ClientCount => _connectedClients.Count;

        public ServerEngine(List<MenuItem> menu)
        {
            _menu = menu;
        }

        public void Start(int port = 8888)
        {
            Port = port;
            _isRunning = true;
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            _listenerThread = new Thread(ListenForClients) { IsBackground = true };
            _listenerThread.Start();

            _orderProcessorThread = new Thread(ProcessOrders) { IsBackground = true };
            _orderProcessorThread.Start();

            OnLog?.Invoke($"[SERVER] Started on port {port}. Waiting for clients...");
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            lock (_lock)
            {
                foreach (var client in _connectedClients.Values)
                    client.Close();
                _connectedClients.Clear();
            }
            OnLog?.Invoke("[SERVER] Stopped.");
        }

        private void ListenForClients()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    Thread clientThread = new Thread(() => HandleClient(client)) { IsBackground = true };
                    clientThread.Start();
                }
                catch { if (_isRunning) OnLog?.Invoke("[ERROR] Listener error."); }
            }
        }

        private void HandleClient(TcpClient tcpClient)
        {
            string clientId = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            string clientName = "Unknown";
            NetworkStream stream = tcpClient.GetStream();

            try
            {
                // Receive client name
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                if (msg.StartsWith("CONNECT:"))
                {
                    clientName = msg.Substring(8);
                    lock (_lock) { _connectedClients[clientId] = tcpClient; }
                    OnClientConnected?.Invoke(clientId, clientName);
                    OnClientCountChanged?.Invoke(_connectedClients.Count);
                    OnLog?.Invoke($"[CLIENT] {clientName} (ID:{clientId}) connected.");

                    // Send welcome + menu
                    string welcomeMsg = $"WELCOME:{clientId}:{clientName}\n";
                    SendMessage(stream, welcomeMsg);
                    SendMenu(stream);

                    // Send AI recommendations
                    SendRecommendations(stream);
                }

                // Listen for messages
                while (_isRunning && tcpClient.Connected)
                {
                    buffer = new byte[4096];
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    OnLog?.Invoke($"[RECEIVED from {clientName}] {message}");

                    if (message.StartsWith("ORDER:"))
                    {
                        ProcessOrderMessage(message, clientId, clientName, stream);
                    }
                    else if (message == "REFRESH_MENU")
                    {
                        SendMenu(stream);
                        SendRecommendations(stream);
                    }
                    else if (message == "STATUS_REQUEST")
                    {
                        SendOrderStatus(clientId, stream);
                    }
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[DISCONNECT] {clientName} disconnected. ({ex.Message})");
            }
            finally
            {
                lock (_lock)
                {
                    _connectedClients.Remove(clientId);
                    OnClientCountChanged?.Invoke(_connectedClients.Count);
                }
                OnClientDisconnected?.Invoke(clientId);
                tcpClient.Close();
                OnLog?.Invoke($"[CLIENT] {clientName} removed.");
            }
        }

        private void ProcessOrderMessage(string message, string clientId, string clientName, NetworkStream stream)
        {
            try
            {
                // FORMAT: ORDER:item1Id:qty1,item2Id:qty2,...
                string orderData = message.Substring(6);
                string[] parts = orderData.Split(',');

                Order order = new Order
                {
                    ClientId = clientId,
                    ClientName = clientName
                };

                foreach (var part in parts)
                {
                    var tokens = part.Split(':');
                    if (tokens.Length == 2)
                    {
                        int itemId = int.Parse(tokens[0]);
                        int qty = int.Parse(tokens[1]);
                        var menuItem = _menu.FirstOrDefault(m => m.Id == itemId);
                        if (menuItem != null)
                        {
                            menuItem.OrderCount++;
                            order.Items.Add(new OrderItem { Item = menuItem, Quantity = qty });
                        }
                    }
                }

                if (order.Items.Count > 0)
                {
                    lock (_lock) { _orderQueue.Enqueue(order); _allOrders.Add(order); }
                    OnOrderReceived?.Invoke(order);
                    OnLog?.Invoke($"[ORDER] {clientName} placed order {order.OrderId} - Rs.{order.TotalAmount:F2}");

                    // Confirm to client
                    SendMessage(stream, $"ORDER_CONFIRMED:{order.OrderId}:{order.TotalAmount:F2}\n");
                    SendRecommendations(stream);

                    // Log to file
                    LogOrder(order);
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[ERROR] Order parse failed: {ex.Message}");
                SendMessage(stream, "ORDER_ERROR:Invalid order format\n");
            }
        }

        private void SendMenu(NetworkStream stream)
        {
            StringBuilder sb = new StringBuilder("MENU_START\n");
            foreach (var item in _menu)
                sb.AppendLine($"MENU_ITEM:{item}");
            sb.AppendLine("MENU_END");
            SendMessage(stream, sb.ToString());
        }

        private void SendRecommendations(NetworkStream stream)
        {
            // AI: Recommend top 3 most ordered items
            var topItems = _menu.OrderByDescending(m => m.OrderCount).Take(3).ToList();
            StringBuilder sb = new StringBuilder("RECOMMENDATIONS:");
            sb.Append(string.Join(",", topItems.Select(m => m.Name)));
            sb.AppendLine();
            SendMessage(stream, sb.ToString());
        }

        private void SendOrderStatus(string clientId, NetworkStream stream)
        {
            var clientOrders = _allOrders.Where(o => o.ClientId == clientId).ToList();
            foreach (var order in clientOrders)
            {
                SendMessage(stream, $"ORDER_STATUS:{order.OrderId}:{order.Status}\n");
            }
        }

        private void ProcessOrders()
        {
            while (_isRunning)
            {
                Order order = null;
                lock (_lock)
                {
                    if (_orderQueue.Count > 0)
                        order = _orderQueue.Dequeue();
                }

                if (order != null)
                {
                    // Simulate processing: Pending -> Preparing -> Ready -> Delivered
                    UpdateOrderStatus(order, OrderStatus.Preparing);
                    Thread.Sleep(5000); // 5 seconds preparing

                    UpdateOrderStatus(order, OrderStatus.Ready);
                    Thread.Sleep(3000); // 3 seconds ready

                    UpdateOrderStatus(order, OrderStatus.Delivered);

                    // Send bill
                    BroadcastToClient(order.ClientId, $"BILL:{order.GetBill()}\n");
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
        }

        private void UpdateOrderStatus(Order order, OrderStatus status)
        {
            order.Status = status;
            OnOrderStatusChanged?.Invoke(order);
            OnLog?.Invoke($"[STATUS] Order {order.OrderId} -> {status}");
            BroadcastToClient(order.ClientId, $"ORDER_STATUS:{order.OrderId}:{status}\n");
        }

        private void BroadcastToClient(string clientId, string message)
        {
            TcpClient client = null;
            lock (_lock) { _connectedClients.TryGetValue(clientId, out client); }
            if (client != null && client.Connected)
            {
                try { SendMessage(client.GetStream(), message); }
                catch { }
            }
        }

        public void BroadcastToAll(string message)
        {
            List<TcpClient> clients;
            lock (_lock) { clients = new List<TcpClient>(_connectedClients.Values); }
            foreach (var client in clients)
            {
                try { if (client.Connected) SendMessage(client.GetStream(), message); }
                catch { }
            }
            OnLog?.Invoke($"[BROADCAST] {message.Trim()}");
        }

        private void SendMessage(NetworkStream stream, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }

        private void LogOrder(Order order)
        {
            try
            {
                string logPath = "orders_log.txt";
                File.AppendAllText(logPath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {order.OrderId} | {order.ClientName} | Rs.{order.TotalAmount:F2}\n");
            }
            catch { }
        }

        public List<Order> GetAllOrders() => _allOrders;
        public List<MenuItem> GetMenu() => _menu;
    }
}
