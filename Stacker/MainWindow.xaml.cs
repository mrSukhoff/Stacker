using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;

namespace Stacker
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

        static private string OrdersFile = @"d:\WORK\Stacker\Orders\instr_exp.txt"; //путь к файлу с заявками
        static private string ArchiveFile = @"d:\WORK\Stacker\Orders\instr_imp.txt"; //путь к файлу с отработанными заявками
        Timer FileTimer;
                
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ReadOrders(null);
            FileTimer = new Timer(ReadOrders, null, 0, 10000);
        }

        private void FillTable()
        {
            /*List<ClaimTable> result = new List<ClaimTable>(3);
            result.Add(new ClaimTable("Майкл Джексон", "Thriller", "1982"));
            result.Add(new ClaimTable("AC/DC", "Back in Black", "1980"));
            result.Add(new ClaimTable("Bee Gees", "Saturday Night Fever", "1977"));
            result.Add(new ClaimTable("Pink Floyd", "The Dark Side of the Moon", "1973"));
            ClaimsDataGrid.ItemsSource = result;
            testListView.ItemsSource = result;*/
        }


        static private void ReadOrders(object obj)
        { 
            FileInfo fileInf = new FileInfo(OrdersFile);
            if (fileInf.Exists)
            {
                MessageBox.Show("file exist!");
            };
              
        }
    }
}
