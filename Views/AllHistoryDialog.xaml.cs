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
            public string UserName { get; set; } = string.Empty;
            public string Note { get; set; } = string.Empty;
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
                using var db = new AppDbContext();
                
                // Load từ database với Include để lấy dữ liệu liên quan
                var transactions = db.InventoryTransactions
                    .Include(t => t.Product)
                    .Include(t => t.Warehouse) 
                    .Include(t => t.User)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new AllHistoryDisplay
                    {
                        CreatedAt = t.CreatedAt,
                        TransactionType = t.TransactionType,
                        TransactionTypeDisplay = t.TransactionType == "IMPORT" ? "Nhập" : "Xuất",
                        ProductName = t.Product != null ? t.Product.Name : "N/A",
                        WarehouseName = t.Warehouse != null ? t.Warehouse.Name : "N/A",
                        Quantity = t.Quantity,
                        Unit = t.Unit,
                        UserName = t.User != null ? t.User.Username : "N/A",
                        Note = t.Note ?? "",
                        WarehouseId = t.WarehouseId
                    })
                    .ToList();

                return transactions;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đọc lịch sử từ database: {ex.Message}", "Lỗi");
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