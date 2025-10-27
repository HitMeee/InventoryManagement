using InventoryManagement.Data;
using InventoryManagement.Models;
using System.Linq;
using System;

namespace InventoryManagement.Services
{
    public static class AuthService
    {
        public static User? CurrentUser { get; private set; }

        public enum AuthResult
        {
            Success,
            UserNotFound,
            WrongPassword,
            WrongRole,
            Error
        }

        public static AuthResult Authenticate(string username, string password, string role, string? connectionString = null)
        {
            try
            {
                username = (username ?? string.Empty).Trim();
                password = password ?? string.Empty;
                role = role ?? string.Empty;

                using var ctx = new AppDbContext(connectionString);
                var user = ctx.Users.FirstOrDefault(u => u.Username == username);
                if (user == null) return AuthResult.UserNotFound;
                if (user.PasswordHash != password) return AuthResult.WrongPassword;
                if (!string.Equals(user.Role ?? string.Empty, role, StringComparison.OrdinalIgnoreCase)) return AuthResult.WrongRole;

                CurrentUser = user;
                return AuthResult.Success;
            }
            catch
            {
                return AuthResult.Error;
            }
        }

        // Backwards-compatible wrapper
        public static bool Login(string username, string password, string role, string? connectionString = null)
        {
            return Authenticate(username, password, role, connectionString) == AuthResult.Success;
        }

        public static void Logout() => CurrentUser = null;
    }
}
