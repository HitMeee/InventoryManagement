# Tài liệu phân quyền người dùng

Tài liệu này liệt kê CHÍNH XÁC vị trí mã nguồn đang thực hiện phân quyền (role/feature) và ràng buộc phạm vi (scope) trong ứng dụng. Mỗi mục ghi rõ file, đoạn mã tiêu biểu, và cách hoạt động.

> Các vai trò hiện có: "Chủ kho", "Admin", "Nhân viên kho", "Nhân viên bán hàng". Hệ thống gán quyền theo "feature" và kiểm tra ở cấp menu + cấp màn hình + cấp dịch vụ.

## 1) Bản đồ Vai trò → Chức năng (Role → Features)

- File: `Services/RolePermissionService.cs`
- Mục đích: Khai báo danh sách feature và ánh xạ role → các feature được phép. Hàm trung tâm `HasPermission(role, feature)` dùng ở UI để ẩn/hiện menu.

Đoạn mã tiêu biểu:

```csharp
public static class RolePermissionService
{
    public static class Features
    {
        public const string ManageUsers = "ManageUsers";
        public const string ManageProducts = "ManageProducts";
        public const string ManageSuppliers = "ManageSuppliers";
        public const string ViewStock = "ViewStock";
        public const string ExportReports = "ExportReports";
        public const string ViewAnalyticsDashboard = "ViewAnalyticsDashboard";
        // ...
    }

    private static readonly Dictionary<string, HashSet<string>> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Chủ kho"] = new HashSet<string>
        {
            Features.ManageSuppliers,
            Features.ManageUsers,
            Features.ManageProducts,
            Features.ViewStock,
            Features.ExportReports,
            Features.ViewAnalyticsDashboard,
            // ...
        },
        ["Admin"] = new HashSet<string>
        {
            Features.ManageSuppliers,
            Features.ManageUsers,
            Features.ManageProducts,
            Features.ViewStock,
            Features.ExportReports,
            Features.ViewAnalyticsDashboard,
            // ...
        },
        ["Nhân viên kho"] = new HashSet<string>
        {
            Features.ManageProducts,
            Features.ViewStock,
            Features.ExportReports,
            Features.ViewAnalyticsDashboard,
            // ...
        },
        ["Nhân viên bán hàng"] = new HashSet<string>
        {
            Features.ManageCustomers,
            Features.ViewStock,
            Features.ExportReports,
            Features.ViewAnalyticsDashboard,
            // ...
        }
    };

    public static bool HasPermission(string? role, string feature)
        => !string.IsNullOrWhiteSpace(role)
           && !string.IsNullOrWhiteSpace(feature)
           && _map.TryGetValue(role.Trim(), out var set)
           && set.Contains(feature);
}
```

Cách hoạt động:
- UI gọi `HasPermission(role, feature)` để quyết định có hiển thị mục menu/chức năng hay không.
- Danh sách Feature là "cờ" cấp chức năng (sẽ được các View/Window tham chiếu).

## 2) Xác định vai trò đăng nhập và phạm vi kho (Auth & Scope)

- File: `Services/AuthService.cs`
- Mục đích: Xác thực, xác định Role hiển thị (Chủ kho/Admin/Nhân viên kho) từ bảng `user_warehouse_roles` và danh sách kho trong phạm vi người dùng (`CurrentUserWarehouseIds`).

Đoạn mã tiêu biểu:

```csharp
public static class AuthService
{
    public static User? CurrentUser { get; private set; }
    public static List<int> CurrentUserWarehouseIds { get; private set; } = new();

    public static AuthResult Authenticate(string username, string password, string? role = null, string? connectionString = null)
    {
        // ... đọc user, kiểm mật khẩu
        var maps = ctx.UserWarehouseRoles.Where(uw => uw.UserId == user.Id).ToList();
        var roles = maps.Select(uw => uw.Role).ToList();
        if (roles.Contains("owner", StringComparer.OrdinalIgnoreCase)) user.Role = "Chủ kho";
        else if (roles.Contains("admin", StringComparer.OrdinalIgnoreCase)) user.Role = "Admin";
        else if (roles.Any()) user.Role = "Nhân viên kho"; else user.Role = "";

        CurrentUser = user;
        CurrentUserWarehouseIds = maps.Select(m => m.WarehouseId).Distinct().ToList();
        return AuthResult.Success;
    }

    public static bool IsAdmin() => string.Equals(CurrentUser?.Role, "Admin", StringComparison.OrdinalIgnoreCase);
    public static bool IsOwner() => string.Equals(CurrentUser?.Role, "Chủ kho", StringComparison.OrdinalIgnoreCase);
}
```

Cách hoạt động:
- Sau khi đăng nhập, `CurrentUser` + `CurrentUserWarehouseIds` được set. Toàn bộ View/Service dùng 2 giá trị này để lọc dữ liệu theo phạm vi.

## 3) Ẩn/hiện menu theo quyền (Navigation gating)

