using System;
using System.IO;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

class Program
{
    static string HashPassword(string password, int iterations = 100000)
    {
        var salt = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password ?? string.Empty, salt, iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    static void Main(string[] args)
    {
        var db = Path.Combine(Directory.GetCurrentDirectory(), "inventory.db");
        if (args.Length > 0) db = args[0];
        Console.WriteLine($"DB: {db}");
        if (!File.Exists(db)) { Console.WriteLine("DB not found"); Environment.Exit(2); }

        var adminPw = "Admin@123";
        var staffPw = "Staff@123";

        using var conn = new SqliteConnection($"Data Source={db}");
        conn.Open();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "UPDATE users SET password = @pw WHERE username = @u";
            cmd.Parameters.AddWithValue("@pw", HashPassword(adminPw));
            cmd.Parameters.AddWithValue("@u", "admin");
            var n = cmd.ExecuteNonQuery();
            Console.WriteLine($"Admin rows updated: {n}");
        }

        using (var cmd2 = conn.CreateCommand())
        {
            cmd2.CommandText = "UPDATE users SET password = @pw WHERE username = @u";
            cmd2.Parameters.AddWithValue("@pw", HashPassword(staffPw));
            cmd2.Parameters.AddWithValue("@u", "staff");
            var n2 = cmd2.ExecuteNonQuery();
            Console.WriteLine($"Staff rows updated: {n2}");
        }

        Console.WriteLine("Done");
    }
}
