using System;
using System.Security.Cryptography;

namespace InventoryManagement.Services
{
    public static class PasswordHelper
    {
        private const int SaltSize = 16; // bytes
        private const int HashSize = 32; // bytes
        private const int Iterations = 100_000;

        public static string HashPassword(string password)
        {
            if (password == null) password = string.Empty;
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(HashSize);
            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (storedHash == null) return false;
            try
            {
                var parts = storedHash.Split('.', 3);
                if (parts.Length != 3) return false;
                var iterations = int.Parse(parts[0]);
                var salt = Convert.FromBase64String(parts[1]);
                var hash = Convert.FromBase64String(parts[2]);
                using var pbkdf2 = new Rfc2898DeriveBytes(password ?? string.Empty, salt, iterations, HashAlgorithmName.SHA256);
                var computed = pbkdf2.GetBytes(hash.Length);
                return CryptographicOperations.FixedTimeEquals(computed, hash);
            }
            catch
            {
                return false;
            }
        }
    }
}
