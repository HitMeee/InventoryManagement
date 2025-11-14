using System;
using System.Linq;
using System.Windows;
using InventoryManagement.Data;
using InventoryManagement.Models;

namespace InventoryManagement.Views
{
    public partial class ProductFormDialog : Window
    {
        // Các thuộc tính sẽ trả lại cho màn hình gọi (ProductsView)
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public int? SupplierId { get; set; }

        // Form thêm/sửa sản phẩm
        // Các tham số cung cấp dữ liệu ban đầu (khi sửa)
        public ProductFormDialog(int? selectedWarehouseId = null, string name = "", int quantity = 0, string unit = "")
        {
            try
            {
                InitializeComponent();

                // Chỉ chạy sau khi UI load xong
                Loaded += (s, e) =>
                {
                    try
                    {
                        LoadWarehouses();     // Load danh sách kho
                        LoadSuppliers();      // Load danh sách nhà cung cấp

                        // Gán dữ liệu cũ nếu là form sửa
                        TxtName.Text = name;
                        TxtQuantity.Text = quantity.ToString();
                        TxtUnit.Text = unit;

                        // Set kho mặc định nếu được truyền vào
                        if (selectedWarehouseId.HasValue && selectedWarehouseId.Value > 0)
                            CboWarehouse.SelectedValue = selectedWarehouseId.Value;

                        // Set supplier nếu có sẵn (chế độ sửa)
                        if (SupplierId.HasValue)
                            CboSupplier.SelectedValue = SupplierId.Value;

                        TxtName.Focus();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi load dữ liệu: {ex.Message}");
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi tạo form: {ex.Message}");
            }
        }

        /// <summary>
        /// Load danh sách kho phù hợp với quyền của user
        /// </summary>
        private void LoadWarehouses()
        {
            try
            {
                using var db = new AppDbContext();
                var q = db.Warehouses.AsQueryable();

                // Owner: chỉ xem kho của mình
                if (Services.AuthService.IsOwner())
                {
                    var ownerId = Services.AuthService.CurrentUser?.Id ?? -1;
                    q = q.Where(w => w.OwnerId == ownerId);
                }
                // Admin/Staff: xem các kho được phân quyền
                else
                {
                    var ids = Services.AuthService.CurrentUserWarehouseIds ?? new();
                    q = q.Where(w => ids.Contains(w.Id));
                }

                var warehouses = q.OrderBy(w => w.Name).ToList();

                CboWarehouse.ItemsSource = warehouses;

                // Nếu chưa chọn kho, chọn kho đầu tiên
                if (warehouses.Count > 0 && CboWarehouse.SelectedValue == null)
                    CboWarehouse.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách kho: {ex.Message}");
            }
        }

        /// <summary>
        /// Load danh sách nhà cung cấp
        /// </summary>
        private void LoadSuppliers()
        {
            try
            {
                using var db = new AppDbContext();
                var suppliers = db.Suppliers.OrderBy(s => s.Name).ToList();

                CboSupplier.ItemsSource = suppliers;

                // Nếu chưa chọn supplier → chọn supplier đầu tiên
                if (suppliers.Count > 0 && CboSupplier.SelectedValue == null)
                    CboSupplier.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải nhà cung cấp: {ex.Message}");
            }
        }

        /// <summary>
        /// Xử lý khi bấm LƯU: kiểm tra dữ liệu + gửi giá trị ra ngoài
        /// </summary>
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validate tên sản phẩm
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("Vui lòng nhập tên sản phẩm!");
                TxtName.Focus();
                return;
            }

            // Validate số lượng (phải là số nguyên >= 0)
            if (!int.TryParse(TxtQuantity.Text, out int qty) || qty < 0)
            {
                MessageBox.Show("Số lượng phải là số nguyên >= 0!");
                TxtQuantity.Focus();
                return;
            }

            // Validate đơn vị
            if (string.IsNullOrWhiteSpace(TxtUnit.Text))
            {
                MessageBox.Show("Vui lòng nhập đơn vị!");
                TxtUnit.Focus();
                return;
            }

            // Kiểm tra chọn kho
            if (CboWarehouse.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn kho!");
                CboWarehouse.Focus();
                return;
            }

            // Kiểm tra chọn supplier
            if (CboSupplier.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn nhà cung cấp!");
                CboSupplier.Focus();
                return;
            }

            // Gán dữ liệu cho thuộc tính để ProductsView lấy về
            ProductName = TxtName.Text.Trim();
            Quantity = qty;
            Unit = TxtUnit.Text.Trim();
            WarehouseId = (int)CboWarehouse.SelectedValue;
            SupplierId = (int?)CboSupplier.SelectedValue;

            // Cho ProductsView biết là người dùng bấm Save
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Xử lý nút HỦY: đóng form và trả về kết quả false
        /// </summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
