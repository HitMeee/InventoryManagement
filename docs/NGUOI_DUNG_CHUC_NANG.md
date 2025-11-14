# Phân tích chức năng "Người dùng"

Tài liệu này mô tả toàn bộ luồng, cấu trúc mã nguồn và các điểm kiểm soát liên quan đến chức năng "Người dùng" trong dự án. Bao gồm: mô hình dữ liệu, dịch vụ, view model, giao diện, dialog tạo người dùng, bảo mật (hash mật khẩu), phạm vi kho hàng (scope) và các quy tắc phân quyền nghiệp vụ.

---
## 1. Mô hình dữ liệu liên quan
### 1.1 `Models/User.cs`
```csharp
[Table("users")]
public class User
{
    [Key]
    [Column("id")] public int Id { get; set; }
    [Column("username")] public string Username { get; set; } = string.Empty;
    [Column("password")] public string PasswordHash { get; set; } = string.Empty;
    [NotMapped] public string Role { get; set; } = string.Empty; // Role hiển thị suy ra từ mapping
    public List<UserWarehouseRole>? UserWarehouseRoles { get; set; }
    [NotMapped] public string? PlainPassword { get; set; } // Chỉ dùng tạm khi tạo/sửa
}
```
Giải thích:
- Bảng `users` lưu thông tin đăng nhập (username/password). Password lưu dạng hash (xử lý bởi `PasswordHelper`).
- Thuộc tính `Role` không được lưu trực tiếp mà suy ra từ bảng `user_warehouse_roles` (owner/admin/staff) trong bước đăng nhập (`AuthService`).

### 1.2 `Models/UserWarehouseRole.cs`
```csharp
[Table("user_warehouse_roles")]
public class UserWarehouseRole
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("user_id")] public int UserId { get; set; }
    public User? User { get; set; }
    [Column("warehouse_id")] public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    [Column("role")] public string Role { get; set; } = string.Empty; // 'owner' | 'admin' | 'staff'
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```
Giải thích:
- Mỗi mapping xác định vai trò của user tại một kho cụ thể.
- Dự án quy ước: hiển thị "Chủ kho" nếu tồn tại bản ghi role="owner"; hiển thị "Admin" nếu có role="admin"; nếu chỉ có role="staff" thì là "Nhân viên kho".

---
## 2. Xác thực & gán vai trò (Authentication & Role Resolution)
### 2.1 `Services/AuthService.cs`
```csharp
public static AuthResult Authenticate(string username, string password, string? role = null, string? connectionString = null)
{
    // ... lấy user theo username
    var maps = ctx.UserWarehouseRoles.Where(uw => uw.UserId == user.Id).ToList();
    var roles = maps.Select(uw => uw.Role).ToList();
    if (roles.Contains("owner", StringComparer.OrdinalIgnoreCase)) user.Role = "Chủ kho";
    else if (roles.Contains("admin", StringComparer.OrdinalIgnoreCase)) user.Role = "Admin";
    else if (roles.Any()) user.Role = "Nhân viên kho"; else user.Role = "";

    CurrentUser = user;
    CurrentUserWarehouseIds = maps.Select(m => m.WarehouseId).Distinct().ToList();
    return AuthResult.Success;
}
```
Giải thích:
- Sau khi xác thực mật khẩu, hệ thống suy ra Role hiển thị từ danh sách mapping.
- `CurrentUserWarehouseIds` chứa phạm vi kho của user: dùng để lọc dữ liệu và kiểm tra quyền thao tác.

---
## 3. Dịch vụ người dùng (Business Rules)
### 3.1 `Services/UserService.cs`
Các phương thức chính:
- `AddWithRoleAndWarehouse`: Tạo user + mapping role/kho, enforce duy nhất 1 Owner/Admin mỗi kho.
- `UpdateUserAndMapping`: Cập nhật username/password + role + warehouse với kiểm tra quyền và phạm vi.
- `Delete`: Áp dụng các rule bảo vệ (không xóa Chủ kho, chỉ Chủ kho mới xóa Admin, phải chung phạm vi kho).
- `GetAllWithDetails`: Trả về danh sách user kèm thông tin hiển thị (vai trò, danh sách kho) có áp dụng lọc phạm vi.

