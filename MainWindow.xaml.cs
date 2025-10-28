using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using InventoryManagement.Services;

namespace InventoryManagement
{
    public partial class MainWindow : Window
    {
        private readonly Models.User? _currentUser;

            private class MenuEntry
            {
                public string Key { get; }
                public string Title { get; }
                public string Feature { get; }
                public Func<UserControl> Factory { get; }

                public MenuEntry(string key, string title, string feature, Func<UserControl> factory)
                {
                    Key = key; Title = title; Feature = feature; Factory = factory;
                }
            }

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
                    MessageBox.Show("Bạn chưa đăng nhập.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Application.Current.Shutdown();
                    return;
                }

                this.Title = $"Quản lý kho - {user.Username} ({user.Role})";

                var navPanel = this.FindName("NavPanel") as StackPanel;
                var contentHost = this.FindName("ContentHost") as ContentControl;
                var txtUser = this.FindName("TxtUserInfo") as TextBlock;
                if (txtUser != null) txtUser.Text = $"{user.Username} — {user.Role}";

                if (navPanel == null || contentHost == null)
                {
                    return;
                }

                navPanel.Children.Clear();

                // define menu entries and required feature
                var menu = new List<MenuEntry>
                {
                    new MenuEntry("home","Trang chủ", RolePermissionService.Features.ViewStock, () => CreateHomeControl()),
                    new MenuEntry("orders","Đơn hàng", RolePermissionService.Features.ManageOrders, () => (UserControl)new Views.OrdersView()),
                    new MenuEntry("customers","Khách hàng", RolePermissionService.Features.ManageCustomers, () => (UserControl)new Views.CustomersView()),
                    new MenuEntry("stock","Kiểm tra tồn kho", RolePermissionService.Features.ViewStock, () => (UserControl)new Views.InventoryView()),
                    new MenuEntry("products","Sản phẩm", RolePermissionService.Features.ManageProducts, () => (UserControl)new Views.ProductsView()),
                    new MenuEntry("inventory","Kho hàng", RolePermissionService.Features.ManageInventory, () => (UserControl)new Views.InventoryView()),
                    new MenuEntry("reports","Báo cáo", RolePermissionService.Features.ViewStockReports, () => (UserControl)new Views.ReportsView()),
                    new MenuEntry("users","Người dùng", RolePermissionService.Features.ManageUsers, () => (UserControl)new Views.UsersView())
                };

                Button? first = null;
                foreach (var item in menu)
                {
                    var key = item.Key;
                    var title = item.Title;
                    var feature = item.Feature;
                    var factory = item.Factory;
                    if (!RolePermissionService.HasPermission(user.Role, feature)) continue;

                    var btn = new Button { Content = title, Tag = key, Style = (Style)FindResource("NavButtonStyle"), Margin = new Thickness(4), HorizontalAlignment = HorizontalAlignment.Stretch };
                    btn.Click += (s, ea) =>
                    {
                        // reset styles
                        foreach (var child in navPanel.Children.OfType<Button>()) child.Style = (Style)FindResource("NavButtonStyle");
                        btn.Style = (Style)FindResource("NavButtonSelected");
                        try
                        {
                            contentHost.Content = factory();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Lỗi khi load view: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    };

                    navPanel.Children.Add(btn);
                    if (first == null) first = btn;
                }

                // select first available
                first?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi khởi tạo giao diện chính: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private UserControl CreateHomeControl()
        {
            var tb = new TextBlock { Text = "Chào mừng đến Quản lý Kho Hàng", FontSize = 18, FontWeight = FontWeights.SemiBold };
            return new UserControl { Content = new StackPanel { Children = { tb } } };
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Services.AuthService.Logout();
                var login = new Views.LoginWindow();
                var r = login.ShowDialog();
                if (r == true && Services.AuthService.CurrentUser != null)
                {
                    // reopen main window with new user
                    var main = new MainWindow(Services.AuthService.CurrentUser);
                    Application.Current.MainWindow = main;
                    main.Show();
                    this.Close();
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi đăng xuất: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
