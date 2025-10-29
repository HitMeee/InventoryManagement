using System;
using Microsoft.Data.Sqlite;
using System.IO;

class Program
{
    static int Main(string[] args)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "inventory.db");
        if (args.Length > 0 && File.Exists(args[0])) path = args[0];

        Console.WriteLine($"Using DB: {path}");
        if (!File.Exists(path)) { Console.WriteLine("DB not found."); return 2; }

        var cs = $"Data Source={path}";
        using var conn = new SqliteConnection(cs);
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
            Console.WriteLine("Adding column password_plain to users table...");
            using var c2 = conn.CreateCommand();
            c2.CommandText = "ALTER TABLE users ADD COLUMN password_plain TEXT;";
            c2.ExecuteNonQuery();
        }

        var adminPw = "Admin@123";
        var staffPw = "Staff@123";

        using (var up = conn.CreateCommand())
        {
            up.CommandText = "UPDATE users SET password_plain = @pw WHERE username = @u;";
            up.Parameters.AddWithValue("@pw", adminPw);
            up.Parameters.AddWithValue("@u", "admin");
            var n = up.ExecuteNonQuery();
            Console.WriteLine($"Updated admin rows: {n}");
        }
        using (var up2 = conn.CreateCommand())
        {
            up2.CommandText = "UPDATE users SET password_plain = @pw WHERE username = @u;";
            up2.Parameters.AddWithValue("@pw", staffPw);
            up2.Parameters.AddWithValue("@u", "staff");
            var n2 = up2.ExecuteNonQuery();
            Console.WriteLine($"Updated staff rows: {n2}");
        }

        Console.WriteLine("Done. Admin password: " + adminPw + " | Staff password: " + staffPw);
        return 0;
    }
}
