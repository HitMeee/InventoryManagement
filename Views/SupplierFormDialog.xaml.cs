using System;
using System.Windows;
using InventoryManagement.Data;
using InventoryManagement.Models;

namespace InventoryManagement.Views
{
    public partial class SupplierFormDialog : Window
    {
        public string SupplierName { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public int? SupplierId { get; set; }

        public SupplierFormDialog(int? supplierId = null, string name = "", string contact = "")
        {
            InitializeComponent();
            Loaded += (s, e) => {
                SupplierId = supplierId;
                TxtName.Text = name ?? string.Empty;
                TxtContact.Text = contact ?? string.Empty;
            };
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("Vui lòng nhập tên nhà cung cấp.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SupplierName = TxtName.Text.Trim();
            Contact = TxtContact.Text?.Trim() ?? string.Empty;
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
