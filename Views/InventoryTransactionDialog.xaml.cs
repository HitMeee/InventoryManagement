using System;
using System.Windows;
using InventoryManagement.Models;

namespace InventoryManagement.Views
{
    public partial class InventoryTransactionDialog : Window
    {
        public Product Product { get; set; }
        public string TransactionType { get; set; } = "IMPORT";
        public int Quantity { get; set; }
        public string Note { get; set; } = string.Empty;

        public InventoryTransactionDialog(Product product)
        {
            InitializeComponent();
            Product = product;
            
            // Display product info
            TxtProductName.Text = product.Name;
            TxtCurrentQuantity.Text = product.Quantity.ToString();
            TxtUnit.Text = product.Unit;
            
            TxtQuantity.Focus();
        }

        public void SetImportMode()
        {
            RbImport.IsChecked = true;
            TransactionType_Changed(RbImport, new RoutedEventArgs());
        }

        public void SetExportMode()
        {
            RbExport.IsChecked = true;
            TransactionType_Changed(RbExport, new RoutedEventArgs());
        }

        private void TransactionType_Changed(object sender, RoutedEventArgs e)
        {
            if (RbImport.IsChecked == true)
            {
                TransactionType = "IMPORT";
                TxtTitle.Text = "Nhập hàng";
                WarningPanel.Visibility = Visibility.Collapsed;
            }
            else if (RbExport.IsChecked == true)
            {
                TransactionType = "EXPORT";
                TxtTitle.Text = "Xuất hàng";
                WarningPanel.Visibility = Visibility.Visible;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate quantity
                if (!int.TryParse(TxtQuantity.Text, out int quantity) || quantity <= 0)
                {
                    MessageBox.Show("Vui lòng nhập số lượng hợp lệ (lớn hơn 0)!", "Lỗi", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtQuantity.Focus();
                    return;
                }

                // Check if export quantity is valid
                if (TransactionType == "EXPORT" && quantity > Product.Quantity)
                {
                    MessageBox.Show($"Số lượng xuất ({quantity}) không thể lớn hơn số lượng hiện có ({Product.Quantity})!", 
                        "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtQuantity.Focus();
                    return;
                }

                Quantity = quantity;
                Note = TxtNote.Text.Trim();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}