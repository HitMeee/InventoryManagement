using InventoryManagement.Commands;
using InventoryManagement.Models;
using InventoryManagement.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace InventoryManagement.ViewModels
{
    public class CustomersViewModel : ViewModelBase
    {
        private readonly CustomerService _service = new();
        public ObservableCollection<Customer> Customers { get; } = new();

        private Customer? _selected;
        public Customer? Selected { get => _selected; set { _selected = value; OnPropertyChanged(nameof(Selected)); } }

        private Customer _editing = new();
        public Customer Editing { get => _editing; set { _editing = value; OnPropertyChanged(nameof(Editing)); } }

        public ICommand LoadCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand DeleteCommand { get; }

        public CustomersViewModel()
        {
            LoadCommand = new RelayCommand(_ => Load());
            AddCommand = new RelayCommand(_ => Add(), _ => !string.IsNullOrWhiteSpace(Editing.Name));
            UpdateCommand = new RelayCommand(_ => Update(), _ => Selected != null);
            DeleteCommand = new RelayCommand(_ => Delete(), _ => Selected != null);
            Load();
        }

        public void Load()
        {
            Customers.Clear();
            foreach (var c in _service.GetAll()) Customers.Add(c);
        }

        public void Add()
        {
            try { var added = _service.Add(Editing); Customers.Add(added); Editing = new Customer(); }
            catch (System.Exception ex) { MessageBox.Show($"Add error: {ex.Message}"); }
        }

        public void Update()
        {
            if (Selected == null) return;
            try { _service.Update(Selected); Load(); }
            catch (System.Exception ex) { MessageBox.Show($"Update error: {ex.Message}"); }
        }

        public void Delete()
        {
            if (Selected == null) return;
            if (MessageBox.Show($"Delete {Selected.Name}?","Confirm",MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try { _service.Delete(Selected.Id); Customers.Remove(Selected); Selected = null; }
            catch (System.Exception ex) { MessageBox.Show($"Delete error: {ex.Message}"); }
        }
    }
}
