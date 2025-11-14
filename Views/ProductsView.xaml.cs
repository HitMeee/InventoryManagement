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
        // Quản lý placeholder của ô tìm kiếm
        private bool isSearchPlaceholder = true;
        private const string SearchPlaceholder = "Tìm kiếm theo tên sản phẩm hoặc đơn vị...";

        // Model hiển thị cho DataGrid (DTO)
        public class ProductDisplay
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public string Unit { get; set; } = string.Empty;
            public int WarehouseId { get; set; }
            public string WarehouseName { get; set; } = string.Empty;
            public int? SupplierId { get; set; }
            public string SupplierName { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }

        public ProductsView()
        {
            InitializeComponent();

            // Khi View load xong → chạy hàm khởi tạo dữ liệu
            this.Loaded += ProductsView_Loaded;
        }

        private async void ProductsView_Loaded(object sender, RoutedEventArgs e)
        {
            // Chờ UI load xong để tránh lỗi null
            await System.Threading.Tasks.Task.Delay(100);

            // Ẩn các nút Add/Edit/Delete nếu user không có quyền
            try
            {
                if (!Services.AuthService.IsOwner() && !Services.AuthService.IsAdmin())
                {
                    BtnAdd.Visibility = Visibility.Collapsed;
                    BtnEdit.Visibility = Visibility.Collapsed;
                    BtnDelete.Visibility = Visibility.Collapsed;
                }
            }
            catch { }

            // Tải danh sách kho + sản phẩm
            LoadWarehouses();
            LoadProducts();
        }
        /// Tải danh sách kho phù hợp với quyền người dùng → đổ vào ComboBox
        private void LoadWarehouses()
        {
            try
            {
                using var db = new AppDbContext();
                var q = db.Warehouses.AsQueryable();

                // Owner: chỉ thấy kho của mình
                if (Services.AuthService.IsOwner())
                {
                    var ownerId = Services.AuthService.CurrentUser?.Id ?? -1;
                    q = q.Where(w => w.OwnerId == ownerId);
                }
                // Admin + Staff: thấy các kho được phân công
                else
                {
                    var ids = Services.AuthService.CurrentUserWarehouseIds ?? new();
                    q = q.Where(w => ids.Contains(w.Id));
                }

                var warehouses = q.OrderBy(w => w.Name).ToList();

                CboWarehouse.ItemsSource = warehouses;

                // Auto chọn kho đầu tiên
                if (warehouses.Any())
                    CboWarehouse.SelectedIndex = 0;
            }
            catch (NullReferenceException)
            {
                // UI chưa sẵn sàng → thử lại sau 200ms
                System.Threading.Tasks.Task.Delay(200).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => LoadWarehouses());
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải kho: {ex.Message}");
            }
        }

        /// Tải danh sách sản phẩm theo kho đang chọn + tìm kiếm nếu có
        private void LoadProducts(string searchText = "")
        {
            try
            {
                // Chưa chọn kho → không load
                if (CboWarehouse?.SelectedValue == null)
                {
                    DgProducts.ItemsSource = null;
                    return;
                }

                var warehouseId = (int)CboWarehouse.SelectedValue;
                using var db = new AppDbContext();

                // Xử lý text tìm kiếm
                var search = "";
                if (!string.IsNullOrEmpty(searchText) && searchText != SearchPlaceholder)
                    search = searchText.Trim().ToLower();

                // Lọc sản phẩm trong kho
                var productsQuery = db.Products.Where(p => p.WarehouseId == warehouseId);

                // Lọc theo tên hoặc đơn vị nếu có search
                if (!string.IsNullOrEmpty(search))
                {
                    productsQuery = productsQuery.Where(p =>
                        p.Name.ToLower().Contains(search) ||
                        (p.Unit != null && p.Unit.ToLower().Contains(search)));
                }

                var products = productsQuery.OrderBy(p => p.Name).ToList();

                // Convert sang DTO để hiển thị
                var productDisplays = products.Select(p => new ProductDisplay
                {
                    Id = p.Id,
                    Name = p.Name,
                    Quantity = p.Quantity,
                    Unit = p.Unit,
                    WarehouseId = p.WarehouseId,
                    WarehouseName = db.Warehouses.Where(w => w.Id == p.WarehouseId)
                                                 .Select(w => w.Name).FirstOrDefault() ?? "",
                    SupplierId = p.SupplierId,
                    SupplierName = db.Suppliers.Where(s => s.Id == p.SupplierId)
                                               .Select(s => s.Name).FirstOrDefault() ?? "",
                    CreatedAt = p.CreatedAt
                }).ToList();

                DgProducts.ItemsSource = productDisplays;
            }
            catch (NullReferenceException)
            {
                // UI chưa load → thử lại sau 200ms
                System.Threading.Tasks.Task.Delay(200).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => LoadProducts(searchText));
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải sản phẩm: {ex.Message}");
            }
        }


        /// Khi chọn kho khác → load sản phẩm tương ứng
        private void CboWarehouse_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboWarehouse?.SelectedValue != null)
                LoadProducts();
        }

        /// Thêm sản phẩm mới
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra quyền
            if (!Services.AuthService.IsOwner() && !Services.AuthService.IsAdmin())
            {
                MessageBox.Show("Bạn không có quyền thêm sản phẩm.");
                return;
            }

            try
            {
                if (CboWarehouse.SelectedValue == null)
                {
                    MessageBox.Show("Vui lòng chọn kho!");
                    return;
                }

                var selectedWarehouseId = (int)CboWarehouse.SelectedValue;

                var dialog = new ProductFormDialog(selectedWarehouseId);
                dialog.Owner = Window.GetWindow(this);

                // Nếu nhấn Save
                if (dialog.ShowDialog() == true)
                {
                    using var db = new AppDbContext();
                    var product = new Product
                    {
                        Name = dialog.ProductName,
                        Quantity = dialog.Quantity,
                        Unit = dialog.Unit,
                        WarehouseId = dialog.WarehouseId,
                        SupplierId = dialog.SupplierId,
                        CreatedAt = DateTime.UtcNow
                    };

                    db.Products.Add(product);
                    db.SaveChanges();

                    LoadProducts();
                    MessageBox.Show("Thêm sản phẩm thành công!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi thêm sản phẩm: {ex.Message}");
            }
        }

        /// Sửa sản phẩm
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            // Không có quyền
            if (!Services.AuthService.IsOwner() && !Services.AuthService.IsAdmin())
            {
                MessageBox.Show("Bạn không có quyền sửa sản phẩm.");
                return;
            }

            try
            {
                var selected = DgProducts.SelectedItem as ProductDisplay;
                if (selected == null)
                {
                    MessageBox.Show("Chọn sản phẩm cần sửa!");
                    return;
                }

                var dialog = new ProductFormDialog(selected.WarehouseId, selected.Name, selected.Quantity, selected.Unit)
                {
                    Owner = Window.GetWindow(this),
                    SupplierId = selected.SupplierId // set supplier mặc định
                };

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
                        product.SupplierId = dialog.SupplierId;

                        db.SaveChanges();

                        LoadProducts();
                        MessageBox.Show("Sửa sản phẩm thành công!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi sửa sản phẩm: {ex.Message}");
            }
        }

        /// Xóa sản phẩm
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!Services.AuthService.IsOwner() && !Services.AuthService.IsAdmin())
            {
                MessageBox.Show("Bạn không có quyền xóa sản phẩm.");
                return;
            }

            try
            {
                var selected = DgProducts.SelectedItem as ProductDisplay;
                if (selected == null)
                {
                    MessageBox.Show("Chọn sản phẩm cần xóa!");
                    return;
                }

                var confirm = MessageBox.Show($"Xóa sản phẩm '{selected.Name}'?",
                    "Xác nhận", MessageBoxButton.YesNo);

                if (confirm == MessageBoxResult.Yes)
                {
                    using var db = new AppDbContext();
                    var product = db.Products.Find(selected.Id);

                    if (product != null)
                    {
                        db.Products.Remove(product);
                        db.SaveChanges();

                        LoadProducts();
                        MessageBox.Show("Xóa thành công!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xóa sản phẩm: {ex.Message}");
            }
        }

        /// Xử lý tìm kiếm mỗi khi gõ phím

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var txt = (TextBox)sender;

            if (!isSearchPlaceholder)
                LoadProducts(txt.Text);
        }

        /// Khi focus vào ô tìm kiếm → xóa placeholder

        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            var txt = (TextBox)sender;

            if (isSearchPlaceholder)
            {
                txt.Text = "";
                txt.FontStyle = FontStyles.Normal;
                txt.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                isSearchPlaceholder = false;
            }
        }

        /// Khi rời ô tìm kiếm → hiện placeholder lại nếu rỗng

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            var txt = (TextBox)sender;

            if (string.IsNullOrWhiteSpace(txt.Text))
            {
                txt.Text = SearchPlaceholder;
                txt.FontStyle = FontStyles.Italic;
                txt.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
                isSearchPlaceholder = true;

                LoadProducts();
            }
        }

        /// Nhập hàng vào kho

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = (Button)sender;
                var pd = (ProductDisplay)button.Tag;

                using var db = new AppDbContext();
                var product = db.Products.Find(pd.Id);
                if (product == null)
                {
                    MessageBox.Show("Không tìm thấy sản phẩm!");
                    return;
                }

                var dialog = new TransactionDialog("IMPORT", product.Name, pd.WarehouseName, product.Quantity, product.Unit)
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    // Ghi history
                    SaveTransaction("IMPORT", product.Id, pd.WarehouseId, dialog.Quantity, product.Unit, dialog.Note);

                    // Cộng số lượng
                    product.Quantity += dialog.Quantity;
                    db.SaveChanges();

                    LoadProducts();
                    MessageBox.Show("Nhập hàng thành công!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nhập hàng: {ex.Message}");
            }
        }

        /// Xuất hàng khỏi kho

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = (Button)sender;
                var pd = (ProductDisplay)button.Tag;

                using var db = new AppDbContext();
                var product = db.Products.Find(pd.Id);

                if (product == null)
                {
                    MessageBox.Show("Không tìm thấy sản phẩm!");
                    return;
                }

                if (product.Quantity <= 0)
                {
                    MessageBox.Show("Sản phẩm hết hàng!");
                    return;
                }

                var dialog = new TransactionDialog("EXPORT", product.Name, pd.WarehouseName, product.Quantity, product.Unit)
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    // Ghi history
                    SaveTransaction("EXPORT", product.Id, pd.WarehouseId, dialog.Quantity, product.Unit, dialog.Note);

                    // Trừ số lượng
                    product.Quantity -= dialog.Quantity;
                    db.SaveChanges();

                    LoadProducts();
                    MessageBox.Show("Xuất hàng thành công!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất hàng: {ex.Message}");
            }
        }

        /// Xem lịch sử giao dịch của 1 sản phẩm

        private void BtnHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var pd = (ProductDisplay)((Button)sender).Tag;

                var dlg = new ProductHistoryDialog(pd.Name, pd.Id, pd.WarehouseName, pd.Unit)
                {
                    Owner = Window.GetWindow(this)
                };

                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở lịch sử: {ex.Message}");
            }
        }

        /// Xem toàn bộ lịch sử giao dịch

        private void BtnViewAllHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new AllHistoryDialog()
                {
                    Owner = Window.GetWindow(this)
                };

                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở lịch sử: {ex.Message}");
            }
        }

        /// Lưu giao dịch nhập/xuất vào database
        private void SaveTransaction(string type, int productId, int warehouseId, int quantity, string unit, string note = "")
        {
            try
            {
                using var db = new AppDbContext();

                var trans = new InventoryTransaction
                {
                    CreatedAt = DateTime.Now,
                    TransactionType = type,
                    ProductId = productId,
                    WarehouseId = warehouseId,
                    Quantity = quantity,
                    Unit = unit,
                    UserId = Services.AuthService.CurrentUser?.Id ?? 1,
                    Note = note
                };

                db.InventoryTransactions.Add(trans);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu giao dịch: {ex.Message}");
            }
        }
    }
}
