using System;
using System.Collections.Generic;

namespace FoodOrderingServer
{
    public class MenuItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public double Price { get; set; }
        public string Description { get; set; }
        public int OrderCount { get; set; } // For AI recommendations

        public override string ToString()
        {
            return $"{Id}|{Name}|{Category}|{Price}|{Description}";
        }
    }

    public enum OrderStatus
    {
        Pending,
        Preparing,
        Ready,
        Delivered
    }

    public class OrderItem
    {
        public MenuItem Item { get; set; }
        public int Quantity { get; set; }
        public double Subtotal => Item.Price * Quantity;
    }

    public class Order
    {
        public string OrderId { get; set; }
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public List<OrderItem> Items { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime OrderTime { get; set; }
        public double TotalAmount => CalculateTotal();

        public Order()
        {
            Items = new List<OrderItem>();
            Status = OrderStatus.Pending;
            OrderTime = DateTime.Now;
            OrderId = "ORD-" + DateTime.Now.ToString("HHmmss") + "-" + new Random().Next(100, 999);
        }

        private double CalculateTotal()
        {
            double total = 0;
            foreach (var item in Items)
                total += item.Subtotal;
            return total;
        }

        public string GetBill()
        {
            string bill = $"\n========== BILL ==========\n";
            bill += $"Order ID: {OrderId}\n";
            bill += $"Customer: {ClientName}\n";
            bill += $"Time: {OrderTime:HH:mm:ss}\n";
            bill += $"--------------------------\n";
            foreach (var item in Items)
            {
                bill += $"{item.Item.Name} x{item.Quantity} = Rs.{item.Subtotal:F2}\n";
            }
            bill += $"--------------------------\n";
            bill += $"TOTAL: Rs.{TotalAmount:F2}\n";
            bill += $"==========================\n";
            return bill;
        }
    }
}
