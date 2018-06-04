
using Stacker.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;


namespace Stacker
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        //модель паттерна MVP
        StackerModel Model;

        //хранилище настроек
        SettingsKeeper Settings;

        //менеджер заявок
        OrdersManager OrderManager;

        //формат ввода координат в textbox'ы
        Regex CoordinateRegex = new Regex(@"\d");

        //Список кнопок, выдавших задание и заблокированных
        List<Button> ButtonList = new List<Button>();

        //для рисования графика веса
        Polyline WeightPolyline = new Polyline();
        PointCollection WeightPointCollection = new PointCollection();
        int c = 0;
        Polyline MeasuredWeightPolyline1 = new Polyline();
        Polyline MeasuredWeightPolyline2 = new Polyline();
        PointCollection MeasuredWeight1PointCollection = new PointCollection();
        PointCollection MeasuredWeight2PointCollection = new PointCollection();

        //стиль оттображения списка заявок
        GridView OrdersGridView = new GridView();

        //флаг закрытия неуправляемых ресурсов
        bool disposed = false;

        //направления сортировки списка заявок
        bool[] SortDirection = new bool[6];

        const string Header1 = " Тип ";
        const string Header2 = " Номер заказа ";
        const string Header3 = " Описание ";
        const string Header4 = " Кол-во ";
        const string Header5 = " Ячейка ";

        //#####################################################################################################
        //Основная точка входа -------------------------------------------------------------------------------!
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Создаем модель
            Model = new StackerModel();
            
            //инициализируем менеджер заявок
            OrderManager = Model.OrderManager;
            //Определяем его источником данных для списка
            OrdersLitsView.ItemsSource = OrderManager.Orders;

            //инициализируем хранилище настроек
            Settings = Model.Settings;

            //Настраиваем вид списка заявок
            ListViewSetUp();
            //Настраиваем визуальные компоненты
            SetUpComponents();

            if (Model.IsConnected)
            {
                //подписываемся на события модели
                Model.CraneState.CommandDone += CommandDone;
                Model.CraneState.ErrorAppeared += ErrorAppeared;
                Model.CraneState.CoordinateReaded += UpdateCoordinate;
                Model.CraneState.StateWordChanged += SomethingChanged;
                //источник данных для списка ошибок
                ErrorListBox.ItemsSource = Model.CraneState.ErrorList;
                //проверяем при старте наличие ящика на платформе и устанавливаем активные кнопки
                bool isBin = Model.Crane.ChekBinOnPlatform();
                TakeAwaySemiAutoButton.IsEnabled = isBin;
                BringSemiAutoButton.IsEnabled = !isBin;
                BringAutoButton.IsEnabled = !isBin;
            }
            else if (!Settings.CloseOrInform) Application.Current.Shutdown(-1);
            //прописываем обработчики для кнопок
            SetEventHandlers();
            OrderManager.Orders.CollectionChanged += OrdersCollectionChanged;
            OrderManager.StartTimer();
        }

        //Настраиваем визуальные компоненты
        private void SetUpComponents()
        {
            //Подписываем кнопки
            ManPlatformLeftButton.Content = Settings.LeftRackName;
            ManPlatformRightButton.Content = Settings.RightRackName;

            //Заполняем combobox'ы номерами рядов
            int[] rowItems = new int[Settings.StackerDepth];
            for (int i = 0; i < rowItems.Length; i++) { rowItems[i] = i + 1; }
            RowSemiAutoComboBox.ItemsSource = rowItems;
            RowComboBox.ItemsSource = rowItems;
            RowXComboBox.ItemsSource = rowItems;
            RowSemiAutoComboBox.SelectedIndex = 0;
            RowComboBox.SelectedIndex = 0;

            // .. и этажей
            int[] floorItems = new int[Settings.StackerHight];
            for (int i = 0; i < floorItems.Length; i++) { floorItems[i] = i + 1; }
            FloorSemiAutoCombobox.ItemsSource = floorItems;
            FloorComboBox.ItemsSource = floorItems;
            FloorYComboBox.ItemsSource = floorItems;
            FloorSemiAutoCombobox.SelectedIndex = 0;
            FloorComboBox.SelectedIndex = 0;

            //.. и названиями стеллажей
            RackComboBox.Items.Add(Settings.LeftRackName);
            RackComboBox.Items.Add(Settings.RightRackName);
            RackComboBox.SelectedIndex = 0;
            RackSemiAutoComboBox.Items.Add(Settings.LeftRackName);
            RackSemiAutoComboBox.Items.Add(Settings.RightRackName);
            RackSemiAutoComboBox.SelectedIndex = 0;

            //при необходимости прячем вкладку "взвесить"
            if (!Settings.ShowWeightTab) WeightTabItem.Visibility = System.Windows.Visibility.Hidden;
            
            //настройка графика веса
            WeightPolyline.Stroke = Brushes.AliceBlue;
            WeightPolyline.StrokeThickness = 2;
            WeightPolyline.FillRule = FillRule.EvenOdd;
            WeightPolyline.Points = WeightPointCollection;
            WeightGrid.Children.Add(WeightPolyline);
            
            MeasuredWeightPolyline1.Stroke = Brushes.Red;
            MeasuredWeightPolyline1.StrokeThickness = 2;
            MeasuredWeightPolyline1.FillRule = FillRule.EvenOdd;
            MeasuredWeightPolyline1.Points = MeasuredWeight1PointCollection;
            WeightGrid.Children.Add(MeasuredWeightPolyline1);

            MeasuredWeightPolyline2.Stroke = Brushes.Red;
            MeasuredWeightPolyline2.StrokeThickness = 2;
            MeasuredWeightPolyline2.FillRule = FillRule.EvenOdd;
            MeasuredWeightPolyline2.Points = MeasuredWeight2PointCollection;
            WeightGrid.Children.Add(MeasuredWeightPolyline2);

            //Кнопка "увезти" активируется только при работе с заявкой
            TakeAwayAutoButton.IsEnabled = false;
            //Кнопка "отмена" активируется при выборе заявки
            CancelAutoButton.IsEnabled = false;
        }

        //Настройки вида списка заявок
        private void ListViewSetUp()
        {
            GridViewColumn gvc0 = new GridViewColumn();
            GridViewColumn gvc1 = new GridViewColumn();
            GridViewColumn gvc2 = new GridViewColumn();
            GridViewColumn gvc3 = new GridViewColumn();
            GridViewColumn gvc4 = new GridViewColumn();

            gvc0.Header = Header1;
            gvc0.DisplayMemberBinding = new Binding("OrderType");
            gvc0.Width = 160;
            
            gvc1.Header = Header2;
            gvc1.DisplayMemberBinding = new Binding("OrderNumber");

            gvc2.Header = Header3;
            gvc2.DisplayMemberBinding = new Binding("ProductDescription");

            gvc3.Header = Header4;
            gvc3.DisplayMemberBinding = new Binding("Amount");

            gvc4.Header = Header5;
            gvc4.DisplayMemberBinding = new Binding("Address");

            OrdersGridView.Columns.Add(gvc0);
            OrdersGridView.Columns.Add(gvc1);
            OrdersGridView.Columns.Add(gvc2);
            OrdersGridView.Columns.Add(gvc3);
            OrdersGridView.Columns.Add(gvc4);

            OrdersLitsView.View = OrdersGridView;
         }

        //прописываем обработчики событий
        private void SetEventHandlers()
        {
            //присваеваем обработчики тут, а не статически, чтобы они не вызывались 
            //во время первоначальных настроек
            //полуавтомат
            RackSemiAutoComboBox.SelectionChanged += SemiAutoComboBox_SelectionChanged;
            RowSemiAutoComboBox.SelectionChanged += SemiAutoComboBox_SelectionChanged;
            FloorSemiAutoCombobox.SelectionChanged += SemiAutoComboBox_SelectionChanged;

            //ручной режим
            RackComboBox.SelectionChanged += CellChanged;
            RowComboBox.SelectionChanged += CellChanged;
            FloorComboBox.SelectionChanged += CellChanged;
            CoordinateXTextBox.TextChanged += CoordinateChanged;
            CoordinateYTextBox.TextChanged += CoordinateChanged;
            IsNOTAvailableCheckBox.Click += IsNOTAvailableCheckBox_Click;

            //дефолтные методы обработки нажатия кнопок перемещения манипулятора в ручном режиме
            FartherButton.PreviewMouseLeftButtonUp += DirectButtonControl;
            FartherButton.PreviewMouseLeftButtonDown += DirectButtonControl;
            CloserButton.PreviewMouseLeftButtonUp += DirectButtonControl;
            CloserButton.PreviewMouseLeftButtonDown += DirectButtonControl;
            UpButton.PreviewMouseLeftButtonUp += DirectButtonControl;
            UpButton.PreviewMouseLeftButtonDown += DirectButtonControl;
            DownButton.PreviewMouseLeftButtonUp += DirectButtonControl;
            DownButton.PreviewMouseLeftButtonDown += DirectButtonControl;

            //считываем координаты первоначально
            CellChanged(null, null);
            SemiAutoComboBox_SelectionChanged(null,null);
        }

        //обработчик события "команда выполнена"
        private void CommandDone()
        {
            //разблокируем все кнопки
            foreach (Button b in ButtonList)
            {
                Dispatcher.Invoke(() => (b.IsEnabled = true));
            }
            ButtonList.Clear();

            //кнопки привезти/увезти устанавливаем в зависиомсти от наличия корзины
            Dispatcher.Invoke(() => BringSemiAutoButton.IsEnabled = !Model.CraneState.IsBinOnPlatform);
            Dispatcher.Invoke(() => TakeAwaySemiAutoButton.IsEnabled = Model.CraneState.IsBinOnPlatform);
        }

        //обработчик события "ошибка"
        private void ErrorAppeared()
        {
            Dispatcher.Invoke( () => { StatusPlane.Background = new SolidColorBrush(Colors.DarkRed); } );
            Dispatcher.Invoke( () => { ErrorTabItem.Background = new SolidColorBrush(Colors.DarkRed); } );
        }

        //Обновление координат и слова состояния
        private void UpdateCoordinate()
        {
            string r = Model.CraneState.ActualRow.ToString();
            r = r.Length == 1 ? "0" + r : r;
            Dispatcher.Invoke( () => RowLabel.Content = "Ряд : " + r);

            string f = Model.CraneState.ActualFloor.ToString();
            f = f.Length == 1 ? "0" + f : f;
            Dispatcher.Invoke( () => FloorLabel.Content = "Этаж : " + f );

            string x = Model.CraneState.ActualX.ToString();
            while (x.Length < 5) x = "0" + x;
            Dispatcher.Invoke( () => XLabel.Content = "X : " +  x);

            string y = Model.CraneState.ActualY.ToString();
            while (y.Length < 5) y = "0" + y;
            Dispatcher.Invoke( () => YLabel.Content = "Y : " + y );
        }
        
        //обработка изменений в слове состояния контроллера крана
        private void SomethingChanged()
        {
            //устанавливаем индикатор начальной позиции
            Dispatcher.Invoke(() => SPLabel.IsEnabled = Model.CraneState.IsStartPosiotion);
            Dispatcher.Invoke(() => RLabel.IsEnabled = Model.CraneState.IsRowMark);
            Dispatcher.Invoke(() => FLabel.IsEnabled = Model.CraneState.IsFloorMark);
        }

        //при изменении размеров окна меняем размеры колонок
        private void OrdersLitsView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (OrdersGridView.Columns.Count < 5) return;
            double s = 0;
            for (ushort i = 0; i < 4; i++)
            {
                if (i == 2) continue;
                s = s + OrdersGridView.Columns[i].ActualWidth;
            }
            //и не спрашивайте почему 107 :-)
            OrdersGridView.Columns[2].Width = OrdersLitsView.ActualWidth - s - 107;
        }

        //запускает подбор ширины столбцов
        public void OrdersCollectionChanged(object sender, NotifyCollectionChangedEventArgs a)
        {
            OrdersLitsView_SizeChanged(null, null);
        }

        //Обработчик нажатия кнопки STOP
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Model.Crane.StopButton();
        }

        //Обработчик нажатия кнопки подтверждения ошибок
        private void SubmitErrorButton_Click(object sender, RoutedEventArgs e)
        {
            //даем команду на сброс ошибки
            Model.Crane.SubmitError();
            //восстанавливаем цвет строки состояния и закладки
            StatusPlane.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x3E, 0x60, 0x6F));
            ErrorTabItem.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xCB, 0xDB, 0xD7));
            //разблокируем кнопки
            CommandDone();
            ErrorListBox.Items.Refresh();
            Model.CraneState.CommandDone -= TakeAwayDone;
            BringAutoButton.IsEnabled = !Model.CraneState.IsBinOnPlatform;
            TakeAwayAutoButton.IsEnabled = false;
        }

        //При изменении адреса ячеек перечитываем координаты
        private void CellChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                //вычисляем адрес ячейки
                char stack = (char)RackComboBox.SelectedItem;
                uint row = (uint)RowComboBox.SelectedIndex+1;
                uint floor = (uint)FloorComboBox.SelectedIndex+1;

                //получаем координаты
                Model.GetCell(stack, row, floor, out uint x, out uint y, out bool isNOTAvailable);

                //отключаем обработчики на изменение координат
                CoordinateXTextBox.TextChanged -= CoordinateChanged;
                CoordinateYTextBox.TextChanged -= CoordinateChanged;
                IsNOTAvailableCheckBox.Click -= IsNOTAvailableCheckBox_Click;

                //модифицируем компоненты
                CoordinateXTextBox.Text = x.ToString();
                CoordinateYTextBox.Text = y.ToString();
                IsNOTAvailableCheckBox.IsChecked = isNOTAvailable;
                
                //включаем обратно обработчики события изменение координат
                CoordinateXTextBox.TextChanged += CoordinateChanged;
                CoordinateYTextBox.TextChanged += CoordinateChanged;
                IsNOTAvailableCheckBox.Click += IsNOTAvailableCheckBox_Click;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "CellChanged");
            }
        }

        //При изменении координат перезаписываем их в cellgrid
        private void CoordinateChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                //вычисляем адрес ячейки
                bool stacker = RackComboBox.SelectedIndex != 0;
                uint row = (uint)RowComboBox.SelectedIndex + 1;
                uint floor = (uint)FloorComboBox.SelectedIndex + 1;

                //если поле пустое, то записываем в него ноль
                CoordinateXTextBox.Text = CoordinateXTextBox.Text == "" ? "0" : CoordinateXTextBox.Text;
                CoordinateYTextBox.Text = CoordinateYTextBox.Text == "" ? "0" : CoordinateYTextBox.Text;

                //получаем целые значения координат
                uint x = Convert.ToUInt32(CoordinateXTextBox.Text);
                uint y = Convert.ToUInt32(CoordinateYTextBox.Text);
                
                //если координата больше максимальноразрешшенной, устанавливаем ее максимальной
                if (x > Model.Settings.MaxX)
                {
                    x = Model.Settings.MaxX;
                    CoordinateXTextBox.Text = x.ToString();
                }
                if (y > Model.Settings.MaxY)
                {
                    y = Model.Settings.MaxY;
                    CoordinateYTextBox.Text = y.ToString();
                }
                
                bool isNotAvailable = (bool)IsNOTAvailableCheckBox.IsChecked;
                //записываем
                Model.SetCell(stacker,row,floor,x,y,isNotAvailable);
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

        //Сохранение массивов координат ячеек в файлы
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Model.SaveCells();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "SaveButton_Click");
            }
        }
        
        //Проверка вводимых в textbox символы на соотвктствие правилам
        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Match match = CoordinateRegex.Match(e.Text);
            if ((!match.Success) || (sender as TextBox).Text.Length > 5) e.Handled = true;
        }

        //при изменении выбранных ячеек в полуавтоматическом режиме меняет доступность кнопок 
        //в зависимости от доступности ячейки и формируем строку адреса выбранной ячейки
        private void SemiAutoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //получаем из базы координаты и доступность ячеек
            char stack = (char)RackComboBox.SelectedItem;
            uint r = (uint)RowSemiAutoComboBox.SelectedIndex + 1;
            uint f = (uint)FloorSemiAutoCombobox.SelectedIndex + 1;
            Model.GetCell(stack, r, f, out uint x, out uint y, out bool isNotAvailable);

            //устанавливаем доступность кнопок в зависимости от состояния ячейки
            bool state = Model.CraneState.IsBinOnPlatform;
            BringSemiAutoButton.IsEnabled = !isNotAvailable & !state;
            TakeAwaySemiAutoButton.IsEnabled &= !isNotAvailable & state;
            IsNotAvailableLabel.Content = isNotAvailable? "Ячейка отсутствует!":"";

            //Формируем адрес ячейки для индикации
            char rack = RackSemiAutoComboBox.SelectedIndex == 0 ? Settings.LeftRackName : Settings.RightRackName;
            SemiAutoAddressLabel.Content = rack + " - " + r.ToString() + " - " + f.ToString();
        }

        //в разделе "движение по координатам" при выборе ячейки записываем её координаты в поля ввода
        private void XYComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            uint r = (uint)RowXComboBox.SelectedIndex + 1;
            uint f = (uint)FloorYComboBox.SelectedIndex + 1;
            if (r < 1 | f < 1) return;
            Model.GetCell(Settings.LeftRackName, r, f, out uint x, out uint y, out bool z);
            GotoXTextBox.Text = x.ToString();
            GotoYTextBox.Text = y.ToString();
        }

        //кнопки сброса значений в TextBox на ноль
        private void XResButton_Click(object sender, RoutedEventArgs e)
        {
            GotoXTextBox.Text = "0";
            RowXComboBox.SelectedIndex = -1;
        }
        private void YResButton_Click(object sender, RoutedEventArgs e)
        {
            GotoYTextBox.Text = "0";
            FloorYComboBox.SelectedIndex = -1;
        }
        
        //Обработчик нажатия кнопки "платформа влево"
        private void ManPlatformLeftButton_Checked(object sender, RoutedEventArgs e)
        {
            Model.Crane.PlatformToRight();
            ButtonList.Add(sender as Button);
            (sender as Button).IsEnabled = false;
        }

        //Обработчик нажатия кнопки платформа "вправо вправо"
        private void ManPlatformRightButton_Checked(object sender, RoutedEventArgs e)
        {
            Model.Crane.PlatformToLeft();
            ButtonList.Add(sender as Button);
            (sender as Button).IsEnabled = false;
        }

        //В зависимости от состояния чекбокса выбираем действия кнопок
        private void LineMotionCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            //
            FartherButton.PreviewMouseLeftButtonUp -= DirectButtonControl;
            FartherButton.PreviewMouseLeftButtonDown -= DirectButtonControl;

            CloserButton.PreviewMouseLeftButtonUp -= DirectButtonControl;
            CloserButton.PreviewMouseLeftButtonDown -= DirectButtonControl;

            UpButton.PreviewMouseLeftButtonUp -= DirectButtonControl;
            UpButton.PreviewMouseLeftButtonDown -= DirectButtonControl;

            DownButton.PreviewMouseLeftButtonUp -= DirectButtonControl;
            DownButton.PreviewMouseLeftButtonDown -= DirectButtonControl;
            
            //
            FartherButton.PreviewMouseLeftButtonDown += NextLineButtonControl;
            CloserButton.PreviewMouseLeftButtonDown += NextLineButtonControl;
            UpButton.PreviewMouseLeftButtonDown += NextLineButtonControl;
            DownButton.PreviewMouseLeftButtonDown += NextLineButtonControl;

            //закрашиваем надписи черным
            FartherButton.Foreground = new SolidColorBrush(Colors.Black);
            CloserButton.Foreground = new SolidColorBrush(Colors.Black);
            UpButton.Foreground = new SolidColorBrush(Colors.Black);
            DownButton.Foreground = new SolidColorBrush(Colors.Black);
        }
        private void LineMotionCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            //
            FartherButton.PreviewMouseLeftButtonDown -= NextLineButtonControl;
            CloserButton.PreviewMouseLeftButtonDown -= NextLineButtonControl;
            UpButton.PreviewMouseLeftButtonDown -= NextLineButtonControl;
            DownButton.PreviewMouseLeftButtonDown -= NextLineButtonControl;

            FartherButton.PreviewMouseLeftButtonUp += DirectButtonControl;
            FartherButton.PreviewMouseLeftButtonDown += DirectButtonControl;

            CloserButton.PreviewMouseLeftButtonUp += DirectButtonControl;
            CloserButton.PreviewMouseLeftButtonDown += DirectButtonControl;

            UpButton.PreviewMouseLeftButtonUp += DirectButtonControl;
            UpButton.PreviewMouseLeftButtonDown += DirectButtonControl;

            DownButton.PreviewMouseLeftButtonUp += DirectButtonControl;
            DownButton.PreviewMouseLeftButtonDown += DirectButtonControl;
            
            //закрашиваем надписи белым
            FartherButton.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xFC, 0xFF, 0xF5));
            CloserButton.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xFC, 0xFF, 0xF5));
            UpButton.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xFC, 0xFF, 0xF5));
            DownButton.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xFC, 0xFF, 0xF5));
        }

        //Обработчик нажатия кнопки "ближе"
        private void DirectButtonControl(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            bool state = e.ButtonState == System.Windows.Input.MouseButtonState.Pressed ? true : false;
            switch (((Button)sender).Name)
            {
                case "FartherButton":
                    Model.Crane.FartherButton(state);
                    break;
                case "CloserButton":
                    Model.Crane.CloserButton(state);
                    break;
                case "UpButton":
                    Model.Crane.UpButton(state);
                    break;
                case "DownButton":
                    Model.Crane.DownButton(state);
                    break;
                default: return;
            }
        }

        //При клике по кнопке даем команду "движение до следующего ряда/этажа"
        private void NextLineButtonControl(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            switch ( ((Button)sender).Name )
            {
                case "FartherButton":
                    Model.Crane.NextLineFartherCommand();
                    break;
                case "CloserButton":
                    Model.Crane.NextLineCloserCommand();
                    break;
                case "UpButton":
                    Model.Crane.NextLineUpCommand();
                    break;
                case "DownButton":
                    Model.Crane.NextLineDownCommand();
                    break;
                default: return;
            }
            ButtonList.Add((Button)sender);
            (sender as Button).IsEnabled = false;
        }

        //Обработчик нажатия кнопки "Перейти на координаты"
        private void GotoButton_Click(object sender, RoutedEventArgs e)
        {
           if (!UInt16.TryParse(GotoXTextBox.Text,out UInt16 x)) 
            {
                GotoXTextBox.Text = "0";
                x = 0;
            }

            if (!UInt16.TryParse(GotoYTextBox.Text, out UInt16 y))
            {
                GotoYTextBox.Text = "0";
                y = 0;
            }
            
            x = x > Model.Settings.MaxX ? (UInt16)Model.Settings.MaxX : x;
            y = y > Model.Settings.MaxY ? (UInt16)Model.Settings.MaxY : y;

            Model.Crane.GotoXY(x, y);

            ButtonList.Add((Button)sender);
            (sender as Button).IsEnabled = false;
        }

        //обрабатывает нажатие кнопок "привезти" и "увезти" в полуавтоматическом режиме
        private void BringOrTakeAwaySemiAutoButton_Click(object sender, RoutedEventArgs e)
        {
            bool stack = RackSemiAutoComboBox.SelectedIndex == 1;
            uint r = (uint)RowSemiAutoComboBox.SelectedIndex + 1;
            uint f = (uint)FloorSemiAutoCombobox.SelectedIndex + 1;
            
            //если была нажата кнопка привезти устанавливае переменную в true
            bool bring = sender == BringSemiAutoButton ? true : false;

            Model.Crane.BringOrTakeAway(stack,r,f,bring);           
            
            (sender as Button).IsEnabled = false; 
        }

        //в автоматическом режиме даем команду на подвоз контейнера
        private void BringAutoButton_Click(object sender, RoutedEventArgs e)
        {
            int i = OrdersLitsView.SelectedIndex;
            OrderManager.SelectedOrderNumber =i;
            try
            {
                //Даем команду привезти
                Model.Crane.BringOrTakeAway(true);
                //Выключаем кнопку "привезти"
                BringAutoButton.IsEnabled = false;
                //и добавляем в список нажатых кнопок кнопку "увезти"
                ButtonList.Add(TakeAwayAutoButton);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        
        //увозим контейнер на место
        private void TakeAwayAutoButton_Click(object sender, RoutedEventArgs e)
        {
            //Даем команду привезти
            Model.Crane.BringOrTakeAway(false);
            //Выключаем кнопку "увезти"
            TakeAwayAutoButton.IsEnabled = false;
            //и добавляем в список нажатых кнопок кнопку "привезти"
            ButtonList.Add(BringAutoButton);
            //к обработчику завершения команды добавляем метод.
            Model.CraneState.CommandDone += TakeAwayDone;
        }

        //после доставки  на место разрешаем кнопкку "привезти"
        private void TakeAwayDone()
        {
            //возвращаем обработчик события
            Model.CraneState.CommandDone -= TakeAwayDone;
            //завершаем заявку
            OrderManager.FinishSelectedOrder(true);
            Dispatcher.Invoke( () => OrdersLitsView.Items.Refresh());
        }
        
        //нажатие кнопки "отменить заявку"
        private void CancelAutoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int i = OrdersLitsView.SelectedIndex;
                OrderManager.SelectedOrderNumber = i;
                OrderManager.FinishSelectedOrder(false);
                OrdersLitsView.SelectedIndex = -1;
                OrdersLitsView.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //нажатие кнопки "взвесить"
        private void WeighButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonList.Add(sender as Button);
            (sender as Button).IsEnabled = false;
            //очищаем графики
            WeightPointCollection.Clear();
            MeasuredWeight1PointCollection.Clear();
            MeasuredWeight2PointCollection.Clear();
            //подписываемся для считывания текущих значений
            Model.CraneState.CoordinateReaded += MakeGraph;
            Model.CraneState.CommandDone += WeighDone;
            //запускаем взвешивание
            Model.Crane.Weigh();
        }

        //по актуальному значению тока строим график
        private void MakeGraph()
        {
            double w = 450 - Model.CraneState.Weight;
            Point point = new Point(c*10, w);
            Dispatcher.Invoke(() => WeightPointCollection.Add(point));
            c++;
        }

        //по окончании взвешивания рисуем 7 перпендикулярных красных линий ;-)
        private void WeighDone()
        {
            int y = 450 - Model.CraneState.MeasuredWeight;
            Point point11 = new Point(0,y);
            Point point12 = new Point(300, y);
            Dispatcher.Invoke(() => MeasuredWeight1PointCollection.Add(point11));
            Dispatcher.Invoke(() => MeasuredWeight1PointCollection.Add(point12));
                       
            y = 450 - Model.CraneState.MeasuredWeight2;
            Point point21 = new Point(0, y);
            Point point22 = new Point(300, y);
            Dispatcher.Invoke(() => MeasuredWeight2PointCollection.Add(point21));
            Dispatcher.Invoke(() => MeasuredWeight2PointCollection.Add(point22));

            float w = Model.CraneState.MeasuredWeight - Settings.WeightAlpha1;
            w = Settings.WeightBeta1 == 0 ? w : w * 100 / Settings.WeightBeta1;
                       
            Dispatcher.Invoke(() => MeasuredWeightLabel.Content = w.ToString() + " кг");

            w = Model.CraneState.MeasuredWeight2 - Settings.WeightAlpha2;
            w = Settings.WeightBeta2 == 0 ? w : w * 100 / Settings.WeightBeta2;

            Dispatcher.Invoke(() => MeasuredWeightLabel2.Content = w.ToString() + " кг");

            Model.CraneState.CommandDone -= WeighDone;
            Model.CraneState.CoordinateReaded -= MakeGraph;
            c = 0;
        }

        //при выборе какой-либо заявки в списке активируем кнопку "отменить"
        private void OrdersLitsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CancelAutoButton.IsEnabled = true;
            int index = OrdersLitsView.SelectedIndex;
            if (index < 0)
            {
                BringAutoButton.IsEnabled = false;
                CancelAutoButton.IsEnabled = false;
                return;
            }
            uint r = OrderManager.Orders[index].Row;
            uint f = OrderManager.Orders[index].Floor;
            char n = OrderManager.Orders[index].StackerName;
            Model.GetCell(n, r, f, out uint x, out uint y, out bool isNotAvailable);
            BringAutoButton.IsEnabled = !isNotAvailable & !Model.CraneState.IsBinOnPlatform;
            CancelAutoButton.IsEnabled = true;
        }

        //закрываем неуправляемые ресурсы
        public void Dispose()
        {
            Dispose(true);
            // подавляем финализацию
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Model?.Dispose();
                    OrderManager?.Dispose();
                }
                disposed = true;
            }
        }

        //сортировка списка заявок по заголоку
        void SortListView(Object sender, RoutedEventArgs e)
        {
            string str = (e.OriginalSource as GridViewColumnHeader).Content.ToString();
            switch (str)
            {
                case Header1:
                    OrderManager.SortList("OrderType", SortDirection[0]);
                    SortDirection[0] = !SortDirection[0];
                    break;
                case Header2:
                    OrderManager.SortList("OrderNumber", SortDirection[1]);
                    SortDirection[1] = !SortDirection[1];
                    break;
                case Header3:
                    OrderManager.SortList("ProductDescription", SortDirection[2]);
                    SortDirection[2] = !SortDirection[2];
                    break;
                case Header4:
                    OrderManager.SortList("Amount", SortDirection[3]);
                    SortDirection[3] = !SortDirection[3];
                    break;
                case Header5:
                    OrderManager.SortList("Address", SortDirection[4]);
                    SortDirection[4] = !SortDirection[4];
                    break;
            }
        }
    }
}

