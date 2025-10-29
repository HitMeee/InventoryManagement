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
        private readonly WarehouseService _warehouseService = new();

        public ObservableCollection<UserListItem> Users { get; } = new();
    public List<string> Roles { get; } = new List<string> { "Chủ kho", "Admin", "Nhân viên kho" };
        public ObservableCollection<Warehouse> Warehouses { get; } = new();

    private UserListItem? _selected;
    public UserListItem? Selected { get => _selected; set { _selected = value; OnPropertyChanged(nameof(Selected)); if (value != null) LoadEditingFromSelected(); } }

        private User _editing = new();
        public User Editing { get => _editing; set { _editing = value; OnPropertyChanged(nameof(Editing)); } }

        public string NewPassword { get; set; } = string.Empty;
        public string SelectedRole { get; set; } = "Admin";
        private Warehouse? _selectedWarehouse;
        public Warehouse? SelectedWarehouse { get => _selectedWarehouse; set { _selectedWarehouse = value; OnPropertyChanged(nameof(SelectedWarehouse)); } }

        public ICommand LoadCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand UpdateCommand { get; }
        public ICommand DeleteCommand { get; }

        public UsersViewModel()
        {
            LoadCommand = new RelayCommand(_ => Load());
            AddCommand = new RelayCommand(_ => Add(), _ => !string.IsNullOrWhiteSpace(Editing.Username) && SelectedWarehouse != null && !string.IsNullOrWhiteSpace(SelectedRole));
            UpdateCommand = new RelayCommand(_ => Update(), _ => Selected != null && !string.IsNullOrWhiteSpace(Editing.Username) && SelectedWarehouse != null && !string.IsNullOrWhiteSpace(SelectedRole));
            DeleteCommand = new RelayCommand(_ => Delete(), _ => Selected != null);
            Load();
        }

        public void Load()
        {
            // Load warehouses
            Warehouses.Clear();
            foreach (var w in _warehouseService.GetAll()) Warehouses.Add(w);
            if (Warehouses.Count > 0) SelectedWarehouse = Warehouses[0];

            Users.Clear();
            foreach (var t in _service.GetAllWithDetails())
            {
                Users.Add(new UserListItem
                {
                    Id = t.user.Id,
                    Username = t.user.Username,
                    RoleDisplay = t.roleDisplay,
                    WarehouseDisplay = t.warehouseDisplay,
                    WarehouseId = t.warehouseId
                });
            }
        }

        public void Add()
        {
            try
            {
                var pwdHash = Services.PasswordHelper.HashPassword(NewPassword ?? string.Empty);
                var added = _service.AddWithRoleAndWarehouse(Editing.Username.Trim(), pwdHash, SelectedRole, SelectedWarehouse!.Id);
                Users.Add(new UserListItem
                {
                    Id = added.Id,
                    Username = added.Username,
                    RoleDisplay = SelectedRole,
                    WarehouseDisplay = SelectedWarehouse!.Name,
                    WarehouseId = SelectedWarehouse!.Id
                });
                Editing = new User();
                NewPassword = string.Empty;
            }
            catch (System.Exception ex) { MessageBox.Show($"Add user error: {ex.Message}"); }
        }

        public void Update()
        {
            if (Selected == null) return;
            try
            {
                string? hash = string.IsNullOrWhiteSpace(NewPassword) ? null : Services.PasswordHelper.HashPassword(NewPassword);
                _service.UpdateUserAndMapping(Selected.Id, Editing.Username?.Trim(), hash, SelectedRole, SelectedWarehouse?.Id);
                // refresh item
                Selected.Username = Editing.Username;
                Selected.RoleDisplay = SelectedRole;
                Selected.WarehouseDisplay = SelectedWarehouse?.Name ?? Selected.WarehouseDisplay;
                Selected.WarehouseId = SelectedWarehouse?.Id;
                OnPropertyChanged(nameof(Users));
                NewPassword = string.Empty;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Update user error: {ex.Message}");
            }
        }

        public void Delete()
        {
            if (Selected == null) return;
            if (MessageBox.Show($"Delete {Selected.Username}?","Confirm",MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try { _service.Delete(Selected.Id); Users.Remove(Selected); Selected = null; }
            catch (System.Exception ex) { MessageBox.Show($"Delete error: {ex.Message}"); }
        }

        private void LoadEditingFromSelected()
        {
            if (Selected == null) return;
            Editing = new User { Id = Selected.Id, Username = Selected.Username };
            SelectedRole = Selected.RoleDisplay ?? "Nhân viên kho";
            if (Selected.WarehouseId.HasValue)
            {
                SelectedWarehouse = Warehouses.FirstOrDefault(w => w.Id == Selected.WarehouseId.Value) ?? Warehouses.FirstOrDefault();
            }
        }
    }

    public class UserListItem
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? RoleDisplay { get; set; }
        public string? WarehouseDisplay { get; set; }
        public int? WarehouseId { get; set; }
    }
}
