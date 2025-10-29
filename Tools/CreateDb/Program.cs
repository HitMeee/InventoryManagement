using System;
using Microsoft.Data.Sqlite;
using System.IO;

class Program
{
    static int Main(string[] args)
    {
    // Create DB in the current working directory (where the command is run).
    // This keeps behavior predictable when invoked from the repository root.
    var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "inventory.db");

        Console.WriteLine($"Creating DB at: {dbPath}");
        var connString = $"Data Source={dbPath}";

        if (File.Exists(dbPath))
        {
            Console.WriteLine("Existing DB will be deleted.");
            File.Delete(dbPath);
        }

        using var conn = new SqliteConnection(connString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"PRAGMA foreign_keys = OFF;";
        cmd.ExecuteNonQuery();

        // create tables per user's DDL
        cmd.CommandText = @"CREATE TABLE warehouses (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    address TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT NOT NULL UNIQUE,
    password TEXT NOT NULL
);";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE user_warehouse_roles (
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

        cmd.CommandText = @"CREATE TABLE products (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    warehouse_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    quantity INTEGER DEFAULT 0,
    unit TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (warehouse_id) REFERENCES warehouses(id) ON DELETE CASCADE
);";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();

        Console.WriteLine("Database created successfully.");
        return 0;
    }
}
