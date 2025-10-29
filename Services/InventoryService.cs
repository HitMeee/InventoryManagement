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
            var q = ctx.InventoryItems.Include(ii => ii.Product).Include(ii => ii.Warehouse).AsNoTracking().AsQueryable();
            var cu = AuthService.CurrentUser;
            if (cu != null && !string.Equals(cu.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                // Restrict staff to their assigned warehouses
                var ids = AuthService.CurrentUserWarehouseIds;
                if (ids != null && ids.Count > 0)
                {
                    q = q.Where(ii => ids.Contains(ii.WarehouseId));
                }
                else
                {
                    // no mapping -> see nothing
                    return new List<InventoryItem>();
                }
            }
            return q.ToList();
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
