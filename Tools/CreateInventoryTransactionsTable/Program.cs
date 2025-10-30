using System;
using Microsoft.Data.Sqlite;

namespace InventoryManagement.Tools
{
    class CreateInventoryTransactionsTable
    {
        static void Main(string[] args)
        {
            try
            {
                // Tìm file inventory.db
                var dbPath = "inventory.db";
                if (!File.Exists(dbPath))
                {
                    dbPath = "../../inventory.db";
                }
                if (!File.Exists(dbPath))
                {
                    Console.WriteLine("❌ Không tìm thấy file inventory.db");
                    return;
                }
                
                Console.WriteLine($"📁 Sử dụng database: {Path.GetFullPath(dbPath)}");
                
                var connectionString = $"Data Source={dbPath}";
                
                using var connection = new SqliteConnection(connectionString);
                connection.Open();
                
                // Kiểm tra xem bảng đã tồn tại chưa
                var checkTableCmd = connection.CreateCommand();
                checkTableCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='inventory_transactions';";
                var result = checkTableCmd.ExecuteScalar();
                
                if (result == null)
                {
                    Console.WriteLine("🔨 Tạo bảng inventory_transactions...");
                    
                    var createTableCmd = connection.CreateCommand();
                    createTableCmd.CommandText = @"
                        CREATE TABLE inventory_transactions (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            product_id INTEGER NOT NULL,
                            warehouse_id INTEGER NOT NULL,
                            user_id INTEGER NOT NULL,
                            transaction_type TEXT NOT NULL CHECK (transaction_type IN ('IMPORT', 'EXPORT')),
                            quantity INTEGER NOT NULL,
                            unit TEXT NOT NULL,
                            note TEXT,
                            created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            FOREIGN KEY (product_id) REFERENCES products(id),
                            FOREIGN KEY (warehouse_id) REFERENCES warehouses(id),
                            FOREIGN KEY (user_id) REFERENCES users(id)
                        );";
                    
                    createTableCmd.ExecuteNonQuery();
                    Console.WriteLine("✅ Bảng inventory_transactions đã được tạo thành công!");
                }
                else
                {
                    Console.WriteLine("ℹ️  Bảng inventory_transactions đã tồn tại.");
                }
                
                // Hiển thị các bảng hiện có
                var showTablesCmd = connection.CreateCommand();
                showTablesCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
                using var reader = showTablesCmd.ExecuteReader();
                
                Console.WriteLine("\n📋 Các bảng trong database:");
                while (reader.Read())
                {
                    Console.WriteLine($"  - {reader["name"]}");
                }
                
                connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi: {ex.Message}");
            }
        }
    }
}