using System.Windows.Controls;

namespace InventoryManagement.Views
{
    public partial class UsersView : UserControl
    {
        public UsersView()
        {
            InitializeComponent();
        }

        // Password handling is done in ViewModel via event wiring in XAML code-behind if needed
        private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is InventoryManagement.ViewModels.UsersViewModel vm && sender is PasswordBox pb)
            {
                vm.NewPassword = pb.Password;
            }
        }
    }
}
