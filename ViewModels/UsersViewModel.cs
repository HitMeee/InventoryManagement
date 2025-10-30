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
        // Khóa chức năng đăng ký Chủ kho tại giao diện Người dùng: loại bỏ "Chủ kho" khỏi danh sách chọn
        public List<string> Roles { get; } = new List<string> { "Admin", "Nhân viên kho" };
        public ObservableCollection<Warehouse> Warehouses { get; } = new();

    private UserListItem? _selected;
    public UserListItem? Selected { get => _selected; set { _selected = value; OnPropertyChanged(nameof(Selected)); if (value != null) LoadEditingFromSelected(); } }

        private User _editing = new();
        public User Editing { get => _editing; set { _editing = value; OnPropertyChanged(nameof(Editing)); } }

        public string NewPassword { get; set; } = string.Empty;
        public string SelectedRole { get; set; } = "Admin";
    private Warehouse? _selectedWarehouse;
    public Warehouse? SelectedWarehouse { get => _selectedWarehouse; set { _selectedWarehouse = value; OnPropertyChanged(nameof(SelectedWarehouse)); } }
    private bool _isRoleEditable = true;
    public bool IsRoleEditable { get => _isRoleEditable; set { _isRoleEditable = value; OnPropertyChanged(nameof(IsRoleEditable)); } }
    private bool _isSensitiveEditable = true;
    public bool IsSensitiveEditable { get => _isSensitiveEditable; set { _isSensitiveEditable = value; OnPropertyChanged(nameof(IsSensitiveEditable)); } }

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
            // Load warehouses. If current user is not Admin/Owner, only show their assigned warehouses
            Warehouses.Clear();
            var allWh = _warehouseService.GetAll();
            var isAdmin = Services.AuthService.IsAdmin();
            var isOwner = Services.AuthService.IsOwner();
            var currentIds = Services.AuthService.CurrentUserWarehouseIds;
            if (isAdmin)
            {
                foreach (var w in allWh) Warehouses.Add(w);
            }
            else if (isOwner)
            {
                var ownerId = Services.AuthService.CurrentUser?.Id ?? -1;
                foreach (var w in allWh.Where(w => w.OwnerId == ownerId)) Warehouses.Add(w);
            }
            else
            {
                foreach (var w in allWh.Where(w => currentIds.Contains(w.Id))) Warehouses.Add(w);
            }
            if (Warehouses.Count > 0) SelectedWarehouse = Warehouses[0];

            Users.Clear();
            foreach (var t in _service.GetAllWithDetails(isAdmin, currentIds))
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
                // Không cho phép đổi sang vai trò "Chủ kho" tại màn Người dùng.
                // Nếu user hiện tại là Chủ kho, giữ nguyên vai trò (cho phép đổi kho/mật khẩu/tên).
                var roleForUpdate = Selected.RoleDisplay;
                if (!string.Equals(Selected.RoleDisplay, "Chủ kho", StringComparison.OrdinalIgnoreCase))
                {
                    roleForUpdate = SelectedRole;
                }
                // Không cho phép thay đổi tài khoản/mật khẩu của người dùng khác
                var currentId = Services.AuthService.CurrentUser?.Id ?? -1;
                string? newUsername = (Selected.Id == currentId) ? (Editing.Username?.Trim()) : null;
                string? newPasswordHash = (Selected.Id == currentId) ? hash : null;
                _service.UpdateUserAndMapping(Selected.Id, newUsername, newPasswordHash, roleForUpdate, SelectedWarehouse?.Id);
                // refresh item
                if (Selected.Id == currentId)
                {
                    Selected.Username = Editing.Username;
                }
                Selected.RoleDisplay = roleForUpdate;
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
            IsRoleEditable = !string.Equals(Selected.RoleDisplay, "Chủ kho", StringComparison.OrdinalIgnoreCase);
            // Chỉ cho phép sửa username/password nếu đang sửa chính tài khoản của mình hoặc đang thêm mới
            var currentId = Services.AuthService.CurrentUser?.Id ?? -1;
            IsSensitiveEditable = (Selected.Id == currentId);
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
