
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
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        //формат ввода координат в textbox'ы
        Regex CoordinateRegex = new Regex(@"\d");

        //Список кнопок, выдавших задание и заблокированных
        List<Button> bt = new List<Button>();
               
        //модель паттерна MVP(если это конечно он)
        StackerModel model;

        //для рисования графика веса
        Polyline WeightPolyline = new Polyline();
        PointCollection WeightPointCollection = new PointCollection();
        int c = 0;
        Polyline MeasuredWeightPolyline1 = new Polyline();
        Polyline MeasuredWeightPolyline2 = new Polyline();
        PointCollection MeasuredWeight1PointCollection = new PointCollection();
        PointCollection MeasuredWeight2PointCollection = new PointCollection();

        //#####################################################################################################
        //Основная точка входа -------------------------------------------------------------------------------!
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //try
            {
                //создаем модель
                model = new StackerModel();
          
            }
            /*catch (Exception)
            {
                Application.Current.Shutdown();
            }*/

            if (model != null)
            {
                //подписываемся на события
                model.CommandDone += CommandDone;
                model.ErrorAppeared += ErrorAppeared;
                model.CoordinateReaded += UpdateCoordinate;
                model.SomethingChanged +=ButtonsAndBins;

                //Настраиваем визуальные компоненты
                SetUpComponents();

                //Настраиваем вид списка заявок
                GridSetUp();
            }
        }
                
        //Настраиваем визуальные компоненты
        private void SetUpComponents()
        {
            //Подписываем кнопки
            ManPlatformLeftButton.Content = model.LeftRackName; 
            ManPlatformRightButton.Content = model.RightRackName;

            //Заполняем combobox'ы номерами рядов
            int[] rowItems = new int[model.StackerDepth];
            for (int i = 0; i < rowItems.Length; i++) { rowItems[i] = i + 1; }
            RowSemiAutoComboBox.ItemsSource = rowItems;
            RowComboBox.ItemsSource = rowItems;
            RowXComboBox.ItemsSource = rowItems;
            RowSemiAutoComboBox.SelectedIndex = 0;
            RowComboBox.SelectedIndex = 0;

            // .. и этажей
            int[] floorItems = new int[model.StackerHight];
            for (int i = 0; i < floorItems.Length; i++) { floorItems[i] = i + 1; }
            FloorSemiAutoCombobox.ItemsSource = floorItems;
            FloorComboBox.ItemsSource = floorItems;
            FloorYComboBox.ItemsSource = floorItems;
            FloorSemiAutoCombobox.SelectedIndex = 0;
            FloorComboBox.SelectedIndex = 0;

            //.. и названиями стеллажей
            RackComboBox.Items.Add(model.LeftRackName);
            RackComboBox.Items.Add(model.RightRackName);
            RackComboBox.SelectedIndex = 0;
            RackSemiAutoComboBox.Items.Add(model.LeftRackName);
            RackSemiAutoComboBox.Items.Add(model.RightRackName);
            RackSemiAutoComboBox.SelectedIndex = 0;

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
            FartherButton.PreviewMouseLeftButtonUp += FartherButton_PreviewMouseLeftButtonUp;
            FartherButton.PreviewMouseLeftButtonDown += FartherButton_PreviewMouseLeftButtonDown;
            CloserButton.PreviewMouseLeftButtonUp += CloserButton_PreviewMouseLeftButtonUp;
            CloserButton.PreviewMouseLeftButtonDown += CloserButton_PreviewMouseLeftButtonDown;
            UpButton.PreviewMouseLeftButtonUp += UpButton_PreviewMouseLeftButtonUp;
            UpButton.PreviewMouseLeftButtonDown += UpButton_PreviewMouseLeftButtonDown;
            DownButton.PreviewMouseLeftButtonUp += DownButton_PreviewMouseLeftButtonUp;
            DownButton.PreviewMouseLeftButtonDown += DownButton_PreviewMouseLeftButtonDown;

            //источник данных для списка ошибок
            ErrorListView.ItemsSource = model.ErrorList;

            //изначально кнопка "увезти" не актиквна, так как предполагается, что увозить пока нечего
            TakeAwayAutoButton.IsEnabled = false;

            //считываем координаты
            CellChanged(null,null);

            //настройка графика
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
        }

        //Настройки вида списка заявок
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
            OrdersLitsView.ItemsSource = model.Orders;
        }

        //обработчик события "команда выполнена"
        private void CommandDone()
        {
            //разблокируем все кнопки
            foreach (Button b in bt)
            {
                Dispatcher.Invoke(() => (b.IsEnabled = true));
            }
            bt.Clear();
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
            string sw = Convert.ToString(model.StateWord, 2);
            while (sw.Length < 16) sw = "0" + sw;
            Dispatcher.Invoke( () => StateWordLabel.Content = "State Word : " + sw );

            string r = model.ActualRow.ToString();
            r = r.Length == 1 ? "0" + r : r;
            Dispatcher.Invoke( () => RowLabel.Content = "R:" + r);

            string f = model.ActualFloor.ToString();
            f = f.Length == 1 ? "0" + f : f;
            Dispatcher.Invoke( () => FloorLabel.Content = "F:" + f );

            string x = model.ActualX.ToString();
            while (x.Length < 5) x = "0" + x;
            Dispatcher.Invoke( () => XLabel.Content = "X:" +  x);

            string y = model.ActualY.ToString();
            while (y.Length < 5) y = "0" + y;
            Dispatcher.Invoke( () => YLabel.Content = "Y:" + y );

            Dispatcher.Invoke( () => WeightLabel.Content = "Weight:" + model.Weight);
        }
        
        //
        private void ButtonsAndBins()
        {
            if ((model.StateWord & 2) == 2)
            {
                Dispatcher.Invoke( () => BringAutoButton.IsEnabled = false );
                Dispatcher.Invoke( () => TakeAwayAutoButton.IsEnabled = true );

                Dispatcher.Invoke( () => BringSemiAutoButton.IsEnabled = false );
                Dispatcher.Invoke( () => TakeAwaySemiAutoButton.IsEnabled = true );
            }
        }

        //При изменении адреса ячеек перечитываем координаты
        private void CellChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                //вычисляем адрес ячейки
                bool stacker = RackComboBox.SelectedIndex != 0;
                int row = RowComboBox.SelectedIndex+1;
                int floor = FloorComboBox.SelectedIndex+1;

                //получаем координаты
                model.GetCell(stacker, row, floor, out int x, out int y, out bool isNOTAvailable);

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
                if (x > model.MaxX)
                {
                    x = model.MaxX;
                    CoordinateXTextBox.Text = x.ToString();
                }
                if (y > model.MaxY)
                {
                    y = model.MaxY;
                    CoordinateYTextBox.Text = y.ToString();
                }
                
                bool isNotAvailable = (bool)IsNOTAvailableCheckBox.IsChecked;
                //записываем
                model.SetCell(stacker,row,floor,x,y,isNotAvailable);
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
                model.SaveCells();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
            try
            {
                bool stacker = RackSemiAutoComboBox.SelectedIndex == 1;
                int r = RowSemiAutoComboBox.SelectedIndex + 1;
                int f = FloorSemiAutoCombobox.SelectedIndex + 1;

                model.GetCell(stacker, r, f,out int x,out int y,out bool isNotAvailable);

                BringSemiAutoButton.IsEnabled = !isNotAvailable;
                TakeAwaySemiAutoButton.IsEnabled = !isNotAvailable;
                SemiAutoAddressLabel.IsEnabled = !isNotAvailable;

                char rack = RackSemiAutoComboBox.SelectedIndex == 0 ? model.LeftRackName : model.RightRackName;
                SemiAutoAddressLabel.Content = rack + " - " + r.ToString() + " - " + f.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "SemiAutoComboBox_SelectionChanged");
            }
        }

        private void XYComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int r = RowXComboBox.SelectedIndex + 1;
            int f = FloorYComboBox.SelectedIndex + 1;
            if (r < 1 | f < 1) return;
            model.GetCell(false, r, f, out int x, out int y, out bool z);
            GotoXTextBox.Text = x.ToString();
            GotoYTextBox.Text = y.ToString();
        }

        //завершение работы программы
        private void Stacker_Closed(object sender, EventArgs e)
        {
            if (model != null) model.Dispose();
        }

        //Обработчик нажатия кнопки подтверждения ошибки
        private void SubmitErrorButton_Click(object sender, RoutedEventArgs e)
        {
            model.SubmitError();
            ErrorListView.Items.Refresh();
            ManPlatformLeftButton.IsEnabled = true;
            ManPlatformRightButton.IsEnabled = true;
            GotoButton.IsEnabled = true;
            BringSemiAutoButton.IsEnabled = true;
            TakeAwaySemiAutoButton.IsEnabled = true;
            BringAutoButton.IsEnabled = true;
            StatusPlane.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x3E, 0x60, 0x6F));
            ErrorTabItem.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xCB, 0xDB, 0xD7));
            //возможны проблемы при возникновении ошибки во время возврата контейнера на место
            //TakeAwayAutoButton.IsEnabled = true;
        }

        //Обработчик нажатия кнопки "ближе"
        private void CloserButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            model.CloserButton(true);
        }
        private void CloserButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            model.CloserButton(false);
        }

        //Обработчик нажатия кнопки "дальше"
        private void FartherButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            model.FartherButton(true);
        }
        private void FartherButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            model.FartherButton(false);
        }

        //Обработчик нажатия кнопки "вверх"
        private void UpButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            model.UpButton(true);
        }
        private void UpButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            model.UpButton(false);
        }

        //Обработчик нажатия кнопки "вниз"
        private void DownButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            model.DownButton(true);
        }
        private void DownButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            model.DownButton(state: false);
        }

        //Обработчик нажатия кнопки "платформа влево"
        private void ManPlatformLeftButton_Checked(object sender, RoutedEventArgs e)
        {
            model.PlatformToRight();   
            bt.Add( sender as Button);
            (sender as Button).IsEnabled = false;
        }

        //Обработчик нажатия кнопки платформа "вправо вправо"
        private void ManPlatformRightButton_Checked(object sender, RoutedEventArgs e)
        {
            model.PlatformToLeft();
            bt.Add(sender as Button);
            (sender as Button).IsEnabled = false;
        }

        //Обработчик нажатия кнопки STOP
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            model.StopButton();
        }

        //Обработчик нажатия кнопки "Перейти на координаты"
        private void GotoButton_Click(object sender, RoutedEventArgs e)
        {
            int x = Convert.ToUInt16(GotoXTextBox.Text);
            int y = Convert.ToUInt16(GotoYTextBox.Text);
            x = x > model.MaxX ? model.MaxX : x;
            y = y > model.MaxY ? model.MaxY : y;
            model.GotoXY(x, y);
            bt.Add((Button)sender);
            (sender as Button).IsEnabled = false;
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

        //При клике по кнопке движение до следующего ряда
        private void FartherButton_NextLine(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            model.NextLineFartherCommand();
            bt.Add((Button)sender);
            (sender as Button).IsEnabled = false;
        }
        private void CloserButton_NextLine(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            model.NextLineCloserCommand();
            bt.Add((Button)sender);
            (sender as Button).IsEnabled = false;
        }
        private void UpButton_NextLine(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            model.NextLineUpCommand();
            bt.Add((Button)sender);
            (sender as Button).IsEnabled = false;
        }
        private void DownButton_NextLine(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            model.NextLineDownCommand();
            bt.Add((Button)sender);
            (sender as Button).IsEnabled = false;
        }

        //В зависимости от состояния чекбокса выбираем действия кнопок
        private void LineMotionCheckbox_Checked(object sender, RoutedEventArgs e)
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

            FartherButton.IsEnabled = true;
            CloserButton.IsEnabled = true;
            UpButton.IsEnabled = true;
            DownButton.IsEnabled = true;
        }

        //обрабатывает нажатие кнопок "привезти" и "увезти" в полуавтоматическом режиме
        private void BringOrTakeAwaySemiAutoButton_Click(object sender, RoutedEventArgs e)
        {
            bool stack = RackSemiAutoComboBox.SelectedIndex == 1;
            int r = RowSemiAutoComboBox.SelectedIndex + 1;
            int f = FloorSemiAutoCombobox.SelectedIndex + 1;
            //если была нажата кнопка привезти устанавливае переменную в true
            bool bring = sender == BringSemiAutoButton ? true : false;
            model.BringOrTakeAwayCommand(stack,r,f,bring);           
            bt.Add(sender as Button);
            (sender as Button).IsEnabled = false; 
        }

        //в автоматическом режиме даем команду на подвоз контейнера
        private void BringAutoButton_Click(object sender, RoutedEventArgs e)
        {
            int i = OrdersLitsView.SelectedIndex;
            if (model.SelectOrder(i))
            {
                //Даем команду привезти
                model.BringOrTakeAwayOrder(true);
                //Выключаем кнопку
                BringAutoButton.IsEnabled = false;
                //Заменяем обработчик события "команда выполена"
                model.CommandDone -= CommandDone;
                model.CommandDone += BringDone;
            }
        }
         
        //после доставки разрешаем кнопку "увезти"
        private void BringDone()
        {
            TakeAwayAutoButton.IsEnabled = true;
        }

        //увозим контейнер на место
        private void TakeAwayAutoButton_Click(object sender, RoutedEventArgs e)
        {
            int i = OrdersLitsView.SelectedIndex;
            //Если не выбран ни один элемент - выходим
            if (i < 0) return;
            //Даем команду привезти
            model.BringOrTakeAwayOrder(false);
            //Выключаем кнопку
            TakeAwayAutoButton.IsEnabled = false;
            //Заменяем обработчик события "команда выполена"
            model.CommandDone -= BringDone;
            model.CommandDone += TakeAwayDone;
        }

        //после доставки  на место разрешаем кнопкку "привезти"
        private void TakeAwayDone()
        {
            //отжимаем кнопку
            BringAutoButton.IsEnabled = true;
            //возвращаем обработчик события
            model.CommandDone -= TakeAwayDone;
            model.CommandDone += CommandDone;
            //завершаем заявку
            model.FinishOrder(true);
        }
        
        //нажатие кнопки "отменить заявку"
        private void CancelAutoButton_Click(object sender, RoutedEventArgs e)
        {
            int i = OrdersLitsView.SelectedIndex;
            //Если не выбран ни один элемент - выходим
            if (model.SelectOrder(i)) model.FinishOrder(false);
        }

        private void WeighButton_Click(object sender, RoutedEventArgs e)
        {
            bt.Add(sender as Button);
            (sender as Button).IsEnabled = false;
            WeightPointCollection.Clear();
            MeasuredWeight1PointCollection.Clear();
            MeasuredWeight2PointCollection.Clear();
            model.CoordinateReaded += MakeGraph;
            model.CommandDone += WeighDone;
            model.Weigh();
        }

        private void MakeGraph()
        {
            double w = 450 - model.Weight;
            Point point = new Point(c*10, w);
            Dispatcher.Invoke(() => WeightPointCollection.Add(point));
            c++;
        }

        private void WeighDone()
        {
            //рисуем 7 перпендикулярных красных линий ;-)
            int y = 450 - model.MeasuredWeight;
            Point point11 = new Point(0,y);
            Point point12 = new Point(300, y);
            Dispatcher.Invoke(() => MeasuredWeight1PointCollection.Add(point11));
            Dispatcher.Invoke(() => MeasuredWeight1PointCollection.Add(point12));
                       
            y = 450 - model.MeasuredWeight2;
            Point point21 = new Point(0, y);
            Point point22 = new Point(300, y);
            Dispatcher.Invoke(() => MeasuredWeight2PointCollection.Add(point21));
            Dispatcher.Invoke(() => MeasuredWeight2PointCollection.Add(point22));

            UInt16 w =(UInt16)((model.MeasuredWeight - 267) * 100 / 42f);
            Dispatcher.Invoke(() => MeasuredWeightLabel.Content = w.ToString()+" кг");

            w = (UInt16)((model.MeasuredWeight2 - 227) * 100 / 30f);
            Dispatcher.Invoke(() => MeasuredWeightLabel2.Content = w.ToString() + " кг");

            model.CommandDone -= WeighDone;
            model.CoordinateReaded -= MakeGraph;
            c = 0;
        }
    }
}

