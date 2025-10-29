using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using InventoryManagement.Models;

namespace InventoryManagement.Views
{
    public partial class UserFormDialog : Window
    {
        public string Username { get; private set; } = string.Empty;
        public string Password { get; private set; } = string.Empty;
        public string SelectedRole { get; private set; } = string.Empty;
        public int WarehouseId { get; private set; }

        private readonly List<string> _roles;
        private readonly List<Warehouse> _warehouses;

        public UserFormDialog(IEnumerable<string> roles, IEnumerable<Warehouse> warehouses)
        {
            InitializeComponent();
            _roles = roles?.ToList() ?? new List<string>();
            _warehouses = warehouses?.ToList() ?? new List<Warehouse>();

            CboRole.ItemsSource = _roles;
            if (_roles.Count > 0) CboRole.SelectedIndex = 0;

            CboWarehouse.ItemsSource = _warehouses;
            if (_warehouses.Count > 0) CboWarehouse.SelectedIndex = 0;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var uname = TxtUsername?.Text?.Trim() ?? string.Empty;
                var pwd = (TxtPassword as PasswordBox)?.Password ?? string.Empty;
                var role = CboRole?.SelectedItem as string ?? string.Empty;
                var wId = CboWarehouse?.SelectedValue as int? ?? (CboWarehouse?.SelectedItem as Warehouse)?.Id ?? 0;

                if (string.IsNullOrWhiteSpace(uname))
                {
                    MessageBox.Show("Vui lòng nhập Tên tài khoản", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(pwd))
                {
                    MessageBox.Show("Vui lòng nhập Mật khẩu", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(role))
                {
                    MessageBox.Show("Vui lòng chọn Chức vụ", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (wId <= 0)
                {
                    MessageBox.Show("Vui lòng chọn Kho", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Username = uname;
                Password = pwd;
                SelectedRole = role;
                WarehouseId = wId;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
