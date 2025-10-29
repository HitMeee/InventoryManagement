using System;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Data;
using InventoryManagement.Models;

namespace InventoryManagement.Views
{
    public partial class InventoryHistoryDialog : Window
    {
        public class HistoryDisplay
        {
            public string TransactionType { get; set; } = string.Empty;
            public string TransactionTypeDisplay { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string Note { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }

        private Product _product;

        public InventoryHistoryDialog(Product product)
        {
            InitializeComponent();
            _product = product;
            
            LoadProductInfo();
            LoadHistory();
        }

        private void LoadProductInfo()
        {
            try
            {
                using var db = new AppDbContext();
                var productWithWarehouse = db.Products
                    .Include(p => p.Warehouse)
                    .FirstOrDefault(p => p.Id == _product.Id);

                if (productWithWarehouse != null)
                {
                    TxtProductName.Text = productWithWarehouse.Name;
                    TxtCurrentQuantity.Text = productWithWarehouse.Quantity.ToString();
                    TxtUnit.Text = productWithWarehouse.Unit;
                    TxtWarehouse.Text = productWithWarehouse.Warehouse?.Name ?? "N/A";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải thông tin sản phẩm: {ex.Message}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadHistory()
        {
            try
            {
                using var db = new AppDbContext();
                var transactions = db.InventoryTransactions
                    .Include(it => it.User)
                    .Where(it => it.ProductId == _product.Id)
                    .OrderByDescending(it => it.CreatedAt)
                    .ToList();

                var historyDisplays = transactions.Select(t => new HistoryDisplay
                {
                    TransactionType = t.TransactionType,
                    TransactionTypeDisplay = t.TransactionType == "IMPORT" ? "Nhập" : "Xuất",
                    Quantity = t.Quantity,
                    UserName = t.User?.Username ?? "N/A",
                    Note = string.IsNullOrEmpty(t.Note) ? "-" : t.Note,
                    CreatedAt = t.CreatedAt
                }).ToList();

                DgHistory.ItemsSource = historyDisplays;

                if (!historyDisplays.Any())
                {
                    MessageBox.Show("Chưa có giao dịch nào cho sản phẩm này.", "Thông tin", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải lịch sử: {ex.Message}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}