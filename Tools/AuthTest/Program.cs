using System;
using System.IO;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 3) return password == storedHash; 
        if (!int.TryParse(parts[0], out var iter)) return false;
        var salt = Convert.FromBase64String(parts[1]);
        var hash = Convert.FromBase64String(parts[2]);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iter, HashAlgorithmName.SHA256);
        var derived = pbkdf2.GetBytes(hash.Length);
        return CryptographicOperations.FixedTimeEquals(derived, hash);
    }

    static int Main(string[] args)
    {
        var db = Path.Combine(Directory.GetCurrentDirectory(), "inventory.db");
        if (args.Length > 0) db = args[0];
        var user = args.Length > 1 ? args[1] : "admin";
        var pass = args.Length > 2 ? args[2] : "Admin@123";

        Console.WriteLine($"AuthTest -> DB: {db}, user: {user}");
        if (!File.Exists(db)) { Console.WriteLine("DB not found."); return 2; }

        try
        {
            using var conn = new SqliteConnection($"Data Source={db}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, username, password FROM users WHERE username = @u LIMIT 1";
            cmd.Parameters.AddWithValue("@u", user);
            using var r = cmd.ExecuteReader();
            if (!r.Read())
            {
                Console.WriteLine("User not found");
                return 3;
            }
            var id = r.GetInt32(0);
            var username = r.GetString(1);
            var pw = r.IsDBNull(2) ? null : r.GetString(2);
            Console.WriteLine($"Found user id={id}, username={username}, storedPassword={(pw?.Length>0?"(present)":"(null)")}");

            var ok = false;
            if (!string.IsNullOrEmpty(pw)) ok = VerifyPassword(pass, pw);
            Console.WriteLine($"Password verify: {ok}");

            using var rc = conn.CreateCommand();
            rc.CommandText = "SELECT role FROM user_warehouse_roles WHERE user_id = @id";
            rc.Parameters.AddWithValue("@id", id);
            using var rr = rc.ExecuteReader();
            var roles = new System.Collections.Generic.List<string>();
            while (rr.Read()) roles.Add(rr.GetString(0));
            Console.WriteLine("Roles: " + (roles.Count==0?"(none)":string.Join(",",roles)));

            return ok ? 0 : 4;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during auth test: " + ex.Message);
            Console.WriteLine(ex.StackTrace);
            return 5;
        }
    }
}
