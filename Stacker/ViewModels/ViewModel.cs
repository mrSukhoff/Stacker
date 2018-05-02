using System.ComponentModel;

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
