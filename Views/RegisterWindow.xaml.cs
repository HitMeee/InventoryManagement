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
            var roleItem = CmbRole.SelectedItem as System.Windows.Controls.ComboBoxItem;
            var role = roleItem?.Content?.ToString() ?? "Nhân viên bán hàng";

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Vui lòng nhập tên đăng nhập và mật khẩu.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                var hashed = InventoryManagement.Services.PasswordHelper.HashPassword(password);
                var user = new User { Username = username, PasswordHash = hashed };
                ctx.Users.Add(user);
                ctx.SaveChanges();

                // Optionally map the selected role to user_warehouse_roles.
                // Map UI role labels to stored role values: Admin -> 'admin', others -> 'staff'
                var mapped = (role?.ToLowerInvariant().StartsWith("admin") ?? false) ? "admin" : "staff";
                // If a warehouse selection control exists in the UI, use it; otherwise, assign no warehouse role now.
                // For safety we won't create a user_warehouse_roles without a valid warehouse id.

                MessageBox.Show("Đăng ký thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
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
