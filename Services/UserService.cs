using InventoryManagement.Data;
using InventoryManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;

namespace InventoryManagement.Services
{
    public class UserService
    {
        private readonly string? _conn;
        public UserService(string? connectionString = null) { _conn = connectionString; }

        private static bool CanCurrentUserAccessWarehouse(AppDbContext ctx, int warehouseId)
        {
            if (Services.AuthService.IsAdmin()) return true;
            var current = Services.AuthService.CurrentUser;
            if (current == null) return false;
            if (Services.AuthService.IsOwner())
            {
                var wh = ctx.Warehouses.AsNoTracking().FirstOrDefault(w => w.Id == warehouseId);
                return wh != null && wh.OwnerId == current.Id;
            }
            // staff: only warehouses mapped to them
            return Services.AuthService.CurrentUserWarehouseIds.Contains(warehouseId);
        }

        private static bool CanCurrentUserManageUsers()
        {
            // Only Admin or Owner can add/update/delete users
            return Services.AuthService.IsAdmin() || Services.AuthService.IsOwner();
        }

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

            if (!CanCurrentUserManageUsers())
            {
                throw new InvalidOperationException("Bạn không có quyền thực hiện thao tác này.");
            }

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

            // Scope restriction: Non-admin (i.e., Owner) may only delete users who belong to warehouses they own
            if (!Services.AuthService.IsAdmin())
            {
                var myId = Services.AuthService.CurrentUser?.Id ?? -1;
                var myWarehouses = ctx.Warehouses.AsNoTracking().Where(w => w.OwnerId == myId).Select(w => w.Id).ToHashSet();
                var shareAny = targetMaps.Any(m => myWarehouses.Contains(m.WarehouseId));
                if (!shareAny)
                {
                    throw new InvalidOperationException("Bạn không thể thao tác với người dùng thuộc kho không do bạn quản lý.");
                }
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

        public List<(User user, string roleDisplay, string warehouseDisplay, int? warehouseId)> GetAllWithDetails(bool isAdminOrOwner, List<int>? currentUserWarehouseIds)
        {
            using var ctx = new AppDbContext(_conn);
            var users = ctx.Users.AsNoTracking().OrderBy(u => u.Username).ToList();
            var maps = ctx.UserWarehouseRoles.AsNoTracking().ToList();
            var wmap = ctx.Warehouses.AsNoTracking().ToDictionary(w => w.Id, w => w.Name);

            var list = new List<(User,string,string,int?)>();
            foreach (var u in users)
            {
                var umaps = maps.Where(m => m.UserId == u.Id).ToList();
                // If the current user is not admin/owner, skip users who don't share a warehouse mapping
                if (!isAdminOrOwner)
                {
                    if (currentUserWarehouseIds == null || currentUserWarehouseIds.Count == 0)
                    {
                        // cannot see any users
                        continue;
                    }
                    var shared = umaps.Any(m => currentUserWarehouseIds.Contains(m.WarehouseId));
                    if (!shared) continue;
                }

                var hasOwner = umaps.Any(m => string.Equals(m.Role, "owner", StringComparison.OrdinalIgnoreCase));
                var isAdmin = umaps.Any(m => string.Equals(m.Role, "admin", StringComparison.OrdinalIgnoreCase));
                var roleDisp = hasOwner ? "Chủ kho" : (isAdmin ? "Admin" : (umaps.Any() ? "Nhân viên kho" : ""));
                string whDisp = "";
                int? whId = null;
                if (umaps.Count > 0)
                {
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
            if (!CanCurrentUserManageUsers())
            {
                throw new InvalidOperationException("Bạn không có quyền thực hiện thao tác này.");
            }
            if (!CanCurrentUserAccessWarehouse(ctx, warehouseId))
            {
                throw new InvalidOperationException("Bạn không thể gán người dùng vào kho không thuộc phạm vi quản lý.");
            }
            var u = new User { Username = username, PasswordHash = passwordHash };
            ctx.Users.Add(u);
            ctx.SaveChanges();
            var role = NormalizeRoleToDb(roleDisplay);
            // Enforce unique Owner/Admin per warehouse
            if (string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase))
            {
                var existsOwner = ctx.UserWarehouseRoles.Any(x => x.WarehouseId == warehouseId && x.Role.ToLower() == "owner");
                if (existsOwner)
                {
                    throw new InvalidOperationException("Mỗi kho chỉ có duy nhất 1 'Chủ kho'.");
                }
            }
            if (string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                var existsAdmin = ctx.UserWarehouseRoles.Any(x => x.WarehouseId == warehouseId && x.Role.ToLower() == "admin");
                if (existsAdmin)
                {
                    throw new InvalidOperationException("Mỗi kho chỉ có duy nhất 1 'Admin'.");
                }
            }
            ctx.UserWarehouseRoles.Add(new UserWarehouseRole { UserId = u.Id, WarehouseId = warehouseId, Role = role, CreatedAt = DateTime.UtcNow });
            // If the new role is owner, set warehouses.owner_id accordingly
            if (string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase))
            {
                var wh = ctx.Warehouses.FirstOrDefault(w => w.Id == warehouseId);
                if (wh != null) { wh.OwnerId = u.Id; }
            }
            ctx.SaveChanges();
            return u;
        }

        public void UpdateUserAndMapping(int userId, string? newUsername, string? newPasswordHash, string? roleDisplay, int? warehouseId)
        {
            using var ctx = new AppDbContext(_conn);
            if (!CanCurrentUserManageUsers())
            {
                throw new InvalidOperationException("Bạn không có quyền thực hiện thao tác này.");
            }
            var u = ctx.Users.FirstOrDefault(x => x.Id == userId) ?? throw new InvalidOperationException("User không tồn tại");
            if (!string.IsNullOrWhiteSpace(newUsername)) u.Username = newUsername!;
            if (!string.IsNullOrEmpty(newPasswordHash)) u.PasswordHash = newPasswordHash!;
            ctx.SaveChanges();

            // ensure a single mapping for simplicity
            var oldMaps = ctx.UserWarehouseRoles.Where(m => m.UserId == userId).ToList();
            if (oldMaps.Count > 0) ctx.UserWarehouseRoles.RemoveRange(oldMaps);

            if (warehouseId.HasValue && !string.IsNullOrWhiteSpace(roleDisplay))
            {
                if (!CanCurrentUserAccessWarehouse(ctx, warehouseId.Value))
                {
                    throw new InvalidOperationException("Bạn không thể gán người dùng vào kho không thuộc phạm vi quản lý.");
                }
                var role = NormalizeRoleToDb(roleDisplay!);
                // Enforce unique Owner/Admin per warehouse
                if (string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase))
                {
                    var existsOwner = ctx.UserWarehouseRoles.Any(x => x.WarehouseId == warehouseId.Value && x.Role.ToLower() == "owner" && x.UserId != userId);
                    if (existsOwner)
                    {
                        throw new InvalidOperationException("Mỗi kho chỉ có duy nhất 1 'Chủ kho'.");
                    }
                }
                if (string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    var existsAdmin = ctx.UserWarehouseRoles.Any(x => x.WarehouseId == warehouseId.Value && x.Role.ToLower() == "admin" && x.UserId != userId);
                    if (existsAdmin)
                    {
                        throw new InvalidOperationException("Mỗi kho chỉ có duy nhất 1 'Admin'.");
                    }
                }
                ctx.UserWarehouseRoles.Add(new UserWarehouseRole { UserId = userId, WarehouseId = warehouseId.Value, Role = role, CreatedAt = DateTime.UtcNow });
                // Reflect owner assignment to warehouses.owner_id
                var wh = ctx.Warehouses.FirstOrDefault(w => w.Id == warehouseId.Value);
                if (wh != null)
                {
                    if (string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase)) wh.OwnerId = userId;
                    else if (wh.OwnerId == userId) wh.OwnerId = null;
                }
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
