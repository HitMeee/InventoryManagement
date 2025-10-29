using System;
using System.IO;
using Microsoft.Data.Sqlite;

class Program
{
    static int Main(string[] args)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "inventory.db");
        if (args.Length>0 && File.Exists(args[0])) path = args[0];
        Console.WriteLine($"Checking DB: {path}");
        if (!File.Exists(path)) { Console.WriteLine("DB not found"); return 2; }
        using var conn = new SqliteConnection($"Data Source={path}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA table_info('users');";
        using var r = cmd.ExecuteReader();
        Console.WriteLine("columns in users:");
        while (r.Read())
        {
            var cid = r.GetInt32(0);
            var name = r.GetString(1);
            var type = r.GetString(2);
            var notnull = r.GetInt32(3);
            var dflt = r.IsDBNull(4)?"(null)":r.GetString(4);
            var pk = r.GetInt32(5);
            Console.WriteLine($" - {name} | {type} | notnull={notnull} | dflt={dflt} | pk={pk}");
        }
        return 0;
    }
}
