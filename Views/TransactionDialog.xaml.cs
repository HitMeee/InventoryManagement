using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace InventoryManagement.Views
{
    public partial class TransactionDialog : Window
    {
        // Kết quả người dùng nhập (trả về cho ProductsView)
        public int Quantity { get; private set; }
        public string Note { get; private set; } = string.Empty;
        
        // Dữ liệu nội bộ: dialog cần biết để validate
        private readonly string _transactionType; // "IMPORT" hoặc "EXPORT"
        private readonly int _currentStock;       // Tồn kho hiện tại
        private readonly string _unit;            // Đơn vị (chiếc, kg, thùng...)

        public TransactionDialog(string transactionType, string productName, string warehouseName, int currentStock, string unit)
        {
            InitializeComponent();
            
            // Lưu thông tin ban đầu
            _transactionType = transactionType;
            _currentStock = currentStock;
            _unit = unit;

            // Set UI theo loại giao dịch
            SetupUI(productName, warehouseName, currentStock, unit);
            
            // Gán mặc định số lượng = 1 + focus vào ô nhập
            TxtQuantity.Focus();
            TxtQuantity.Text = "1";
            TxtQuantity.SelectAll();
        }

        /// <summary>
        /// Thiết lập giao diện dựa trên loại giao dịch (IMPORT/EXPORT)
        /// </summary>
        private void SetupUI(string productName, string warehouseName, int currentStock, string unit)
        {
            if (_transactionType == "IMPORT")
            {
                // Nhập hàng
                TxtIcon.Text = "📥";
                BorderIcon.Background = new SolidColorBrush(Color.FromRgb(232, 245, 232)); // Xanh nhạt
                TxtTitle.Text = "Nhập hàng";
                TxtSubtitle.Text = "Thêm sản phẩm vào kho";
                TxtQuantityLabel.Text = "Số lượng nhập:";
                BtnConfirm.Content = "Xác nhận nhập";
                BtnConfirm.Background = new SolidColorBrush(Color.FromRgb(46, 125, 50)); 
            }
            else
            {
                // Xuất hàng
                TxtIcon.Text = "📤";
                BorderIcon.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224)); 
                TxtTitle.Text = "Xuất hàng";
                TxtSubtitle.Text = "Lấy sản phẩm ra khỏi kho";
                TxtQuantityLabel.Text = "Số lượng xuất:";
                BtnConfirm.Content = "Xác nhận xuất";
                BtnConfirm.Background = new SolidColorBrush(Color.FromRgb(245, 124, 0)); 
            }

            // Set dữ liệu hiển thị chung
            TxtProductName.Text = productName;
            TxtWarehouse.Text = warehouseName;
            TxtCurrentStock.Text = $"{currentStock:N0} {unit}";
            TxtUnit.Text = unit;
        }

        /// <summary>
        /// Validate mỗi khi user thay đổi Text
        /// </summary>
        private void TxtQuantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInput();
        }

        /// <summary>
        /// Kiểm tra dữ liệu hợp lệ cho số lượng nhập/xuất
        /// </summary>
        private void ValidateInput()
        {
            var input = TxtQuantity.Text.Trim();
            TxtValidationMessage.Visibility = Visibility.Collapsed;

            // Rỗng -> không hợp lệ
            if (string.IsNullOrEmpty(input))
            {
                BtnConfirm.IsEnabled = false;
                return;
            }

            // Không phải số nguyên
            if (!int.TryParse(input, out int quantity))
            {
                ShowValidationError("Vui lòng nhập số nguyên hợp lệ");
                BtnConfirm.IsEnabled = false;
                return;
            }

            // Số phải > 0
            if (quantity <= 0)
            {
                ShowValidationError("Số lượng phải lớn hơn 0");
                BtnConfirm.IsEnabled = false;
                return;
            }

            // Xuất hàng không được > tồn kho
            if (_transactionType == "EXPORT" && quantity > _currentStock)
            {
                ShowValidationError($"Số lượng xuất không thể lớn hơn tồn kho ({_currentStock:N0} {_unit})");
                BtnConfirm.IsEnabled = false;
                return;
            }

            // Chặn số quá lớn
            if (quantity > 999999)
            {
                ShowValidationError("Số lượng quá lớn (tối đa 999,999)");
                BtnConfirm.IsEnabled = false;
                return;
            }

            // → Hợp lệ
            BtnConfirm.IsEnabled = true;
        }

        private void ShowValidationError(string message)
        {
            TxtValidationMessage.Text = message;
            TxtValidationMessage.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Khi nhấn Xác nhận
        /// </summary>
        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtQuantity.Text.Trim(), out int quantity) || quantity <= 0)
            {
                ShowValidationError("Vui lòng nhập số lượng hợp lệ");
                return;
            }

            // Chỉ áp dụng cho xuất hàng
            if (_transactionType == "EXPORT" && quantity > _currentStock)
            {
                ShowValidationError($"Số lượng xuất không thể lớn hơn tồn kho ({_currentStock:N0} {_unit})");
                return;
            }

            // Gán dữ liệu trả ra ngoài
            Quantity = quantity;
            Note = TxtNote.Text.Trim();

            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Khi nhấn Hủy → đóng form
        /// </summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Khi load xong cửa sổ → focus vào ô nhập
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TxtQuantity.Focus();
        }
    }
}
