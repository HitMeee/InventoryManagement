using System;
using System.IO;
using Microsoft.Data.Sqlite;

class Program
{
    static int Main(string[] args)
    {
        try
        {
            var projectRoot = FindProjectRoot();
            var dbPath = Path.Combine(projectRoot ?? Directory.GetCurrentDirectory(), "inventory.db");
            Console.WriteLine($"Using DB: {dbPath}");

            string username = args.Length > 0 ? args[0] : "owner";
            string password = args.Length > 1 ? args[1] : "owner123";
            int warehouseId = args.Length > 2 && int.TryParse(args[2], out var wid) ? wid : 1;

            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();

            // Ensure schema allows 'owner' role
            EnsureOwnerRoleAllowed(conn);

            // Ensure only one owner account
            using (var chk = conn.CreateCommand())
            {
                chk.CommandText = "SELECT COUNT(1) FROM user_warehouse_roles WHERE role='owner'";
                var count = Convert.ToInt32(chk.ExecuteScalar());
                if (count > 0)
                {
                    Console.WriteLine("An owner account already exists. Aborting.");
                    return 2;
                }
            }

            // Ensure warehouse exists
            using (var chkW = conn.CreateCommand())
            {
                chkW.CommandText = "SELECT COUNT(1) FROM warehouses WHERE id=$id";
                chkW.Parameters.AddWithValue("$id", warehouseId);
                var exists = Convert.ToInt32(chkW.ExecuteScalar()) > 0;
                if (!exists)
                {
                    Console.WriteLine($"Warehouse {warehouseId} not found. Aborting.");
                    return 3;
                }
            }

            // Ensure user exists or create
            int userId = 0;
            using (var chkUser = conn.CreateCommand())
            {
                chkUser.CommandText = "SELECT id FROM users WHERE username=$u";
                chkUser.Parameters.AddWithValue("$u", username);
                var existing = chkUser.ExecuteScalar();
                if (existing != null && existing != DBNull.Value)
                {
                    userId = Convert.ToInt32(existing);
                }
            }
            if (userId == 0)
            {
                using var ins = conn.CreateCommand();
                var hash = HashPassword(password);
                ins.CommandText = "INSERT INTO users(username, password) VALUES($u,$p); SELECT last_insert_rowid();";
                ins.Parameters.AddWithValue("$u", username);
                ins.Parameters.AddWithValue("$p", hash);
                userId = Convert.ToInt32(ins.ExecuteScalar());
            }

            // Map owner role
            using (var map = conn.CreateCommand())
            {
                map.CommandText = "INSERT OR IGNORE INTO user_warehouse_roles(user_id, warehouse_id, role) VALUES($uid,$wid,'owner')";
                map.Parameters.AddWithValue("$uid", userId);
                map.Parameters.AddWithValue("$wid", warehouseId);
                map.ExecuteNonQuery();
            }

            Console.WriteLine($"Created owner user '{username}' for warehouse {warehouseId}.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERROR: " + ex);
            return 1;
        }
    }

    static string HashPassword(string password)
    {
        const int iterations = 100_000;
        const int saltSize = 16;
        const int hashSize = 32;
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var salt = new byte[saltSize];
        rng.GetBytes(salt);
        using var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(password, salt, iterations, System.Security.Cryptography.HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(hashSize);
        return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    static string? FindProjectRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "InventoryManagement.csproj")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
    static void EnsureOwnerRoleAllowed(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = OFF;";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='user_warehouse_roles'";
        var ddl = cmd.ExecuteScalar()?.ToString() ?? string.Empty;
        if (ddl.Contains("'owner'"))
        {
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();
            return;
        }
        cmd.CommandText = @"CREATE TABLE user_warehouse_roles_new (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    warehouse_id INTEGER NOT NULL,
    role TEXT CHECK(role IN ('owner','admin','staff')) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (warehouse_id, role),
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (warehouse_id) REFERENCES warehouses(id) ON DELETE CASCADE
);";
        cmd.ExecuteNonQuery();
        cmd.CommandText = @"INSERT INTO user_warehouse_roles_new (id,user_id,warehouse_id,role,created_at)
SELECT id,user_id,warehouse_id,role,created_at FROM user_warehouse_roles";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DROP TABLE user_warehouse_roles";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "ALTER TABLE user_warehouse_roles_new RENAME TO user_warehouse_roles";
        cmd.ExecuteNonQuery();
        cmd.CommandText = @"CREATE INDEX IF NOT EXISTS idx_user_warehouse_roles_user ON user_warehouse_roles(user_id);";
        cmd.ExecuteNonQuery();
        cmd.CommandText = @"CREATE INDEX IF NOT EXISTS idx_user_warehouse_roles_warehouse ON user_warehouse_roles(warehouse_id);";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();
    }
}
