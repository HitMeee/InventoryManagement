using InventoryManagement.Data;
using InventoryManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace InventoryManagement.Services
{
    public class UserService
    {
        private readonly string? _conn;
        public UserService(string? connectionString = null) { _conn = connectionString; }

        public List<User> GetAll()
        {
            using var ctx = new AppDbContext(_conn);
            return ctx.Users.AsNoTracking().OrderBy(u => u.Username).ToList();
        }

        public User Add(User u)
        {
            using var ctx = new AppDbContext(_conn);
            ctx.Users.Add(u);
            ctx.SaveChanges();
            return u;
        }

        public bool Delete(int id)
        {
            using var ctx = new AppDbContext(_conn);
            var e = ctx.Users.Find(id);
            if (e == null) return false;
            ctx.Users.Remove(e);
            return ctx.SaveChanges() > 0;
        }
    }
}
