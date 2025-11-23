using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartCamping.Models
{
    public enum DayPeriod { Breakfast, Lunch, Dinner, Anytime }
    public enum MenuCategory { Coffee, Drink, Meal, Dessert, Snack }

    public sealed class MenuItem
    {
        public string Id { get; }
        public string Name { get; }
        public MenuCategory Category { get; }
        public DayPeriod Period { get; }
        public decimal Price { get; }

        public MenuItem(string id, string name, MenuCategory cat, DayPeriod period, decimal price)
        {
            Id = id; Name = name; Category = cat; Period = period; Price = price;
        }

        public override string ToString() => $"{Name}  •  {Price:0.00}€";
    }

    public enum OrderStatus { Open, Preparing, Ready, Paid, Charged }

    public sealed class OrderItem
    {
        public MenuItem Item { get; }
        public int Qty { get; private set; }
        public decimal LineTotal => Item.Price * Qty;

        public OrderItem(MenuItem item, int qty) { Item = item; Qty = Math.Max(1, qty); }
        public void Add(int qty) => Qty = Math.Max(1, Qty + qty);
    }

    public sealed class Order
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Tent { get; set; } = "A1";
        public DateTime Created { get; } = DateTime.Now;
        public OrderStatus Status { get; set; } = OrderStatus.Open;
        public List<OrderItem> Items { get; } = new();

        public string ShortId => Id.ToString("N")[..8];
        public decimal Total => Items.Sum(i => i.LineTotal);

        public void AddItem(MenuItem item, int qty)
        {
            var line = Items.FirstOrDefault(x => x.Item.Id == item.Id);
            if (line == null) Items.Add(new OrderItem(item, qty));
            else line.Add(qty);
        }
    }
}
