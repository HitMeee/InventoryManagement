# Phân công dự án WPF Quản lý Kho Hàng

## 1. Dev F — Frontend (UI/UX)
**Nhiệm vụ chính:**
- [ ] Thiết kế tất cả giao diện: Login, MainWindow, navbar trái, ContentHost, logout
- [ ] Style & theme app (màu xanh/xám, bo góc, icons, tooltip, hover, highlight)
- [ ] Bind ViewModels vào Views (MVVM), DataGrid đẹp có search/sort
- [ ] Auto-refresh UI khi dữ liệu thay đổi

**File chính:**
- `Views/*.xaml`, `Views/*Window.xaml`
- `Themes/Styles.xaml`, `App.xaml`
- `MainWindow.xaml(.cs)`, `LoginView.xaml(.cs)`

**Acceptance Criteria:**
- Giao diện responsive, màu sắc hài hòa
- DataGrid có search, sort, tooltip hoạt động
- Navbar hiển thị đúng menu theo role
- Auto-refresh UI khi dữ liệu thay đổi

---

## 2. Dev B1 — Backend Core & Auth
**Nhiệm vụ chính:**
- [ ] Chuẩn hóa DB (`AppDbContext`) và seed 3 tài khoản mẫu
- [ ] Xây dựng `AuthService`: Login, Logout, CurrentUser, hash mật khẩu
- [ ] Unit test cho `AuthService` & `DbInitializer`

**File chính:**
- `Data/AppDbContext.cs`, `Data/DbInitializer.cs`
- `Services/AuthService.cs`, `Models/User.cs`

**Acceptance Criteria:**
- Đăng nhập hoạt động với 3 role (Admin, Nhân viên kho, Nhân viên bán hàng)
- Mật khẩu lưu dưới dạng hash
- API AuthService rõ ràng cho Frontend sử dụng

---

## 3. Dev B2 — Backend Business
**Nhiệm vụ chính:**
- [ ] Quản lý nghiệp vụ: Products, Inventory (nhập/xuất, tồn kho, low-stock), Orders, Reports
- [ ] CRUD, validation, transaction, auto-refresh qua events/INotify
- [ ] Xuất báo cáo CSV/XLSX

**File chính:**
- `Services/ProductService.cs`, `InventoryService.cs`, `OrderService.cs`, `ReportService.cs`
- `Models/Product.cs`, `InventoryItem.cs`, `Order*.cs`

**Acceptance Criteria:**
- CRUD hoạt động, transactions đúng
- Reports xuất CSV/XLSX được
- UI cập nhật khi dữ liệu thay đổi

---

## Quy ước làm việc
- Branch: `feature/<task>` (ví dụ `feature/login-ui`, `feature/auth-hash`)
- PR nhỏ kèm mô tả + cách test + ảnh chụp
- Merge khi có 1 review và build pass
- Daily check-in 15’ để đồng bộ
