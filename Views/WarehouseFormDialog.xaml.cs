using System;
using System.Windows;

namespace InventoryManagement.Views
{
    public partial class WarehouseFormDialog : Window
    {
        public string WarehouseName { get; set; } = string.Empty;
        public string WarehouseAddress { get; set; } = string.Empty;

        public WarehouseFormDialog(string name = "", string address = "")
        {
            try
            {
                InitializeComponent();
                
                // Load data after controls are initialized
                Loaded += (s, e) =>
                {
                    try
                    {
                        if (TxtName != null) TxtName.Text = name;
                        if (TxtAddress != null) TxtAddress.Text = address;
                        TxtName?.Focus();
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

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TxtName == null || string.IsNullOrWhiteSpace(TxtName.Text))
                {
                    MessageBox.Show("Vui lòng nhập tên kho hàng!", "Thông báo", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtName?.Focus();
                    return;
                }

                WarehouseName = TxtName.Text.Trim();
                WarehouseAddress = TxtAddress?.Text?.Trim() ?? "";
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu kho: {ex.Message}\n\n{ex.StackTrace}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
