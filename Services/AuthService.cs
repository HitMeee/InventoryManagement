using InventoryManagement.Data;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Models;
using System.Linq;
using System;

namespace InventoryManagement.Services
{
    public static class AuthService
    {
    public static User? CurrentUser { get; private set; }
        public static List<int> CurrentUserWarehouseIds { get; private set; } = new();

        public enum AuthResult
        {
            Success,
            UserNotFound,
            WrongPassword,
            WrongRole,
            Error
        }

        public static AuthResult Authenticate(string username, string password, string? role = null, string? connectionString = null)
        {
            try
            {
                username = (username ?? string.Empty).Trim();
                password = password ?? string.Empty;
                role = role ?? string.Empty;

                using var ctx = new AppDbContext(connectionString);
                try
                {
                    var dbg = ctx.Database.GetDbConnection()?.ConnectionString ?? "(unknown)";
                    var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "auth_debug.log");
                    System.IO.File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] Authenticate called for '{username}'. DbConnectionString={dbg}\n");
                }
                catch { }
                var unameNormalized = (username ?? string.Empty).ToLowerInvariant();
                var user = ctx.Users.AsEnumerable().FirstOrDefault(u => (u.Username ?? string.Empty).ToLowerInvariant() == unameNormalized);
                if (user == null)
                {
                    try
                    {
                        var available = string.Join(",", ctx.Users.Select(u => (u.Username ?? string.Empty)).Take(50));
                        var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "auth_debug.log");
                        System.IO.File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] UserNotFound for '{username}'. AvailableUsers={available}\n");
                    }
                    catch { }
                    return AuthResult.UserNotFound;
                }
                var ok = false;
                if (!string.IsNullOrWhiteSpace(user.PasswordHash) && user.PasswordHash.Split('.').Length == 3)
                {
                    ok = PasswordHelper.VerifyPassword(password, user.PasswordHash);
                }
                else
                {
                    ok = string.Equals(user.PasswordHash ?? string.Empty, password ?? string.Empty);
                }
                if (!ok) return AuthResult.WrongPassword;
                var maps = ctx.UserWarehouseRoles.Where(uw => uw.UserId == user.Id).ToList();
                var roles = maps.Select(uw => uw.Role).ToList();
                if (roles.Contains("owner", StringComparer.OrdinalIgnoreCase))
                {
                    user.Role = "Chủ kho";
                }
                else if (roles.Contains("admin", StringComparer.OrdinalIgnoreCase))
                {
                    user.Role = "Admin";
                }
                else if (roles.Any())
                {
                    user.Role = "Nhân viên kho"; 
                }
                else
                {
                    user.Role = "";
                }

                if (!string.IsNullOrWhiteSpace(role) && !string.Equals(user.Role ?? string.Empty, role, StringComparison.OrdinalIgnoreCase)) return AuthResult.WrongRole;

                CurrentUser = user;
                CurrentUserWarehouseIds = maps.Select(m => m.WarehouseId).Distinct().ToList();
                return AuthResult.Success;
            }
            catch (Exception ex)
            {
                try
                {
                    var log = System.IO.Path.Combine(AppContext.BaseDirectory, "auth_error.log");
                    var txt = $"[{DateTime.UtcNow:O}] Exception during Authenticate: {ex}\n";
                    System.IO.File.AppendAllText(log, txt);
                }
                catch { }
                return AuthResult.Error;
            }
        }

        public static bool Login(string username, string password, string? role = null, string? connectionString = null)
        {
            return Authenticate(username, password, role, connectionString) == AuthResult.Success;
        }

        public static void Logout() => CurrentUser = null;
        
        public static bool IsAdmin() => string.Equals(CurrentUser?.Role, "Admin", StringComparison.OrdinalIgnoreCase);
        public static bool IsOwner() => string.Equals(CurrentUser?.Role, "Chủ kho", StringComparison.OrdinalIgnoreCase);
    }
}
