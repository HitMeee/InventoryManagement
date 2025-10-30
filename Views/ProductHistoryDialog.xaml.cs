using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

namespace InventoryManagement.Views
{
    public partial class ProductHistoryDialog : Window
    {
        public class HistoryDisplay
        {
            public string TransactionType { get; set; } = string.Empty;
            public string TransactionTypeDisplay { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public string Unit { get; set; } = string.Empty;
            public string WarehouseName { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }

        private readonly string _productName;
        private readonly int _productId;
        
        public ProductHistoryDialog(string productName, int productId, string warehouseName, string unit)
        {
            InitializeComponent();
            _productName = productName;
            _productId = productId;
            
            TxtProductTitle.Text = $"Lịch sử: {productName}";
            TxtProductDetails.Text = $"Kho: {warehouseName} • Đơn vị: {unit}";
            
            LoadHistory();
        }

        private void LoadHistory()
        {
            try
            {
                var allTransactions = LoadTransactionsFromFile();
                
                // Lọc theo sản phẩm
                var productTransactions = allTransactions
                    .Where(t => t.ProductName.Equals(_productName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // Lọc theo loại giao dịch
                var selectedType = ((ComboBoxItem)CboFilterType.SelectedItem)?.Tag?.ToString() ?? "ALL";
                if (selectedType != "ALL")
                {
                    productTransactions = productTransactions
                        .Where(t => t.TransactionType == selectedType)
                        .ToList();
                }

                // Sắp xếp theo thời gian giảm dần
                productTransactions = productTransactions
                    .OrderByDescending(t => t.CreatedAt)
                    .ToList();

                DgHistory.ItemsSource = productTransactions;
                
                // Cập nhật thông tin tổng hợp
                var importCount = productTransactions.Count(t => t.TransactionType == "IMPORT");
                var exportCount = productTransactions.Count(t => t.TransactionType == "EXPORT");
                var totalImported = productTransactions.Where(t => t.TransactionType == "IMPORT").Sum(t => t.Quantity);
                var totalExported = productTransactions.Where(t => t.TransactionType == "EXPORT").Sum(t => t.Quantity);
                
                TxtSummary.Text = $"Tổng: {productTransactions.Count} giao dịch • " +
                                 $"Nhập: {importCount} lần ({totalImported}) • " +
                                 $"Xuất: {exportCount} lần ({totalExported})";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải lịch sử: {ex.Message}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<HistoryDisplay> LoadTransactionsFromFile()
        {
            try
            {
                // Tìm file log trong project directory hoặc working directory
                var logFile = "transactions.log";
                
                // Thử trong working directory trước
                if (!System.IO.File.Exists(logFile))
                {
                    // Thử trong project directory
                    var projectDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    if (!string.IsNullOrEmpty(projectDir))
                    {
                        var projectLogFile = System.IO.Path.Combine(projectDir, "transactions.log");
                        if (System.IO.File.Exists(projectLogFile))
                        {
                            logFile = projectLogFile;
                        }
                        else
                        {
                            // Thử tìm trong parent directories
                            var parentDir = new System.IO.DirectoryInfo(projectDir);
                            for (int i = 0; i < 5 && parentDir != null; i++)
                            {
                                var testPath = System.IO.Path.Combine(parentDir.FullName, "transactions.log");
                                if (System.IO.File.Exists(testPath))
                                {
                                    logFile = testPath;
                                    break;
                                }
                                parentDir = parentDir.Parent;
                            }
                        }
                    }
                }

                if (!System.IO.File.Exists(logFile))
                {
                    return new List<HistoryDisplay>();
                }

                var transactions = new List<HistoryDisplay>();
                var lines = System.IO.File.ReadAllLines(logFile, System.Text.Encoding.UTF8);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split('\t');
                    if (parts.Length >= 6)
                    {
                        if (DateTime.TryParse(parts[0], out DateTime createdAt))
                        {
                            var transaction = new HistoryDisplay
                            {
                                CreatedAt = createdAt,
                                TransactionType = parts[1].Trim(),
                                TransactionTypeDisplay = parts[1].Trim() == "IMPORT" ? "Nhập hàng" : "Xuất hàng",
                                ProductName = parts[2].Trim(),
                                WarehouseName = parts[3].Trim(),
                                Quantity = int.TryParse(parts[4].Trim(), out int qty) ? qty : 0,
                                Unit = parts[5].Trim()
                            };
                            transactions.Add(transaction);
                        }
                    }
                }

                return transactions;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đọc file log: {ex.Message}", "Lỗi");
                return new List<HistoryDisplay>();
            }
        }

        private void CboFilterType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                LoadHistory();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}