Đoạn mã tiêu biểu (kiểm tra quyền + phạm vi):
```csharp
private static bool CanCurrentUserManageUsers() => AuthService.IsAdmin() || AuthService.IsOwner();
private static bool CanCurrentUserAccessWarehouse(AppDbContext ctx, int warehouseId)
{
    if (AuthService.IsAdmin()) return AuthService.CurrentUserWarehouseIds.Contains(warehouseId);
    if (AuthService.IsOwner())
    {
        var wh = ctx.Warehouses.AsNoTracking().FirstOrDefault(w => w.Id == warehouseId);
        return wh != null && wh.OwnerId == AuthService.CurrentUser!.Id;
    }
    return AuthService.CurrentUserWarehouseIds.Contains(warehouseId); // staff
}
```
Rule khi xóa:
```csharp
var targetMaps = ctx.UserWarehouseRoles.Where(uw => uw.UserId == id).ToList();
var isAdmin = targetMaps.Any(uw => uw.Role == "admin");
var isOwner = targetMaps.Any(uw => uw.Role == "owner");
if (isOwner) throw new InvalidOperationException("Không thể xoá tài khoản Chủ kho.");
if (isAdmin && !AuthService.IsOwner()) throw new InvalidOperationException("Chỉ Chủ kho mới được xoá tài khoản Admin.");
```
Enforce duy nhất 1 Owner/Admin:
```csharp
if (role == "owner" && ctx.UserWarehouseRoles.Any(x => x.WarehouseId == warehouseId && x.Role == "owner"))
    throw new InvalidOperationException("Mỗi kho chỉ có duy nhất 1 'Chủ kho'.");
if (role == "admin" && ctx.UserWarehouseRoles.Any(x => x.WarehouseId == warehouseId && x.Role == "admin"))
    throw new InvalidOperationException("Mỗi kho chỉ có duy nhất 1 'Admin'.");
```

---
## 4. ViewModel quản lý người dùng
### 4.1 `ViewModels/UsersViewModel.cs`
Nhiệm vụ:
- Tải danh sách kho theo vai trò (Owner: kho thuộc OwnerId, Admin/Staff: kho được gán).
- Ẩn lựa chọn tạo "Chủ kho" (Roles chỉ gồm `Admin`, `Nhân viên kho`).
- Cung cấp các `ICommand`: `AddCommand`, `UpdateCommand`, `DeleteCommand` ràng buộc vào điều kiện hợp lệ.
- Giới hạn sửa username/password chỉ cho chính tài khoản đang đăng nhập.

Đoạn mã chọn phạm vi kho:
```csharp
var allWh = _warehouseService.GetAll();
if (AuthService.IsOwner()) { /* kho có OwnerId == current */ }
else { /* Admin/Staff: kho trong CurrentUserWarehouseIds */ }
```
Chặn đổi role thành "Chủ kho":
```csharp
if (!string.Equals(Selected.RoleDisplay, "Chủ kho", StringComparison.OrdinalIgnoreCase))
{
    roleForUpdate = SelectedRole;
}
```
Chỉ sửa nhạy cảm (username/password) nếu đang sửa chính mình:
```csharp
IsSensitiveEditable = (Selected.Id == currentId);
```

---
## 5. View hiển thị danh sách người dùng
### 5.1 `Views/UsersView.xaml`
- DataContext: `UsersViewModel`.
- DataGrid hiển thị các cột: Tên tài khoản, Chức vụ, Kho đang làm việc.
- Panel form bên phải cho phép nhập thông tin và thực thi Add/Update/Delete.

### 5.2 `Views/UsersView.xaml.cs`
- Quản lý ô tìm kiếm (placeholder + filter).
- Nút thêm (`BtnAdd_Click`) mở dialog tạo user (`UserFormDialog`).
```csharp
var dialog = new UserFormDialog(roles, warehouses)
{
    Owner = Window.GetWindow(this)
};
if (dialog.ShowDialog() == true)
{
    var hash = PasswordHelper.HashPassword(dialog.Password);
    var service = new UserService();
    service.AddWithRoleAndWarehouse(dialog.Username, hash, dialog.SelectedRole, dialog.WarehouseId);
    vm.Load();
}
```

---
## 6. Dialog tạo người dùng
### 6.1 `Views/UserFormDialog.xaml` & `.cs`
Chức năng:
- Thu thập Username / Password / Role / Warehouse.
- Validate dữ liệu trước khi trả về `DialogResult=true`.
```csharp
if (string.IsNullOrWhiteSpace(uname)) { MessageBox.Show("Vui lòng nhập Tên tài khoản"); return; }
// ... tương tự cho password, role, warehouse
Username = uname; Password = pwd; SelectedRole = role; WarehouseId = wId;
DialogResult = true;
```

---
## 7. Hash mật khẩu
### 7.1 `Services/PasswordHelper.cs` (gián tiếp sử dụng)
- ViewModel / Dialog gọi `PasswordHelper.HashPassword` trước khi lưu.
- Khi đăng nhập: `AuthService` gọi `PasswordHelper.VerifyPassword` nếu định dạng hash chuẩn (3 phần).

Cơ chế:
- Giúp tránh lưu plaintext; `User.PlainPassword` chỉ dùng tạm thời và không lưu DB.

---
## 8. Phân quyền ở tầng giao diện (Navigation)
### 8.1 `MainWindow.xaml.cs`
Menu "Người dùng" chỉ xuất hiện nếu role có feature `ManageUsers` (định nghĩa trong `RolePermissionService.Features`).
```csharp
new MenuEntry("users","Người dùng", RolePermissionService.Features.ManageUsers, () => new Views.UsersView())
```

