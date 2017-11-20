using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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

        //места хранения файлов
        //private string OrdersFile = @"d:\WORK\Stacker\Orders\instr_exp.txt"; //путь к файлу с заявками
        //private string ArchiveFile = @"d:\WORK\Stacker\Orders\instr_imp.txt"; //путь к файлу с отработанными заявками
        private string OrdersFile = @"D:\БЕРТА\БЕРТА Сухарев\Projects\Stacker\Orders\instr_exp.txt"; //путь к файлу с заявками
        private string ArchiveFile = @"D:\БЕРТА\БЕРТА Сухарев\Projects\Stacker\Orders\instr_imp.txt"; //путь к файлу с отработанными заявками

        // переменная для контроля изменения файла заявок
        DateTime LastOrdersFileAccessTime = DateTime.Now;
        // таймер для контроля изменения файла заявок
        Timer FileTimer;
        delegate void RefreshList();

        //коллекция заявок
        List<Order> Orders= new List<Order>();
        
                 
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FileTimer = new Timer(ReadOrdersFile, null, 0, 10000);
            GridSetUp();
            CellsGrid LeftStacker = new CellsGrid(30, 20);
            int x = LeftStacker[0, 0].X;
        }
        
        //метод настройки вида списка
        private void GridSetUp()
        {
            GridView OrdersGridView = new GridView();
            GridViewColumn gvc1 = new GridViewColumn();
            GridViewColumn gvc2 = new GridViewColumn();
            GridViewColumn gvc3 = new GridViewColumn();
            GridViewColumn gvc4 = new GridViewColumn();
            GridViewColumn gvc5 = new GridViewColumn();
            GridViewColumn gvc6 = new GridViewColumn();
            gvc1.Header = "Тип";
            gvc1.DisplayMemberBinding = new Binding("OrderType");
            gvc2.Header = "Номер заказа";
            gvc2.DisplayMemberBinding = new Binding("OrderNumber");
            gvc3.Header = "Кодовое обозначение";
            gvc3.DisplayMemberBinding = new Binding("ProductCode");
            gvc4.Header = "Описание";
            gvc4.DisplayMemberBinding = new Binding("ProductDescription");
            gvc5.Header = "Кол-во";
            gvc5.DisplayMemberBinding = new Binding("Amount");
            gvc6.Header = "Ячейка";
            gvc6.DisplayMemberBinding = new Binding("Address");
            OrdersGridView.Columns.Add(gvc1);
            OrdersGridView.Columns.Add(gvc2);
            OrdersGridView.Columns.Add(gvc3);
            OrdersGridView.Columns.Add(gvc4);
            OrdersGridView.Columns.Add(gvc5);
            OrdersGridView.Columns.Add(gvc6);
            OrdersLitsView.View = OrdersGridView;
            OrdersLitsView.ItemsSource = Orders;

        }

        //метода проверки и чтения заявок из файла
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
                    Dispatcher.Invoke(new RefreshList( () =>  OrdersLitsView.Items.Refresh()));
                }
            }
            catch (ArgumentException ae)
            {
                FileTimer.Dispose();
                MessageBox.Show(messageBoxText: "Чтение заявок приостановлено!", caption: ae.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        
        //метод сохранения отработанной заявки в архиве и удаления из исходного файла 
        //и коллекции заявок
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
            int i = OrdersLitsView.SelectedIndex;
            if (i != -1)
            {
                SaveAndDeleteOrder(Orders[i], "done");
                OrdersLitsView.Items.Refresh();
            }
        }
    }
}
