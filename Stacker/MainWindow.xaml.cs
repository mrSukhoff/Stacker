using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            FileTimer = new Timer(ReadOrders, null, 0, 10000);
            OrdersDataGrid.ItemsSource = Orders;
        }
        
        private void ReadOrders(object obj)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(OrdersFile, FileMode.Open);
                using (StreamReader sr = new StreamReader(fs,System.Text.Encoding.Default))
                {
                    string[] lines = sr.ReadToEnd().TrimEnd('\r', '\n').Split('\n');
                    foreach (string str in lines)
                    {
                        Order o = null;
                        try
                        {
                            Console.WriteLine(str);
                            o = new Order(str);
                        }
                        catch (ArgumentException e)
                        {
                            MessageBox.Show(str,"Неверный формат строки заказа");
                            //MessageBox.Show(e.Message);
                        }
                        if ((o != null) && !Orders.Contains(o))
                        {
                            Orders.Add(o);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (fs != null) fs.Dispose();
            }
              
        }

        private void SaveAndDeleteOrder(Order order,string res)
        {
            int orderIndex = Orders.IndexOf(order);
            try
            {
                File.AppendAllText(ArchiveFile,
                    DateTime.Now.ToString() + " : " + order.OriginalString + " - " + res + '\r' + '\n',
                        System.Text.Encoding.Default);

                string[] strings = File.ReadAllLines(OrdersFile, System.Text.Encoding.Default).
                    Where(v => v.TrimEnd('\r', '\n').IndexOf(order.OriginalString) == -1).ToArray();

                File.WriteAllLines(OrdersFile, strings, System.Text.Encoding.Default);

                Orders.Remove(order);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            SaveAndDeleteOrder(Orders[1], "done");
        }
    }
}
