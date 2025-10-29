using InventoryManagement.Data;
using InventoryManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace InventoryManagement.Services
{
    public class SupplierService
    {
        private readonly string? _conn;
        public SupplierService(string? connectionString = null) { _conn = connectionString; }

        public List<Supplier> GetAll()
        {
            using var ctx = new AppDbContext(_conn);
            return ctx.Suppliers.AsNoTracking().OrderBy(s => s.Name).ToList();
        }

        public Supplier Add(Supplier s)
        {
            using var ctx = new AppDbContext(_conn);
            ctx.Suppliers.Add(s);
            ctx.SaveChanges();
            return s;
        }

        public bool Update(Supplier s)
        {
            using var ctx = new AppDbContext(_conn);
            ctx.Suppliers.Update(s);
            return ctx.SaveChanges() > 0;
        }

        public bool Delete(int id)
        {
            using var ctx = new AppDbContext(_conn);
            var e = ctx.Suppliers.Find(id);
            if (e == null) return false;
            ctx.Suppliers.Remove(e);
            return ctx.SaveChanges() > 0;
        }
    }
}
