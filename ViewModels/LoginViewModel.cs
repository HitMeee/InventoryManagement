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

        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }
    public ICommand RegisterCommand { get; }

        public event Action<User?>? LoginSucceeded;

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(_ => Login());
            CancelCommand = new RelayCommand(_ => Application.Current.Shutdown());
            RegisterCommand = new RelayCommand(_ => ShowRegister());
        }

        private void ShowRegister()
        {
            Views.LoginWindow? loginWin = null;
            try
            {
                // Tìm đúng LoginWindow hiện có thay vì lấy phần tử đầu tiên ngẫu nhiên
                foreach (Window w in Application.Current.Windows)
                {
                    if (w is Views.LoginWindow lw)
                    {
                        loginWin = lw;
                        break;
                    }
                }
                if (loginWin == null)
                {
                    loginWin = Application.Current.MainWindow as Views.LoginWindow;
                }

                // Ẩn login trong lúc mở đăng ký (không đóng để tránh app shutdown)
                loginWin?.Hide();

                var win = new Views.RegisterWindow
                {
                    Owner = loginWin
                };
                // Đảm bảo modal đúng và căn giữa theo Owner
                win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                win.ShowDialog();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở cửa sổ đăng ký: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Luôn hiển thị lại Login sau khi đóng đăng ký để tránh cảm giác app bị tắt
                if (loginWin != null)
                {
                    try
                    {
                        loginWin.Show();
                        loginWin.Activate();
                    }
                    catch { /* ignore */ }
                }
            }
        }

        private void Login()
        {
            try
            {
                var result = AuthService.Authenticate(Username?.Trim() ?? string.Empty, Password ?? string.Empty);
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
                            try
                            {
                                var log = System.IO.Path.Combine(AppContext.BaseDirectory, "auth_error.log");
                                if (System.IO.File.Exists(log))
                                {
                                    var lines = System.IO.File.ReadAllLines(log);
                                    var tail = string.Join("\n", lines.Length > 20 ? lines[^20..] : lines);
                                    MessageBox.Show($"Lỗi không xác định khi xác thực.\n\nChi tiết (cuối file log):\n{tail}", "Đăng nhập", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                                else
                                {
                                    MessageBox.Show("Lỗi không xác định khi xác thực.", "Đăng nhập", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            catch
                            {
                                MessageBox.Show("Lỗi không xác định khi xác thực.", "Đăng nhập", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi đăng nhập: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
