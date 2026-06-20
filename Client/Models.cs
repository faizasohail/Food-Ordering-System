using System.Collections.Generic;

namespace FoodOrderingClient
{
    public class MenuItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public double Price { get; set; }
        public string Description { get; set; }

        public static MenuItem Parse(string raw)
        {
            // Format: Id|Name|Category|Price|Description
            var parts = raw.Split('|');
            if (parts.Length < 5) return null;
            return new MenuItem
            {
                Id = int.Parse(parts[0]),
                Name = parts[1],
                Category = parts[2],
                Price = double.Parse(parts[3]),
                Description = parts[4]
            };
        }
    }

    public class CartItem
    {
        public MenuItem Item { get; set; }
        public int Quantity { get; set; }
        public double Subtotal => Item.Price * Quantity;
    }
}
