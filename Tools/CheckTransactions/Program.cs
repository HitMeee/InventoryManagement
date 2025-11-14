using System;
using Microsoft.Data.Sqlite;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "inventory.db");
        Console.WriteLine($"Checking transactions in: {path}");
        
        if (!File.Exists(path))
        {
            Console.WriteLine("Database file not found!");
            return;
        }

        var cs = $"Data Source={path}";
        using var conn = new SqliteConnection(cs);
        conn.Open();

        // Check if table exists
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='inventory_transactions';";
            var result = cmd.ExecuteScalar();
            if (result == null)
            {
                Console.WriteLine("Table 'inventory_transactions' does not exist!");
                return;
            }
            Console.WriteLine("✓ Table 'inventory_transactions' exists");
        }

        // Count transactions
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM inventory_transactions;";
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            Console.WriteLine($"\nTotal transactions: {count}");
        }

        // Show all transactions
        Console.WriteLine("\n=== ALL TRANSACTIONS ===");
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT 
                    id, 
                    product_id, 
                    warehouse_id, 
                    user_id, 
                    transaction_type, 
                    quantity, 
                    unit, 
                    note, 
                    created_at 
                FROM inventory_transactions 
                ORDER BY created_at DESC 
                LIMIT 20;";
            
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine($"\nID: {reader["id"]}");
                Console.WriteLine($"  Type: {reader["transaction_type"]}");
                Console.WriteLine($"  Product ID: {reader["product_id"]}");
                Console.WriteLine($"  Warehouse ID: {reader["warehouse_id"]}");
                Console.WriteLine($"  User ID: {reader["user_id"]}");
                Console.WriteLine($"  Quantity: {reader["quantity"]} {reader["unit"]}");
                Console.WriteLine($"  Note: {reader["note"]}");
                Console.WriteLine($"  Created: {reader["created_at"]}");
                Console.WriteLine("  ---");
            }
        }
    }
}
