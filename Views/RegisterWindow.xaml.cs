using System.Linq;
using System.Windows;
using InventoryManagement.Data;
using InventoryManagement.Models;

namespace InventoryManagement.Views
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
            this.Loaded += RegisterWindow_Loaded;
        }

        private void RegisterWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using var ctx = new AppDbContext();
                var warehouses = ctx.Warehouses.OrderBy(w => w.Name).ToList();
                CmbWarehouse.ItemsSource = warehouses;
                if (warehouses.Count > 0) CmbWarehouse.SelectedIndex = 0;

                // Default UI state: if role = Chủ kho, hide warehouse selector
                UpdateWarehouseVisibility();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách kho: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var username = TxtUsername.Text?.Trim() ?? string.Empty;
            var password = Pwd.Password ?? string.Empty;
            var wh = CmbWarehouse.SelectedItem as Warehouse;
            var roleItem = CmbRole.SelectedItem as System.Windows.Controls.ComboBoxItem;
            var roleDisplay = roleItem?.Content?.ToString() ?? "Admin"; // default Admin

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Vui lòng nhập tên đăng nhập và mật khẩu.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // For Admin: require selected warehouse. For Chủ kho: warehouse will be created after account created.
            if (string.Equals(roleDisplay, "Admin", System.StringComparison.OrdinalIgnoreCase) && wh == null)
            {
                MessageBox.Show("Vui lòng chọn kho hàng (vai trò Admin).", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var ctx = new AppDbContext();
                var exists = ctx.Users.Any(u => u.Username == username);
                if (exists)
                {
                    MessageBox.Show("Tên đăng nhập đã tồn tại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.Equals(roleDisplay, "Admin", System.StringComparison.OrdinalIgnoreCase))
                {
                    // Admin: chỉ cho phép 1 Admin mỗi kho
                    if (wh == null)
                    {
                        MessageBox.Show("Vui lòng chọn kho hàng (vai trò Admin).", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    var existsAdminInWarehouse = ctx.UserWarehouseRoles.Any(x => x.WarehouseId == wh.Id && x.Role == "admin");
                    if (existsAdminInWarehouse)
                    {
                        MessageBox.Show("Kho này đã có tài khoản Admin.", "Không thể tạo Admin", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                var hashed = InventoryManagement.Services.PasswordHelper.HashPassword(password);
                var user = new User { Username = username, PasswordHash = hashed };
                ctx.Users.Add(user);
                ctx.SaveChanges();

                if (string.Equals(roleDisplay, "Chủ kho", System.StringComparison.OrdinalIgnoreCase))
                {
                    // Chủ kho: tạo kho mới ngay sau khi tạo tài khoản, gán owner_id và mapping owner
                    var dialog = new WarehouseFormDialog();
                    dialog.Owner = this;
                    var ok = dialog.ShowDialog();
                    if (ok == true)
                    {
                        var whNew = new Warehouse { Name = dialog.WarehouseName, Address = dialog.WarehouseAddress, CreatedAt = System.DateTime.UtcNow, OwnerId = user.Id };
                        ctx.Warehouses.Add(whNew);
                        ctx.SaveChanges();

                        var mapOwner = new UserWarehouseRole { UserId = user.Id, WarehouseId = whNew.Id, Role = "owner", CreatedAt = System.DateTime.UtcNow };
                        ctx.UserWarehouseRoles.Add(mapOwner);
                        ctx.SaveChanges();

                        MessageBox.Show("Đăng ký Chủ kho thành công và đã tạo kho sở hữu.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.DialogResult = true;
                        this.Close();
                        return;
                    }
                    else
                    {
                        MessageBox.Show("Bạn cần tạo kho cho tài khoản Chủ kho để hoàn tất đăng ký.", "Yêu cầu", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                else
                {
                    // Admin: gán vào kho đã chọn
                    var map = new UserWarehouseRole
                    {
                        UserId = user.Id,
                        WarehouseId = wh!.Id,
                        Role = "admin",
                        CreatedAt = System.DateTime.UtcNow
                    };
                    ctx.UserWarehouseRoles.Add(map);
                    ctx.SaveChanges();

                    MessageBox.Show("Đăng ký Admin thành công. Vui lòng đăng nhập lại.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu người dùng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CmbRole_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateWarehouseVisibility();
        }

        private void UpdateWarehouseVisibility()
        {
            var roleItem = CmbRole.SelectedItem as System.Windows.Controls.ComboBoxItem;
            var roleDisplay = roleItem?.Content?.ToString() ?? "Chủ kho";
            // Hide warehouse selector for Chủ kho; show for Admin
            if (string.Equals(roleDisplay, "Chủ kho", System.StringComparison.OrdinalIgnoreCase))
            {
                WarehouseContainer.Visibility = Visibility.Collapsed;
            }
            else
            {
                WarehouseContainer.Visibility = Visibility.Visible;
            }
        }
    }
}
