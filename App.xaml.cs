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
            // Debug logging removed: proceed to open MainWindow if login succeeded.

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
