using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Stacker
{
    public class MainVM : INotifyPropertyChanged
    {
        
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
                _selectedCellAddress = value.ToString();
            }
        }
        private int _gotoXTextBoxValue;

    }
}