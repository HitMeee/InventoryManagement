using InventoryManagement.Commands;
using InventoryManagement.Models;
using InventoryManagement.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace InventoryManagement.ViewModels
{
    public class SuppliersViewModel : ViewModelBase
    {
        private readonly SupplierService _service = new();
        public ObservableCollection<Supplier> Suppliers { get; } = new();

        private Supplier? _selected;
        public Supplier? Selected { get => _selected; set { _selected = value; OnPropertyChanged(nameof(Selected)); } }

        private Supplier _editing = new();
        public Supplier Editing { get => _editing; set { _editing = value; OnPropertyChanged(nameof(Editing)); } }

        public ICommand LoadCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand DeleteCommand { get; }

        public SuppliersViewModel()
        {
            LoadCommand = new RelayCommand(_ => Load());
            AddCommand = new RelayCommand(_ => Add(), _ => !string.IsNullOrWhiteSpace(Editing.Name));
            UpdateCommand = new RelayCommand(_ => Update(), _ => Selected != null);
            DeleteCommand = new RelayCommand(_ => Delete(), _ => Selected != null);
            Load();
        }

        public void Load()
        {
            Suppliers.Clear();
            foreach (var s in _service.GetAll()) Suppliers.Add(s);
        }

        public void Add()
        {
            try
            {
                var added = _service.Add(Editing);
                Suppliers.Add(added);
                Editing = new Supplier();
            }
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
            try { _service.Delete(Selected.Id); Suppliers.Remove(Selected); Selected = null; }
            catch (System.Exception ex) { MessageBox.Show($"Delete error: {ex.Message}"); }
        }
    }
}
