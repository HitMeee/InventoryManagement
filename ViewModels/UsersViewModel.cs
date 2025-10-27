using InventoryManagement.Commands;
using InventoryManagement.Models;
using InventoryManagement.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace InventoryManagement.ViewModels
{
    public class UsersViewModel : ViewModelBase
    {
        private readonly UserService _service = new();
        public ObservableCollection<User> Users { get; } = new();
    public List<string> Roles { get; } = new List<string> { "Admin", "Kho", "BanHang" };

        private User? _selected;
        public User? Selected { get => _selected; set { _selected = value; OnPropertyChanged(nameof(Selected)); } }

        private User _editing = new();
        public User Editing { get => _editing; set { _editing = value; OnPropertyChanged(nameof(Editing)); } }

        public string NewPassword { get; set; } = string.Empty;
    public string SelectedRole { get; set; } = "Admin";

        public ICommand LoadCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand DeleteCommand { get; }

        public UsersViewModel()
        {
            LoadCommand = new RelayCommand(_ => Load());
            AddCommand = new RelayCommand(_ => Add(), _ => !string.IsNullOrWhiteSpace(Editing.Username));
            DeleteCommand = new RelayCommand(_ => Delete(), _ => Selected != null);
            Load();
        }

        public void Load()
        {
            Users.Clear();
            foreach (var u in _service.GetAll()) Users.Add(u);
        }

        public void Add()
        {
            try
            {
                Editing.Role = SelectedRole;
                Editing.PasswordHash = NewPassword; // in real app hash this
                var added = _service.Add(Editing);
                Users.Add(added);
                Editing = new User();
                NewPassword = string.Empty;
            }
            catch (System.Exception ex) { MessageBox.Show($"Add user error: {ex.Message}"); }
        }

        public void Delete()
        {
            if (Selected == null) return;
            if (MessageBox.Show($"Delete {Selected.Username}?","Confirm",MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try { _service.Delete(Selected.Id); Users.Remove(Selected); Selected = null; }
            catch (System.Exception ex) { MessageBox.Show($"Delete error: {ex.Message}"); }
        }
    }
}
