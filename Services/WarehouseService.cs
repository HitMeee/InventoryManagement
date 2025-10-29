using InventoryManagement.Data;
using InventoryManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace InventoryManagement.Services
{
    public class WarehouseService
    {
        private readonly string? _conn;
        public WarehouseService(string? connectionString = null) { _conn = connectionString; }

        public List<Warehouse> GetAll()
        {
            using var ctx = new AppDbContext(_conn);
            return ctx.Warehouses.AsNoTracking().OrderBy(w => w.Name).ToList();
        }
    }
}
