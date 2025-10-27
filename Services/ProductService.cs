using InventoryManagement.Data;
using InventoryManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace InventoryManagement.Services
{
    public class ProductService
    {
        private readonly string? _connectionString;

        public ProductService(string? connectionString = null)
        {
            _connectionString = connectionString;
        }

        public List<Product> GetAll()
        {
            using var ctx = new AppDbContext(_connectionString);
            return ctx.Products.AsNoTracking().OrderBy(p => p.Name).ToList();
        }

        public Product? Get(int id)
        {
            using var ctx = new AppDbContext(_connectionString);
            return ctx.Products.Find(id);
        }

        public Product Add(Product p)
        {
            using var ctx = new AppDbContext(_connectionString);
            ctx.Products.Add(p);
            ctx.SaveChanges();
            return p;
        }

        public bool Update(Product p)
        {
            using var ctx = new AppDbContext(_connectionString);
            ctx.Products.Update(p);
            return ctx.SaveChanges() > 0;
        }

        public bool Delete(int id)
        {
            using var ctx = new AppDbContext(_connectionString);
            var e = ctx.Products.Find(id);
            if (e == null) return false;
            ctx.Products.Remove(e);
            return ctx.SaveChanges() > 0;
        }
    }
}
