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

        //private string OrdersFile = @"d:\WORK\Stacker\Orders\instr_exp.txt"; //путь к файлу с заявками
        //private string ArchiveFile = @"d:\WORK\Stacker\Orders\instr_imp.txt"; //путь к файлу с отработанными заявками
        private string OrdersFile = @"D:\БЕРТА\БЕРТА Сухарев\Projects\Stacker\Orders\instr_exp.txt"; //путь к файлу с заявками
        private string ArchiveFile = @"D:\БЕРТА\БЕРТА Сухарев\Projects\Stacker\Orders\instr_imp.txt"; //путь к файлу с отработанными заявками
        
        DateTime LastOrdersFileAccessTime = DateTime.Now;
        Timer FileTimer;

        List<Order> Orders= new List<Order>();
                
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FileTimer = new Timer(ReadOrdersFile, null, 0, 10000);
            OrdersLitsView.ItemsSource = Orders;
            testListView.ItemsSource = Orders;
        }
        
        private void ReadOrdersFile(object ob)
        {
            try
            {
                if (File.GetLastWriteTime(OrdersFile) != LastOrdersFileAccessTime)
                {

                    string[] lines = File.ReadAllLines(OrdersFile, System.Text.Encoding.Default);
                    foreach (string str in lines)
                    {
                        Order o = new Order(str);
                        if (!Orders.Contains(o)) Orders.Add(o);
                    }
                    LastOrdersFileAccessTime = File.GetLastWriteTime(OrdersFile);
                }
            }
            catch (ArgumentException ae)
            {
                FileTimer.Dispose();
                MessageBox.Show(messageBoxText: "Чтение заявок приостановлено!", caption: ae.Message);
            }
            catch (Exception ex)
            {
                FileTimer.Dispose();
                MessageBox.Show(ex.Message);
            }
        }

        private void SaveAndDeleteOrder(Order order,string res)
        {
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
