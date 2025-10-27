using InventoryManagement.Commands;
using InventoryManagement.Models;
using InventoryManagement.Services;
using System;
using System.Windows;
using System.Windows.Input;

namespace InventoryManagement.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public List<string> Roles { get; } = new List<string> { "Admin", "Nhân viên kho", "Nhân viên bán hàng" };
        public string SelectedRole { get; set; } = "Admin";

        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }
    public ICommand ShowUsersCommand { get; }

        public event Action<User?>? LoginSucceeded;

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(_ => Login());
            CancelCommand = new RelayCommand(_ => Application.Current.Shutdown());
            ShowUsersCommand = new RelayCommand(_ => ShowUsers());
        }

        private void Login()
        {
            try
            {
                var result = AuthService.Authenticate(Username?.Trim() ?? string.Empty, Password ?? string.Empty, SelectedRole);
                switch (result)
                {
                    case AuthService.AuthResult.Success:
                        LoginSucceeded?.Invoke(AuthService.CurrentUser);
                        break;
                    case AuthService.AuthResult.UserNotFound:
                        MessageBox.Show("Tên đăng nhập không tồn tại.", "Đăng nhập", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    case AuthService.AuthResult.WrongPassword:
                        MessageBox.Show("Mật khẩu không đúng.", "Đăng nhập", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    case AuthService.AuthResult.WrongRole:
                        MessageBox.Show("Vai trò không đúng. Vui lòng chọn vai trò chính xác.", "Đăng nhập", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    default:
                        MessageBox.Show("Lỗi không xác định khi xác thực.", "Đăng nhập", MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi đăng nhập: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowUsers()
        {
            try
            {
                using var ctx = new Data.AppDbContext();
                var users = ctx.Users.Select(u => new { u.Username, u.PasswordHash, u.Role }).ToList();
                if (!users.Any())
                {
                    MessageBox.Show("Chưa có user nào trong CSDL.", "DBG Users", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                var sb = new System.Text.StringBuilder();
                foreach (var u in users)
                {
                    sb.AppendLine($"{u.Username} \t {u.PasswordHash} \t {u.Role}");
                }
                MessageBox.Show(sb.ToString(), "DBG Users", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi khi đọc Users: {ex.Message}", "DBG Users", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
