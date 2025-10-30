using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Models;

namespace InventoryManagement.Data
{
    public static class DbInitializer
    {
        public static void Initialize(string? connectionString = null)
        {
            using var context = new AppDbContext(connectionString);

            context.Database.EnsureCreated();

            var conn = context.Database.GetDbConnection();
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"PRAGMA foreign_keys = OFF;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS warehouses (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    address TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    owner_id INTEGER
);";
                cmd.ExecuteNonQuery();

                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT NOT NULL UNIQUE,
    password TEXT NOT NULL
);";
                cmd.ExecuteNonQuery();

                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS user_warehouse_roles (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    warehouse_id INTEGER NOT NULL,
    role TEXT CHECK(role IN ('owner','admin','staff')) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (warehouse_id) REFERENCES warehouses(id) ON DELETE CASCADE
);";
                cmd.ExecuteNonQuery();

                // Migrate existing table to include 'owner' in CHECK constraint if needed
                cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='user_warehouse_roles'";
                var tableSql = (cmd.ExecuteScalar() as string) ?? string.Empty;
                // Rebuild table if schema outdated (missing 'owner' in CHECK) OR still contains global UNIQUE (warehouse_id, role)
                if (!tableSql.Contains("'owner'") || tableSql.Contains("UNIQUE (warehouse_id, role)"))
                {
                    // Rebuild table with updated CHECK constraint
                    cmd.CommandText = @"CREATE TABLE user_warehouse_roles_new (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    warehouse_id INTEGER NOT NULL,
    role TEXT CHECK(role IN ('owner','admin','staff')) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
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

                    // Recreate indexes
                    cmd.CommandText = @"CREATE INDEX IF NOT EXISTS idx_user_warehouse_roles_user ON user_warehouse_roles(user_id);";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = @"CREATE INDEX IF NOT EXISTS idx_user_warehouse_roles_warehouse ON user_warehouse_roles(warehouse_id);";
                    cmd.ExecuteNonQuery();
                }

                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS products (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    warehouse_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    quantity INTEGER DEFAULT 0,
    unit TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (warehouse_id) REFERENCES warehouses(id) ON DELETE CASCADE
);";
                cmd.ExecuteNonQuery();

                cmd.CommandText = @"CREATE INDEX IF NOT EXISTS idx_user_warehouse_roles_user ON user_warehouse_roles(user_id);";
                cmd.ExecuteNonQuery();
                cmd.CommandText = @"CREATE INDEX IF NOT EXISTS idx_user_warehouse_roles_warehouse ON user_warehouse_roles(warehouse_id);";
                cmd.ExecuteNonQuery();

                // Enforce unique Owner/Admin per warehouse via partial unique indexes (SQLite supports WHERE)
                cmd.CommandText = @"CREATE UNIQUE INDEX IF NOT EXISTS ux_uwr_owner_per_warehouse ON user_warehouse_roles(warehouse_id) WHERE role = 'owner';";
                cmd.ExecuteNonQuery();
                cmd.CommandText = @"CREATE UNIQUE INDEX IF NOT EXISTS ux_uwr_admin_per_warehouse ON user_warehouse_roles(warehouse_id) WHERE role = 'admin';";
                cmd.ExecuteNonQuery();

                // Ensure owner_id column exists in warehouses (for upgrades)
                cmd.CommandText = "PRAGMA table_info(warehouses);";
                using (var reader = cmd.ExecuteReader())
                {
                    bool hasOwner = false;
                    while (reader.Read())
                    {
                        var colName = reader[1]?.ToString() ?? string.Empty;
                        if (string.Equals(colName, "owner_id", StringComparison.OrdinalIgnoreCase))
                        {
                            hasOwner = true;
                            break;
                        }
                    }
                    reader.Close();
                    if (!hasOwner)
                    {
                        cmd.CommandText = "ALTER TABLE warehouses ADD COLUMN owner_id INTEGER;";
                        try { cmd.ExecuteNonQuery(); } catch { }
                    }
                }

                // Index for faster lookup by owner
                cmd.CommandText = @"CREATE INDEX IF NOT EXISTS idx_warehouses_owner ON warehouses(owner_id);";
                cmd.ExecuteNonQuery();

                // Backfill owner_id from existing owner mappings if any
                cmd.CommandText = @"UPDATE warehouses 
SET owner_id = (
    SELECT user_id FROM user_warehouse_roles uw
    WHERE uw.warehouse_id = warehouses.id AND uw.role = 'owner'
    LIMIT 1
) WHERE owner_id IS NULL;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "PRAGMA foreign_keys = ON;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "SELECT COUNT(1) FROM users;";
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count == 0)
                {
                    try
                    {
                        var adminHash = InventoryManagement.Services.PasswordHelper.HashPassword("admin123");
                        var staffHash = InventoryManagement.Services.PasswordHelper.HashPassword("staff123");

                        cmd.CommandText = "INSERT INTO warehouses (name, address) VALUES (@wn,@wa); SELECT last_insert_rowid();";
                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@wn"; p1.Value = "Main Warehouse"; cmd.Parameters.Add(p1);
                        var p2 = cmd.CreateParameter(); p2.ParameterName = "@wa"; p2.Value = "Hanoi"; cmd.Parameters.Add(p2);
                        var warehouseId = Convert.ToInt32(cmd.ExecuteScalar());
                        cmd.Parameters.Clear();

                        cmd.CommandText = "INSERT INTO users (username, password) VALUES (@u,@p); SELECT last_insert_rowid();";
                        var pa = cmd.CreateParameter(); pa.ParameterName = "@u"; pa.Value = "admin"; cmd.Parameters.Add(pa);
                        var pb = cmd.CreateParameter(); pb.ParameterName = "@p"; pb.Value = adminHash; cmd.Parameters.Add(pb);
                        var adminId = Convert.ToInt32(cmd.ExecuteScalar());
                        cmd.Parameters.Clear();

                        cmd.CommandText = "INSERT INTO users (username, password) VALUES (@u,@p); SELECT last_insert_rowid();";
                        var pc = cmd.CreateParameter(); pc.ParameterName = "@u"; pc.Value = "staff"; cmd.Parameters.Add(pc);
                        var pd = cmd.CreateParameter(); pd.ParameterName = "@p"; pd.Value = staffHash; cmd.Parameters.Add(pd);
                        var staffId = Convert.ToInt32(cmd.ExecuteScalar());
                        cmd.Parameters.Clear();

                        cmd.CommandText = "INSERT INTO user_warehouse_roles (user_id, warehouse_id, role) VALUES (@uid,@wid,@role);";
                        var r1 = cmd.CreateParameter(); r1.ParameterName = "@uid"; r1.Value = adminId; cmd.Parameters.Add(r1);
                        var r2 = cmd.CreateParameter(); r2.ParameterName = "@wid"; r2.Value = warehouseId; cmd.Parameters.Add(r2);
                        var r3 = cmd.CreateParameter(); r3.ParameterName = "@role"; r3.Value = "admin"; cmd.Parameters.Add(r3);
                        cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();

                        var s1 = cmd.CreateParameter(); s1.ParameterName = "@uid"; s1.Value = staffId; cmd.Parameters.Add(s1);
                        var s2 = cmd.CreateParameter(); s2.ParameterName = "@wid"; s2.Value = warehouseId; cmd.Parameters.Add(s2);
                        var s3 = cmd.CreateParameter(); s3.ParameterName = "@role"; s3.Value = "staff"; cmd.Parameters.Add(s3);
                        cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();
                    }
                    catch
                    {
                    }
                }
            }
            conn.Close();
        }
    }
}
