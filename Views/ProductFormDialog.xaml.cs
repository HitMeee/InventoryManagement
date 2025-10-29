using System;
using System.Linq;
using System.Windows;
using InventoryManagement.Data;
using InventoryManagement.Models;

namespace InventoryManagement.Views
{
    public partial class ProductFormDialog : Window
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
    // Use DialogResult to indicate saved/cancelled state

        public ProductFormDialog(int? selectedWarehouseId = null, string name = "", int quantity = 0, string unit = "")
        {
            try
            {
                InitializeComponent();
                
                // Load data after controls are initialized
                Loaded += (s, e) =>
                {
                    try
                    {
                        LoadWarehouses();
                
                        TxtName.Text = name ?? string.Empty;
                        TxtQuantity.Text = quantity.ToString();
                        TxtUnit.Text = unit ?? string.Empty;
                
                        if (selectedWarehouseId.HasValue && selectedWarehouseId.Value > 0)
                        {
                            CboWarehouse.SelectedValue = selectedWarehouseId.Value;
                        }
                
                        TxtName.Focus();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi load dữ liệu: {ex.Message}\n\n{ex.StackTrace}", "Lỗi", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi tạo form: {ex.Message}\n\n{ex.StackTrace}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadWarehouses()
        {
            try
            {
                using var db = new AppDbContext();
                var warehouses = db.Warehouses.OrderBy(w => w.Name).ToList();
                CboWarehouse.ItemsSource = warehouses;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách kho: {ex.Message}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validate tên sản phẩm
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("Vui lòng nhập tên sản phẩm!", "Thông báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtName.Focus();
                return;
            }

            // Validate số lượng
            if (!int.TryParse(TxtQuantity.Text, out int qty) || qty < 0)
            {
                MessageBox.Show("Vui lòng nhập số lượng hợp lệ (số nguyên >= 0)!", "Thông báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtQuantity.Focus();
                return;
            }

            // Validate đơn vị
            if (string.IsNullOrWhiteSpace(TxtUnit.Text))
            {
                MessageBox.Show("Vui lòng nhập đơn vị!", "Thông báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtUnit.Focus();
                return;
            }

            // Validate kho hàng
            if (CboWarehouse.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn kho hàng!", "Thông báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CboWarehouse.Focus();
                return;
            }

            ProductName = TxtName.Text.Trim();
            Quantity = qty;
            Unit = TxtUnit.Text.Trim();
            WarehouseId = (int)CboWarehouse.SelectedValue;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
