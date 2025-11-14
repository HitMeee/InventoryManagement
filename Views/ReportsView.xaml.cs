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

            // Initialize supplier/product lists for the selected warehouse
            ReloadSupplierAndProducts();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        private void ReloadSupplierAndProducts()
        {
            if (CboWarehouse.SelectedValue == null) return;
            var warehouseId = (int)CboWarehouse.SelectedValue;
            using var db = new AppDbContext();
            var prods = db.Products.Where(p => p.WarehouseId == warehouseId).ToList();
            var supplierIds = prods.Select(p => p.SupplierId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            var suppliers = db.Suppliers.Where(s => supplierIds.Contains(s.Id)).OrderBy(s => s.Name).ToList();
            CboSupplier.ItemsSource = suppliers;
            CboSupplier.SelectedIndex = -1;
            CboProduct.ItemsSource = prods.OrderBy(p => p.Name).ToList();
            CboProduct.SelectedIndex = -1;
        }

        private void RefreshData()
        {
            if (CboWarehouse.SelectedValue == null) return;
            var warehouseId = (int)CboWarehouse.SelectedValue;
            var from = DpFrom.SelectedDate ?? DateTime.Today.AddDays(-30);
            var to = (DpTo.SelectedDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1); // inclusive end
            int topN = 10;
            int.TryParse(TxtTopN.Text, out topN);
            if (topN <= 0) topN = 10;

            int? supplierFilter = CboSupplier.SelectedValue as int?;
            int? productFilter = CboProduct.SelectedValue as int?;

            using var db = new AppDbContext();

            // Base product query with optional filters
            var productQuery = db.Products.Where(p => p.WarehouseId == warehouseId);
            if (supplierFilter.HasValue)
                productQuery = productQuery.Where(p => p.SupplierId == supplierFilter.Value);
            if (productFilter.HasValue)
                productQuery = productQuery.Where(p => p.Id == productFilter.Value);

            var products = productQuery
                .Join(db.Warehouses, p => p.WarehouseId, w => w.Id, (p, w) => new { p, w })
                .GroupJoin(db.Suppliers, pw => pw.p.SupplierId, s => s.Id, (pw, s) => new { pw.p, pw.w, s })
                .SelectMany(x => x.s.DefaultIfEmpty(), (x, s) => new StockRow
                {
                    ProductId = x.p.Id,
                    Name = x.p.Name,
                    SupplierName = s != null ? s.Name : "",
                    WarehouseName = x.w.Name,
                    Quantity = x.p.Quantity
                })
                .OrderBy(r => r.Name)
                .ToList();
            DgStockByProduct.ItemsSource = products;

            // Transactions grouped by day (optimized join not needed, simple group)
            var txBase = db.InventoryTransactions
                .Where(t => t.WarehouseId == warehouseId && t.CreatedAt >= from && t.CreatedAt <= to);
            if (productFilter.HasValue)
                txBase = txBase.Where(t => t.ProductId == productFilter.Value);

            var tx = txBase
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

            // Top products (import/export sum)
            var topQuery = db.InventoryTransactions
                .Where(t => t.WarehouseId == warehouseId && t.CreatedAt >= from && t.CreatedAt <= to)
                .Join(db.Products, t => t.ProductId, p => p.Id, (t, p) => new { t, p })
                .GroupJoin(db.Suppliers, tp => tp.p.SupplierId, s => s.Id, (tp, s) => new { tp.t, tp.p, s })
                .SelectMany(x => x.s.DefaultIfEmpty(), (x, s) => new { x.t, x.p, s })
                .GroupBy(x => new { x.p.Id, x.p.Name, Supplier = (x.s == null ? "" : x.s.Name) })
                .Select(g => new TopProductRow
                {
                    ProductId = g.Key.Id,
                    Name = g.Key.Name,
                    SupplierName = g.Key.Supplier,
                    Imported = g.Where(x => x.t.TransactionType == "IMPORT").Sum(x => x.t.Quantity),
                    Exported = g.Where(x => x.t.TransactionType == "EXPORT").Sum(x => x.t.Quantity)
                })
                .OrderByDescending(r => r.Imported + r.Exported)
                .Take(topN)
                .ToList();
            DgTopProducts.ItemsSource = topQuery;

            // Top suppliers (aggregate imported/exported across products)
            var topSuppliers = db.InventoryTransactions
                .Where(t => t.WarehouseId == warehouseId && t.CreatedAt >= from && t.CreatedAt <= to)
                .Join(db.Products, t => t.ProductId, p => p.Id, (t, p) => new { t, p })
                .GroupJoin(db.Suppliers, tp => tp.p.SupplierId, s => s.Id, (tp, s) => new { tp.t, tp.p, s })
                .SelectMany(x => x.s.DefaultIfEmpty(), (x, s) => new { x.t, x.p, s })
                .GroupBy(x => (x.s == null ? "" : x.s.Name))
                .Select(g => new TopSupplierRow
                {
                    SupplierName = g.Key,
                    TotalImported = g.Where(x => x.t.TransactionType == "IMPORT").Sum(x => x.t.Quantity),
                    TotalExported = g.Where(x => x.t.TransactionType == "EXPORT").Sum(x => x.t.Quantity)
                })
                .OrderByDescending(r => r.TotalImported + r.TotalExported)
                .Take(topN)
                .ToList();
            DgTopSuppliers.ItemsSource = topSuppliers;

            // Low stock: threshold heuristic (e.g., quantity < average/5 or < fixed 10)
            int thresholdFixed = 10;
            var lowStock = products
                .Where(p => p.Quantity <= thresholdFixed)
                .Select(p => new LowStockRow
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    SupplierName = p.SupplierName,
                    WarehouseName = p.WarehouseName,
                    Quantity = p.Quantity,
                    Threshold = thresholdFixed
                }).OrderBy(p => p.Quantity).ToList();
            DgLowStock.ItemsSource = lowStock;

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
            var topProducts = (DgTopProducts.ItemsSource as System.Collections.IEnumerable)?.Cast<object>().ToList() ?? new List<object>();
            var topSuppliers = (DgTopSuppliers.ItemsSource as System.Collections.IEnumerable)?.Cast<object>().ToList() ?? new List<object>();
            var lowStock = (DgLowStock.ItemsSource as System.Collections.IEnumerable)?.Cast<object>().ToList() ?? new List<object>();

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

            // Top products sheet
            var wsTopProd = wb.Worksheets.Add("TopSanPham");
            wsTopProd.Cell(1, 1).Value = "Sản phẩm";
            wsTopProd.Cell(1, 2).Value = "Nhà cung cấp";
            wsTopProd.Cell(1, 3).Value = "Nhập";
            wsTopProd.Cell(1, 4).Value = "Xuất";
            int tr = 2;
            foreach (var row in topProducts)
            {
                wsTopProd.Cell(tr, 1).Value = row.GetType().GetProperty("Name")?.GetValue(row)?.ToString() ?? "";
                wsTopProd.Cell(tr, 2).Value = row.GetType().GetProperty("SupplierName")?.GetValue(row)?.ToString() ?? "";
                wsTopProd.Cell(tr, 3).Value = Convert.ToInt32(row.GetType().GetProperty("Imported")?.GetValue(row) ?? 0);
                wsTopProd.Cell(tr, 4).Value = Convert.ToInt32(row.GetType().GetProperty("Exported")?.GetValue(row) ?? 0);
                tr++;
            }
            wsTopProd.Columns().AdjustToContents();

            // Top suppliers sheet
            var wsTopSup = wb.Worksheets.Add("TopNhaCungCap");
            wsTopSup.Cell(1, 1).Value = "Nhà cung cấp";
            wsTopSup.Cell(1, 2).Value = "Tổng nhập";
            wsTopSup.Cell(1, 3).Value = "Tổng xuất";
            int sr = 2;
            foreach (var row in topSuppliers)
            {
                wsTopSup.Cell(sr, 1).Value = row.GetType().GetProperty("SupplierName")?.GetValue(row)?.ToString() ?? "";
                wsTopSup.Cell(sr, 2).Value = Convert.ToInt32(row.GetType().GetProperty("TotalImported")?.GetValue(row) ?? 0);
                wsTopSup.Cell(sr, 3).Value = Convert.ToInt32(row.GetType().GetProperty("TotalExported")?.GetValue(row) ?? 0);
                sr++;
            }
            wsTopSup.Columns().AdjustToContents();

            // Low stock sheet
            var wsLow = wb.Worksheets.Add("TonThap");
            wsLow.Cell(1, 1).Value = "Sản phẩm";
            wsLow.Cell(1, 2).Value = "Nhà cung cấp";
            wsLow.Cell(1, 3).Value = "Kho";
            wsLow.Cell(1, 4).Value = "SL";
            wsLow.Cell(1, 5).Value = "Ngưỡng";
            int lr = 2;
            foreach (var row in lowStock)
            {
                wsLow.Cell(lr, 1).Value = row.GetType().GetProperty("Name")?.GetValue(row)?.ToString() ?? "";
                wsLow.Cell(lr, 2).Value = row.GetType().GetProperty("SupplierName")?.GetValue(row)?.ToString() ?? "";
                wsLow.Cell(lr, 3).Value = row.GetType().GetProperty("WarehouseName")?.GetValue(row)?.ToString() ?? "";
                wsLow.Cell(lr, 4).Value = Convert.ToInt32(row.GetType().GetProperty("Quantity")?.GetValue(row) ?? 0);
                wsLow.Cell(lr, 5).Value = Convert.ToInt32(row.GetType().GetProperty("Threshold")?.GetValue(row) ?? 0);
                lr++;
            }
            wsLow.Columns().AdjustToContents();

            // Detailed transactions sheet (per product rows)
            var warehouseId = (int)(CboWarehouse.SelectedValue ?? 0);
            var fromDate = DpFrom.SelectedDate ?? DateTime.Today.AddDays(-30);
            var toDate = (DpTo.SelectedDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);
            using (var db = new AppDbContext())
            {
                var detail = db.InventoryTransactions
                    .Where(t => t.WarehouseId == warehouseId && t.CreatedAt >= fromDate && t.CreatedAt <= toDate)
                    .Join(db.Products, t => t.ProductId, p => p.Id, (t, p) => new { t, p })
                    .GroupJoin(db.Suppliers, tp => tp.p.SupplierId, s => s.Id, (tp, s) => new { tp.t, tp.p, s })
                    .SelectMany(x => x.s.DefaultIfEmpty(), (x, s) => new { x.t, x.p, s })
                    .Join(db.Warehouses, tps => tps.t.WarehouseId, w => w.Id, (tps, w) => new
                    {
                        tps.t.CreatedAt,
                        ProductName = tps.p.Name,
                        SupplierName = tps.s == null ? "" : tps.s.Name,
                        tps.t.TransactionType,
                        tps.t.Quantity,
                        tps.t.Unit,
                        WarehouseName = w.Name
                    })
                    .OrderBy(r => r.CreatedAt)
                    .ToList();

                var wsDetail = wb.Worksheets.Add("ChiTietGiaoDich");
                wsDetail.Cell(1, 1).Value = "Ngày";
                wsDetail.Cell(1, 2).Value = "Sản phẩm";
                wsDetail.Cell(1, 3).Value = "Nhà cung cấp";
                wsDetail.Cell(1, 4).Value = "Loại";
                wsDetail.Cell(1, 5).Value = "SL";
                wsDetail.Cell(1, 6).Value = "Đơn vị";
                wsDetail.Cell(1, 7).Value = "Kho";
                int dr = 2;
                foreach (var row in detail)
                {
                    wsDetail.Cell(dr, 1).Value = row.CreatedAt;
                    wsDetail.Cell(dr, 1).Style.DateFormat.Format = "yyyy-MM-dd HH:mm";
                    wsDetail.Cell(dr, 2).Value = row.ProductName;
                    wsDetail.Cell(dr, 3).Value = row.SupplierName;
                    wsDetail.Cell(dr, 4).Value = row.TransactionType;
                    wsDetail.Cell(dr, 5).Value = row.Quantity;
                    wsDetail.Cell(dr, 6).Value = row.Unit;
                    wsDetail.Cell(dr, 7).Value = row.WarehouseName;
                    dr++;
                }
                wsDetail.Columns().AdjustToContents();
            }

            wb.SaveAs(path);
        }

        private class StockRow
        {
            public int ProductId { get; set; }
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

        private class TopProductRow
        {
            public int ProductId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string SupplierName { get; set; } = string.Empty;
            public int Imported { get; set; }
            public int Exported { get; set; }
        }

        private class TopSupplierRow
        {
            public string SupplierName { get; set; } = string.Empty;
            public int TotalImported { get; set; }
            public int TotalExported { get; set; }
        }

        private class LowStockRow
        {
            public int ProductId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string SupplierName { get; set; } = string.Empty;
            public string WarehouseName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public int Threshold { get; set; }
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            // Passive refresh on selection changes
            if (!IsLoaded) return;
            if (sender == CboWarehouse)
            {
                ReloadSupplierAndProducts();
            }
            RefreshData();
        }
    }
}