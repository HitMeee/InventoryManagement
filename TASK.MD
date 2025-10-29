# Phân công dự án WPF Quản lý Kho Hàng (Full-stack theo nhóm chức năng)

Mỗi Dev chịu trách nhiệm **toàn bộ (frontend + backend)** cho nhóm chức năng được giao, đảm bảo MVVM hoạt động đầy đủ, UI đẹp, logic chạy đúng và dữ liệu được lưu/đọc từ SQLite.

---

## 1. Dev 1 — Quản lý người dùng & Đăng nhập (Auth + User Management)
**Nhiệm vụ chính:**
- [x] Thiết kế giao diện `LoginView`, `UserManagementView`
- [ ] Xây dựng `AuthService`: Login, Logout, CurrentUser, hash mật khẩu
- [ ] CRUD người dùng (thêm/sửa/xóa tài khoản)
- [ ] Phân quyền: Admin, Nhân viên kho, Nhân viên bán hàng
- [ ] Giao diện hiển thị menu và quyền theo role

**File chính:**
- `Views/LoginView.xaml`, `Views/UsersView.xaml`
- `Services/AuthService.cs`, `Data/DbInitializer.cs`
- `Models/User.cs`, `ViewModels/LoginViewModel.cs`, `UsersViewModel.cs`

**Acceptance Criteria:**
- Đăng nhập hoạt động với 3 role
- Hiển thị menu đúng role sau khi login
- CRUD user hoạt động, mật khẩu hash trong DB
- UI gọn gàng, validation hợp lý

---

## 2. Dev 2 — Quản lý sản phẩm & kho hàng (Products + Inventory)
**Nhiệm vụ chính:**
- [ ] Thiết kế `ProductsView`, `InventoryView` (DataGrid, CRUD, search, sort)
- [ ] Quản lý sản phẩm: thêm, sửa, xóa, tìm kiếm
- [ ] Quản lý kho: nhập – xuất – kiểm kê hàng hóa
- [ ] Cảnh báo hàng sắp hết (low-stock)
- [ ] Auto-refresh khi thay đổi dữ liệu

**File chính:**
- `Views/ProductsView.xaml`, `Views/InventoryView.xaml`
- `Services/ProductService.cs`, `Services/InventoryService.cs`
- `Models/Product.cs`, `Models/InventoryItem.cs`
- `ViewModels/ProductsViewModel.cs`, `InventoryViewModel.cs`

**Acceptance Criteria:**
- CRUD sản phẩm và kho hoạt động
- Low-stock alert hiển thị rõ ràng
- UI đẹp, có search/sort
- Dữ liệu cập nhật ngay khi thao tác

---

## 3. Dev 3 — Quản lý bán hàng & báo cáo (Orders + Reports)
**Nhiệm vụ chính:**
- [ ] Thiết kế `OrdersView`, `ReportsView` (DataGrid, biểu đồ, export)
- [ ] Quản lý đơn hàng: tạo đơn, kiểm tồn, lưu lịch sử
- [ ] Báo cáo doanh thu, tồn kho, xuất CSV/XLSX
- [ ] Auto-refresh khi đơn hàng thay đổi
- [ ] Giao diện đẹp, có filter thời gian, tìm kiếm

**File chính:**
- `Views/OrdersView.xaml`, `Views/ReportsView.xaml`
- `Services/OrderService.cs`, `Services/ReportService.cs`
- `Models/Order.cs`, `Models/OrderItem.cs`
- `ViewModels/OrdersViewModel.cs`, `ReportsViewModel.cs`

**Acceptance Criteria:**
- Đơn hàng xử lý đúng tồn kho
- Báo cáo xuất file hoạt động
- Giao diện có lọc, thống kê rõ ràng
- Auto-refresh khi dữ liệu thay đổi

---

## Quy ước làm việc
- Branch: `feature/<chức_năng>` (ví dụ `feature/auth`, `feature/inventory`, `feature/reports`)
- PR nhỏ, có mô tả + ảnh test UI
- Merge khi có 1 review và build pass
- Daily check-in 15’ đồng bộ tiến độ