- File: `MainWindow.xaml.cs`
- Mục đích: Tạo cây menu theo danh sách `MenuEntry`, chỉ add vào UI những entry mà `HasPermission` trả về true. Đồng thời ẩn riêng mục "Kho hàng" cho "Nhân viên kho".

Đoạn mã tiêu biểu:

```csharp
var menu = new List<MenuEntry>
{
    new MenuEntry("home","Trang chủ", RolePermissionService.Features.ViewStock, () => CreateDashboard()),
    new MenuEntry("products","Sản phẩm", RolePermissionService.Features.ManageProducts, () => new Views.ProductsView()),
    new MenuEntry("suppliers","Nhà cung cấp", RolePermissionService.Features.ManageSuppliers, () => new Views.SuppliersView()),
    new MenuEntry("reports","Báo cáo", RolePermissionService.Features.ViewAnalyticsDashboard, () => new Views.ReportsView()),
    new MenuEntry("warehouses","Kho hàng", RolePermissionService.Features.ViewStock, () => new Views.WarehousesView()),
    new MenuEntry("users","Người dùng", RolePermissionService.Features.ManageUsers, () => new Views.UsersView())
};

foreach (var item in menu)
{
    if (!RolePermissionService.HasPermission(user.Role, item.Feature)) continue;
    // Ẩn riêng mục Kho hàng đối với Nhân viên kho
    if (item.Key == "warehouses" && string.Equals(user.Role, "Nhân viên kho", StringComparison.OrdinalIgnoreCase)) continue;
    // ... add button vào navPanel
}
```

Cách hoạt động:
- Chỉ các menu có `feature` được role cho phép mới xuất hiện. Staff sẽ không thấy "Nhà cung cấp" (vì thiếu `ManageSuppliers`) và không thấy "Kho hàng" do rule bổ sung.

## 4) Ràng buộc tại màn hình Sản phẩm (UI + Logic)

- File: `Views/ProductsView.xaml.cs`
- Mục đích: Ẩn các nút Thêm/Sửa/Xóa cho Nhân viên; lọc danh sách kho trong ComboBox theo phạm vi; kiểm tra quyền lần cuối trước khi thao tác.

Đoạn mã tiêu biểu (ẩn nút):

```csharp
if (!Services.AuthService.IsOwner() && !Services.AuthService.IsAdmin())
{
    if (BtnAdd != null) BtnAdd.Visibility = Visibility.Collapsed;
    if (BtnEdit != null) BtnEdit.Visibility = Visibility.Collapsed;
    if (BtnDelete != null) BtnDelete.Visibility = Visibility.Collapsed;
}
```

Đoạn mã tiêu biểu (lọc kho theo phạm vi):

```csharp
var q = db.Warehouses.AsQueryable();
if (Services.AuthService.IsOwner())
{
    var ownerId = Services.AuthService.CurrentUser?.Id ?? -1;
    q = q.Where(w => w.OwnerId == ownerId);
}
else // Admin + Staff
{
    var ids = Services.AuthService.CurrentUserWarehouseIds ?? new List<int>();
    q = q.Where(w => ids.Contains(w.Id));
}
```

Đoạn mã tiêu biểu (chặn thao tác nếu không đủ quyền):

```csharp
// BtnAdd_Click / BtnEdit_Click / BtnDelete_Click
if (!Services.AuthService.IsOwner() && !Services.AuthService.IsAdmin())
{
    MessageBox.Show("Bạn không có quyền ...");
    return;
}
```

Cách hoạt động:
- UI thân thiện: ẩn nút với vai trò không đủ quyền. Logic an toàn: thêm điều kiện kiểm tra server-side (code-behind) trước khi ghi DB.

## 5) Màn hình Thêm/Sửa sản phẩm (lọc kho theo scope)

- File: `Views/ProductFormDialog.xaml.cs`
- Mục đích: ComboBox kho chỉ liệt kê kho thuộc phạm vi của người dùng.

Đoạn mã tiêu biểu:

```csharp
var q = db.Warehouses.AsQueryable();
if (Services.AuthService.IsOwner())
{
    var ownerId = Services.AuthService.CurrentUser?.Id ?? -1;
    q = q.Where(w => w.OwnerId == ownerId);
}
else // Admin + Staff
{
    var ids = Services.AuthService.CurrentUserWarehouseIds ?? new List<int>();
    q = q.Where(w => ids.Contains(w.Id));
}
```

## 6) Màn hình Nhà cung cấp (gating qua menu)

- File: `Views/SuppliersView.xaml(.cs)` và menu tại `MainWindow.xaml.cs`
- Mục đích: Chức năng "Nhà cung cấp" chỉ hiển thị khi có `Features.ManageSuppliers`. Trong view, các thao tác CRUD trực tiếp vì giả định chỉ Owner/Admin vào được view này.

Giải thích:
- Không có kiểm tra quyền chi tiết trong `SuppliersView.xaml.cs` vì đã gate ở menu bằng `ManageSuppliers`.

