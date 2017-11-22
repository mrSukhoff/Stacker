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

        //места хранения файлов заявлок и архива
        private string OrdersFile;
        private string ArchiveFile;

        //размеры, имена и номера штабелеров
        int StackerDepth=0;
        int StackerHight=0;
        char LeftRackName;
        int LeftRackNumber;
        char RightRackName;
        int RightRackNumber;

        //порт к которому подключен контроллер
        string ComPort;

        // переменная для контроля изменения файла заявок
        DateTime LastOrdersFileAccessTime = DateTime.Now;
        // таймер для контроля изменения файла заявок
        Timer FileTimer;
        delegate void RefreshList();

        //коллекция заявок
        List<Order> Orders= new List<Order>();
        //Координаты ячеек
        CellsGrid LeftStacker;
        CellsGrid RightStacker;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Читаем первоначальные настройки
            ReadINISettings();
            //Загружаем таблицы координат ячеек
            LoadCellGrid();
            //Настраиваем визуальные компоненты
            SetUpButtons();
            //Настраиваем вид списка заявок
            GridSetUp();
            //Запускаем таймер для проверки изменений списка заявок
            FileTimer = new Timer(ReadOrdersFile, null, 0, 10000);
            
        }

        //Читаем первоначальные настройки
        private void ReadINISettings()
        {
            string path = Environment.CurrentDirectory + "\\Stacker.ini";
            try
            {
                INIManager manager = new INIManager(path);
                OrdersFile = manager.GetPrivateString("General", "OrderFile");
                ArchiveFile = manager.GetPrivateString("General", "ArchiveFile");
                StackerDepth = Convert.ToInt16(manager.GetPrivateString("Stacker", "Depth").TrimEnd());
                StackerHight = Convert.ToInt16(manager.GetPrivateString("Stacker", "Hight"));
                LeftRackName = Convert.ToChar(manager.GetPrivateString("Stacker", "LeftRackName"));
                LeftRackNumber = Convert.ToInt16(manager.GetPrivateString("Stacker", "LeftRackNumber"));
                RightRackName = Convert.ToChar(manager.GetPrivateString("Stacker", "RightRackName"));
                RightRackNumber = Convert.ToInt16(manager.GetPrivateString("Stacker", "RightRackNumber"));
                ComPort = manager.GetPrivateString("PLC", "ComPort");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "ReadINISettings");
            }
        }

        //Загружаем таблицы координат ячеек
        private void LoadCellGrid()
        {
            string path = Environment.CurrentDirectory;
            LeftStacker = File.Exists(path + "\\LeftStack.cell") ? 
                    new CellsGrid(path + "\\LeftStack.cell") : new CellsGrid(StackerDepth,StackerHight);
            RightStacker = File.Exists(path + "\\RightStack.cell") ?
                    new CellsGrid(path + "\\RightStack.cell") : new CellsGrid(StackerDepth, StackerHight);
        }

        //Настраиваем визуальные компоненты
        private void SetUpButtons()
        {
            LeftRackManualButton.Content = LeftRackName;
            RightRackManualButton.Content = RightRackName;

            int[] rowItems = new int[StackerDepth];
            for (int i=0; i<rowItems.Length;i++) { rowItems[i] = i; }
            RowManualComboBox.ItemsSource = rowItems;
            RowComboBox.ItemsSource = rowItems; 
            RowManualComboBox.SelectedIndex = 0;
            RowComboBox.SelectedIndex = 0;

            int[] floorItems = new int[StackerHight];
            for (int i = 0; i < floorItems.Length; i++) { floorItems[i] = i; }
            FloorManualCombobox.ItemsSource = floorItems;
            FloorComboBox.ItemsSource = floorItems;
            FloorManualCombobox.SelectedIndex = 0;
            FloorComboBox.SelectedIndex = 0;

            RackComboBox.Items.Add(LeftRackName);
            RackComboBox.Items.Add(RightRackName);
            RackComboBox.SelectedIndex = 0;
            //присваеваем обработчик тут, а не в визуальной части, чтобы он не вызывался 
            //во времы первоначальных настроек
            RackComboBox.SelectionChanged += CellChanged;
            RowComboBox.SelectionChanged += CellChanged;
            FloorComboBox.SelectionChanged += CellChanged;
            CoordinateXTextBox.TextChanged += CoordinateChanged;
            CoordinateYTextBox.TextChanged += CoordinateChanged;
            IsNOTAvailableCheckBox.Click += IsNOTAvailableCheckBox_Click;

        }

        //метод настройки вида списка заявок
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
                        Order o = new Order(str,LeftRackName,LeftRackNumber,RightRackName,RightRackNumber);
                        if ((!Orders.Contains(o)) && (o.StackerName != '?')) Orders.Add(o);
                    }
                    LastOrdersFileAccessTime = File.GetLastWriteTime(OrdersFile);
                    Dispatcher.Invoke(new RefreshList( () =>  OrdersLitsView.Items.Refresh()));
                }
            }
            catch (ArgumentException ae)
            {
                FileTimer.Dispose();
                MessageBox.Show(messageBoxText: "Чтение заявок остановлено! " + ae.Message, caption: "ReadOrdersFile");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "ReadOrdersFile");
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
                MessageBox.Show(ex.Message, caption:"Save&DeleteOrder");
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LeftStacker.SaveCellsGrid("LeftStack.cell");
                RightStacker.SaveCellsGrid("RightStack.cell");
            }
           catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //метод при изменении адреса ячеек перечитывает координаты
        private void CellChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                CellsGrid stacker;
                stacker = RackComboBox.SelectedIndex == 0 ? LeftStacker : RightStacker;
                int row = RowComboBox.SelectedIndex;
                int floor = FloorComboBox.SelectedIndex;

                CoordinateXTextBox.TextChanged -= CoordinateChanged;
                CoordinateYTextBox.TextChanged -= CoordinateChanged;
                IsNOTAvailableCheckBox.Click -= IsNOTAvailableCheckBox_Click;

                CoordinateXTextBox.Text = stacker[row, floor].X.ToString();
                CoordinateYTextBox.Text = stacker[row, floor].Y.ToString();
                IsNOTAvailableCheckBox.IsChecked = stacker[row, floor].IsNotAvailable;

                CoordinateXTextBox.TextChanged += CoordinateChanged;
                CoordinateYTextBox.TextChanged += CoordinateChanged;
                IsNOTAvailableCheckBox.Click += IsNOTAvailableCheckBox_Click;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "CellChanged");
            }
        }
        
        //методы перезаписывают координаты в cellgrid
        private void CoordinateChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                CellsGrid stacker;
                stacker = RackComboBox.SelectedIndex == 0 ? LeftStacker : RightStacker;
                int row = RowComboBox.SelectedIndex;
                int floor = FloorComboBox.SelectedIndex;

                stacker[row, floor].X = Convert.ToInt32(CoordinateXTextBox.Text);
                stacker[row, floor].Y = Convert.ToInt32(CoordinateYTextBox.Text);
                stacker[row, floor].IsNotAvailable = (bool)IsNOTAvailableCheckBox.IsChecked;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "CoordinateChanged");
            }
        }
        private void IsNOTAvailableCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CoordinateChanged(sender, null);
        }

        //методы отжимают) противоположную кнопку
        private void LeftRackManualButton_Checked(object sender, RoutedEventArgs e) => RightRackManualButton.IsChecked = false;
        private void RightRackManualButton_Checked(object sender, RoutedEventArgs e) => LeftRackManualButton.IsChecked = false;
    }
}
