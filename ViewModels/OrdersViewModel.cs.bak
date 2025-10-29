using InventoryManagement.Commands;
using InventoryManagement.Models;
using InventoryManagement.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace InventoryManagement.ViewModels
{
    public class OrdersViewModel : ViewModelBase
    {
        private readonly OrderService _orderService = new();
        private readonly CustomerService _customerService = new();
        private readonly ProductService _productService = new();

        public ObservableCollection<Order> Orders { get; } = new();
        public ObservableCollection<Customer> Customers { get; } = new();
        public ObservableCollection<Product> Products { get; } = new();

        public Order? SelectedOrder { get; set; }
        public Customer? SelectedCustomer { get; set; }
        public Product? SelectedProduct { get; set; }
        public int OrderQuantity { get; set; }

        public ICommand LoadCommand { get; }
        public ICommand CreateOrderCommand { get; }

        public OrdersViewModel()
        {
            LoadCommand = new RelayCommand(_ => Load());
            CreateOrderCommand = new RelayCommand(_ => CreateOrder(), _ => SelectedCustomer != null && SelectedProduct != null && OrderQuantity > 0);
            Load();
        }

        public void Load()
        {
            Orders.Clear();
            foreach (var o in _orderService.GetAll()) Orders.Add(o);

            Customers.Clear();
            foreach (var c in _customerService.GetAll()) Customers.Add(c);

            Products.Clear();
            foreach (var p in _productService.GetAll()) Products.Add(p);
        }

        public void CreateOrder()
        {
            if (SelectedCustomer == null || SelectedProduct == null) return;
            try
            {
                _orderService.CreateOrder(SelectedCustomer.Id, SelectedProduct.Id, OrderQuantity);
                MessageBox.Show("Order created.");
                Load();
            }
            catch (System.Exception ex) { MessageBox.Show($"Create order error: {ex.Message}"); }
        }
    }
}
