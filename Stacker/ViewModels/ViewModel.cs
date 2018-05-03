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

        public string SelectedAddress { get { return _selectedRack + "-" + _selectedRow + "-" + _selectedFloor; } }

        public char[] RackItems { get => _rackItems; }

        public int[] RowItems { get => _rowItems; }

        public int[] FloorItems { get => _floorItems; }

        public char SelectedRack
        {
            get => _selectedRack;
            set { _selectedRack = value; OnPropertyChahged("SelectedAddress"); OnPropertyChahged("IsCellNotAvailable"); }
        }

        public int SelectedRow
        {
            get => _selectedRow;
            set { _selectedRow = value; OnPropertyChahged("SelectedAddress"); OnPropertyChahged("IsCellNotAvailable"); }
        }

        public int SelectedFloor
        {
            get => _selectedFloor;
            set { _selectedFloor = value; OnPropertyChahged("SelectedAddress"); OnPropertyChahged("IsCellNotAvailable"); }
        }

        public string IsCellNotAvailable
        { get
            { return Model.IsCellNotAvailable(_selectedRack, _selectedRow, _selectedFloor) == true ? "Ячейка не доступна!" : ""; }
        }

        public bool IsTakeAwayButtonAvailable
        {
            get => isTakeAwayButtonAvailable;
            set => isTakeAwayButtonAvailable = value;
        }

        public bool IsBringButtonAvailable
        {
            get => isBringButtonAvailable;
            set => isBringButtonAvailable = value;
        }


        //Внутренние поля класса
        //Модель штабелёра
        StackerModel Model;

        //Наборы значений для комбобоксов
        char[] _rackItems;
        int[]  _rowItems;
        int[]  _floorItems;

        char _selectedRack;
        int _selectedRow;
        int _selectedFloor;

        bool isTakeAwayButtonAvailable;
        bool isBringButtonAvailable;

        //конструктор
        public ViewModel()
        {
            Model = new StackerModel();
            FillItems();
        }

        //заполняем комбобоксы
        private void FillItems()
        {
            _rackItems = new char[]{Model.Settings.LeftRackName, Model.Settings.RightRackName};
            _rowItems = new int[Model.Settings.StackerDepth];
            for (int i = 0; i < _rowItems.Length; i++) { _rowItems[i] = i + 1; }
            _floorItems = new int[Model.Settings.StackerHight];
            for (int i = 0; i < _floorItems.Length; i++) { _floorItems[i] = i + 1; }
        }
                
        
    }
}
