using System;
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

        private string OrdersFile = @"d:\WORK\Stacker\Orders\instr_exp.txt"; //путь к файлу с заявками
        private string ArchiveFile = @"d:\WORK\Stacker\Orders\instr_imp.txt"; //путь к файлу с отработанными заявками
        Timer FileTimer;

        List<Order> Orders= new List<Order>();
                
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //ReadOrders(null);
            FileTimer = new Timer(ReadOrders, null, 0, 10000);
            OrdersDataGrid.ItemsSource = Orders;
        }
        
        private void ReadOrders(object obj)
        { 
            FileInfo fileInf = new FileInfo(OrdersFile);
            if (fileInf.Exists)
            {
                using (StreamReader sr = new StreamReader(OrdersFile))
                {
                    string[] lines = sr.ReadToEnd().Split('\n');
                    foreach (string str in lines)
                    {
                        Order o = null;
                        try
                        {
                            o = new Order(str);
                            Console.WriteLine(Orders.Count);
                            
                        }
                        catch (ArgumentException e)
                        {
                            MessageBox.Show(str,"Неверный формат строки заказа");
                        }
                        if ((o != null) && (Orders.Contains(o) != true))
                        {
                            Orders.Add(o);
                        }
                    }
                    
                }
            };
              
        }
    }
}
