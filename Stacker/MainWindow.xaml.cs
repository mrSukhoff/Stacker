
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

        //Кнопка, выдавшая задание)
        Button bt = null;
        
        //контроллер паттерна MVC
        StackerModel model;

        delegate void RefreshStatusBar();
        
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
                //model.ErrorAppeared += ErrorAppeared;
                model.SomethingChanged += UpdateCoordinate;

                //Настраиваем визуальные компоненты
                SetUpButtons();

                //Настраиваем вид списка заявок
                GridSetUp();
            }
        }
                
        //Настраиваем визуальные компоненты
        private void SetUpButtons()
        {
            //Подписываем кнопки рядов
            LeftRackSemiAutoButton.Content = model.LeftRackName;
            RightRackSemiAutoButton.Content = model.RightRackName;
            ManPlatformLeftButton.Content = model.LeftRackName; 
            ManPlatformRightButton.Content = model.RightRackName;

            //Заполняем combobox'ы номерами рядов
            int[] rowItems = new int[model.StackerDepth];
            for (int i = 0; i < rowItems.Length; i++) { rowItems[i] = i + 1; }
            RowSemiAutoComboBox.ItemsSource = rowItems;
            RowComboBox.ItemsSource = rowItems;
            RowSemiAutoComboBox.SelectedIndex = 0;
            RowComboBox.SelectedIndex = 0;

            // .. и этажей
            int[] floorItems = new int[model.StackerHight];
            for (int i = 0; i < floorItems.Length; i++) { floorItems[i] = i + 1; }
            FloorSemiAutoCombobox.ItemsSource = floorItems;
            FloorComboBox.ItemsSource = floorItems;
            FloorSemiAutoCombobox.SelectedIndex = 0;
            FloorComboBox.SelectedIndex = 0;

            RackComboBox.Items.Add(model.LeftRackName);
            RackComboBox.Items.Add(model.RightRackName);
            RackComboBox.SelectedIndex = 0;

            //присваеваем обработчики тут, а не статически, чтобы они не вызывались 
            //во время первоначальных настроек
            //полуавтомат
            LeftRackSemiAutoButton.Checked += LeftRackSemiAutoButton_Click;
            LeftRackSemiAutoButton.Unchecked += LeftRackSemiAutoButton_Click;
            RightRackSemiAutoButton.Unchecked += RightRackSemiAutoButton_Click;
            RightRackSemiAutoButton.Checked += RightRackSemiAutoButton_Click;
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

            //включаем кнопку левого ряда
            LeftRackSemiAutoButton.IsChecked = true;
            LeftRackSemiAutoButton_Click(null, null);

            //источник данных для списка ошибок
            ErrorListView.ItemsSource = model.ErrorList;

            //изначально кнопка "увезти" не актиквна, так как предполагается, что увозить пока нечего
            TakeAwayAutoButton.IsEnabled = false;

            //считываем координаты
            CellChanged(null,null);
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
            Dispatcher.Invoke(UnblockButton);
        }

        private void UnblockButton()
        {
            if (bt!=null) bt.IsEnabled = true;
            bt = null;
        }

        //обработчик события "ошибка"
        private void ErrorAppeared() => ErrorListView.Items.Refresh();

        //Обновление координат и слова состояния
        private void UpdateCoordinate()
        {
            string sw = Convert.ToString(model.StateWord, 2);
            while (sw.Length < 15) sw = "0" + sw;
            Dispatcher.Invoke(new RefreshStatusBar (() =>  StateWordLabel.Content = "State Word : " +sw));

            Dispatcher.Invoke(new RefreshStatusBar(() => RowLabel.Content = "R:" + model.ActualRow));
            Dispatcher.Invoke(new RefreshStatusBar(() => FloorLabel.Content = "F:" + model.ActualFloor));
            Dispatcher.Invoke(new RefreshStatusBar(() => XLabel.Content = "X:" + model.ActualX));
            Dispatcher.Invoke(new RefreshStatusBar(() => YLabel.Content = "Y:" + model.ActualY));
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
        
        //отжимаем) противоположную кнопку
        private void RightRackSemiAutoButton_Click(object sender, RoutedEventArgs e)
        {
            if (RightRackSemiAutoButton.IsChecked == true)
            {
                RightRackSemiAutoButton.Effect = null;
                LeftRackSemiAutoButton.IsChecked = false;
            }
            else
            {
                LeftRackSemiAutoButton.IsChecked = true;
                LeftRackSemiAutoButton.Effect = null;
            }
            SemiAutoComboBox_SelectionChanged(sender, null);
        }
        private void LeftRackSemiAutoButton_Click(object sender, RoutedEventArgs e)
        {
            if (LeftRackSemiAutoButton.IsChecked == true)
            {
                LeftRackSemiAutoButton.Effect = null;
                RightRackSemiAutoButton.IsChecked = false;
            }
            else
            {
                RightRackSemiAutoButton.IsChecked = true;
                RightRackSemiAutoButton.Effect = null;
            }
            SemiAutoComboBox_SelectionChanged(sender, null);
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
                bool stacker = RightRackSemiAutoButton.IsChecked == true;
                int r = RowSemiAutoComboBox.SelectedIndex + 1;
                int f = FloorSemiAutoCombobox.SelectedIndex + 1;

                model.GetCell(stacker, r, f,out int x,out int y,out bool isNotAvailable);

                bool isEnabled = !isNotAvailable;

                BringSemiAutoButton.IsEnabled = isEnabled;
                TakeAwaySemiAutoButton.IsEnabled = isEnabled;
                SemiAutoAddressLabel.IsEnabled = isEnabled;

                char rack = LeftRackSemiAutoButton.IsChecked == true ? model.LeftRackName : model.RightRackName;
                SemiAutoAddressLabel.Content = rack + " - " + r.ToString() + " - " + f.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "SemiAutoComboBox_SelectionChanged");
            }
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
            bt = sender as Button;
            bt.IsEnabled = false;
        }

        //Обработчик нажатия кнопки платформа "вправо вправо"
        private void ManPlatformRightButton_Checked(object sender, RoutedEventArgs e)
        {
            model.PlatformToLeft();
            bt = sender as Button;
            bt.IsEnabled = false;
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
            bt = (Button)sender;
            bt.IsEnabled = false;
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
            model.NextLineFartherCommand();
            bt = sender as Button;
            bt.IsEnabled = false;
        }
        private void CloserButton_NextLine(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            model.NextLineCloserCommand();
            bt = sender as Button;
            bt.IsEnabled = false;
        }
        private void UpButton_NextLine(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            model.NextLineUpCommand();
            bt = sender as Button;
            bt.IsEnabled = false;
        }
        private void DownButton_NextLine(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            model.NextLineDownCommand();
            bt = sender as Button;
            bt.IsEnabled = false;
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
            bool stacker = RightRackSemiAutoButton.IsChecked == true;
            int r = RowSemiAutoComboBox.SelectedIndex + 1;
            int f = FloorSemiAutoCombobox.SelectedIndex + 1;
            //если была нажата кнопка привезти устанавливае переменную в true
            bool bring = sender == BringSemiAutoButton ? true : false;
            model.BringOrTakeAwayCommand(stacker,r,f,bring);           
            bt = sender as Button;
            bt.IsEnabled = false;
        }

        //в автоматическом режиме даем команду на подвоз контейнера
        private void BringAutoButton_Click(object sender, RoutedEventArgs e)
        {
            int i = OrdersLitsView.SelectedIndex;
            //Если не выбран ни один элемент - выходим
            if (i < 0) return;
            //Устанавливаем номер заявки
            model.SelectOrder(i);
            //Даем команду привезти
            model.BringOrTakeAwayOrder(true);
            //Выключаем кнопку
            BringAutoButton.IsEnabled = false;
            //Заменяем обработчик события "команда выполена"
            model.CommandDone -= CommandDone;
            model.CommandDone += BringDone;
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
            if (i < 0) return;

            model.SelectOrder(i);
            model.FinishOrder(false);
        }


    }
}

