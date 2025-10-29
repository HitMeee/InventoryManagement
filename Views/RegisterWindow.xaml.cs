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

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Vui lòng nhập tên đăng nhập và mật khẩu.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (wh == null)
            {
                MessageBox.Show("Vui lòng chọn kho hàng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                // Không cho phép tạo thêm tài khoản Admin nếu đã tồn tại bất kỳ Admin nào
                var anyAdmin = ctx.UserWarehouseRoles.Any(x => x.Role == "admin");
                if (anyAdmin)
                {
                    MessageBox.Show("Đã tồn tại tài khoản Admin. Không thể tạo thêm tài khoản Admin mới.", "Không thể tạo Admin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var hashed = InventoryManagement.Services.PasswordHelper.HashPassword(password);
                var user = new User { Username = username, PasswordHash = hashed };
                ctx.Users.Add(user);
                ctx.SaveChanges();

                // Gán vai trò admin cho kho đã chọn
                var map = new UserWarehouseRole
                {
                    UserId = user.Id,
                    WarehouseId = wh.Id,
                    Role = "admin",
                    CreatedAt = System.DateTime.UtcNow
                };
                ctx.UserWarehouseRoles.Add(map);
                ctx.SaveChanges();

                MessageBox.Show("Đăng ký Admin thành công. Vui lòng đăng nhập lại.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu người dùng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
