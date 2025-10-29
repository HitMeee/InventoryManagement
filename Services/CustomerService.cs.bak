using InventoryManagement.Data;
using InventoryManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace InventoryManagement.Services
{
    public class CustomerService
    {
        private readonly string? _conn;
        public CustomerService(string? connectionString = null) { _conn = connectionString; }

        public List<Customer> GetAll()
        {
            using var ctx = new AppDbContext(_conn);
            return ctx.Customers.AsNoTracking().OrderBy(c => c.Name).ToList();
        }

        public Customer Add(Customer c)
        {
            using var ctx = new AppDbContext(_conn);
            ctx.Customers.Add(c);
            ctx.SaveChanges();
            return c;
        }

        public bool Update(Customer c)
        {
            using var ctx = new AppDbContext(_conn);
            ctx.Customers.Update(c);
            return ctx.SaveChanges() > 0;
        }

        public bool Delete(int id)
        {
            using var ctx = new AppDbContext(_conn);
            var e = ctx.Customers.Find(id);
            if (e == null) return false;
            ctx.Customers.Remove(e);
            return ctx.SaveChanges() > 0;
        }
    }
}
