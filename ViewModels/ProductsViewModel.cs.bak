using InventoryManagement.Commands;
using InventoryManagement.Models;
using InventoryManagement.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace InventoryManagement.ViewModels
{
    public class ProductsViewModel : ViewModelBase
    {
        private readonly ProductService _service;

        public ObservableCollection<Product> Products { get; } = new();

        private Product? _selected;
        public Product? Selected
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(nameof(Selected)); }
        }

        private Product _editing = new();
        public Product Editing
        {
            get => _editing;
            set { _editing = value; OnPropertyChanged(nameof(Editing)); }
        }

        public ICommand LoadCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand DeleteCommand { get; }

        public ProductsViewModel()
        {
            _service = new ProductService();
            LoadCommand = new RelayCommand(_ => Load());
            AddCommand = new RelayCommand(_ => Add(), _ => !string.IsNullOrWhiteSpace(Editing?.Name));
            UpdateCommand = new RelayCommand(_ => Update(), _ => Selected != null);
            DeleteCommand = new RelayCommand(_ => Delete(), _ => Selected != null);

            Load();
        }

        public void Load()
        {
            Products.Clear();
            foreach (var p in _service.GetAll()) Products.Add(p);
        }

        public void Add()
        {
            try
            {
                var added = _service.Add(Editing);
                Products.Add(added);
                Editing = new Product();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Add error: {ex.Message}");
            }
        }

        public void Update()
        {
            if (Selected == null) return;
            try
            {
                _service.Update(Selected);
                Load();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update error: {ex.Message}");
            }
        }

        public void Delete()
        {
            if (Selected == null) return;
            if (MessageBox.Show($"Delete {Selected.Name}?","Confirm",MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                _service.Delete(Selected.Id);
                Products.Remove(Selected);
                Selected = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Delete error: {ex.Message}");
            }
        }
    }
}
