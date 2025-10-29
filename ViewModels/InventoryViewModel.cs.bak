using InventoryManagement.Commands;
using InventoryManagement.Models;
using InventoryManagement.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace InventoryManagement.ViewModels
{
    public class InventoryViewModel : ViewModelBase
    {
        private readonly InventoryService _service = new();

        public ObservableCollection<InventoryItem> InventoryItems { get; } = new();

        private InventoryItem? _selected;
        public InventoryItem? Selected { get => _selected; set { _selected = value; OnPropertyChanged(nameof(Selected)); } }

        public int AdjustQuantity { get; set; }
        public string AdjustReason { get; set; } = string.Empty;

        public ICommand LoadCommand { get; }
        public ICommand IncreaseCommand { get; }
        public ICommand DecreaseCommand { get; }

        public InventoryViewModel()
        {
            LoadCommand = new RelayCommand(_ => Load());
            IncreaseCommand = new RelayCommand(_ => Adjust(true), _ => Selected != null && AdjustQuantity > 0);
            DecreaseCommand = new RelayCommand(_ => Adjust(false), _ => Selected != null && AdjustQuantity > 0);
            Load();
        }

        public void Load()
        {
            InventoryItems.Clear();
            foreach (var ii in _service.GetAll()) InventoryItems.Add(ii);
        }

        private void Adjust(bool increase)
        {
            if (Selected == null) return;
            try
            {
                _service.Adjust(Selected.Id, increase ? AdjustQuantity : -AdjustQuantity, AdjustReason);
                Load();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error adjusting inventory: {ex.Message}");
            }
        }
    }
}
