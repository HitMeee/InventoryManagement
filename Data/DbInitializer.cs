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
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
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
    role TEXT CHECK(role IN ('admin','staff')) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (warehouse_id, role),
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (warehouse_id) REFERENCES warehouses(id) ON DELETE CASCADE
);";
                cmd.ExecuteNonQuery();

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
