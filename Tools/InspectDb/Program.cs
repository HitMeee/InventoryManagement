using System;
using Microsoft.Data.Sqlite;
using System.IO;

class Program
{
    static int Main(string[] args)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "inventory.db");
        if (args.Length > 0 && File.Exists(args[0])) path = args[0];

        Console.WriteLine($"Inspecting DB: {path}");
        if (!File.Exists(path))
        {
            Console.WriteLine("File not found.");
            return 1;
        }

        var cs = $"Data Source={path}";
        using var conn = new SqliteConnection(cs);
        conn.Open();

        void Dump(string query)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            using var r = cmd.ExecuteReader();
            var cols = new System.Collections.Generic.List<string>();
            for (int i = 0; i < r.FieldCount; i++) cols.Add(r.GetName(i));
            Console.WriteLine($"\nColumns: {string.Join(", ", cols)}");
            while (r.Read())
            {
                for (int i = 0; i < r.FieldCount; i++)
                {
                    var name = r.GetName(i);
                    var val = r.IsDBNull(i) ? "NULL" : r.GetValue(i).ToString();
                    Console.WriteLine($"  {name}: {val}");
                }
                Console.WriteLine("-");
            }
        }

        Console.WriteLine("Tables in DB:");
        using (var tcmd = conn.CreateCommand())
        {
            tcmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
            using var tr = tcmd.ExecuteReader();
            while (tr.Read()) Console.WriteLine(" - " + tr.GetString(0));
        }

        try
        {
            Dump("SELECT id, username, password FROM users;");
        }
        catch (Exception ex) { Console.WriteLine("Cannot read users table: " + ex.Message); }

        try
        {
            Dump("SELECT id, user_id, warehouse_id, role, created_at FROM user_warehouse_roles;");
            using var cmd2 = conn.CreateCommand();
            cmd2.CommandText = "SELECT COUNT(1) FROM user_warehouse_roles WHERE role='owner'";
            var c = Convert.ToInt32(cmd2.ExecuteScalar());
            Console.WriteLine($"\nOwner mappings count: {c}");
            using var cmd3 = conn.CreateCommand();
            cmd3.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='user_warehouse_roles'";
            var ddl = cmd3.ExecuteScalar()?.ToString() ?? "(no ddl)";
            Console.WriteLine("\nuser_warehouse_roles DDL: \n" + ddl);
        }
        catch (Exception ex) { Console.WriteLine("Cannot read user_warehouse_roles table: " + ex.Message); }

        return 0;
    }
}
