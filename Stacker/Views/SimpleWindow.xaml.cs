using System.Windows;
using System.Windows.Input;

namespace Stacker
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class SimpleWindow : Window
    {
        public SimpleWindow()
        {
            DataContext = new ViewModel.ViewModel();
            InitializeComponent();
        }

        private void DirectButtonControl(object sender, MouseButtonEventArgs e)
        {
            ((ViewModel.ViewModel)DataContext).DirectButtonControl(sender, e);
        }
    }
}
