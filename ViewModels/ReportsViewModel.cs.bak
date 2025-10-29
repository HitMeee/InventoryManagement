using InventoryManagement.Commands;
using InventoryManagement.Services;
using System.Windows;
using System.Windows.Input;

namespace InventoryManagement.ViewModels
{
    public class ReportsViewModel : ViewModelBase
    {
        private readonly ProductService _productService = new();
        private readonly InventoryService _inventoryService = new();

        public ICommand ExportProductsCommand { get; }
        public ICommand ExportInventoryCommand { get; }

        public ReportsViewModel()
        {
            ExportProductsCommand = new RelayCommand(_ => ExportProducts());
            ExportInventoryCommand = new RelayCommand(_ => ExportInventory());
        }

        private void ExportProducts()
        {
            try
            {
                var list = _productService.GetAll();
                var file = System.IO.Path.Combine(System.Environment.CurrentDirectory, "products_export.csv");
                using var sw = new System.IO.StreamWriter(file);
                sw.WriteLine("Id,Code,Name,Price,ReorderLevel");
                foreach (var p in list) sw.WriteLine($"{p.Id},{p.Code},{p.Name},{p.Price},{p.ReorderLevel}");
                MessageBox.Show($"Exported to {file}");
            }
            catch (System.Exception ex) { MessageBox.Show($"Export error: {ex.Message}"); }
        }

        private void ExportInventory()
        {
            try
            {
                var list = _inventoryService.GetAll();
                var file = System.IO.Path.Combine(System.Environment.CurrentDirectory, "inventory_export.csv");
                using var sw = new System.IO.StreamWriter(file);
                sw.WriteLine("Product,Warehouse,Quantity");
                foreach (var i in list) sw.WriteLine($"{i.Product?.Name},{i.Warehouse?.Name},{i.Quantity}");
                MessageBox.Show($"Exported to {file}");
            }
            catch (System.Exception ex) { MessageBox.Show($"Export error: {ex.Message}"); }
        }
    }
}
