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
        private bool isSearchPlaceholder = true;
        private const string SearchPlaceholder = "Tìm kiếm theo tên sản phẩm hoặc đơn vị...";
        
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
                
                // Get search text (ignore placeholder)
                var search = "";
                if (!string.IsNullOrEmpty(searchText) && searchText != SearchPlaceholder)
                {
                    search = searchText.Trim().ToLower();
                }
                
                var productsQuery = db.Products.Where(p => p.WarehouseId == warehouseId);
                
                // Apply smart search if search text is provided
                if (!string.IsNullOrEmpty(search))
                {
                    productsQuery = productsQuery.Where(p => 
                        p.Name.ToLower().Contains(search) ||
                        (p.Unit != null && p.Unit.ToLower().Contains(search)));
                }
                
                var products = productsQuery.OrderBy(p => p.Name).ToList();

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
                var textBox = (TextBox)sender;
                var searchText = textBox.Text;
                
                // Only search if it's not the placeholder text
                if (!isSearchPlaceholder)
                {
                    LoadProducts(searchText);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tìm kiếm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (isSearchPlaceholder)
            {
                textBox.Text = "";
                textBox.FontStyle = FontStyles.Normal;
                textBox.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                isSearchPlaceholder = false;
            }
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = SearchPlaceholder;
                textBox.FontStyle = FontStyles.Italic;
                textBox.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
                isSearchPlaceholder = true;
                LoadProducts(); // Reset to show all products
            }
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = (Button)sender;
                var productDisplay = (ProductDisplay)button.Tag;
                
                using var db = new AppDbContext();
                var product = db.Products.Find(productDisplay.Id);
                if (product == null)
                {
                    MessageBox.Show("Không tìm thấy sản phẩm!", "Lỗi", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var dialog = new TransactionDialog("IMPORT", product.Name, productDisplay.WarehouseName, product.Quantity, product.Unit);
                dialog.Owner = Window.GetWindow(this);
                
                if (dialog.ShowDialog() == true)
                {
                    // Lưu giao dịch với thông tin chi tiết
                    SaveTransaction("IMPORT", product.Id, productDisplay.WarehouseId, dialog.Quantity, product.Unit, dialog.Note);

                    // Cập nhật số lượng sản phẩm
                    product.Quantity += dialog.Quantity;
                    db.Products.Update(product);
                    db.SaveChanges();

                    LoadProducts();
                    
                    var noteText = string.IsNullOrEmpty(dialog.Note) ? "" : $"\nGhi chú: {dialog.Note}";
                    MessageBox.Show($"Nhập hàng thành công!\n\n" +
                                   $"Sản phẩm: {product.Name}\n" +
                                   $"Số lượng nhập: {dialog.Quantity:N0} {product.Unit}\n" +
                                   $"Tồn kho mới: {product.Quantity:N0} {product.Unit}" +
                                   noteText, 
                        "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nhập hàng: {ex.Message}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = (Button)sender;
                var productDisplay = (ProductDisplay)button.Tag;
                
                using var db = new AppDbContext();
                var product = db.Products.Find(productDisplay.Id);
                if (product == null)
                {
                    MessageBox.Show("Không tìm thấy sản phẩm!", "Lỗi", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (product.Quantity <= 0)
                {
                    MessageBox.Show("Sản phẩm này hiện không có hàng trong kho!", "Cảnh báo", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dialog = new TransactionDialog("EXPORT", product.Name, productDisplay.WarehouseName, product.Quantity, product.Unit);
                dialog.Owner = Window.GetWindow(this);
                
                if (dialog.ShowDialog() == true)
                {
                    // Lưu giao dịch với thông tin chi tiết
                    SaveTransaction("EXPORT", product.Id, productDisplay.WarehouseId, dialog.Quantity, product.Unit, dialog.Note);

                    // Cập nhật số lượng sản phẩm
                    product.Quantity -= dialog.Quantity;
                    db.Products.Update(product);
                    db.SaveChanges();

                    LoadProducts();
                    
                    var noteText = string.IsNullOrEmpty(dialog.Note) ? "" : $"\nGhi chú: {dialog.Note}";
                    MessageBox.Show($"Xuất hàng thành công!\n\n" +
                                   $"Sản phẩm: {product.Name}\n" +
                                   $"Số lượng xuất: {dialog.Quantity:N0} {product.Unit}\n" +
                                   $"Tồn kho còn lại: {product.Quantity:N0} {product.Unit}" +
                                   noteText, 
                        "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất hàng: {ex.Message}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = (Button)sender;
                var productDisplay = (ProductDisplay)button.Tag;
                
                var historyDialog = new ProductHistoryDialog(
                    productDisplay.Name, 
                    productDisplay.Id, 
                    productDisplay.WarehouseName, 
                    productDisplay.Unit);
                historyDialog.Owner = Window.GetWindow(this);
                historyDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở lịch sử: {ex.Message}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnViewAllHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var historyDialog = new AllHistoryDialog();
                historyDialog.Owner = Window.GetWindow(this);
                historyDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở lịch sử: {ex.Message}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveTransaction(string type, int productId, int warehouseId, int quantity, string unit, string note = "")
        {
            try
            {
                using var db = new AppDbContext();
                
                var transaction = new InventoryTransaction
                {
                    CreatedAt = DateTime.Now,
                    TransactionType = type,
                    ProductId = productId,
                    WarehouseId = warehouseId,
                    Quantity = quantity,
                    Unit = unit,
                    UserId = Services.AuthService.CurrentUser?.Id ?? 1, // Default user nếu không có current user
                    Note = note
                };
                
                db.InventoryTransactions.Add(transaction);
                db.SaveChanges();
                
                System.Diagnostics.Debug.WriteLine($"Transaction saved to database: {type} - ProductId {productId} - Quantity {quantity}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi lưu transaction vào database: {ex.Message}");
                MessageBox.Show($"Lỗi lưu lịch sử giao dịch: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}