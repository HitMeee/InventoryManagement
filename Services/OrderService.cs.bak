using InventoryManagement.Data;
using InventoryManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace InventoryManagement.Services
{
    public class OrderService
    {
        private readonly string? _conn;
        public OrderService(string? connectionString = null) { _conn = connectionString; }

        public List<Order> GetAll()
        {
            using var ctx = new AppDbContext(_conn);
            return ctx.Orders.Include(o => o.Customer).Include(o => o.Items).ThenInclude(i => i.Product).AsNoTracking().ToList();
        }

        public Order CreateOrder(int customerId, int productId, int qty)
        {
            using var ctx = new AppDbContext(_conn);
            var order = new Order { CustomerId = customerId, CreatedAt = DateTime.UtcNow };
            order.Items = new List<OrderItem> { new OrderItem { ProductId = productId, Quantity = qty, UnitPrice = ctx.Products.Find(productId)!.Price } };
            ctx.Orders.Add(order);

            // decrease inventory (simple: find any inventory item and reduce)
            var inv = ctx.InventoryItems.FirstOrDefault(ii => ii.ProductId == productId && ii.Quantity >= qty);
            if (inv != null) inv.Quantity -= qty;

            ctx.SaveChanges();
            return order;
        }
    }
}
