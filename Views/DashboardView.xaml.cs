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

                // Load stats
                var warehouseCount = db.Warehouses.Count();
                var productCount = db.Products.Count();
                var userCount = db.Users.Count();

                TxtWarehouseCount.Text = warehouseCount.ToString();
                TxtProductCount.Text = productCount.ToString();
                TxtUserCount.Text = userCount.ToString();

                // Load user's warehouses
                var userWarehouses = db.UserWarehouseRoles
                    .Include(uwr => uwr.Warehouse)
                    .Where(uwr => uwr.UserId == user.Id)
                    .Select(uwr => new WarehouseInfo
                    {
                        Name = uwr.Warehouse!.Name,
                        Address = uwr.Warehouse.Address,
                        Role = uwr.Role == "admin" ? "Quản trị" : "Nhân viên",
                        ProductCount = db.Products.Count(p => p.WarehouseId == uwr.WarehouseId)
                    })
                    .ToList();

                DgWarehouses.ItemsSource = userWarehouses;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi khi tải dashboard: {ex.Message}", "Lỗi", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
