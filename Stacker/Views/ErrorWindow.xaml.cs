using Stacker.ViewModels;
using System.Windows;

namespace Stacker
{
    /// <summary>
    /// Логика взаимодействия для ErrorWindow.xaml
    /// </summary>
    public partial class ErrorWindow : Window
    {
        public ErrorWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as ViewModel).ResetCmd.Execute(this);
            //Close();
        }
    }
}
