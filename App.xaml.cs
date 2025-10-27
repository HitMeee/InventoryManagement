using System.Windows;
using InventoryManagement.Data;

namespace InventoryManagement
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // ensure DB created and seeded
            DbInitializer.Initialize();

            // show login window first (modal). If login succeeds, open MainWindow.
            // Prevent WPF from shutting down when the login dialog (the only window) closes.
            ShutdownMode previousShutdownMode = Application.Current.ShutdownMode;
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var login = new Views.LoginWindow();
            var result = login.ShowDialog();
            // DEBUG: show why we may not open MainWindow
            try
            {
                var cu = Services.AuthService.CurrentUser;
                var cuInfo = cu != null ? $"{cu.Username} ({cu.Role})" : "<null>";
                MessageBox.Show($"Login dialog returned: {result}\nAuthService.CurrentUser: {cuInfo}", "DEBUG Login", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"DEBUG: Exception checking current user: {ex.Message}", "DEBUG Login", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (result == true && Services.AuthService.CurrentUser != null)
            {
                try
                {
                    var main = new MainWindow(Services.AuthService.CurrentUser);
                    // set as application's main window and restore ShutdownMode so closing main closes app
                    Application.Current.MainWindow = main;
                    Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                    main.Show();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Lỗi khi mở MainWindow: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                }
            }
            else
            {
                // user cancelled or login failed; restore previous shutdown mode and shutdown
                Application.Current.ShutdownMode = previousShutdownMode;
                Application.Current.Shutdown();
            }
        }
    }
}
