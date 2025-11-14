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
        // Model dùng để hiển thị lên DataGrid
        public class AllHistoryDisplay
        {
            public string TransactionType { get; set; } = string.Empty;        // IMPORT / EXPORT
            public string TransactionTypeDisplay { get; set; } = string.Empty; // Nhập / Xuất (hiển thị)
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

            LoadWarehouses();  // Load danh sách kho vào ComboBox
            LoadAllHistory();  // Load toàn bộ lịch sử giao dịch
        }

        // ---------------------------------------------------------
        // Load danh sách kho + thêm lựa chọn "Tất cả kho"
        // ---------------------------------------------------------
        private void LoadWarehouses()
        {
            try
            {
                using var db = new AppDbContext();
                var warehouses = db.Warehouses.OrderBy(w => w.Name).ToList();

                // Thêm dòng "Tất cả kho"
                var allOption = new Warehouse { Id = -1, Name = "Tất cả kho" };
                warehouses.Insert(0, allOption);

                CboWarehouseFilter.ItemsSource = warehouses;
                CboWarehouseFilter.SelectedIndex = 0;  // Default: tất cả
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách kho: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Load lịch sử giao dịch, có lọc theo loại + kho
        private void LoadAllHistory()
        {
            try
            {
                var allTransactions = LoadTransactionsFromFile(); // Lấy toàn bộ giao dịch

                // Lấy filter loại giao dịch
                var selectedType = ((ComboBoxItem)CboTransactionType.SelectedItem)?.Tag?.ToString() ?? "ALL";

                // Lọc theo kho
                var selectedWarehouseId = CboWarehouseFilter?.SelectedValue != null ?
                    (int)CboWarehouseFilter.SelectedValue : -1;

                var filtered = allTransactions;

                // Lọc theo loại: Nhập / Xuất
                if (selectedType != "ALL")
                    filtered = filtered.Where(h => h.TransactionType == selectedType).ToList();

                // Lọc theo kho
                if (selectedWarehouseId != -1)
                    filtered = filtered.Where(h => h.WarehouseId == selectedWarehouseId).ToList();

                // Gán vào DataGrid
                DgAllHistory.ItemsSource = filtered;
                TxtSummary.Text = $"Tổng: {filtered.Count} giao dịch";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải lịch sử: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------------------------------------------------------
        // Lấy danh sách giao dịch từ database → chuyển thành AllHistoryDisplay
        // ---------------------------------------------------------
        private List<AllHistoryDisplay> LoadTransactionsFromFile()
        {
            try
            {
                using var db = new AppDbContext();

                // Lấy toàn bộ transactions
                var transactionsFromDb = db.InventoryTransactions
                    .OrderByDescending(t => t.CreatedAt)
                    .ToList();

                // Lấy id liên quan (product, user, warehouse)
                var productIds = transactionsFromDb.Select(t => t.ProductId).Distinct().ToList();
                var warehouseIds = transactionsFromDb.Select(t => t.WarehouseId).Distinct().ToList();
                var userIds = transactionsFromDb.Select(t => t.UserId).Distinct().ToList();

                // Load dữ liệu liên quan (tránh lazy loading lỗi)
                var products = db.Products.Where(p => productIds.Contains(p.Id))
                    .ToDictionary(p => p.Id, p => p.Name);

                var warehouses = db.Warehouses.Where(w => warehouseIds.Contains(w.Id))
                    .ToDictionary(w => w.Id, w => w.Name);

                var users = db.Users.Where(u => userIds.Contains(u.Id))
                    .ToDictionary(u => u.Id, u => u.Username);

                // Map sang model hiển thị
                var list = transactionsFromDb.Select(t => new AllHistoryDisplay
                {
                    CreatedAt = t.CreatedAt,
                    TransactionType = t.TransactionType,
                    TransactionTypeDisplay = t.TransactionType == "IMPORT" ? "Nhập" : "Xuất",
                    ProductName = products.ContainsKey(t.ProductId) ? products[t.ProductId] : "N/A",
                    WarehouseName = warehouses.ContainsKey(t.WarehouseId) ? warehouses[t.WarehouseId] : "N/A",
                    Quantity = t.Quantity,
                    Unit = t.Unit ?? "",
                    UserName = users.ContainsKey(t.UserId) ? users[t.UserId] : "N/A",
                    Note = t.Note ?? "",
                    WarehouseId = t.WarehouseId
                }).ToList();

                return list;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đọc lịch sử: {ex.Message}", "Lỗi");
                return new List<AllHistoryDisplay>();
            }
        }

        // ---------------------------------------------------------
        // Một trong các filter thay đổi → reload danh sách
        // ---------------------------------------------------------
        private void CboTransactionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) LoadAllHistory();
        }

        private void CboWarehouseFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) LoadAllHistory();
        }

        // ---------------------------------------------------------
        // Đóng dialog
        // ---------------------------------------------------------
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
