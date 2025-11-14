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

            // Khi UserControl load xong → gọi LoadSuppliers()
            Loaded += SuppliersView_Loaded;

            // Khi chọn 1 nhà cung cấp → load sản phẩm của nhà cung cấp đó
            DgSuppliers.SelectionChanged += DgSuppliers_SelectionChanged;
        }

        // ============================
        // 1. THÊM NHÀ CUNG CẤP
        // ============================
        private void BtnAddSupplier_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Mở form nhập thông tin supplier
                var dlg = new SupplierFormDialog();
                dlg.Owner = Window.GetWindow(this);

                if (dlg.ShowDialog() == true)
                {
                    // Lưu vào database
                    using var db = new AppDbContext();
                    var s = new Models.Supplier { Name = dlg.SupplierName, Contact = dlg.Contact };
                    db.Suppliers.Add(s);
                    db.SaveChanges();

                    // Reload bảng
                    LoadSuppliers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi thêm nhà cung cấp: {ex.Message}");
            }
        }

        // ============================
        // 2. SỬA NHÀ CUNG CẤP
        // ============================
        private void BtnEditSupplier_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Lấy dòng đang chọn
                var row = DgSuppliers.SelectedItem;
                if (row == null)
                {
                    MessageBox.Show("Vui lòng chọn nhà cung cấp để sửa.");
                    return;
                }

                // Lấy Id bằng Reflection (vì ItemsSource là anonymous object)
                var id = (int)row.GetType().GetProperty("Id")!.GetValue(row)!;

                using var db = new AppDbContext();
                var s = db.Suppliers.Find(id);
                if (s == null) return;

                // Mở dialog điền sẵn thông tin
                var dlg = new SupplierFormDialog(s.Id, s.Name, s.Contact);
                dlg.Owner = Window.GetWindow(this);

                if (dlg.ShowDialog() == true)
                {
                    // Cập nhật database
                    s.Name = dlg.SupplierName;
                    s.Contact = dlg.Contact;
                    db.SaveChanges();
                    LoadSuppliers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi sửa nhà cung cấp: {ex.Message}");
            }
        }

        // ============================
        // 3. XÓA NHÀ CUNG CẤP
        // ============================
        private void BtnDeleteSupplier_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var row = DgSuppliers.SelectedItem;
                if (row == null)
                {
                    MessageBox.Show("Vui lòng chọn nhà cung cấp để xóa.");
                    return;
                }

                // Lấy ID supplier
                var id = (int)row.GetType().GetProperty("Id")!.GetValue(row)!;

                // Hỏi xác nhận
                var confirm = MessageBox.Show("Bạn có chắc muốn xóa nhà cung cấp này?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes) return;

                using var db = new AppDbContext();
                var s = db.Suppliers.Find(id);

                if (s != null)
                {
                    // Xóa supplier → các sản phẩm của supplier đó phải gỡ SupplierId
                    var prods = db.Products.Where(p => p.SupplierId == id).ToList();
                    foreach (var p in prods) p.SupplierId = null;

                    db.Suppliers.Remove(s);
                    db.SaveChanges();

                    LoadSuppliers();
                    DgSupplierProducts.ItemsSource = null;  // clear bảng phải
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xóa nhà cung cấp: {ex.Message}");
            }
        }

        // =====================================================
        // 4. THÊM SẢN PHẨM CHO NHÀ CUNG CẤP
        // =====================================================
        private void BtnAddSupplierProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var row = DgSuppliers.SelectedItem;
                if (row == null)
                {
                    MessageBox.Show("Vui lòng chọn nhà cung cấp trước.");
                    return;
                }

                // Lấy SupplierId
                var supplierId = (int)row.GetType().GetProperty("Id")!.GetValue(row)!;

                // Mở form sản phẩm (truyền trước supplierId)
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
                MessageBox.Show($"Lỗi thêm sản phẩm: {ex.Message}");
            }
        }

        // =====================================================
        // 5. SỬA SẢN PHẨM
        // =====================================================
        private void BtnEditSupplierProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var productRow = DgSupplierProducts.SelectedItem;
                if (productRow == null)
                {
                    MessageBox.Show("Vui lòng chọn sản phẩm để sửa.");
                    return;
                }

                // Lấy Id sản phẩm
                var prodId = (int)productRow.GetType().GetProperty("Id")!.GetValue(productRow)!;

                using var db = new AppDbContext();
                var prod = db.Products.Find(prodId);
                if (prod == null) return;

                // Mở dialog sửa, truyền dữ liệu cũ
                var dlg = new ProductFormDialog(prod.WarehouseId, prod.Name, prod.Quantity, prod.Unit);
                dlg.Owner = Window.GetWindow(this);
                dlg.SupplierId = prod.SupplierId;

                if (dlg.ShowDialog() == true)
                {
                    // Update database
                    prod.Name = dlg.ProductName;
                    prod.Quantity = dlg.Quantity;
                    prod.Unit = dlg.Unit;
                    prod.WarehouseId = dlg.WarehouseId;
                    prod.SupplierId = dlg.SupplierId;
                    db.SaveChanges();

                    LoadProductsForSupplier(prod.SupplierId ?? -1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi sửa sản phẩm: {ex.Message}");
            }
        }

        // =====================================================
        // 6. XÓA SẢN PHẨM
        // =====================================================
        private void BtnDeleteSupplierProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var productRow = DgSupplierProducts.SelectedItem;
                if (productRow == null)
                {
                    MessageBox.Show("Vui lòng chọn sản phẩm để xóa.");
                    return;
                }

                var prodId = (int)productRow.GetType().GetProperty("Id")!.GetValue(productRow)!;

                var confirm = MessageBox.Show("Bạn có chắc muốn xóa sản phẩm này?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes) return;

                using var db = new AppDbContext();
                var prod = db.Products.Find(prodId);

                if (prod != null)
                {
                    db.Products.Remove(prod);
                    db.SaveChanges();
                    LoadProductsForSupplier(prod.SupplierId ?? -1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xóa sản phẩm: {ex.Message}");
            }
        }

        // =====================================================
        // 7. Khi UserControl load lần đầu → load danh sách suppliers
        // =====================================================
        private void SuppliersView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSuppliers();
        }

        // =====================================================
        // 8. Load danh sách nhà cung cấp (cột trái)
        // =====================================================
        private void LoadSuppliers()
        {
            try
            {
                using var db = new AppDbContext();

                // Lấy danh sách supplier + số sản phẩm của từng supplier
                var suppliers = db.Suppliers
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        ProductCount = db.Products.Count(p => p.SupplierId == s.Id)
                    })
                    .OrderBy(s => s.Name)
                    .ToList();

                DgSuppliers.ItemsSource = suppliers;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải nhà cung cấp: {ex.Message}");
            }
        }

        // =====================================================
        // 9. Khi chọn 1 supplier → load danh sách sản phẩm bên phải
        // =====================================================
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

                var id = (int)row.GetType().GetProperty("Id")!.GetValue(row)!;

                using var db = new AppDbContext();

                var products = db.Products
                    .Where(p => p.SupplierId == id)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Quantity,
                        p.Unit,
                        WarehouseName = db.Warehouses.Where(w => w.Id == p.WarehouseId)
                                                     .Select(w => w.Name)
                                                     .FirstOrDefault() ?? "",
                        CreatedAt = p.CreatedAt
                    })
                    .ToList();

                DgSupplierProducts.ItemsSource = products;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải sản phẩm nhà cung cấp: {ex.Message}");
            }
        }

        // =====================================================
        // 10. Load lại bảng sản phẩm theo SupplierId
        // =====================================================
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
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Quantity,
                        p.Unit,
                        WarehouseName = db.Warehouses.Where(w => w.Id == p.WarehouseId)
                                                     .Select(w => w.Name)
                                                     .FirstOrDefault() ?? ""
                    })
                    .ToList();

                DgSupplierProducts.ItemsSource = products;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải sản phẩm nhà cung cấp: {ex.Message}");
            }
        }
    }
}
