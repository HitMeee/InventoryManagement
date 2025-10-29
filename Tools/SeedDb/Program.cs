using System;
using System.IO;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

class Program
{
    // Simple PBKDF2-SHA256 hasher compatible with PasswordHelper used in main app.
    static string HashPassword(string password)
    {
        const int iterations = 100_000;
        const int saltSize = 16;
        const int hashSize = 32;
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[saltSize];
        rng.GetBytes(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(hashSize);
        return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    static int Main(string[] args)
    {
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "inventory.db");
        Console.WriteLine($"Seeding DB at: {dbPath}");
        if (!File.Exists(dbPath))
        {
            Console.WriteLine("Error: inventory.db not found in current directory. Run this from the repository root where inventory.db is located.");
            return 2;
        }

        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();

        // Insert a warehouse
        cmd.CommandText = "INSERT INTO warehouses (name, address) VALUES (@n,@a); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@n", "Main Warehouse");
        cmd.Parameters.AddWithValue("@a", "Hanoi");
        var warehouseId = Convert.ToInt32(cmd.ExecuteScalar());
        cmd.Parameters.Clear();

        // Insert Admin user
        var adminUser = "admin";
        var adminPass = "admin123";
        var adminHash = HashPassword(adminPass);
        cmd.CommandText = "INSERT INTO users (username, password) VALUES (@u,@p); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@u", adminUser);
        cmd.Parameters.AddWithValue("@p", adminHash);
        var adminId = Convert.ToInt32(cmd.ExecuteScalar());
        cmd.Parameters.Clear();

        // Insert staff user
        var staffUser = "staff";
        var staffPass = "staff123";
        var staffHash = HashPassword(staffPass);
        cmd.CommandText = "INSERT INTO users (username, password) VALUES (@u,@p); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@u", staffUser);
        cmd.Parameters.AddWithValue("@p", staffHash);
        var staffId = Convert.ToInt32(cmd.ExecuteScalar());
        cmd.Parameters.Clear();

        // Map roles in user_warehouse_roles
        cmd.CommandText = "INSERT INTO user_warehouse_roles (user_id, warehouse_id, role) VALUES (@uid,@wid,@role);";
        cmd.Parameters.AddWithValue("@uid", adminId);
        cmd.Parameters.AddWithValue("@wid", warehouseId);
        cmd.Parameters.AddWithValue("@role", "admin");
        cmd.ExecuteNonQuery();
        cmd.Parameters.Clear();

        cmd.CommandText = "INSERT INTO user_warehouse_roles (user_id, warehouse_id, role) VALUES (@uid,@wid,@role);";
        cmd.Parameters.AddWithValue("@uid", staffId);
        cmd.Parameters.AddWithValue("@wid", warehouseId);
        cmd.Parameters.AddWithValue("@role", "staff");
        cmd.ExecuteNonQuery();
        cmd.Parameters.Clear();

        tx.Commit();
        Console.WriteLine($"Seeded users: admin (password: {adminPass}), staff (password: {staffPass}) into warehouse id {warehouseId}");
        return 0;
    }
}
