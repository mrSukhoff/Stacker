using Modbus.Device;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;

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
        //нулевая позиция по горизонтали - место погрузки
        int StackerDepth = 30;
        int StackerHight = 16;
        char LeftRackName;
        int LeftRackNumber;
        char RightRackName;
        int RightRackNumber;

        // переменная для контроля изменения файла заявок
        DateTime LastOrdersFileAccessTime = DateTime.Now;

        // таймер для контроля изменения файла заявок
        Timer FileTimer;
        delegate void RefreshList();

        //Таймер для чтения слова состояния контроллера
        Timer PlcTimer;
        delegate void WriteStateWord();
        delegate void WriteLabel();

        //коллекция заявок
        List<Order> Orders = new List<Order>();

        //Координаты ячеек
        CellsGrid LeftStacker;
        CellsGrid RightStacker;

        //Максимальные значения координат
        const int MaxX = 55000;
        const int MaxY = 14000;
        //формат ввода координат в textbox'ы
        Regex CoordinateRegex = new Regex(@"\d");

        //Com-порт к которому подсоединен контроллер
        private SerialPort ComPort = null;
        private IModbusMaster PLC;

        //Слово состояния контроллера
        int StateWord;

        //Кнопка, выдавшая задание)
        object bt = null;
        delegate void ChangeButtonState();

        //##########################################################################################################################
        //Основная точка входа ----------------------------------------------------------------------------------------------------!
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

            //Открываем порт и создаем контроллер
            try
            {
                ComPort.Open();
                PLC = ModbusSerialMaster.CreateAscii(ComPort);
                //временно включаем ручной режим
                WriteDword(PLC, 8, 1);
                //Записываем максимальные значения координат
                WriteDword(PLC, 10, MaxX);
                WriteDword(PLC, 12, MaxY);
                //и максимальные значения ячеек
                WriteDword(PLC, 14, 29);
                WriteDword(PLC, 16, 16);
                PlcTimer = new Timer(ReadStateWord, null, 0, 500);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "Ошибка открытия порта");
            }

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
                LeftRackName = Convert.ToChar(manager.GetPrivateString("Stacker", "LeftRackName"));
                LeftRackNumber = Convert.ToInt16(manager.GetPrivateString("Stacker", "LeftRackNumber"));
                RightRackName = Convert.ToChar(manager.GetPrivateString("Stacker", "RightRackName"));
                RightRackNumber = Convert.ToInt16(manager.GetPrivateString("Stacker", "RightRackNumber"));
                string port = manager.GetPrivateString("PLC", "ComPort");
                ComPort = new SerialPort(port, 115200, Parity.Even, 7, StopBits.One);
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
                    new CellsGrid(path + "\\LeftStack.cell") : new CellsGrid(StackerDepth, StackerHight);
            RightStacker = File.Exists(path + "\\RightStack.cell") ?
                    new CellsGrid(path + "\\RightStack.cell") : new CellsGrid(StackerDepth, StackerHight);
        }

        //Настраиваем визуальные компоненты
        private void SetUpButtons()
        {
            //Подписываем кнопки рядов
            LeftRackManualButton.Content = LeftRackName;
            RightRackManualButton.Content = RightRackName;

            //Заполняем combobox'ы номерами рядов
            int[] rowItems = new int[StackerDepth - 1];
            for (int i = 0; i < rowItems.Length; i++) { rowItems[i] = i + 1; }
            RowManualComboBox.ItemsSource = rowItems;
            RowComboBox.ItemsSource = rowItems;
            RowManualComboBox.SelectedIndex = 0;
            RowComboBox.SelectedIndex = 0;

            // .. и этажей
            int[] floorItems = new int[StackerHight];
            for (int i = 0; i < floorItems.Length; i++) { floorItems[i] = i + 1; }
            FloorManualCombobox.ItemsSource = floorItems;
            FloorComboBox.ItemsSource = floorItems;
            FloorManualCombobox.SelectedIndex = 0;
            FloorComboBox.SelectedIndex = 0;

            RackComboBox.Items.Add(LeftRackName);
            RackComboBox.Items.Add(RightRackName);
            RackComboBox.SelectedIndex = 0;

            //присваеваем обработчики тут, а не в визуальной части, чтобы они не вызывались 
            //во время первоначальных настроек
            LeftRackManualButton.Checked += LeftRackManualButton_Click;
            LeftRackManualButton.Unchecked += LeftRackManualButton_Click;
            RightRackManualButton.Unchecked += RightRackManualButton_Click;
            RightRackManualButton.Checked += RightRackManualButton_Click;

            RowManualComboBox.SelectionChanged += ManualComboBox_SelectionChanged;
            FloorManualCombobox.SelectionChanged += ManualComboBox_SelectionChanged;
            RackComboBox.SelectionChanged += CellChanged;
            RowComboBox.SelectionChanged += CellChanged;
            FloorComboBox.SelectionChanged += CellChanged;
            CoordinateXTextBox.TextChanged += CoordinateChanged;
            CoordinateYTextBox.TextChanged += CoordinateChanged;
            IsNOTAvailableCheckBox.Click += IsNOTAvailableCheckBox_Click;

            //дефолтные методы обработки нажатия кнопок перемещения манипулятора в ручном режиме
            FartherButton.PreviewMouseLeftButtonUp += FartherButton_PreviewMouseLeftButtonUp;
            FartherButton.PreviewMouseLeftButtonDown += FartherButton_PreviewMouseLeftButtonDown;
            CloserButton.PreviewMouseLeftButtonUp += CloserButton_PreviewMouseLeftButtonUp;
            CloserButton.PreviewMouseLeftButtonDown += CloserButton_PreviewMouseLeftButtonDown;
            UpButton.PreviewMouseLeftButtonUp += UpButton_PreviewMouseLeftButtonUp;
            UpButton.PreviewMouseLeftButtonDown += UpButton_PreviewMouseLeftButtonDown;
            DownButton.PreviewMouseLeftButtonUp += DownButton_PreviewMouseLeftButtonUp;
            DownButton.PreviewMouseLeftButtonDown += DownButton_PreviewMouseLeftButtonDown;

            //включаем кнопку левого ряда
            LeftRackManualButton.IsChecked = true;
            LeftRackManualButton_Click(null, null);
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
            gvc1.Header = " Тип ";
            gvc1.DisplayMemberBinding = new Binding("OrderType");
            gvc2.Header = " Номер заказа ";
            gvc2.DisplayMemberBinding = new Binding("OrderNumber");
            gvc3.Header = " Кодовое обозначение ";
            gvc3.DisplayMemberBinding = new Binding("ProductCode");
            gvc4.Header = " Описание ";
            gvc4.DisplayMemberBinding = new Binding("ProductDescription");
            gvc5.Header = " Кол-во ";
            gvc5.DisplayMemberBinding = new Binding("Amount");
            gvc6.Header = " Ячейка ";
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
                        Order o = new Order(str, LeftRackName, LeftRackNumber, RightRackName, RightRackNumber);
                        if ((!Orders.Contains(o)) && (o.StackerName != '?')) Orders.Add(o);
                    }
                    LastOrdersFileAccessTime = File.GetLastWriteTime(OrdersFile);
                    Dispatcher.Invoke(new RefreshList(() => OrdersLitsView.Items.Refresh()));
                }
            }
            catch (ArgumentException ae)
            {
                FileTimer.Dispose();
                MessageBox.Show(messageBoxText: "Чтение заявок остановлено! " + ae.Message, caption: "ReadOrdersFile");
            }
            catch (Exception ex)
            {
                FileTimer.Dispose();
                MessageBox.Show(ex.Message, caption: "ReadOrdersFile");
            }
        }

        //метод сохранения отработанной заявки в архиве и удаления из исходного файла и коллекции заявок
        private void SaveAndDeleteOrder(Order order, string res)
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
                MessageBox.Show(ex.Message, caption: "Save&DeleteOrder");
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

        //Метод записывает массивы ячеек в файлы
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
                CoordinateXTextBox.Text = CoordinateXTextBox.Text == "" ? "0" : CoordinateXTextBox.Text;
                CoordinateYTextBox.Text = CoordinateYTextBox.Text == "" ? "0" : CoordinateYTextBox.Text;
                int x = Convert.ToInt32(CoordinateXTextBox.Text);
                int y = Convert.ToInt32(CoordinateYTextBox.Text);
                if (x > MaxX)
                {
                    x = MaxX;
                    CoordinateXTextBox.Text = MaxX.ToString();
                }
                if (y > MaxY)
                {
                    y = MaxY;
                    CoordinateYTextBox.Text = MaxY.ToString();
                }
                stacker[row, floor].X = x;
                stacker[row, floor].Y = y;
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
        private void RightRackManualButton_Click(object sender, RoutedEventArgs e)
        {
            if (RightRackManualButton.IsChecked == true)
            {
                RightRackManualButton.Effect = null;
                LeftRackManualButton.IsChecked = false;
            }
            else
            {
                LeftRackManualButton.IsChecked = true;
                LeftRackManualButton.Effect = null;
            }
            ManualComboBox_SelectionChanged(sender, null);
        }
        private void LeftRackManualButton_Click(object sender, RoutedEventArgs e)
        {
            if (LeftRackManualButton.IsChecked == true)
            {
                LeftRackManualButton.Effect = null;
                RightRackManualButton.IsChecked = false;
            }
            else
            {
                RightRackManualButton.IsChecked = true;
                RightRackManualButton.Effect = null;
            }
            ManualComboBox_SelectionChanged(sender, null);
        }

        //метод проверяет вводимы в textbox символы на соотвктствие правилам
        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Match match = CoordinateRegex.Match(e.Text);
            if ((!match.Success) || (sender as TextBox).Text.Length > 5) e.Handled = true;
        }

        //при изменении выбранных ячеек в ручном режиме меняет доступность кнопок в зависимости от доступности ячейки
        private void ManualComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                CellsGrid stacker = LeftRackManualButton.IsChecked == true ? LeftStacker : RightStacker;
                int r = RowManualComboBox.SelectedIndex;
                int f = FloorManualCombobox.SelectedIndex;
                bool isEnabled = !stacker[r, f].IsNotAvailable;
                BringManualButton.IsEnabled = isEnabled;
                TakeAwayManualButton.IsEnabled = isEnabled;
                ManualAddressLabel.IsEnabled = isEnabled;
                char rack = LeftRackManualButton.IsChecked == true ? LeftRackName : RightRackName;
                r++;f++;
                ManualAddressLabel.Content = rack + " - " + r.ToString() + " - " + f.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "ManualComboBox_SelectionChanged");
            }
        }

        //завершение работы программы
        private void Stacker_Closed(object sender, EventArgs e)
        {
            if (FileTimer != null) FileTimer.Dispose();
            if (PLC != null) PLC.Dispose();
            if (ComPort != null) ComPort.Dispose();
        }

        //метод записывает 32-битное число в контроллер
        public bool WriteDword(IModbusMaster plc, int adr, int d)
        {
            try
            {
                ushort dlo = (ushort)(d % 0x10000);
                ushort dhi = (ushort)(d / 0x10000);
                UInt16 address = Convert.ToUInt16(adr);
                address += 0x1000;
                plc.WriteSingleRegister(1, address, dlo);
                plc.WriteSingleRegister(1, ++address, dhi);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "WriteDwordToPLC");
                return false;
            }
        }

        //метод читает 32-битное число из контроллера
        public bool ReadDword(IModbusMaster plc, ushort address, out int d)
        {
            try
            {
                d = 0;
                address += 0x1000;
                ushort[] x = plc.ReadHoldingRegisters(1, address, 2);
                d = x[0] + x[1] * 0x10000;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "ReadDwordFromPLC");
                d = 0;
                return false;
            }
        }

        //метод читает меркер из ПЛК
        public bool ReadMerker(IModbusMaster plc, ushort address, out bool m)
        {
            try
            {
                bool[] ms;
                address += 0x800;
                ms = plc.ReadCoils(1, address, 1);
                m = ms[0];
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "ReadMerker");
                m = false;
                return false;
            }
        }

        //метод устанавливает меркер в ПЛК
        public bool SetMerker(IModbusMaster plc, ushort address, bool m)
        {
            try
            {
                address += 0x800;
                plc.WriteSingleCoil(1, address, m);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "SetMerker");
                return false;
            }
        }

        //Обработчик нажатия кнопки подтверждения ошибки
        private void SubmitErrorButton_Click(object sender, RoutedEventArgs e)
        {
            if (PLC != null) SetMerker(PLC, 101, true);
        }

        //Обработчик нажатия кнопки "ближе"
        private void CloserButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PLC != null)
            {
                WriteDword(PLC, 8, 1);
                SetMerker(PLC, 11, true);
            }
        }
        private void CloserButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PLC != null) SetMerker(PLC, 11, false);
        }

        //Обработчик нажатия кнопки "дальше"
        private void FartherButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PLC != null)
            {
                WriteDword(PLC, 8, 1);
                SetMerker(PLC, 10, true);
            }
        }
        private void FartherButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PLC != null) SetMerker(PLC, 10, false);
        }

        //Обработчик нажатия кнопки "вверх"
        private void UpButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PLC != null)
            {
                WriteDword(PLC, 8, 1);
                SetMerker(PLC, 12, true);
            }
        }
        private void UpButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PLC != null) SetMerker(PLC, 12, false);
        }

        //Обработчик нажатия кнопки "вниз"
        private void DownButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PLC != null)
            {
                WriteDword(PLC, 8, 1);
                SetMerker(PLC, 13, true);
            }
        }
        private void DownButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PLC != null) SetMerker(PLC, 13, false);
        }

        //Обработчик нажатия кнопки "платформа влево"
        private void ManPlatformToLeftButton_Checked(object sender, RoutedEventArgs e)
        {
            if (PLC != null)
            {
                //включаем ручной режим
                WriteDword(PLC, 8, 1);
                SetMerker(PLC, 14, true);
                bt = sender;
                (bt as ButtonBase).IsEnabled = true;
            }
        }

        //Обработчик нажатия кнопки платформа "вправо вправо"
        private void ManPlatformToRightButton_Checked(object sender, RoutedEventArgs e)
        {
            if (PLC != null)
            {
                //включаем ручной режим
                WriteDword(PLC, 8, 1);
                SetMerker(PLC, 15, true);
                bt = sender;
                (bt as ButtonBase).IsEnabled = true;
            }
        }

        //Обработчик нажатия кнопки STOP
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (PLC != null) SetMerker(PLC, 0, true);
        }

        //Обработчик нажатия кнопки "Перейти на координаты"
        private void GotoButton_Click(object sender, RoutedEventArgs e)
        {
            //ReadMerker(PLC, 20, out bool m);
            if (PLC!=null)
            {
                int x = Convert.ToUInt16(GotoXTextBox.Text);
                int y = Convert.ToUInt16(GotoYTextBox.Text);
                x = x > MaxX ? MaxX : x;
                y = y > MaxY ? MaxY : y;
                //Включаем режим перемещения по координатам
                WriteDword(PLC, 8, 3);
                WriteDword(PLC, 0, x);
                WriteDword(PLC, 2, y);
                SetMerker(PLC, 20, true);
                bt = sender;
                (bt as ButtonBase).IsEnabled = true;
            }
        }

        //по таймеру читаем слово состояния контроллера
        private void ReadStateWord(object ob)
        {
            if (PLC != null)
            {
                try
                {
                    ReadDword(PLC, 100, out int word);
                    //word = Convert.ToUInt16(word);
                    string lbltxt = Convert.ToString(word, 2) + " ";
                    while (lbltxt.Length < 16) { lbltxt = "0" + lbltxt; }
                    Dispatcher.Invoke(new WriteStateWord(() => StateWordLabel.Content = "State Word: " + lbltxt));

                    //ushort[] d = PLC.ReadHoldingRegisters(1, 0x1408, 2);
                    ReadDword(PLC, 408, out word);
                    string t = Convert.ToString(word);
                    while (t.Length < 7) { t = "0" + t; }
                    Dispatcher.Invoke(new WriteLabel(() => XLabel.Content = "X: " + t));
                    //Dispatcher.Invoke(new WriteLabel(() => CoordinateXTextBox.Text = t));

                    ReadDword(PLC, 410, out word);
                    t = Convert.ToString(word);
                    while (t.Length < 7) { t = "0" + t; }
                    Dispatcher.Invoke(new WriteLabel(() => YLabel.Content = "Y: " + t));
                    //Dispatcher.Invoke(new WriteLabel(() => CoordinateYTextBox.Text = t));

                    ReadDword(PLC, 412, out word);
                    t = Convert.ToString(word);
                    while (t.Length < 2) { t = "0" + t; }
                    Dispatcher.Invoke(new WriteLabel(() => RowLabel.Content = "R: " + t));

                    ReadDword(PLC, 414, out word);
                    t = Convert.ToString(word);
                    while (t.Length < 2) { t = "0" + t; }
                    Dispatcher.Invoke(new WriteLabel(() => FloorLabel.Content = "F: " + t));
                    if ((bt != null) && ((word & 0x80) == 0x80))
                    {
                        //Dispatcher.Invoke(new ChangeButtonState(()=> (bt as ButtonBase).IsEnabled = true));
                        MessageBox.Show(bt.ToString());
                        bt = null;
                    }
                    StateWord = word;
                }
                catch (Exception ex)
                {
                    PlcTimer.Dispose();
                    MessageBox.Show(ex.Message, caption: "ReadStateWord");
                }
            }
        }

        //кнопки сброса значений в TextBox на ноль
        private void XResButton_Click(object sender, RoutedEventArgs e)
        {
            GotoXTextBox.Text = "0";
        }
        private void YResButton_Click(object sender, RoutedEventArgs e)
        {
            GotoYTextBox.Text = "0";
        }

        //При клике по кнопке движение до следующего ряда
        private void FartherButton_NextLine(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PLC != null)
            {
                WriteDword(PLC, 8, 4);
                SetMerker(PLC, 10, true);
            }
        }
        private void CloserButton_NextLine(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PLC != null)
            {
                WriteDword(PLC, 8, 4);
                SetMerker(PLC, 11, true);
            }
        }
        private void UpButton_NextLine(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PLC != null)
            {
                WriteDword(PLC, 8, 4);
                SetMerker(PLC, 12, true);
            }
        }
        private void DownButton_NextLine(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PLC != null)
            {
                WriteDword(PLC, 8, 4);
                SetMerker(PLC, 13, true);
            }
        }


        //В зависимости от состояния чекбокса выбираем действия кнопок
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            FartherButton.PreviewMouseLeftButtonUp -= FartherButton_PreviewMouseLeftButtonUp;
            FartherButton.PreviewMouseLeftButtonDown -= FartherButton_PreviewMouseLeftButtonDown;

            CloserButton.PreviewMouseLeftButtonUp -= CloserButton_PreviewMouseLeftButtonUp;
            CloserButton.PreviewMouseLeftButtonDown -= CloserButton_PreviewMouseLeftButtonDown;

            UpButton.PreviewMouseLeftButtonUp -= UpButton_PreviewMouseLeftButtonUp;
            UpButton.PreviewMouseLeftButtonDown -= UpButton_PreviewMouseLeftButtonDown;

            DownButton.PreviewMouseLeftButtonUp -= DownButton_PreviewMouseLeftButtonUp;
            DownButton.PreviewMouseLeftButtonDown -= DownButton_PreviewMouseLeftButtonDown;

            FartherButton.PreviewMouseLeftButtonDown += FartherButton_NextLine;
            CloserButton.PreviewMouseLeftButtonDown += CloserButton_NextLine;
            UpButton.PreviewMouseLeftButtonDown += UpButton_NextLine;
            DownButton.PreviewMouseLeftButtonDown += DownButton_NextLine;
        }
        private void LineMotionCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            FartherButton.PreviewMouseLeftButtonDown -= FartherButton_NextLine;
            CloserButton.PreviewMouseLeftButtonDown -= CloserButton_NextLine;
            UpButton.PreviewMouseLeftButtonDown -= UpButton_NextLine;
            DownButton.PreviewMouseLeftButtonDown -= DownButton_NextLine;

            FartherButton.PreviewMouseLeftButtonUp += FartherButton_PreviewMouseLeftButtonUp;
            FartherButton.PreviewMouseLeftButtonDown += FartherButton_PreviewMouseLeftButtonDown;

            CloserButton.PreviewMouseLeftButtonUp += CloserButton_PreviewMouseLeftButtonUp;
            CloserButton.PreviewMouseLeftButtonDown += CloserButton_PreviewMouseLeftButtonDown;

            UpButton.PreviewMouseLeftButtonUp += UpButton_PreviewMouseLeftButtonUp;
            UpButton.PreviewMouseLeftButtonDown += UpButton_PreviewMouseLeftButtonDown;

            DownButton.PreviewMouseLeftButtonUp += DownButton_PreviewMouseLeftButtonUp;
            DownButton.PreviewMouseLeftButtonDown += DownButton_PreviewMouseLeftButtonDown;


        }

        //метод обрабатывает нажатие кнопки "привезти"
        private void BringManualButton_Click(object sender, RoutedEventArgs e)
        {
            if (PLC != null)
            {
                CellsGrid stacker = LeftRackManualButton.IsChecked == true ? LeftStacker : RightStacker;
                int r = RowManualComboBox.SelectedIndex;
                int f = FloorManualCombobox.SelectedIndex;
                int x = stacker[r, f].X;
                int y = stacker[r, f].Y;
                r++;f++;
                bool side = RightRackManualButton.IsChecked == true;

                //Включаем режим перемещения по координатам
                WriteDword(PLC, 8, 2);
                //Пишем координаты
                WriteDword(PLC, 0, x);
                WriteDword(PLC, 2, y);
                //Пишем ряд и этаж
                WriteDword(PLC, 4, r);
                WriteDword(PLC, 6, f);
                //Устанваливаем сторону
                SetMerker(PLC, 2, side);
                //Устанавливаем флаг в "привезти"
                SetMerker(PLC, 3, true);
                //Даем команду на старт
                SetMerker(PLC, 1, true);
                bt = sender;
                (bt as ButtonBase).IsEnabled = true;
            }
        }

        //метод обрабатывает нажатие кнопки "увезти"
        private void TakeAwayManualButton_Click(object sender, RoutedEventArgs e)
        {
            if (PLC != null)
            {
                CellsGrid stacker = LeftRackManualButton.IsChecked == true ? LeftStacker : RightStacker;
                int r = RowManualComboBox.SelectedIndex;
                int f = FloorManualCombobox.SelectedIndex;
                int x = stacker[r, f].X;
                int y = stacker[r, f].Y;
                r++; f++;
                bool side = RightRackManualButton.IsChecked == true;

                //Включаем режим перемещения по координатам
                WriteDword(PLC, 8, 2);
                //Пишем координаты
                WriteDword(PLC, 0, x);
                WriteDword(PLC, 2, y);
                //Пишем ряд и этаж
                WriteDword(PLC, 4, r);
                WriteDword(PLC, 6, f);
                //Устанваливаем сторону
                SetMerker(PLC, 2, side);
                //Устанавливаем флаг в "увезти"
                SetMerker(PLC, 3, false);
                //Даем команду на старт
                SetMerker(PLC, 1, true);
                bt = sender;
                (bt as Button).IsEnabled = true;
            }
        }



    }






}