## 7) Màn hình Báo cáo (scope + export)

- File: `Views/ReportsView.xaml(.cs)`
- Mục đích: Gate vào menu bằng `ViewAnalyticsDashboard`. Trong View, danh sách kho lọc theo phạm vi; các tab báo cáo và nút "Xuất Excel" luôn hiện khi đã vào được view.

Đoạn mã tiêu biểu (lọc kho theo scope):

```csharp
var q = db.Warehouses.AsQueryable();
if (Services.AuthService.IsOwner())
{
    var ownerId = Services.AuthService.CurrentUser?.Id ?? -1;
    q = q.Where(w => w.OwnerId == ownerId);
}
else // Admin + Staff
{
    var ids = Services.AuthService.CurrentUserWarehouseIds ?? new List<int>();
    q = q.Where(w => ids.Contains(w.Id));
}
```

Lưu ý:
- Feature `ExportReports` đã được định nghĩa trong `RolePermissionService`, nhưng view hiện không kiểm tra riêng trước khi export. Quyền truy cập view "Báo cáo" phụ thuộc `ViewAnalyticsDashboard` (Staff vẫn có quyền xem và xuất nếu vào được view).

## 8) Ràng buộc phân quyền ở tầng Dịch vụ Người dùng

- File: `Services/UserService.cs`
- Mục đích: Chống lạm dụng ở tầng nghiệp vụ: chỉ Owner/Admin mới được quản lý người dùng; chỉ được thao tác trong phạm vi kho của mình; không được xóa Chủ kho; chỉ Chủ kho mới xóa được Admin; đảm bảo mỗi kho chỉ có 1 Owner/Admin.

Đoạn mã tiêu biểu:

```csharp
private static bool CanCurrentUserManageUsers()
    => Services.AuthService.IsAdmin() || Services.AuthService.IsOwner();

private static bool CanCurrentUserAccessWarehouse(AppDbContext ctx, int warehouseId)
{
    if (Services.AuthService.IsAdmin())
        return Services.AuthService.CurrentUserWarehouseIds.Contains(warehouseId);
    if (Services.AuthService.IsOwner())
    {
        var current = Services.AuthService.CurrentUser;
        var wh = ctx.Warehouses.AsNoTracking().FirstOrDefault(w => w.Id == warehouseId);
        return current != null && wh != null && wh.OwnerId == current.Id;
    }
    // Staff: chỉ kho được gán
    return Services.AuthService.CurrentUserWarehouseIds.Contains(warehouseId);
}
```

Các rule nổi bật khi Xóa/Cập nhật/Thêm:
- Không cho xóa tài khoản "Chủ kho"; chỉ "Chủ kho" mới được xóa tài khoản "Admin".
- Chỉ được thao tác với người dùng cùng phạm vi kho với mình (có share warehouse).
- Khi gán vai trò, đảm bảo mỗi kho chỉ có duy nhất 1 Owner/Admin.

## 9) Dashboard (hiển thị số liệu theo role)

- File: `Views/DashboardView.xaml.cs`
- Mục đích: Chủ kho thấy thống kê phạm vi kho do mình sở hữu; Admin thấy thống kê theo kho được phân công; nhân viên xem danh sách kho mình thuộc về.

Đoạn mã tiêu biểu:

```csharp
var isOwner = AuthService.IsOwner();
var isAdmin = AuthService.IsAdmin();
if (isOwner)
{
    var ownerId = user.Id;
    var ownerWarehouseIds = db.Warehouses.Where(w => w.OwnerId == ownerId).Select(w => w.Id).ToList();
    // ... tính số liệu trong phạm vi này
}
else if (isAdmin)
{
    var assignedIds = AuthService.CurrentUserWarehouseIds ?? new List<int>();
    // ... tính số liệu trong phạm vi assignedIds
}
else
{
    // Nhân viên: ẩn thống kê tổng quan; hiển thị danh sách kho được gán
}
```

---

## Tóm tắt luồng phân quyền
1) Đăng nhập: `AuthService` xác định Role hiển thị + `CurrentUserWarehouseIds`.
2) Tạo menu: `MainWindow` lọc theo `RolePermissionService.HasPermission`, và ẩn thêm mục "Kho hàng" với Nhân viên.
3) Trong View: Ẩn/hiện nút theo role, lọc dữ liệu theo phạm vi kho (`Owner` = theo `OwnerId`; `Admin/Staff` = theo `CurrentUserWarehouseIds`).
4) Tầng Dịch vụ: Bảo vệ nghiệp vụ (quản lý người dùng) với các luật nghiêm ngặt và kiểm tra phạm vi.

## Gợi ý mở rộng (nếu cần)
- Áp dụng kiểm tra `ExportReports` tại `ReportsView` để chỉ cho phép export khi role có cờ này.
- Tách `Features` và cấu hình role-feature ra file JSON để dễ tinh chỉnh mà không build lại.
- Bổ sung unit test cho `HasPermission` và các rule then chốt trong `UserService`.
