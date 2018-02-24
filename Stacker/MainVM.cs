using System;
using System.ComponentModel;
using System.Windows.Input;

namespace Stacker
{
    public class MainVM : INotifyPropertyChanged
    {
        StackerModel Stacker;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _selectedCellAddress = "X-0-0";
        public string SelectedCellAdsress { get => _selectedCellAddress;}

        public int GotoXTextBoxValue
        {
            get => _gotoXTextBoxValue;
            set
            {
                _gotoXTextBoxValue = value;
            }
        }
        private int _gotoXTextBoxValue;

        public int GotoYTextBoxValue
        {
            get => _gotoYTextBoxValue;
            set
            {
                _gotoYTextBoxValue = value;
            }
        }
        private int _gotoYTextBoxValue;

        private RelayCommand bringAuto;
        public RelayCommand BringAuto
        {
            get
            {
                return bringAuto ??
                    (bringAuto = new RelayCommand(obj =>
                    {
                        Stacker.BringOrTakeAway(true);
                   
                    }));
            }
        }
    }
}