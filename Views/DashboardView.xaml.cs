using System;
using System.Linq;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Data;
using InventoryManagement.Services;

namespace InventoryManagement.Views
{
    public partial class DashboardView : UserControl
    {
        public class WarehouseInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public int ProductCount { get; set; }
        }

        public DashboardView()
        {
            InitializeComponent();
            Loaded += DashboardView_Loaded;
        }

        private void DashboardView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            try
            {
                var user = AuthService.CurrentUser;
                if (user == null) return;

                using var db = new AppDbContext();

                var isOwner = AuthService.IsOwner();
                var isAdmin = AuthService.IsAdmin();

                if (isOwner)
                {
                    // Chỉ hiển thị số liệu thuộc về Chủ kho
                    var ownerId = user.Id;
                    var ownerWarehouseIds = db.Warehouses
                        .Where(w => w.OwnerId == ownerId)
                        .Select(w => w.Id)
                        .ToList();

                    var warehouseCount = ownerWarehouseIds.Count;
                    var productCount = db.Products.Count(p => ownerWarehouseIds.Contains(p.WarehouseId));
                    var staffCount = db.UserWarehouseRoles
                        .Where(uwr => ownerWarehouseIds.Contains(uwr.WarehouseId) && uwr.Role.ToLower() == "staff")
                        .Select(uwr => uwr.UserId)
                        .Distinct()
                        .Count();

                    TxtWarehouseCount.Text = warehouseCount.ToString();
                    TxtProductCount.Text = productCount.ToString();
                    TxtUserCount.Text = staffCount.ToString();

                    // Danh sách kho của Chủ kho
                    var ownerWarehouses = db.Warehouses
                        .Where(w => w.OwnerId == ownerId)
                        .Select(w => new WarehouseInfo
                        {
                            Name = w.Name,
                            Address = w.Address,
                            Role = "Chủ kho",
                            ProductCount = db.Products.Count(p => p.WarehouseId == w.Id)
                        })
                        .ToList();

                    DgWarehouses.ItemsSource = ownerWarehouses;
                    if (StatsGrid != null) StatsGrid.Visibility = System.Windows.Visibility.Visible;
                }
                else if (isAdmin)
                {
                    // Hiển thị số liệu thuộc về Admin (chỉ phạm vi các kho được phân công)
                    var assignedIds = AuthService.CurrentUserWarehouseIds ?? new System.Collections.Generic.List<int>();

                    var warehouseCount = assignedIds.Count;
                    var productCount = db.Products.Count(p => assignedIds.Contains(p.WarehouseId));
                    var staffCount = db.UserWarehouseRoles
                        .Where(uwr => assignedIds.Contains(uwr.WarehouseId) && uwr.Role.ToLower() == "staff")
                        .Select(uwr => uwr.UserId)
                        .Distinct()
                        .Count();

                    TxtWarehouseCount.Text = warehouseCount.ToString();
                    TxtProductCount.Text = productCount.ToString();
                    TxtUserCount.Text = staffCount.ToString();

                    if (StatsGrid != null) StatsGrid.Visibility = System.Windows.Visibility.Visible;

                    // Danh sách kho được phân công cho Admin
                    var adminWarehouses = db.Warehouses
                        .Where(w => assignedIds.Contains(w.Id))
                        .Select(w => new WarehouseInfo
                        {
                            Name = w.Name,
                            Address = w.Address,
                            Role = "Admin",
                            ProductCount = db.Products.Count(p => p.WarehouseId == w.Id)
                        })
                        .ToList();

                    DgWarehouses.ItemsSource = adminWarehouses;
                }
                else
                {
                    // Ẩn phần thống kê đối với Admin/Nhân viên kho
                    if (StatsGrid != null) StatsGrid.Visibility = System.Windows.Visibility.Collapsed;

                    // Danh sách kho được phân công cho người dùng hiện tại
                    var userWarehouses = db.UserWarehouseRoles
                        .Include(uwr => uwr.Warehouse)
                        .Where(uwr => uwr.UserId == user.Id)
                        .Select(uwr => new WarehouseInfo
                        {
                            Name = uwr.Warehouse!.Name,
                            Address = uwr.Warehouse.Address,
                            Role = uwr.Role.ToLower() == "owner" ? "Chủ kho" : (uwr.Role.ToLower() == "admin" ? "Admin" : "Nhân viên kho"),
                            ProductCount = db.Products.Count(p => p.WarehouseId == uwr.WarehouseId)
                        })
                        .ToList();

                    DgWarehouses.ItemsSource = userWarehouses;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi khi tải dashboard: {ex.Message}", "Lỗi", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
