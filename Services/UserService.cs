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

            // Check role mappings of target
            var targetMaps = ctx.UserWarehouseRoles.Where(uw => uw.UserId == id).ToList();
            var isAdmin = targetMaps.Any(uw => string.Equals(uw.Role, "admin", StringComparison.OrdinalIgnoreCase));
            var isOwner = targetMaps.Any(uw => string.Equals(uw.Role, "owner", StringComparison.OrdinalIgnoreCase));

            // Never allow deleting an Owner account
            if (isOwner)
            {
                throw new InvalidOperationException("Không thể xoá tài khoản Chủ kho.");
            }
            // Only Owner can delete Admin accounts
            if (isAdmin && !Services.AuthService.IsOwner())
            {
                throw new InvalidOperationException("Chỉ Chủ kho mới được xoá tài khoản Admin.");
            }

            // Remove role mappings first to keep FK integrity
            var maps = ctx.UserWarehouseRoles.Where(uw => uw.UserId == id).ToList();
            if (maps.Count > 0)
            {
                ctx.UserWarehouseRoles.RemoveRange(maps);
            }

            ctx.Users.Remove(e);
            return ctx.SaveChanges() > 0;
        }

        public List<(User user, string roleDisplay, string warehouseDisplay, int? warehouseId)> GetAllWithDetails()
        {
            using var ctx = new AppDbContext(_conn);
            var users = ctx.Users.AsNoTracking().OrderBy(u => u.Username).ToList();
            var maps = ctx.UserWarehouseRoles.AsNoTracking().ToList();
            var wmap = ctx.Warehouses.AsNoTracking().ToDictionary(w => w.Id, w => w.Name);

            var list = new List<(User,string,string,int?)>();
            foreach (var u in users)
            {
                var umaps = maps.Where(m => m.UserId == u.Id).ToList();
                var hasOwner = umaps.Any(m => string.Equals(m.Role, "owner", StringComparison.OrdinalIgnoreCase));
                var isAdmin = umaps.Any(m => string.Equals(m.Role, "admin", StringComparison.OrdinalIgnoreCase));
                var roleDisp = hasOwner ? "Chủ kho" : (isAdmin ? "Admin" : (umaps.Any() ? "Nhân viên kho" : ""));
                string whDisp = "";
                int? whId = null;
                if (umaps.Count > 0)
                {
                    // Pick the first mapping for display; concat names if many
                    var names = umaps.Select(m => wmap.TryGetValue(m.WarehouseId, out var nm) ? nm : m.WarehouseId.ToString()).ToList();
                    whDisp = string.Join(", ", names);
                    whId = umaps.First().WarehouseId;
                }
                list.Add((u, roleDisp, whDisp, whId));
            }
            return list;
        }

        public User AddWithRoleAndWarehouse(string username, string passwordHash, string roleDisplay, int warehouseId)
        {
            using var ctx = new AppDbContext(_conn);
            var u = new User { Username = username, PasswordHash = passwordHash };
            ctx.Users.Add(u);
            ctx.SaveChanges();
            var role = NormalizeRoleToDb(roleDisplay);
            ctx.UserWarehouseRoles.Add(new UserWarehouseRole { UserId = u.Id, WarehouseId = warehouseId, Role = role, CreatedAt = DateTime.UtcNow });
            ctx.SaveChanges();
            return u;
        }

        public void UpdateUserAndMapping(int userId, string? newUsername, string? newPasswordHash, string? roleDisplay, int? warehouseId)
        {
            using var ctx = new AppDbContext(_conn);
            var u = ctx.Users.FirstOrDefault(x => x.Id == userId) ?? throw new InvalidOperationException("User không tồn tại");
            if (!string.IsNullOrWhiteSpace(newUsername)) u.Username = newUsername!;
            if (!string.IsNullOrEmpty(newPasswordHash)) u.PasswordHash = newPasswordHash!;
            ctx.SaveChanges();

            // ensure a single mapping for simplicity
            var oldMaps = ctx.UserWarehouseRoles.Where(m => m.UserId == userId).ToList();
            if (oldMaps.Count > 0) ctx.UserWarehouseRoles.RemoveRange(oldMaps);

            if (warehouseId.HasValue && !string.IsNullOrWhiteSpace(roleDisplay))
            {
                var role = NormalizeRoleToDb(roleDisplay!);
                ctx.UserWarehouseRoles.Add(new UserWarehouseRole { UserId = userId, WarehouseId = warehouseId.Value, Role = role, CreatedAt = DateTime.UtcNow });
            }
            ctx.SaveChanges();
        }

        private static string NormalizeRoleToDb(string roleDisplay)
        {
            if (string.Equals(roleDisplay, "Chủ kho", StringComparison.OrdinalIgnoreCase)) return "owner";
            if (string.Equals(roleDisplay, "Admin", StringComparison.OrdinalIgnoreCase)) return "admin";
            return "staff";
        }
    }
}
