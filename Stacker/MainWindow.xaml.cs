using System.Collections.Generic;
using System.Windows;

namespace Probe1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //FillTable();
        }

        private void FillTable()
        {
            List<ClaimTable> result = new List<ClaimTable>(3);
            result.Add(new ClaimTable("Майкл Джексон", "Thriller", "1982"));
            result.Add(new ClaimTable("AC/DC", "Back in Black", "1980"));
            result.Add(new ClaimTable("Bee Gees", "Saturday Night Fever", "1977"));
            result.Add(new ClaimTable("Pink Floyd", "The Dark Side of the Moon", "1973"));
            ClaimsDataGrid.ItemsSource = result;
            testListView.ItemsSource = result;
        }

      
    }
}
