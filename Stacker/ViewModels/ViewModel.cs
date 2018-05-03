using System.ComponentModel;
using Stacker.Model;


namespace Stacker.ViewModel
{
    class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChahged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //Модель штабелёра
        StackerModel Model;

        //Наборы значений для комбобоксов
        char[] _rackItems;
        int[]  _rowItems;
        int[]  _floorItems;

        char _selectedRack;
        int _selectedRow;
        int _selectedFloor;

        //конструктор
        public ViewModel()
        {
            Model = new StackerModel();
            FillItems();
        }

        private void FillItems()
        {
            _rackItems = new char[]{Model.Settings.LeftRackName, Model.Settings.RightRackName};
            _rowItems = new int[Model.Settings.StackerDepth];
            for (int i = 0; i < _rowItems.Length; i++) { _rowItems[i] = i + 1; }
            _floorItems = new int[Model.Settings.StackerHight];
            for (int i = 0; i < _floorItems.Length; i++) { _floorItems[i] = i + 1; }
        }

        private string _selectedAddress="X-0-0";

        public string SelectedAddress { get {  return _selectedAddress = _selectedRack + "-" + _selectedRow + 1 + "-" + _selectedFloor + 1; } }
        public char[] RackItems  { get => _rackItems; }
        public int[] RowItems    { get => _rowItems; }
        public int[] FloorItems  { get => _floorItems; }
        public char SelectedRack  { get => _selectedRack; set { _selectedRack = value; OnPropertyChahged("SelectedAddress"); } }
        public int SelectedRow   { get => _selectedRow; set { _selectedRow = value; OnPropertyChahged("SelectedAddress"); } }
        public int SelectedFloor { get => _selectedFloor; set { _selectedFloor = value; OnPropertyChahged("SelectedAddress"); } }
    }
}
