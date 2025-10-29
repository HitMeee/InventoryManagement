using System;
using Microsoft.Data.Sqlite;
using System.IO;

class Program
{
    static int Main(string[] args)
    {
        try
        {
            // Resolve DB path: prefer project-root inventory.db
            var projectRoot = FindProjectRoot();
            var dbPath = Path.Combine(projectRoot ?? Directory.GetCurrentDirectory(), "inventory.db");
            Console.WriteLine($"Using DB: {dbPath}");

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            // Ensure tables exist (non-destructive)
            ExecNonQuery(connection, @"CREATE TABLE IF NOT EXISTS warehouses (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                address TEXT,
                created_at TEXT DEFAULT (datetime('now'))
            );");

            ExecNonQuery(connection, @"CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT NOT NULL UNIQUE,
                password TEXT NOT NULL,
                email TEXT,
                full_name TEXT,
                created_at TEXT DEFAULT (datetime('now'))
            );");

            ExecNonQuery(connection, @"CREATE TABLE IF NOT EXISTS user_warehouse_roles (
                user_id INTEGER NOT NULL,
                warehouse_id INTEGER NOT NULL,
                role TEXT NOT NULL,
                created_at TEXT DEFAULT (datetime('now')),
                PRIMARY KEY (user_id, warehouse_id, role),
                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
                FOREIGN KEY (warehouse_id) REFERENCES warehouses(id) ON DELETE CASCADE
            );");

            // Insert two warehouses if missing
            var wh1Id = UpsertWarehouse(connection, "Kho Trung Tâm", "123 Đường A, Quận 1");
            var wh2Id = UpsertWarehouse(connection, "Kho Khu Vực", "456 Đường B, Quận 7");
            Console.WriteLine($"Warehouse IDs: {wh1Id}, {wh2Id}");

            // Find admin user (username = 'admin')
            long? adminId = null;
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT id FROM users WHERE lower(username) = lower('admin') LIMIT 1";
                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value) adminId = Convert.ToInt64(result);
            }

            if (adminId == null)
            {
                Console.WriteLine("No 'admin' user found. Nothing to assign. Exiting with code 2.");
                return 2;
            }

            // Assign admin to the first warehouse (if not already)
            UpsertUserWarehouseRole(connection, adminId.Value, wh1Id, "admin");
            Console.WriteLine("Assigned 'admin' to first warehouse as admin.");

            // Show current mapping summary
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"SELECT u.username, w.name AS warehouse, uwr.role
                                    FROM user_warehouse_roles uwr
                                    JOIN users u ON u.id = uwr.user_id
                                    JOIN warehouses w ON w.id = uwr.warehouse_id
                                    WHERE u.id = $uid";
                cmd.Parameters.AddWithValue("$uid", adminId.Value);
                using var reader = cmd.ExecuteReader();
                Console.WriteLine("Current roles for 'admin':");
                while (reader.Read())
                {
                    Console.WriteLine($" - {reader["warehouse"]}: {reader["role"]}");
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERROR: " + ex);
            return 1;
        }
    }

    static string? FindProjectRoot()
    {
        // Walk up to find InventoryManagement.csproj
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "InventoryManagement.csproj")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    static void ExecNonQuery(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    static long UpsertWarehouse(SqliteConnection conn, string name, string? address)
    {
        // Try get existing
        using (var get = conn.CreateCommand())
        {
            get.CommandText = "SELECT id FROM warehouses WHERE name = $name LIMIT 1";
            get.Parameters.AddWithValue("$name", name);
            var result = get.ExecuteScalar();
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt64(result);
            }
        }

        using (var ins = conn.CreateCommand())
        {
            ins.CommandText = "INSERT INTO warehouses(name, address) VALUES ($name, $addr); SELECT last_insert_rowid();";
            ins.Parameters.AddWithValue("$name", name);
            ins.Parameters.AddWithValue("$addr", (object?)address ?? DBNull.Value);
            var id = ins.ExecuteScalar();
            return Convert.ToInt64(id);
        }
    }

    static void UpsertUserWarehouseRole(SqliteConnection conn, long userId, long warehouseId, string role)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT OR IGNORE INTO user_warehouse_roles(user_id, warehouse_id, role)
                            VALUES ($uid, $wid, $role);";
        cmd.Parameters.AddWithValue("$uid", userId);
        cmd.Parameters.AddWithValue("$wid", warehouseId);
        cmd.Parameters.AddWithValue("$role", role);
        cmd.ExecuteNonQuery();
    }
}
