# 🍔 Advanced Network Food Ordering System
**Network Programming Project | Faiza Sohail (65560) & Areeba Asghar (65532)**

---

## 📁 Project Structure

```
FoodOrderingSystem/
├── Server/
│   ├── Program.cs          ← Server entry point
│   ├── ServerForm.cs       ← Server GUI (Control Panel)
│   ├── ServerEngine.cs     ← TCP socket server, multithreading, order queue
│   ├── Models.cs           ← MenuItem, Order, OrderItem classes
│   └── FoodOrderingServer.csproj
│
└── Client/
    ├── Program.cs          ← Client entry point
    ├── LoginForm.cs        ← Login/Connect screen
    ├── ClientForm.cs       ← Main ordering GUI (Menu, Cart, Orders, Bill)
    ├── ClientEngine.cs     ← TCP client, message handling
    ├── Models.cs           ← MenuItem, CartItem classes
    └── FoodOrderingClient.csproj
```

---

## ⚙️ Requirements

- Windows 10/11
- Visual Studio 2022 (Community Edition - FREE)
- .NET 6.0 SDK (installed with Visual Studio)

---

## 🚀 HOW TO RUN (Same Laptop - Server + 2 Clients)

### Step 1: Open Server Project
1. Open Visual Studio 2022
2. File → Open → Project/Solution
3. Navigate to `Server/` folder → open `FoodOrderingServer.csproj`
4. Press **F5** or click **▶ Start**
5. Server window opens → Click **"▶ START SERVER"** button
6. Note your IP shown in the log (e.g. `192.168.1.5`)

### Step 2: Open Client Project (First Client)
1. Open **another Visual Studio window** (File → New Window)
2. Open `Client/FoodOrderingClient.csproj`
3. Press **F5**
4. Login screen opens:
   - **Name:** Enter your name (e.g. "Faiza")
   - **Server IP:** `127.0.0.1` (same laptop)
   - **Port:** `8888`
5. Click **"🔗 CONNECT TO SERVER"**

### Step 3: Open Second Client
1. Repeat Step 2 in **another Visual Studio window**
2. Use a different name (e.g. "Areeba")
3. Same IP `127.0.0.1`, same port `8888`

### Step 4: Order Food!
- Browse the **Menu** tab
- Select an item → set quantity → click **Add to Cart**
- Go to **Cart & Orders** tab → Click **PLACE ORDER**
- Watch real-time status updates: Pending → Preparing → Ready → Delivered
- See the **Bill** appear automatically!

---

## 🔌 For Real 2-Laptop Setup (Teacher's Requirement)

### On Laptop 1 (SERVER):
- Run Server project
- Start server (note the IP shown in log, e.g. `192.168.1.5`)
- Both laptops must be on **same WiFi network**

### On Laptop 2 & 3 (CLIENTS):
- Run Client project
- In login screen, enter Laptop 1's IP (e.g. `192.168.1.5`)
- Port: `8888`
- Connect!

**Windows Firewall Note:** If clients can't connect, on Server laptop:
- Windows Defender Firewall → Allow an app → Add FoodOrderingServer.exe
- OR temporarily disable firewall for testing

---

## ✨ Features Implemented

| Feature | Description |
|---------|-------------|
| **TCP Sockets** | Reliable client-server communication |
| **Multi-client** | Multiple clients connect simultaneously using threads |
| **FIFO Queue** | Orders processed in sequence (first come first served) |
| **Real-time Notifications** | Status updates pushed to client instantly |
| **AI Recommendations** | Top-3 most ordered items shown to every client |
| **GUI (Windows Forms)** | Professional UI for both server and client |
| **Order Status Tracking** | Pending → Preparing → Ready → Delivered |
| **Bill Generation** | Auto bill sent to client after delivery |
| **Broadcast** | Server can send announcements to all clients |
| **Order Logging** | All orders saved to `orders_log.txt` |
| **Category Filter** | Filter menu by category (Burgers, Pizza, etc.) |

---

## 🏗️ Architecture

```
CLIENT 1 ──────┐
               │  TCP Sockets (Port 8888)
CLIENT 2 ──────┼─────────────────────► SERVER
               │                       ├── MenuManager
CLIENT 3 ──────┘                       ├── FIFO Order Queue
                                       ├── Thread per Client
                                       ├── AI Recommendation Engine
                                       └── Notification Broadcaster
```

---

## 📡 Protocol (Message Format)

| Direction | Message | Meaning |
|-----------|---------|---------|
| Client→Server | `CONNECT:Faiza` | Login with name |
| Server→Client | `WELCOME:ABC123:Faiza` | Assign client ID |
| Server→Client | `MENU_START ... MENU_END` | Send full menu |
| Client→Server | `ORDER:1:2,3:1` | Order item 1 qty 2, item 3 qty 1 |
| Server→Client | `ORDER_CONFIRMED:ORD-001:Rs.850` | Order accepted |
| Server→Client | `ORDER_STATUS:ORD-001:Preparing` | Status update |
| Server→Client | `BILL:=====...` | Final bill |
| Server→Client | `RECOMMENDATIONS:Biryani,Coke,Fries` | AI suggestions |
| Server→Client | `BROADCAST:message` | Announcement |

---

## 👩‍💻 Made By
- **Faiza Sohail** — 65560
- **Areeba Asghar** — 65532
- **Course:** Network Programming
