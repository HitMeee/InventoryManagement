using System;
using System.Windows;

namespace InventoryManagement
{
    public partial class MainWindow : Window
    {
        private readonly Models.User? _currentUser;

        public MainWindow(Models.User? user)
        {
            _currentUser = user;
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                var user = _currentUser ?? Services.AuthService.CurrentUser;
                if (user == null)
                {
                    // No user: close app for safety
                    MessageBox.Show("Bạn chưa đăng nhập.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Application.Current.Shutdown();
                    return;
                }

                // Set title with username and role
                this.Title = $"Quản lý kho - {user.Username} ({user.Role})";

                // Role-based visibility
                // Admin: all tabs
                // Kho: Tồn kho, Sản phẩm (view only), Báo cáo
                // BanHang: Đơn hàng, Khách hàng, Tồn kho
                var role = user.Role ?? string.Empty;

                if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    // nothing to hide
                }
                else if (string.Equals(role, "Nhân viên kho", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(role, "Kho", StringComparison.OrdinalIgnoreCase) ||
                         role.IndexOf("kho", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // show only inventory, products, reports
                    TabProducts.Visibility = Visibility.Visible;
                    TabInventory.Visibility = Visibility.Visible;
                    TabSuppliers.Visibility = Visibility.Collapsed;
                    TabCustomers.Visibility = Visibility.Collapsed;
                    TabOrders.Visibility = Visibility.Collapsed;
                    TabUsers.Visibility = Visibility.Collapsed;
                    TabReports.Visibility = Visibility.Visible;
                }
                else if (string.Equals(role, "Nhân viên bán hàng", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(role, "BanHang", StringComparison.OrdinalIgnoreCase) ||
                         role.IndexOf("ban", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    TabProducts.Visibility = Visibility.Visible;
                    TabInventory.Visibility = Visibility.Visible;
                    TabSuppliers.Visibility = Visibility.Collapsed;
                    TabCustomers.Visibility = Visibility.Visible;
                    TabOrders.Visibility = Visibility.Visible;
                    TabUsers.Visibility = Visibility.Collapsed;
                    TabReports.Visibility = Visibility.Visible;
                }
                else
                {
                    // unknown role: be conservative
                    TabProducts.Visibility = Visibility.Collapsed;
                    TabInventory.Visibility = Visibility.Collapsed;
                    TabSuppliers.Visibility = Visibility.Collapsed;
                    TabCustomers.Visibility = Visibility.Collapsed;
                    TabOrders.Visibility = Visibility.Collapsed;
                    TabUsers.Visibility = Visibility.Collapsed;
                    TabReports.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi khởi tạo giao diện chính: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
