using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Linq;
using InventoryManagement.Services;

namespace InventoryManagement.Views
{
    public partial class UsersView : UserControl
    {
        private bool _isSearchPlaceholder = true;
        private const string SearchPlaceholder = "Tìm kiếm theo tên, vai trò hoặc kho...";

        public UsersView()
        {
            InitializeComponent();
            this.Loaded += UsersView_Loaded;
        }

        // Password handling is done in ViewModel via event wiring in XAML code-behind if needed
        private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is InventoryManagement.ViewModels.UsersViewModel vm && sender is PasswordBox pb)
            {
                vm.NewPassword = pb.Password;
            }
        }

        private void UsersView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize placeholder state for search box
                if (TxtSearch != null)
                {
                    TxtSearch.Text = SearchPlaceholder;
                    TxtSearch.FontStyle = FontStyles.Italic;
                    TxtSearch.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
                    _isSearchPlaceholder = true;
                }

                // Attach filter on collection view for Users
                if (DataContext is InventoryManagement.ViewModels.UsersViewModel vm)
                {
                    var view = CollectionViewSource.GetDefaultView(vm.Users);
                    view.Filter = UserFilter;
                }
            }
            catch { }
        }

        private bool UserFilter(object? obj)
        {
            try
            {
                if (obj is not InventoryManagement.ViewModels.UserListItem item)
                    return true;

                var text = TxtSearch?.Text ?? string.Empty;
                if (_isSearchPlaceholder || string.IsNullOrWhiteSpace(text)) return true;

                text = text.Trim().ToLowerInvariant();
                bool Match(string? s) => (s ?? string.Empty).ToLowerInvariant().Contains(text);
                return Match(item.Username) || Match(item.RoleDisplay) || Match(item.WarehouseDisplay);
            }
            catch
            {
                return true;
            }
        }

        private void RefreshFilter()
        {
            if (DataContext is InventoryManagement.ViewModels.UsersViewModel vm)
            {
                CollectionViewSource.GetDefaultView(vm.Users)?.Refresh();
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isSearchPlaceholder)
            {
                RefreshFilter();
            }
        }

        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = (TextBox)sender;
            if (_isSearchPlaceholder)
            {
                tb.Text = string.Empty;
                tb.FontStyle = FontStyles.Normal;
                tb.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                _isSearchPlaceholder = false;
            }
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(tb.Text))
            {
                tb.Text = SearchPlaceholder;
                tb.FontStyle = FontStyles.Italic;
                tb.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
                _isSearchPlaceholder = true;
                RefreshFilter();
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is InventoryManagement.ViewModels.UsersViewModel vm)
                {
                    // Use existing roles and filtered warehouses from ViewModel
                    var roles = vm.Roles.ToList();
                    var warehouses = vm.Warehouses.ToList();

                    var dialog = new UserFormDialog(roles, warehouses)
                    {
                        Owner = Window.GetWindow(this)
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        var hash = PasswordHelper.HashPassword(dialog.Password);
                        var service = new UserService();
                        service.AddWithRoleAndWarehouse(dialog.Username, hash, dialog.SelectedRole, dialog.WarehouseId);

                        vm.Load();
                        MessageBox.Show("Thêm người dùng thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi thêm người dùng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
