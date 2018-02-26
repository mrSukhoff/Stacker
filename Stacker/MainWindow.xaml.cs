
using System;
using System.Collections.Generic;
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

        //хранилище настроек
        SettingsKeeper Settings;

        //менеджер заявок
        public OrdersManager OrderManager;

        //формат ввода координат в textbox'ы
        private Regex CoordinateRegex = new Regex(@"\d");

        //Список кнопок, выдавших задание и заблокированных
        private List<Button> ButtonList = new List<Button>();

        //модель паттерна MVP(если это конечно он)
        private StackerModel Model;

        //для рисования графика веса
        private Polyline WeightPolyline = new Polyline();
        private PointCollection WeightPointCollection = new PointCollection();
        private int c = 0;
        private Polyline MeasuredWeightPolyline1 = new Polyline();
        private Polyline MeasuredWeightPolyline2 = new Polyline();
        private PointCollection MeasuredWeight1PointCollection = new PointCollection();
        private PointCollection MeasuredWeight2PointCollection = new PointCollection();

        //стиль оттображения списка заявок
        GridView OrdersGridView = new GridView();

        //флаг закрытия неуправляемых ресурсов
        private bool disposed = false;

        //#####################################################################################################
        //Основная точка входа -------------------------------------------------------------------------------!
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Инициализируем хранилище настроек
            Settings = new SettingsKeeper();

            //Создаем менеджер заявок
            OrderManager = new OrdersManager(Settings.OrdersFile, Settings.ArchiveFile, Settings.WrongOrdersFile,
                Settings.LeftRackName, Settings.RightRackName);
            //Определяем его источником данных для списка
            OrdersLitsView.ItemsSource = OrderManager.Orders;
            //и подписываемся на обновления
            OrderManager.NewOrderAppeared += UpdateOrderList;

            //Настраиваем вид списка заявок
            ListViewSetUp();
            //Настраиваем визуальные компоненты
            SetUpComponents();
            
            
            //создаем модель
            try
            {
                
                Model = new StackerModel(OrderManager,Settings);
            }
            catch 
            {
                Model?.Dispose();
                if (!Settings.CloseOrInform) Application.Current.Shutdown();
            }

            if (Model != null)
            {
                //подписываемся на события модели
                Model.CommandDone += CommandDone;
                Model.ErrorAppeared += ErrorAppeared;
                Model.CoordinateReaded += UpdateCoordinate;
                Model.StateWordChanged += SomethingChanged;
                
                //источник данных для списка ошибок
                ErrorListBox.ItemsSource = Model.ErrorList;
                
                //проверяем при старте наличие ящика на платформе и устанавливаем активные кнопки
                bool isBin = Model.ChekBinOnPlatform();
                TakeAwaySemiAutoButton.IsEnabled = isBin;
                BringSemiAutoButton.IsEnabled = !isBin;
                BringAutoButton.IsEnabled = !isBin;
            }
            //прописываем обработчики для кнопок
            SetEventHandlers();
            //запускаем чтение заявок
            OrderManager.TimerStart();
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
            GridViewColumn gvc5 = new GridViewColumn();
            
            gvc0.Header = " Тип ";
            gvc0.DisplayMemberBinding = new Binding("OrderType");
            
            gvc1.Header = " Номер заказа ";
            gvc1.DisplayMemberBinding = new Binding("OrderNumber");

            gvc2.Header = " Кодовое обозначение ";
            gvc2.DisplayMemberBinding = new Binding("ProductCode");
                        
            gvc3.Header = " Описание ";
            gvc3.DisplayMemberBinding = new Binding("ProductDescription");

            gvc4.Header = " Кол-во ";
            gvc4.DisplayMemberBinding = new Binding("Amount");

            gvc5.Header = " Ячейка ";
            gvc5.DisplayMemberBinding = new Binding("Address");

            OrdersGridView.Columns.Add(gvc0);
            OrdersGridView.Columns.Add(gvc1);
            OrdersGridView.Columns.Add(gvc2);
            OrdersGridView.Columns.Add(gvc3);
            OrdersGridView.Columns.Add(gvc4);
            OrdersGridView.Columns.Add(gvc5);

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

        //обработчик события "ошибка"
        private void UpdateOrderList()
        {
            Dispatcher.Invoke(() => OrdersLitsView.Items.Refresh());
            Dispatcher.Invoke( ()=>OrdersLitsView_SizeChanged(null, null));
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
            Dispatcher.Invoke(() => BringSemiAutoButton.IsEnabled = !Model.IsBinOnPlatform);
            Dispatcher.Invoke(() => TakeAwaySemiAutoButton.IsEnabled = Model.IsBinOnPlatform);
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
            string r = Model.ActualRow.ToString();
            r = r.Length == 1 ? "0" + r : r;
            Dispatcher.Invoke( () => RowLabel.Content = "Ряд : " + r);

            string f = Model.ActualFloor.ToString();
            f = f.Length == 1 ? "0" + f : f;
            Dispatcher.Invoke( () => FloorLabel.Content = "Этаж : " + f );

            string x = Model.ActualX.ToString();
            while (x.Length < 5) x = "0" + x;
            Dispatcher.Invoke( () => XLabel.Content = "X : " +  x);

            string y = Model.ActualY.ToString();
            while (y.Length < 5) y = "0" + y;
            Dispatcher.Invoke( () => YLabel.Content = "Y : " + y );
        }
        
        //обработка изменений в слове состояния контроллера крана
        private void SomethingChanged()
        {
            //устанавливаем индикатор начальной позиции
            Dispatcher.Invoke(() => SPLabel.IsEnabled = Model.IsStartPosiotion);
            Dispatcher.Invoke(() => RLabel.IsEnabled = Model.IsRowMark);
            Dispatcher.Invoke(() => FLabel.IsEnabled = Model.IsFloorMark);
        }

        //при изменении размеров окна меняем размеры колонок
        private void OrdersLitsView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (OrdersGridView.Columns.Count < 6) return;
            double s = 0;
            for (ushort i = 0; i < 5; i++)
            {
                if (i == 3) continue;
                s = s + OrdersGridView.Columns[i].ActualWidth;
            }
            //и не спрашивайте почему 107 :-)
            OrdersGridView.Columns[3].Width = OrdersLitsView.ActualWidth - s - 107;
        }

        //Обработчик нажатия кнопки STOP
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Model.StopButton();
        }

        //Обработчик нажатия кнопки подтверждения ошибок
        private void SubmitErrorButton_Click(object sender, RoutedEventArgs e)
        {
            //даем команду на сброс ошибки
            Model.SubmitError();
            //восстанавливаем цвет строки состояния и закладки
            StatusPlane.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x3E, 0x60, 0x6F));
            ErrorTabItem.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xCB, 0xDB, 0xD7));
            //разблокируем кнопки
            CommandDone();
            ErrorListBox.Items.Refresh();
            Model.CommandDone -= TakeAwayDone;
            BringAutoButton.IsEnabled = !Model.IsBinOnPlatform;
            TakeAwayAutoButton.IsEnabled = false;
        }

        //При изменении адреса ячеек перечитываем координаты
        private void CellChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                //вычисляем адрес ячейки
                char stack = (char)RackComboBox.SelectedItem;
                int row = RowComboBox.SelectedIndex+1;
                int floor = FloorComboBox.SelectedIndex+1;

                //получаем координаты
                Model.GetCell(stack, row, floor, out int x, out int y, out bool isNOTAvailable);

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
                int row = RowComboBox.SelectedIndex + 1;
                int floor = FloorComboBox.SelectedIndex + 1;

                //если поле пустое, то записываем в него ноль
                CoordinateXTextBox.Text = CoordinateXTextBox.Text == "" ? "0" : CoordinateXTextBox.Text;
                CoordinateYTextBox.Text = CoordinateYTextBox.Text == "" ? "0" : CoordinateYTextBox.Text;

                //получаем целые значения координат
                int x = Convert.ToInt32(CoordinateXTextBox.Text);
                int y = Convert.ToInt32(CoordinateYTextBox.Text);
                
                //если координата больше максимальноразрешшенной, устанавливаем ее максимальной
                if (x > Model.MaxX)
                {
                    x = Model.MaxX;
                    CoordinateXTextBox.Text = x.ToString();
                }
                if (y > Model.MaxY)
                {
                    y = Model.MaxY;
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
            int r = RowSemiAutoComboBox.SelectedIndex + 1;
            int f = FloorSemiAutoCombobox.SelectedIndex + 1;
            Model.GetCell(stack, r, f, out int x, out int y, out bool isNotAvailable);

            //устанавливаем доступность кнопок в зависимости от состояния ячейки
            bool state = Model.IsBinOnPlatform;
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
            int r = RowXComboBox.SelectedIndex + 1;
            int f = FloorYComboBox.SelectedIndex + 1;
            if (r < 1 | f < 1) return;
            Model.GetCell(Settings.LeftRackName, r, f, out int x, out int y, out bool z);
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
            Model.PlatformToRight();
            ButtonList.Add(sender as Button);
            (sender as Button).IsEnabled = false;
        }

        //Обработчик нажатия кнопки платформа "вправо вправо"
        private void ManPlatformRightButton_Checked(object sender, RoutedEventArgs e)
        {
            Model.PlatformToLeft();
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
                    Model.FartherButton(state);
                    break;
                case "CloserButton":
                    Model.CloserButton(state);
                    break;
                case "UpButton":
                    Model.UpButton(state);
                    break;
                case "DownButton":
                    Model.DownButton(state);
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
                    Model.NextLineFartherCommand();
                    break;
                case "CloserButton":
                    Model.NextLineCloserCommand();
                    break;
                case "UpButton":
                    Model.NextLineUpCommand();
                    break;
                case "DownButton":
                    Model.NextLineDownCommand();
                    break;
                default: return;
            }
            ButtonList.Add((Button)sender);
            (sender as Button).IsEnabled = false;
        }

        //Обработчик нажатия кнопки "Перейти на координаты"
        private void GotoButton_Click(object sender, RoutedEventArgs e)
        {
            UInt16 x, y;
;           if (!UInt16.TryParse(GotoXTextBox.Text,out x)) 
            {
                GotoXTextBox.Text = "0";
                x = 0;
            }

            if (!UInt16.TryParse(GotoYTextBox.Text, out y))
            {
                GotoYTextBox.Text = "0";
                y = 0;
            }
            
            x = x > Model.MaxX ? (UInt16)Model.MaxX : x;
            y = y > Model.MaxY ? (UInt16)Model.MaxY : y;

            Model.GotoXY(x, y);

            ButtonList.Add((Button)sender);
            (sender as Button).IsEnabled = false;
        }

        //обрабатывает нажатие кнопок "привезти" и "увезти" в полуавтоматическом режиме
        private void BringOrTakeAwaySemiAutoButton_Click(object sender, RoutedEventArgs e)
        {
            bool stack = RackSemiAutoComboBox.SelectedIndex == 1;
            int r = RowSemiAutoComboBox.SelectedIndex + 1;
            int f = FloorSemiAutoCombobox.SelectedIndex + 1;
            
            //если была нажата кнопка привезти устанавливае переменную в true
            bool bring = sender == BringSemiAutoButton ? true : false;

            Model.BringOrTakeAway(stack,r,f,bring);           
            
            (sender as Button).IsEnabled = false; 
        }

        //в автоматическом режиме даем команду на подвоз контейнера
        private void BringAutoButton_Click(object sender, RoutedEventArgs e)
        {
            int i = OrdersLitsView.SelectedIndex;
            if (OrderManager.SelectOrder(i))
            {
                //Даем команду привезти
                Model.BringOrTakeAway(true);
                //Выключаем кнопку "привезти"
                BringAutoButton.IsEnabled = false;
                //и добавляем в список нажатых кнопок кнопку "увезти"
                ButtonList.Add(TakeAwayAutoButton);
            }
        }
        
        //увозим контейнер на место
        private void TakeAwayAutoButton_Click(object sender, RoutedEventArgs e)
        {
            //Даем команду привезти
            Model.BringOrTakeAway(false);
            //Выключаем кнопку "увезти"
            TakeAwayAutoButton.IsEnabled = false;
            //и добавляем в список нажатых кнопок кнопку "привезти"
            ButtonList.Add(BringAutoButton);
            //к обработчику завершения команды добавляем метод.
            Model.CommandDone += TakeAwayDone;
        }

        //после доставки  на место разрешаем кнопкку "привезти"
        private void TakeAwayDone()
        {
            //возвращаем обработчик события
            Model.CommandDone -= TakeAwayDone;
            //завершаем заявку
            OrderManager.FinishSelectedOrder(true);
            Dispatcher.Invoke( () => OrdersLitsView.Items.Refresh());
        }
        
        //нажатие кнопки "отменить заявку"
        private void CancelAutoButton_Click(object sender, RoutedEventArgs e)
        {
            int i = OrdersLitsView.SelectedIndex;
            //Если элемент выбран, удаляем его, иначе выходим
            if (OrderManager.SelectOrder(i)) OrderManager.FinishSelectedOrder(false);
            OrdersLitsView.SelectedIndex = -1;
            OrdersLitsView.Items.Refresh();
            
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
            Model.CoordinateReaded += MakeGraph;
            Model.CommandDone += WeighDone;
            //запускаем взвешивание
            Model.Weigh();
        }

        //по актуальному значению тока строим график
        private void MakeGraph()
        {
            double w = 450 - Model.Weight;
            Point point = new Point(c*10, w);
            Dispatcher.Invoke(() => WeightPointCollection.Add(point));
            c++;
        }

        //по окончании взвешивания рисуем 7 перпендикулярных красных линий ;-)
        private void WeighDone()
        {
            int y = 450 - Model.MeasuredWeight;
            Point point11 = new Point(0,y);
            Point point12 = new Point(300, y);
            Dispatcher.Invoke(() => MeasuredWeight1PointCollection.Add(point11));
            Dispatcher.Invoke(() => MeasuredWeight1PointCollection.Add(point12));
                       
            y = 450 - Model.MeasuredWeight2;
            Point point21 = new Point(0, y);
            Point point22 = new Point(300, y);
            Dispatcher.Invoke(() => MeasuredWeight2PointCollection.Add(point21));
            Dispatcher.Invoke(() => MeasuredWeight2PointCollection.Add(point22));

            float w = Model.MeasuredWeight - Settings.WeightAlpha1;
            w = Settings.WeightBeta1 == 0 ? w : w * 100 / Settings.WeightBeta1;
                       
            Dispatcher.Invoke(() => MeasuredWeightLabel.Content = w.ToString() + " кг");

            w = Model.MeasuredWeight2 - Settings.WeightAlpha2;
            w = Settings.WeightBeta2 == 0 ? w : w * 100 / Settings.WeightBeta2;

            Dispatcher.Invoke(() => MeasuredWeightLabel2.Content = w.ToString() + " кг");

            Model.CommandDone -= WeighDone;
            Model.CoordinateReaded -= MakeGraph;
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
            int r = OrderManager.Orders[index].Row;
            int f = OrderManager.Orders[index].Floor;
            char n = OrderManager.Orders[index].StackerName;
            Model.GetCell(n, r, f, out int x, out int y, out bool isNotAvailable);
            BringAutoButton.IsEnabled = !isNotAvailable & !Model.IsBinOnPlatform;
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
    }
}

