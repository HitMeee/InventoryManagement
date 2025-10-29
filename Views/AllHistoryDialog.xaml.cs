using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Data;
using InventoryManagement.Models;
using System.Collections.Generic;

namespace InventoryManagement.Views
{
    public partial class AllHistoryDialog : Window
    {
        public class AllHistoryDisplay
        {
            public string TransactionType { get; set; } = string.Empty;
            public string TransactionTypeDisplay { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public string WarehouseName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public string Unit { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public int WarehouseId { get; set; }
        }

        public AllHistoryDialog()
        {
            InitializeComponent();
            LoadWarehouses();
            LoadAllHistory();
        }

        private void LoadWarehouses()
        {
            try
            {
                using var db = new AppDbContext();
                var warehouses = db.Warehouses.OrderBy(w => w.Name).ToList();
                
                var allOption = new Warehouse { Id = -1, Name = "Tất cả kho" };
                warehouses.Insert(0, allOption);
                
                CboWarehouseFilter.ItemsSource = warehouses;
                CboWarehouseFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách kho: {ex.Message}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAllHistory()
        {
            try
            {
                var allTransactions = LoadTransactionsFromFile();
                
                var selectedType = ((ComboBoxItem)CboTransactionType.SelectedItem)?.Tag?.ToString() ?? "ALL";
                var selectedWarehouseId = CboWarehouseFilter?.SelectedValue != null ? 
                    (int)CboWarehouseFilter.SelectedValue : -1;

                var filteredTransactions = allTransactions;

                if (selectedType != "ALL")
                {
                    filteredTransactions = filteredTransactions.Where(h => h.TransactionType == selectedType).ToList();
                }

                if (selectedWarehouseId != -1)
                {
                    filteredTransactions = filteredTransactions.Where(h => h.WarehouseId == selectedWarehouseId).ToList();
                }

                DgAllHistory.ItemsSource = filteredTransactions;
                TxtSummary.Text = $"Tổng: {filteredTransactions.Count} giao dịch";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải lịch sử: {ex.Message}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<AllHistoryDisplay> LoadTransactionsFromFile()
        {
            try
            {
                var logFile = "transactions.log";
                if (!System.IO.File.Exists(logFile))
                {
                    return new List<AllHistoryDisplay>();
                }

                var transactions = new List<AllHistoryDisplay>();
                var lines = System.IO.File.ReadAllLines(logFile, System.Text.Encoding.UTF8);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split('\t');
                    if (parts.Length >= 6)
                    {
                        if (DateTime.TryParse(parts[0], out DateTime createdAt))
                        {
                            var transaction = new AllHistoryDisplay
                            {
                                CreatedAt = createdAt,
                                TransactionType = parts[1].Trim(),
                                TransactionTypeDisplay = parts[1].Trim() == "IMPORT" ? "Nhập" : "Xuất",
                                ProductName = parts[2].Trim(),
                                WarehouseName = parts[3].Trim(),
                                Quantity = int.TryParse(parts[4].Trim(), out int qty) ? qty : 0,
                                Unit = parts[5].Trim(),
                                WarehouseId = parts[3].Trim().Contains("Hà Nội") || parts[3].Trim().Contains("Hà nội") ? 1 : 2
                            };
                            transactions.Add(transaction);
                        }
                    }
                    else
                    {
                        // Parse format bị lỗi, thử tách theo space
                        var allParts = line.Replace('\t', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        
                        if (allParts.Length >= 5)
                        {
                            var typeIndex = Array.FindIndex(allParts, p => p == "IMPORT" || p == "EXPORT");
                            if (typeIndex >= 0)
                            {
                                var productName = "";
                                var quantity = 1;
                                var unit = "cái";
                                
                                // Tìm tên sản phẩm
                                for (int i = typeIndex + 1; i < allParts.Length; i++)
                                {
                                    if (!int.TryParse(allParts[i], out _) && 
                                        !allParts[i].Contains("Hà") && 
                                        allParts[i] != "kg" && allParts[i] != "cái")
                                    {
                                        productName += allParts[i] + " ";
                                    }
                                    else break;
                                }
                                
                                // Tìm số lượng
                                foreach (var part in allParts)
                                {
                                    if (int.TryParse(part, out int q) && q > 0 && q < 1000)
                                    {
                                        quantity = q;
                                        break;
                                    }
                                }
                                
                                // Tìm đơn vị
                                foreach (var part in allParts)
                                {
                                    if (part == "kg" || part == "cái" || part == "thùng")
                                    {
                                        unit = part;
                                        break;
                                    }
                                }

                                var transaction = new AllHistoryDisplay
                                {
                                    CreatedAt = DateTime.Now,
                                    TransactionType = allParts[typeIndex],
                                    TransactionTypeDisplay = allParts[typeIndex] == "IMPORT" ? "Nhập" : "Xuất",
                                    ProductName = productName.Trim(),
                                    WarehouseName = "Hà Nội",
                                    Quantity = quantity,
                                    Unit = unit,
                                    WarehouseId = 1
                                };
                                transactions.Add(transaction);
                            }
                        }
                    }
                }

                return transactions.OrderByDescending(t => t.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đọc file log: {ex.Message}", "Lỗi");
                return new List<AllHistoryDisplay>();
            }
        }

        private void CboTransactionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                LoadAllHistory();
            }
        }

        private void CboWarehouseFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                LoadAllHistory();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}