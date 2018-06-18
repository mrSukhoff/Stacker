﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Stacker.Model;


namespace Stacker.ViewModels
{
    class ViewModel : INotifyPropertyChanged
    {
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
                SelectionChanged();
                
            }
        }

        public int SelectedRow
        {
            get => (int)_selectedRow;

            set
            {
                _selectedRow = (uint)value;
                SelectionChanged();
            }
        }

        public int SelectedFloor
        {
            get => (int)_selectedFloor;
            set
            {
                _selectedFloor = (uint)value;
                SelectionChanged();
            }
        }

        public string IsCellNotAvailable
        {
            get
            {
                return Model.IsCellNotAvailable(_selectedRack, _selectedRow, _selectedFloor) == true ? "Ячейка не доступна!" : "";
            }
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

        //команды
        public RelayCommand BringCmd           { get => _bringCmd;           set => _bringCmd = value; }
        public RelayCommand TakeAwayCmd        { get => _takeAwayCmd;        set => _takeAwayCmd = value; }
        public RelayCommand PlatformToLeftCmd  { get => _platformToLeftCmd;  set => _platformToLeftCmd = value; }
        public RelayCommand PlatformToRightCmd { get => _platformToRightCmd; set => _platformToRightCmd = value; }
        public RelayCommand ToStartPositionCmd { get => _toStartPositionCmd; set => _toStartPositionCmd = value; }
        public RelayCommand ResetCmd           { get => _resetCmd;           set => _resetCmd = value; }
        public RelayCommand StopCmd            { get => _stopCmd;            set => _stopCmd = value; }
        public RelayCommand CloseErrorWindowCmd{ get => _closeErrorWindow;   set => _closeErrorWindow = value; }

        public ObservableCollection<string> Errors;

        //Внутренние поля класса-----------------------------------------------------------------------------
        //Модель штабелёра
        StackerModel Model;
        ErrorWindow ErrorWindow;

        //Наборы значений для комбобоксов
        char[] _rackItems;
        int[]  _rowItems;
        int[]  _floorItems;

        char _selectedRack;
        uint _selectedRow;
        uint _selectedFloor;

        RelayCommand _bringCmd;
        RelayCommand _takeAwayCmd;
        RelayCommand _platformToLeftCmd;
        RelayCommand _platformToRightCmd;
        RelayCommand _toStartPositionCmd;
        RelayCommand _resetCmd;
        RelayCommand _stopCmd;
        RelayCommand _closeErrorWindow;

        //конструктор ---------------------------------------------------------------------------------------
        public ViewModel()
        {
            Model = new StackerModel();
            if (!Model.IsConnected && !Model.Settings.CloseOrInform) App.Current.Shutdown(1);
            FillItems();
            InitCommands();
            Errors = Model.CraneState.ErrorList;
            Errors.CollectionChanged += ErrorAppeared;
            Model.CraneState.StateWordChanged += UpdatePosition;
            Model.CraneState.CommandDone += UpdateButtonState;
        }
         
        //реализация INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChahged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //вызывается при изменении списка ошибок
        private void ErrorAppeared(object sender, NotifyCollectionChangedEventArgs e)
        {
            //если ошибок нет, выходим
            if (Errors.Count == 0) return;
            //создаем окно с ошибками
            ErrorWindow = new ErrorWindow
            {
                DataContext = this,
                Owner = App.Current.MainWindow
            };
            ErrorWindow.ErrorsLitsView.ItemsSource = Errors;
            ErrorWindow.Show();
            //выключаем основное окно
            App.Current.MainWindow.IsEnabled = false;
        }

        //готовим списки для комбобоксов
        private void FillItems()
        {
            _rackItems = new char[] { Model.Settings.LeftRackName, Model.Settings.RightRackName };
            _rowItems = new int[Model.Settings.StackerDepth];
            for (int i = 0; i < _rowItems.Length; i++) { _rowItems[i] = i + 1; }
            _floorItems = new int[Model.Settings.StackerHight];
            for (int i = 0; i < _floorItems.Length; i++) { _floorItems[i] = i + 1; }
        }

        //инициализируем команды
        private void InitCommands()
        {
            BringCmd = new RelayCommand(DoBringCommand, CanExecuteBringCommand);
            TakeAwayCmd = new RelayCommand(DoTakeAwayCmd, CanExecuteTakeAwayCmd);
            PlatformToLeftCmd = new RelayCommand(DoPlatformToLeftCmd, CanExecutePlatformToLeftCmd);
            PlatformToRightCmd = new RelayCommand(DoPlatformToRightCmd, CanExecutePlatformToRightCmd);
            ToStartPositionCmd = new RelayCommand(DoToStartPositionCmd, CanExecuteToStartPositionCmd);
            ResetCmd = new RelayCommand(DoResetCmd);
            StopCmd = new RelayCommand(DoStopCmd);
            CloseErrorWindowCmd = new RelayCommand(DoCloseErrorWindow);
        }

        //оповещене кого требуется при изменении выбранной ячейки
        void SelectionChanged()
        {
            OnPropertyChahged("SelectedAddress");
            OnPropertyChahged("IsCellNotAvailable");
            OnPropertyChahged("IsBringButtonAvailable");
            OnPropertyChahged("IsTakeAwayButtonAvailable");
        }

        //Оповещение о изменении координат
        private void UpdatePosition(object sender, EventArgs e)
        {
            OnPropertyChahged("CurrentRow");
            OnPropertyChahged("CurrentFloor");
            OnPropertyChahged("IsStartPosition");
            OnPropertyChahged("IsRowMark");
            OnPropertyChahged("IsFloorMark");

        }

        //оповещение о выполнении команды
        private void UpdateButtonState(object sender, EventArgs e)
        {
            //неплохо бы тут апдейтить CanExecute, но пока не знаю как
        }

        //управления движением крана
        public void DirectButtonControl(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            bool state = e.ButtonState == System.Windows.Input.MouseButtonState.Pressed ? true : false;
            switch (((System.Windows.Controls.Button)sender).Name)
            {
                case "FartherButton":
                    Model.Crane.FartherButton(state);
                    break;
                case "CloserButton":
                    Model.Crane.CloserButton(state);
                    break;
                case "UpButton":
                    Model.Crane.UpButton(state);
                    break;
                case "DownButton":
                    Model.Crane.DownButton(state);
                    break;
                default: return;
            }
        }

        //Команды -------------------------------------------------------------------------------------------
        //Команда "Сбросить"
        private void DoResetCmd(object obj)
        {
            Model.Crane.SubmitError();
        }

        //Команда на закрытие окна со списком ошибок
        private void DoCloseErrorWindow (object obj)
        {
            Model.Crane.SubmitError();
            ErrorWindow.Close();
            //закрываем окно с ошибками и активируем основное окно            
            App.Current.MainWindow.IsEnabled = true;
            App.Current.MainWindow.Activate();
        }

        //команда "Привезти"
        private void DoBringCommand(object obj)
        {
            bool rack = _selectedRack == Model.Settings.RightRackName;
            Model.Crane.BringOrTakeAway(rack, _selectedRow, _selectedFloor, true);
            //MessageBox.Show("Row = " + _selectedRow.ToString() + " Floor = " + _selectedFloor.ToString());
        }
        private bool CanExecuteBringCommand(object obj)
        {
            return Model.CraneState.IsStartPosiotion & !Model.CraneState.IsBinOnPlatform;
        }

        //команда "Увезти"
        private void DoTakeAwayCmd(object obj)
        {
            bool rack = _selectedRack == Model.Settings.RightRackName;
            Model.Crane.BringOrTakeAway(rack, _selectedRow, _selectedFloor, false);
            //MessageBox.Show("Row = "+_selectedRow.ToString()+" Floor = "+_selectedFloor.ToString());
        }
        private bool CanExecuteTakeAwayCmd(object arg)
        {
            return Model.CraneState.IsStartPosiotion & Model.CraneState.IsBinOnPlatform;
        }

        //команда "Платформа влево"
        private void DoPlatformToLeftCmd(object obj) => Model.Crane.PlatformToRight();
        private bool CanExecutePlatformToLeftCmd(object arg)
        {
            return IsStartPosition | ( IsRowMark & IsFloorMark );
        }

        //команда "Платформа вправо"
        private void DoPlatformToRightCmd(object obj) => Model.Crane.PlatformToLeft();
        private bool CanExecutePlatformToRightCmd(object obj)
        {
            return IsStartPosition | (IsRowMark & IsFloorMark);
        }

        //команда "переместить на начальную позицию"
        private void DoToStartPositionCmd(object obj)
        {
            Model.Crane.GotoXY(0, 0);
        }
        private bool CanExecuteToStartPositionCmd(object arg)
        {
            return true;
        }

        //команда "СТОП"
        private void DoStopCmd(object obj)
        {
            Model.Crane.StopButton();
        }
    }
}