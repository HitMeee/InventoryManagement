using System.Windows;
using InventoryManagement.ViewModels;
using InventoryManagement.Services;

namespace InventoryManagement.Views
{
    public partial class LoginWindow : Window
    {
        public string? SelectedRole { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
            Loaded += LoginWindow_Loaded;
        }

        private void LoginWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            // LoginViewControl is the named UserControl in XAML
            if (LoginViewControl != null && LoginViewControl.DataContext is LoginViewModel vm)
            {
                vm.LoginSucceeded += OnLoginSucceeded;
            }
        }

        private void OnLoginSucceeded(Models.User? user)
        {
            // set dialog result true so App.OnStartup can open MainWindow
            this.SelectedRole = user?.Role;
            this.DialogResult = true;
        }
    }
}
