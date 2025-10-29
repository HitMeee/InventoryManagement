using InventoryManagement.Data;
using InventoryManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace InventoryManagement.Services
{
    public class InventoryService
    {
        private readonly string? _conn;
        public InventoryService(string? connectionString = null) { _conn = connectionString; }

        public List<InventoryItem> GetAll()
        {
            using var ctx = new AppDbContext(_conn);
            return ctx.InventoryItems.Include(ii => ii.Product).Include(ii => ii.Warehouse).AsNoTracking().ToList();
        }

        public void Adjust(int inventoryItemId, int delta, string reason)
        {
            using var ctx = new AppDbContext(_conn);
            var item = ctx.InventoryItems.Find(inventoryItemId);
            if (item == null) throw new Exception("Inventory item not found");
            item.Quantity += delta;
            ctx.SaveChanges();
            // In a full implementation, log the transaction with reason, user, timestamp
        }
    }
}
