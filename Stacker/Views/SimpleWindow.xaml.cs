using System.Windows;
using System.Windows.Input;
using Stacker.ViewModels;

namespace Stacker
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class SimpleWindow : Window
    {
        public SimpleWindow()
        {
            
            InitializeComponent();
        }

        private void DirectButtonControl(object sender, MouseButtonEventArgs e)
        {
            (DataContext as ViewModel).DirectButtonControl(sender, e);
        }
    }
}
