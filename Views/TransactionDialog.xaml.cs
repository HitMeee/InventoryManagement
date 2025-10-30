using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace InventoryManagement.Views
{
    public partial class TransactionDialog : Window
    {
        public int Quantity { get; private set; }
        public string Note { get; private set; } = string.Empty;
        
        private readonly string _transactionType; // "IMPORT" or "EXPORT"
        private readonly int _currentStock;
        private readonly string _unit;

        public TransactionDialog(string transactionType, string productName, string warehouseName, int currentStock, string unit)
        {
            InitializeComponent();
            
            _transactionType = transactionType;
            _currentStock = currentStock;
            _unit = unit;

            SetupUI(productName, warehouseName, currentStock, unit);
            
            TxtQuantity.Focus();
            TxtQuantity.Text = "1";
            TxtQuantity.SelectAll();
        }

        private void SetupUI(string productName, string warehouseName, int currentStock, string unit)
        {
            if (_transactionType == "IMPORT")
            {
                // Nhập hàng
                TxtIcon.Text = "📥";
                BorderIcon.Background = new SolidColorBrush(Color.FromRgb(232, 245, 232)); // #E8F5E8
                TxtTitle.Text = "Nhập hàng";
                TxtSubtitle.Text = "Thêm sản phẩm vào kho";
                TxtQuantityLabel.Text = "Số lượng nhập:";
                BtnConfirm.Content = "Xác nhận nhập";
                BtnConfirm.Background = new SolidColorBrush(Color.FromRgb(46, 125, 50)); // #2E7D32
            }
            else
            {
                // Xuất hàng
                TxtIcon.Text = "📤";
                BorderIcon.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224)); // #FFF3E0
                TxtTitle.Text = "Xuất hàng";
                TxtSubtitle.Text = "Lấy sản phẩm ra khỏi kho";
                TxtQuantityLabel.Text = "Số lượng xuất:";
                BtnConfirm.Content = "Xác nhận xuất";
                BtnConfirm.Background = new SolidColorBrush(Color.FromRgb(245, 124, 0)); // #F57C00
            }

            TxtProductName.Text = productName;
            TxtWarehouse.Text = warehouseName;
            TxtCurrentStock.Text = $"{currentStock:N0} {unit}";
            TxtUnit.Text = unit;
        }

        private void TxtQuantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInput();
        }

        private void ValidateInput()
        {
            var input = TxtQuantity.Text.Trim();
            TxtValidationMessage.Visibility = Visibility.Collapsed;
            
            if (string.IsNullOrEmpty(input))
            {
                BtnConfirm.IsEnabled = false;
                return;
            }

            if (!int.TryParse(input, out int quantity))
            {
                ShowValidationError("Vui lòng nhập số nguyên hợp lệ");
                BtnConfirm.IsEnabled = false;
                return;
            }

            if (quantity <= 0)
            {
                ShowValidationError("Số lượng phải lớn hơn 0");
                BtnConfirm.IsEnabled = false;
                return;
            }

            if (_transactionType == "EXPORT" && quantity > _currentStock)
            {
                ShowValidationError($"Số lượng xuất không thể lớn hơn tồn kho ({_currentStock:N0} {_unit})");
                BtnConfirm.IsEnabled = false;
                return;
            }

            if (quantity > 999999)
            {
                ShowValidationError("Số lượng quá lớn (tối đa 999,999)");
                BtnConfirm.IsEnabled = false;
                return;
            }

            // Valid input
            BtnConfirm.IsEnabled = true;
        }

        private void ShowValidationError(string message)
        {
            TxtValidationMessage.Text = message;
            TxtValidationMessage.Visibility = Visibility.Visible;
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtQuantity.Text.Trim(), out int quantity) || quantity <= 0)
            {
                ShowValidationError("Vui lòng nhập số lượng hợp lệ");
                return;
            }

            if (_transactionType == "EXPORT" && quantity > _currentStock)
            {
                ShowValidationError($"Số lượng xuất không thể lớn hơn tồn kho ({_currentStock:N0} {_unit})");
                return;
            }

            Quantity = quantity;
            Note = TxtNote.Text.Trim();
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TxtQuantity.Focus();
        }
    }
}