using System;
using System.Collections.Generic;
using System.Linq;
using SmartCamping.Models;

namespace SmartCamping.Services
{
    public sealed class OrdersService
    {
        private readonly List<MenuItem> _menu = new();
        private readonly List<Order> _orders = new();

        public event Action? OrdersChanged;

        public IReadOnlyList<Order> Orders => _orders;

        // using System.Linq;

        public void ClearOrder(Guid orderId, bool removeIfEmpty = false)
        {
            var order = _orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return;

            // Άδειασε τις γραμμές της παραγγελίας
            order.Items.Clear(); 

            if (order.Status != OrderStatus.Paid && order.Status != OrderStatus.Charged)
                order.Status = OrderStatus.Open;

            
            // Προαιρετικά: βγάλε την άδεια παραγγελία από τη λίστα
            if (removeIfEmpty)
                _orders.Remove(order);

            OrdersChanged?.Invoke();
        }


        public OrdersService()
        {
            SeedMenu();
        }

        //  Menu 
        public IQueryable<MenuItem> FilterMenu(DayPeriod period, MenuCategory? cat) =>
            _menu
              .Where(m => period == DayPeriod.Anytime || m.Period == period || m.Period == DayPeriod.Anytime)
              .Where(m => cat == null || m.Category == cat.Value)
              .AsQueryable();

        private void SeedMenu()
        {
            _menu.Clear();

            // Anytime
            _menu.Add(new MenuItem("water", "Εμφιαλωμένο Νερό 500ml", MenuCategory.Drink, DayPeriod.Anytime, 1.20m));
            _menu.Add(new MenuItem("nuts", "Ανάμεικτοι Ξηροί Καρποί", MenuCategory.Snack, DayPeriod.Anytime, 2.50m));

            // Breakfast
            _menu.Add(new MenuItem("espresso", "Espresso", MenuCategory.Coffee, DayPeriod.Breakfast, 1.80m));
            _menu.Add(new MenuItem("capp", "Cappuccino", MenuCategory.Coffee, DayPeriod.Breakfast, 2.20m));
            _menu.Add(new MenuItem("freddo", "Freddo Espresso", MenuCategory.Coffee, DayPeriod.Breakfast, 2.30m));
            _menu.Add(new MenuItem("yogurt", "Γιαούρτι με Μέλι", MenuCategory.Dessert, DayPeriod.Breakfast, 3.90m));
            _menu.Add(new MenuItem("toast", "Τοστ Ζαμπόν-Τυρί", MenuCategory.Meal, DayPeriod.Breakfast, 3.50m));

            // Lunch
            _menu.Add(new MenuItem("juice", "Φρεσκοστυμμένος Χυμός", MenuCategory.Drink, DayPeriod.Lunch, 3.50m));
            _menu.Add(new MenuItem("icedtea", "Iced Tea", MenuCategory.Drink, DayPeriod.Lunch, 2.80m));
            _menu.Add(new MenuItem("salad", "Χωριάτικη Σαλάτα", MenuCategory.Meal, DayPeriod.Lunch, 6.90m));
            _menu.Add(new MenuItem("burger", "Burger Κοτόπουλο", MenuCategory.Meal, DayPeriod.Lunch, 8.50m));
            _menu.Add(new MenuItem("brownie", "Brownie", MenuCategory.Dessert, DayPeriod.Lunch, 3.50m));
            _menu.Add(new MenuItem("chips", "Πατατάκια", MenuCategory.Snack, DayPeriod.Lunch, 1.80m));

            // Dinner
            _menu.Add(new MenuItem("beer", "Μπύρα 330ml", MenuCategory.Drink, DayPeriod.Dinner, 3.50m));
            _menu.Add(new MenuItem("wine", "Κρασί ποτήρι", MenuCategory.Drink, DayPeriod.Dinner, 4.00m));
            _menu.Add(new MenuItem("pasta", "Ζυμαρικά Ναπολιτέν", MenuCategory.Meal, DayPeriod.Dinner, 9.50m));
            _menu.Add(new MenuItem("souvlaki", "Σουβλάκι Μερίδα", MenuCategory.Meal, DayPeriod.Dinner, 8.90m));
            _menu.Add(new MenuItem("pizza", "Pizza Margherita", MenuCategory.Meal, DayPeriod.Dinner, 9.20m));
            _menu.Add(new MenuItem("tiramisu", "Τιραμισού", MenuCategory.Dessert, DayPeriod.Dinner, 4.20m));
        }

        // ---------------- Orders ----------------
        public Order StartOrGetOpenOrder(string tent)
        {
            var open = _orders
                .Where(o => o.Tent.Equals(tent, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault(o => o.Status == OrderStatus.Open || o.Status == OrderStatus.Preparing);

            if (open != null) return open;

            var created = new Order { Tent = tent, Status = OrderStatus.Open };
            _orders.Add(created);
            OrdersChanged?.Invoke();
            return created;
        }

        public Order? GetById(Guid id) => _orders.FirstOrDefault(o => o.Id == id);

        public void AddItem(Guid orderId, MenuItem item, int qty)
        {
            var order = GetById(orderId);
            if (order == null) return;

            order.AddItem(item, qty);
            if (order.Status == OrderStatus.Open) order.Status = OrderStatus.Preparing;

            OrdersChanged?.Invoke();
        }

        public void PayOrder(Guid id)
        {
            var o = GetById(id);
            if (o == null) return;
            o.Status = OrderStatus.Paid;
            OrdersChanged?.Invoke();
        }

        public void ChargeToTent(Guid id)
        {
            var o = GetById(id);
            if (o == null) return;
            o.Status = OrderStatus.Charged;
            OrdersChanged?.Invoke();
        }
    }
}
