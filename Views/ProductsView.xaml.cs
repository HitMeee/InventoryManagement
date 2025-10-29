using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Data;
using InventoryManagement.Models;

namespace InventoryManagement.Views
{
    public partial class ProductsView : UserControl
    {
        public class ProductDisplay
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public string Unit { get; set; } = string.Empty;
            public int WarehouseId { get; set; }
            public string WarehouseName { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }

        public ProductsView()
        {
            InitializeComponent();
            this.Loaded += ProductsView_Loaded;
        }

        private async void ProductsView_Loaded(object sender, RoutedEventArgs e)
        {
            // Đợi UI render xong
            await System.Threading.Tasks.Task.Delay(100);
            
            LoadWarehouses();
            LoadProducts();
        }

        private void LoadWarehouses()
        {
            try
            {
                using var db = new AppDbContext();
                var warehouses = db.Warehouses.OrderBy(w => w.Name).ToList();
                
                CboWarehouse.ItemsSource = warehouses;
                if (warehouses.Any())
                {
                    CboWarehouse.SelectedIndex = 0;
                }
            }
            catch (NullReferenceException)
            {
                // Controls chưa sẵn sàng - thử lại sau 200ms
                System.Threading.Tasks.Task.Delay(200).ContinueWith(_ => {
                    this.Dispatcher.Invoke(() => LoadWarehouses());
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải kho: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProducts(string searchText = "")
        {
            try
            {
                if (CboWarehouse?.SelectedValue == null)
                {
                    DgProducts.ItemsSource = null;
                    return;
                }
                
                var warehouseId = (int)CboWarehouse.SelectedValue;
                using var db = new AppDbContext();
                var products = db.Products.Where(p => p.WarehouseId == warehouseId).OrderBy(p => p.Name).ToList();

                var productDisplays = products.Select(p => new ProductDisplay
                {
                    Id = p.Id,
                    Name = p.Name,
                    Quantity = p.Quantity,
                    Unit = p.Unit,
                    WarehouseId = p.WarehouseId,
                    WarehouseName = db.Warehouses.Where(w => w.Id == p.WarehouseId).Select(w => w.Name).FirstOrDefault() ?? "",
                    CreatedAt = p.CreatedAt
                }).ToList();

                DgProducts.ItemsSource = productDisplays;
            }
            catch (NullReferenceException)
            {
                // Controls chưa sẵn sàng - thử lại sau 200ms
                System.Threading.Tasks.Task.Delay(200).ContinueWith(_ => {
                    this.Dispatcher.Invoke(() => LoadProducts(searchText));
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải sản phẩm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CboWarehouse_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (CboWarehouse?.SelectedValue != null)
                {
                    LoadProducts();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CboWarehouse?.SelectedValue == null)
                {
                    MessageBox.Show("Vui lòng chọn kho hàng trước!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedWarehouseId = (int)CboWarehouse.SelectedValue;
                var dialog = new ProductFormDialog(selectedWarehouseId);
                dialog.Owner = Window.GetWindow(this);
                
                if (dialog.ShowDialog() == true)
                {
                    using var db = new AppDbContext();
                    var product = new Product
                    {
                        Name = dialog.ProductName,
                        Quantity = dialog.Quantity,
                        Unit = dialog.Unit,
                        WarehouseId = dialog.WarehouseId,
                        CreatedAt = DateTime.UtcNow
                    };
                    db.Products.Add(product);
                    db.SaveChanges();
                    
                    LoadProducts();
                    MessageBox.Show("Thêm sản phẩm thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi thêm sản phẩm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selected = DgProducts.SelectedItem as ProductDisplay;
                if (selected == null)
                {
                    MessageBox.Show("Vui lòng chọn sản phẩm cần sửa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dialog = new ProductFormDialog(selected.WarehouseId, selected.Name, selected.Quantity, selected.Unit);
                dialog.Owner = Window.GetWindow(this);
                
                if (dialog.ShowDialog() == true)
                {
                    using var db = new AppDbContext();
                    var product = db.Products.Find(selected.Id);
                    if (product != null)
                    {
                        product.Name = dialog.ProductName;
                        product.Quantity = dialog.Quantity;
                        product.Unit = dialog.Unit;
                        product.WarehouseId = dialog.WarehouseId;
                        db.SaveChanges();
                        
                        LoadProducts();
                        MessageBox.Show("Cập nhật sản phẩm thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi sửa sản phẩm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selected = DgProducts.SelectedItem as ProductDisplay;
                if (selected == null)
                {
                    MessageBox.Show("Vui lòng chọn sản phẩm cần xóa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Bạn có chắc muốn xóa sản phẩm '{selected.Name}'?", 
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    using var db = new AppDbContext();
                    var product = db.Products.Find(selected.Id);
                    if (product != null)
                    {
                        db.Products.Remove(product);
                        db.SaveChanges();
                        
                        LoadProducts();
                        MessageBox.Show("Xóa sản phẩm thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xóa sản phẩm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var searchText = ((TextBox)sender).Text;
                LoadProducts(searchText);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tìm kiếm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}