---
## 9. Dòng chảy tạo/cập nhật/xóa người dùng
1. Người dùng có quyền (Owner/Admin) mở màn hình "Người dùng" từ menu.
2. ViewModel tải danh sách user giới hạn theo phạm vi kho chung (`GetAllWithDetails` + scope filter).
3. Thêm user: mở dialog → nhập thông tin → hash mật khẩu → gọi `UserService.AddWithRoleAndWarehouse` → enforce rule duy nhất Owner/Admin.
4. Cập nhật user: ViewModel tính toán role hợp lệ, chặn đổi sang "Chủ kho" nếu không phải chính Chủ kho; chặn sửa tài khoản khác.
5. Xóa user: Gọi `UserService.Delete` → kiểm tra rule (không xóa Chủ kho, chỉ Chủ kho xóa Admin, phải chung warehouse scope).

---
## 10. Kiểm soát phạm vi hiển thị người dùng
- Hàm `GetAllWithDetails` trong `UserService` chỉ trả về user nếu có ít nhất một warehouse chung với `CurrentUserWarehouseIds`.
```csharp
var shared = umaps.Any(m => scopeIds.Contains(m.WarehouseId));
if (!shared) continue; // bỏ qua user ngoài phạm vi
```

---
## 11. Các điểm cải tiến tiềm năng
| Chủ đề | Gợi ý |
|--------|-------|
| Phân quyền chi tiết | Tách rule ra lớp chuyên trách (Policy/AuthorizationService). |
| Logging bảo mật | Log thao tác Add/Update/Delete user kèm user thực hiện. |
| Lịch sử thay đổi | Thêm bảng audit (user_id, action, old_values, new_values, timestamp). |
| Nâng cấp vai trò | Cho phép gán nhiều warehouse với nhiều vai trò cùng lúc (giữ danh sách mapping thay vì 1). |
| Rate limiting | Giới hạn số lần thử mật khẩu sai để tránh brute force. |
| MFA | Bổ sung xác thực hai lớp cho tài khoản Owner/Admin. |

---
## 12. Tóm tắt kiến trúc chức năng "Người dùng"
| Lớp/Tầng | Vai trò |
|----------|---------|
| Model (`User`, `UserWarehouseRole`) | Lưu dữ liệu & sơ đồ quan hệ User–Warehouse–Role. |
| Auth (`AuthService`) | Xác thực, suy luận Role hiển thị & phạm vi kho. |
| Business (`UserService`) | Áp dụng rule nghiệp vụ, kiểm soát quyền thao tác, scope, uniqueness Owner/Admin. |
| ViewModel (`UsersViewModel`) | Chuẩn bị dữ liệu, xử lý điều kiện hiển thị và tương tác UI. |
| View (`UsersView.xaml`) | Trình bày danh sách và form CRUD. |
| Dialog (`UserFormDialog`) | Thu thập input tạo user. |
| Helper (`PasswordHelper`) | Hash & verify mật khẩu an toàn. |

---
## 13. Sơ đồ luồng tóm tắt
```
[Login] -> AuthService.Authenticate -> (User, WarehouseIds, RoleDisplay)
    -> UsersViewModel.Load -> UserService.GetAllWithDetails(scope)
        -> UsersView (DataGrid Bind)
            -> Add (Dialog) -> UserService.AddWithRoleAndWarehouse (rules + mapping) -> Reload
            -> Update -> UserService.UpdateUserAndMapping (rules + uniqueness) -> Reload
            -> Delete -> UserService.Delete (checks) -> Reload
```

---
## 14. Checklist nhanh khi xem lại logic
- [x] Hash mật khẩu khi thêm / đổi mật khẩu.
- [x] Không cho staff quản lý user (chặn ở business layer).
- [x] Không thể tạo 2 Owner hoặc 2 Admin cùng một kho.
- [x] Không xóa được Chủ kho.
- [x] Chỉ Chủ kho xóa được Admin.
- [x] Chỉ thấy user có chung warehouse scope.
- [x] Không chỉnh sửa username/password của người khác.

---
## 15. Đề xuất test tự động (ý tưởng)
| Test Case | Mô tả |
|-----------|-------|
| Add Admin duplicate | Tạo Admin thứ hai cùng kho → phải lỗi. |
| Add Owner duplicate | Tạo Owner thứ hai cùng kho → phải lỗi. |
| Delete Owner | Thử xóa user Owner → phải lỗi. |
| Delete Admin by Admin | Admin tự xóa Admin khác → phải lỗi. |
| Delete Admin by Owner | Owner xóa Admin → thành công. |
| Scope filter | User không chung kho không xuất hiện trong `GetAllWithDetails`. |
| Update other user credentials | Thử đổi password của user khác → phải lỗi hoặc bị bỏ qua. |

---
## 16. Kết luận
Chức năng "Người dùng" được phân tách rõ ràng:
- Dữ liệu: bảng `users` + mapping `user_warehouse_roles`.
- Xác thực & phạm vi: `AuthService`.
- Luật nghiệp vụ chặt chẽ: `UserService` bảo vệ mọi thao tác.
- UI và ViewModel tuân thủ role & scope và chỉ hiển thị/cho phép thao tác hợp lệ.

> Tài liệu này giúp nhanh chóng định vị, hiểu và mở rộng chức năng "Người dùng" một cách an toàn.
