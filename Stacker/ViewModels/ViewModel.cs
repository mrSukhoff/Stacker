using System;
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

        //StatusBar
        public string CurrentRow
        {
            get => String.Concat("Ряд: ", Model.CraneState.ActualRow.ToString().PadLeft(2, '0'));
        }

        public string CurrentFloor
        {
            get => String.Concat("Этаж: ", Model.CraneState.ActualFloor.ToString().PadLeft(2, '0'));
        }

        public bool IsStartPosition
        {
            get => Model.CraneState.IsStartPosiotion;
        }

        public bool IsRowMark
        {
            get => Model.CraneState.IsRowMark;
        }

        public bool IsFloorMark
        {
            get => Model.CraneState.IsFloorMark;
        }

        //SemiAutoTab

        public string SelectedAddress { get => String.Concat(_selectedRack, "-", _selectedRow, "-", _selectedFloor); }

        public char[] RackItems { get => _rackItems; }

        public int[] RowItems { get => _rowItems; }

        public int[] FloorItems { get => _floorItems; }

        public char SelectedRack
        {
            get => _selectedRack;
            set
            {
                _selectedRack = value;
                availabilityChanged();
                
            }
        }

        public int SelectedRow
        {
            get => _selectedRow;
            set
            {
                _selectedRow = value;
                availabilityChanged();
            }
        }

        public int SelectedFloor
        {
            get => _selectedFloor;
            set
            {
                _selectedFloor = value;
                availabilityChanged();
            }
        }

        public string IsCellNotAvailable
        { get
            { return Model.IsCellNotAvailable(_selectedRack, _selectedRow, _selectedFloor) == true ? "Ячейка не доступна!" : ""; }
        }

        public bool IsBringButtonAvailable
        {
            get => !Model.IsCellNotAvailable(_selectedRack, _selectedRow, _selectedFloor);
        }

        public bool IsTakeAwayButtonAvailable
        {
            get => !Model.IsCellNotAvailable(_selectedRack, _selectedRow, _selectedFloor);
        }

        //ManualModeTab
        public char LeftPlatformButtonName
        {
            get => Model.Settings.LeftRackName;
        }

        public char RightPlatformButtonName
        {
            get => Model.Settings.RightRackName;
        }

        public bool IsLeftPlatformButtonAvailable //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        {
            get => true;
        }

        public bool IsRightPlatformButtonAvailable //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        {
            get => true;
        }

        //комманды

        /*public RelayCommand BringCommand
        {
            get { };
        }*/

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
                
        void availabilityChanged()
        {
            OnPropertyChahged("SelectedAddress");
            OnPropertyChahged("IsCellNotAvailable");
            OnPropertyChahged("IsBringButtonAvailable");
            OnPropertyChahged("IsTakeAwayButtonAvailable");
        }
    }
}
