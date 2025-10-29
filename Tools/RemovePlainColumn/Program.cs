using System;
using System.IO;
using Microsoft.Data.Sqlite;

class Program
{
    static int Main(string[] args)
    {
        var db = Path.Combine(Directory.GetCurrentDirectory(), "inventory.db");
        if (args.Length > 0) db = args[0];
        Console.WriteLine($"DB: {db}");
        if (!File.Exists(db)) { Console.WriteLine("DB not found"); return 2; }

        using var conn = new SqliteConnection($"Data Source={db}");
        conn.Open();

        bool hasPlain = false;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info('users');";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var name = r.GetString(1);
                if (name == "password_plain") { hasPlain = true; break; }
            }
        }

        if (!hasPlain)
        {
            Console.WriteLine("Column 'password_plain' not present. Nothing to do.");
            return 0;
        }

        // backup
        var bak = db + ".bak." + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        File.Copy(db, bak);
        Console.WriteLine($"Backup created: {bak}");

        // perform safe migration: create temp table without password_plain, copy, drop, rename
        using (var tcmd = conn.CreateCommand())
        {
            tcmd.CommandText = "PRAGMA foreign_keys = OFF;";
            tcmd.ExecuteNonQuery();

            tcmd.CommandText = @"CREATE TABLE users_new (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT NOT NULL UNIQUE,
    password TEXT NOT NULL
);";
            tcmd.ExecuteNonQuery();

            tcmd.CommandText = "INSERT INTO users_new (id, username, password) SELECT id, username, password FROM users;";
            tcmd.ExecuteNonQuery();

            tcmd.CommandText = "DROP TABLE users;";
            tcmd.ExecuteNonQuery();

            tcmd.CommandText = "ALTER TABLE users_new RENAME TO users;";
            tcmd.ExecuteNonQuery();

            tcmd.CommandText = "PRAGMA foreign_keys = ON;";
            tcmd.ExecuteNonQuery();
        }

        Console.WriteLine("password_plain column removed (users table migrated).\nCheck DB and restore from backup if needed.");
        return 0;
    }
}
