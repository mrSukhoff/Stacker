using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stacker.ViewModel
{
    class ViewModel : INotifyPropertyChanged
    {
        public ViewModel()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

    }
}
