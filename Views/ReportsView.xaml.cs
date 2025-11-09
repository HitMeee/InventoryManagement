using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using InventoryManagement.Data;

namespace InventoryManagement.Views
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
            Loaded += ReportsView_Loaded;
        }

        private void ReportsView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                InitFilters();
                RefreshData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi tạo báo cáo: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitFilters()
        {
            using var db = new AppDbContext();
            var q = db.Warehouses.AsQueryable();
            if (Services.AuthService.IsOwner())
            {
                var ownerId = Services.AuthService.CurrentUser?.Id ?? -1;
                q = q.Where(w => w.OwnerId == ownerId);
            }
            else if (Services.AuthService.IsAdmin())
            {
                var ids = Services.AuthService.CurrentUserWarehouseIds ?? new List<int>();
                q = q.Where(w => ids.Contains(w.Id));
            }
            else
            {
                var ids = Services.AuthService.CurrentUserWarehouseIds ?? new List<int>();
                q = q.Where(w => ids.Contains(w.Id));
            }
            var warehouses = q.OrderBy(w => w.Name).ToList();
            CboWarehouse.ItemsSource = warehouses;
            if (warehouses.Any()) CboWarehouse.SelectedIndex = 0;

            DpFrom.SelectedDate = DateTime.Today.AddDays(-30);
            DpTo.SelectedDate = DateTime.Today;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        private void RefreshData()
        {
            if (CboWarehouse.SelectedValue == null) return;
            var warehouseId = (int)CboWarehouse.SelectedValue;
            var from = DpFrom.SelectedDate ?? DateTime.Today.AddDays(-30);
            var to = (DpTo.SelectedDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1); // inclusive end

            using var db = new AppDbContext();

            // Stock by product
            var products = db.Products.Where(p => p.WarehouseId == warehouseId)
                .Select(p => new StockRow
                {
                    Name = p.Name,
                    SupplierName = db.Suppliers.Where(s => s.Id == p.SupplierId).Select(s => s.Name).FirstOrDefault() ?? "",
                    WarehouseName = db.Warehouses.Where(w => w.Id == p.WarehouseId).Select(w => w.Name).FirstOrDefault() ?? "",
                    Quantity = p.Quantity
                }).OrderBy(r => r.Name).ToList();
            DgStockByProduct.ItemsSource = products;

            // Transactions grouped by day
            var tx = db.InventoryTransactions
                .Where(t => t.WarehouseId == warehouseId && t.CreatedAt >= from && t.CreatedAt <= to)
                .Select(t => new { t.CreatedAt, t.TransactionType, t.Quantity })
                .ToList()
                .GroupBy(t => t.CreatedAt.Date)
                .Select(g => new TxRow
                {
                    Date = g.Key,
                    Imported = g.Where(x => x.TransactionType == "IMPORT").Sum(x => x.Quantity),
                    Exported = g.Where(x => x.TransactionType == "EXPORT").Sum(x => x.Quantity)
                })
                .OrderBy(r => r.Date)
                .ToList();
            DgTransactions.ItemsSource = tx;

            // KPIs
            TxtTotalProducts.Text = products.Count.ToString();
            TxtTotalStock.Text = products.Sum(p => p.Quantity).ToString();
            TxtImported.Text = tx.Sum(r => r.Imported).ToString();
            TxtExported.Text = tx.Sum(r => r.Exported).ToString();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    FileName = $"BaoCao_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
                };
                if (dlg.ShowDialog() != true) return;

                ExportToExcel(dlg.FileName);
                MessageBox.Show("Xuất báo cáo thành công.", "Xuất báo cáo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất báo cáo: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToExcel(string path)
        {
            // gather data
            var stock = (DgStockByProduct.ItemsSource as System.Collections.IEnumerable)?.Cast<object>().ToList() ?? new List<object>();
            var tx = (DgTransactions.ItemsSource as System.Collections.IEnumerable)?.Cast<object>().ToList() ?? new List<object>();

            var warehouseName = (CboWarehouse.SelectedItem?.GetType().GetProperty("Name")?.GetValue(CboWarehouse.SelectedItem) as string) ?? "";
            var from = DpFrom.SelectedDate?.ToString("yyyy-MM-dd") ?? "";
            var to = DpTo.SelectedDate?.ToString("yyyy-MM-dd") ?? "";

            using var wb = new ClosedXML.Excel.XLWorkbook();

            // Summary sheet
            var wsSummary = wb.Worksheets.Add("TongQuan");
            wsSummary.Cell(1, 1).Value = "Báo cáo tồn kho & giao dịch";
            wsSummary.Cell(2, 1).Value = $"Kho: {warehouseName}";
            wsSummary.Cell(3, 1).Value = $"Kỳ: {from} - {to}";
            wsSummary.Cell(5, 1).Value = "Tổng sản phẩm";
            wsSummary.Cell(5, 2).Value = TxtTotalProducts.Text;
            wsSummary.Cell(6, 1).Value = "Tổng tồn kho";
            wsSummary.Cell(6, 2).Value = TxtTotalStock.Text;
            wsSummary.Cell(7, 1).Value = "Nhập trong kỳ";
            wsSummary.Cell(7, 2).Value = TxtImported.Text;
            wsSummary.Cell(8, 1).Value = "Xuất trong kỳ";
            wsSummary.Cell(8, 2).Value = TxtExported.Text;
            wsSummary.Columns().AdjustToContents();

            // Stock sheet
            var wsStock = wb.Worksheets.Add("TonKho");
            wsStock.Cell(1, 1).Value = "Sản phẩm";
            wsStock.Cell(1, 2).Value = "Nhà cung cấp";
            wsStock.Cell(1, 3).Value = "Kho";
            wsStock.Cell(1, 4).Value = "SL";
            int r = 2;
            foreach (var row in stock)
            {
                var name = row.GetType().GetProperty("Name")?.GetValue(row)?.ToString() ?? "";
                var supplier = row.GetType().GetProperty("SupplierName")?.GetValue(row)?.ToString() ?? "";
                var wh = row.GetType().GetProperty("WarehouseName")?.GetValue(row)?.ToString() ?? "";
                var qty = row.GetType().GetProperty("Quantity")?.GetValue(row)?.ToString() ?? "0";
                wsStock.Cell(r, 1).Value = name;
                wsStock.Cell(r, 2).Value = supplier;
                wsStock.Cell(r, 3).Value = wh;
                wsStock.Cell(r, 4).Value = qty;
                r++;
            }
            wsStock.Columns().AdjustToContents();

            // Transactions sheet
            var wsTx = wb.Worksheets.Add("GiaoDich");
            wsTx.Cell(1, 1).Value = "Ngày";
            wsTx.Cell(1, 2).Value = "Nhập";
            wsTx.Cell(1, 3).Value = "Xuất";
            r = 2;
            foreach (var row in tx)
            {
                var dateObj = row.GetType().GetProperty("Date")?.GetValue(row);
                DateTime date = dateObj is DateTime d ? d : DateTime.MinValue;
                var importedObj = row.GetType().GetProperty("Imported")?.GetValue(row) ?? 0;
                var exportedObj = row.GetType().GetProperty("Exported")?.GetValue(row) ?? 0;
                wsTx.Cell(r, 1).Value = date;
                wsTx.Cell(r, 1).Style.DateFormat.Format = "yyyy-MM-dd";
                wsTx.Cell(r, 2).Value = Convert.ToInt32(importedObj);
                wsTx.Cell(r, 3).Value = Convert.ToInt32(exportedObj);
                r++;
            }
            wsTx.Columns().AdjustToContents();

            wb.SaveAs(path);
        }

        private class StockRow
        {
            public string Name { get; set; } = string.Empty;
            public string SupplierName { get; set; } = string.Empty;
            public string WarehouseName { get; set; } = string.Empty;
            public int Quantity { get; set; }
        }

        private class TxRow
        {
            public DateTime Date { get; set; }
            public int Imported { get; set; }
            public int Exported { get; set; }
        }
    }
}