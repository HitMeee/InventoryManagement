using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using InventoryManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Views
{
    public partial class SuppliersView : UserControl
    {
        public SuppliersView()
        {
            InitializeComponent();
            Loaded += SuppliersView_Loaded;
            DgSuppliers.SelectionChanged += DgSuppliers_SelectionChanged;
        }

        private void BtnAddSupplier_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var dlg = new SupplierFormDialog();
                dlg.Owner = Window.GetWindow(this);
                if (dlg.ShowDialog() == true)
                {
                    using var db = new AppDbContext();
                    var s = new Models.Supplier { Name = dlg.SupplierName, Contact = dlg.Contact };
                    db.Suppliers.Add(s);
                    db.SaveChanges();
                    LoadSuppliers();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi thêm nhà cung cấp: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnEditSupplier_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var row = DgSuppliers.SelectedItem;
                if (row == null) { System.Windows.MessageBox.Show("Vui lòng chọn nhà cung cấp để sửa."); return; }
                var id = (int)row.GetType().GetProperty("Id")!.GetValue(row)!;
                using var db = new AppDbContext();
                var s = db.Suppliers.Find(id);
                if (s == null) return;
                var dlg = new SupplierFormDialog(s.Id, s.Name, s.Contact);
                dlg.Owner = Window.GetWindow(this);
                if (dlg.ShowDialog() == true)
                {
                    s.Name = dlg.SupplierName;
                    s.Contact = dlg.Contact;
                    db.SaveChanges();
                    LoadSuppliers();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi sửa nhà cung cấp: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnDeleteSupplier_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var row = DgSuppliers.SelectedItem;
                if (row == null) { System.Windows.MessageBox.Show("Vui lòng chọn nhà cung cấp để xóa."); return; }
                var id = (int)row.GetType().GetProperty("Id")!.GetValue(row)!;
                var confirm = System.Windows.MessageBox.Show("Bạn có chắc muốn xóa nhà cung cấp này?", "Xác nhận", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                if (confirm != System.Windows.MessageBoxResult.Yes) return;
                using var db = new AppDbContext();
                var s = db.Suppliers.Find(id);
                if (s != null)
                {
                    // set supplier_id null on products
                    var prods = db.Products.Where(p => p.SupplierId == id).ToList();
                    foreach (var p in prods) p.SupplierId = null;
                    db.Suppliers.Remove(s);
                    db.SaveChanges();
                    LoadSuppliers();
                    DgSupplierProducts.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi xóa nhà cung cấp: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // Supplier products management
        private void BtnAddSupplierProduct_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var row = DgSuppliers.SelectedItem;
                if (row == null) { System.Windows.MessageBox.Show("Vui lòng chọn nhà cung cấp trước."); return; }
                var supplierId = (int)row.GetType().GetProperty("Id")!.GetValue(row)!;
                // Open product dialog with supplier preselected
                var dialog = new ProductFormDialog();
                dialog.Owner = Window.GetWindow(this);
                dialog.SupplierId = supplierId;
                if (dialog.ShowDialog() == true)
                {
                    using var db = new AppDbContext();
                    var prod = new Models.Product
                    {
                        Name = dialog.ProductName,
                        Quantity = dialog.Quantity,
                        Unit = dialog.Unit,
                        WarehouseId = dialog.WarehouseId,
                        SupplierId = dialog.SupplierId,
                        CreatedAt = DateTime.UtcNow
                    };
                    db.Products.Add(prod);
                    db.SaveChanges();
                    LoadProductsForSupplier(supplierId);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi thêm sản phẩm: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnEditSupplierProduct_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var productRow = DgSupplierProducts.SelectedItem;
                if (productRow == null) { System.Windows.MessageBox.Show("Vui lòng chọn sản phẩm để sửa."); return; }
                var prodId = (int)productRow.GetType().GetProperty("Id")!.GetValue(productRow)!;
                using var db = new AppDbContext();
                var prod = db.Products.Find(prodId);
                if (prod == null) return;
                var dlg = new ProductFormDialog(prod.WarehouseId, prod.Name, prod.Quantity, prod.Unit);
                dlg.Owner = Window.GetWindow(this);
                dlg.SupplierId = prod.SupplierId;
                if (dlg.ShowDialog() == true)
                {
                    prod.Name = dlg.ProductName;
                    prod.Quantity = dlg.Quantity;
                    prod.Unit = dlg.Unit;
                    prod.WarehouseId = dlg.WarehouseId;
                    prod.SupplierId = dlg.SupplierId;
                    db.SaveChanges();
                    // refresh list
                    var supplierId = prod.SupplierId ?? -1;
                    LoadProductsForSupplier(supplierId);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi sửa sản phẩm: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnDeleteSupplierProduct_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var productRow = DgSupplierProducts.SelectedItem;
                if (productRow == null) { System.Windows.MessageBox.Show("Vui lòng chọn sản phẩm để xóa."); return; }
                var prodId = (int)productRow.GetType().GetProperty("Id")!.GetValue(productRow)!;
                var confirm = System.Windows.MessageBox.Show("Bạn có chắc muốn xóa sản phẩm này?", "Xác nhận", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                if (confirm != System.Windows.MessageBoxResult.Yes) return;
                using var db = new AppDbContext();
                var prod = db.Products.Find(prodId);
                if (prod != null)
                {
                    db.Products.Remove(prod);
                    db.SaveChanges();
                    var supplierId = prod.SupplierId ?? -1;
                    LoadProductsForSupplier(supplierId);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi xóa sản phẩm: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void SuppliersView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            LoadSuppliers();
        }

        private void LoadSuppliers()
        {
            try
            {
                using var db = new AppDbContext();
                // Only Owner/Admin can access this view; filter suppliers globally
                var suppliers = db.Suppliers
                    .Select(s => new { s.Id, s.Name,
                        ProductCount = db.Products.Count(p => p.SupplierId == s.Id)
                    })
                    .OrderBy(s => s.Name)
                    .ToList()
                    .Select(s => new { s.Id, s.Name, s.ProductCount })
                    .ToList();

                DgSuppliers.ItemsSource = suppliers;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi tải nhà cung cấp: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void DgSuppliers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var row = DgSuppliers.SelectedItem;
                if (row == null)
                {
                    DgSupplierProducts.ItemsSource = null;
                    return;
                }
                var idProp = row.GetType().GetProperty("Id");
                if (idProp == null) return;
                var id = (int)idProp.GetValue(row)!;

                using var db = new AppDbContext();
                var products = db.Products
                    .Where(p => p.SupplierId == id)
                    .Select(p => new {
                        p.Id,
                        p.Name,
                        p.Quantity,
                        p.Unit,
                        WarehouseName = db.Warehouses.Where(w => w.Id == p.WarehouseId).Select(w => w.Name).FirstOrDefault() ?? "",
                        CreatedAt = p.CreatedAt
                    })
                    .ToList();

                DgSupplierProducts.ItemsSource = products;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi tải sản phẩm nhà cung cấp: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void LoadProductsForSupplier(int supplierId)
        {
            if (supplierId <= 0)
            {
                DgSupplierProducts.ItemsSource = null;
                return;
            }
            try
            {
                using var db = new AppDbContext();
                var products = db.Products
                    .Where(p => p.SupplierId == supplierId)
                    .Select(p => new {
                        p.Id,
                        p.Name,
                        p.Quantity,
                        p.Unit,
                        WarehouseName = db.Warehouses.Where(w => w.Id == p.WarehouseId).Select(w => w.Name).FirstOrDefault() ?? ""
                    })
                    .ToList();

                DgSupplierProducts.ItemsSource = products;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải sản phẩm nhà cung cấp: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
