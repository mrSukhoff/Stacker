
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
        delegate void ChangeButtonState();

        //контроллер паттерна MVC
        Controller control;

        //##########################################################################################################################
        //Основная точка входа ----------------------------------------------------------------------------------------------------!
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            control = new Controller();
            
            //Настраиваем визуальные компоненты
            SetUpButtons();

            //Настраиваем вид списка заявок
            GridSetUp();
        }
                
        //Настраиваем визуальные компоненты
        private void SetUpButtons()
        {
            //Подписываем кнопки рядов
            LeftRackSemiAutoButton.Content = control.LeftRackName;
            RightRackSemiAutoButton.Content = control.RightRackName;

            //Заполняем combobox'ы номерами рядов
            int[] rowItems = new int[control.StackerDepth - 1];
            for (int i = 0; i < rowItems.Length; i++) { rowItems[i] = i + 1; }
            RowSemiAutoComboBox.ItemsSource = rowItems;
            RowComboBox.ItemsSource = rowItems;
            RowSemiAutoComboBox.SelectedIndex = 0;
            RowComboBox.SelectedIndex = 0;

            // .. и этажей
            int[] floorItems = new int[control.StackerHight];
            for (int i = 0; i < floorItems.Length; i++) { floorItems[i] = i + 1; }
            FloorSemiAutoCombobox.ItemsSource = floorItems;
            FloorComboBox.ItemsSource = floorItems;
            FloorSemiAutoCombobox.SelectedIndex = 0;
            FloorComboBox.SelectedIndex = 0;

            RackComboBox.Items.Add(control.LeftRackName);
            RackComboBox.Items.Add(control.RightRackName);
            RackComboBox.SelectedIndex = 0;

            //присваеваем обработчики тут, а не в визуальной части, чтобы они не вызывались 
            //во время первоначальных настроек
            LeftRackSemiAutoButton.Checked += LeftRackManualButton_Click;
            LeftRackSemiAutoButton.Unchecked += LeftRackManualButton_Click;
            RightRackSemiAutoButton.Unchecked += RightRackManualButton_Click;
            RightRackSemiAutoButton.Checked += RightRackManualButton_Click;

            RowSemiAutoComboBox.SelectionChanged += ManualComboBox_SelectionChanged;
            FloorSemiAutoCombobox.SelectionChanged += ManualComboBox_SelectionChanged;
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
            LeftRackManualButton_Click(null, null);

            ErrorListView.ItemsSource = control.ErrorList; 
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
            OrdersLitsView.ItemsSource = control.Orders;

        }
        
        //При изменении адреса ячеек перечитываем координаты
        private void CellChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                //вычисляем адрес ячейки
                bool stacker = RackComboBox.SelectedIndex != 0;
                int row = RowComboBox.SelectedIndex;
                int floor = FloorComboBox.SelectedIndex;

                //получаем координаты
                control.GetCell(stacker, row, floor, out int x, out int y, out bool isNOTAvailable);

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
                bool stacker = RackComboBox.SelectedIndex == 0;
                int row = RowComboBox.SelectedIndex;
                int floor = FloorComboBox.SelectedIndex;

                //если поле пустое, то записываем в него ноль
                CoordinateXTextBox.Text = CoordinateXTextBox.Text == "" ? "0" : CoordinateXTextBox.Text;
                CoordinateYTextBox.Text = CoordinateYTextBox.Text == "" ? "0" : CoordinateYTextBox.Text;

                //получаем целые значения координат
                int x = Convert.ToInt32(CoordinateXTextBox.Text);
                int y = Convert.ToInt32(CoordinateYTextBox.Text);
                
                //если координата больше максимальноразрешшенной, устанавливаем ее максимальной
                if (x > control.MaxX)
                {
                    x = control.MaxX;
                    CoordinateXTextBox.Text = x.ToString();
                }
                if (y > control.MaxY)
                {
                    y = control.MaxY;
                    CoordinateYTextBox.Text = y.ToString();
                }
                
                bool isNotAvailable = (bool)IsNOTAvailableCheckBox.IsChecked;
                //записываем
                control.SetCell(stacker,row,floor,x,y,isNotAvailable);
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
                control.SaveCells();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        //отжимаем) противоположную кнопку
        private void RightRackManualButton_Click(object sender, RoutedEventArgs e)
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
            ManualComboBox_SelectionChanged(sender, null);
        }
        private void LeftRackManualButton_Click(object sender, RoutedEventArgs e)
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
            ManualComboBox_SelectionChanged(sender, null);
        }

        //Проверка вводимых в textbox символы на соотвктствие правилам
        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Match match = CoordinateRegex.Match(e.Text);
            if ((!match.Success) || (sender as TextBox).Text.Length > 5) e.Handled = true;
        }

        //при изменении выбранных ячеек в полуавтоматическом режиме меняет доступность кнопок в зависимости от доступности ячейки
        private void ManualComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                bool stacker = RightRackSemiAutoButton.IsChecked == true;
                int r = RowSemiAutoComboBox.SelectedIndex;
                int f = FloorSemiAutoCombobox.SelectedIndex;

                control.GetCell(stacker, r, f,out int x,out int y,out bool isNotAvailable);

                bool isEnabled = !isNotAvailable;

                BringSemiAutoButton.IsEnabled = isEnabled;
                TakeAwaySemiAutoButton.IsEnabled = isEnabled;
                ManualAddressLabel.IsEnabled = isEnabled;

                char rack = LeftRackSemiAutoButton.IsChecked == true ? control.LeftRackName : control.RightRackName;
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
            if (control != null) control.Dispose();
        }

        //Обработчик нажатия кнопки подтверждения ошибки
        private void SubmitErrorButton_Click(object sender, RoutedEventArgs e)
        {
            control.SubmitError();
            ErrorListView.Items.Refresh();
            ManPlatformToLeftButton.IsEnabled = true;
            ManPlatformToRightButton.IsEnabled = true;
            GotoButton.IsEnabled = true;
            BringSemiAutoButton.IsEnabled = true;
            TakeAwaySemiAutoButton.IsEnabled = true;
            BringAutoButton.IsEnabled = true;
            TakeAwayAutoButton.IsEnabled = true;
        }

        //Обработчик нажатия кнопки "ближе"
        private void CloserButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            control.CloserButton(true);
        }
        private void CloserButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            control.CloserButton(false);
        }

        //Обработчик нажатия кнопки "дальше"
        private void FartherButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            control.FartherButton(true);
        }
        private void FartherButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            control.FartherButton(false);
        }

        //Обработчик нажатия кнопки "вверх"
        private void UpButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            control.UpButton(true);
        }
        private void UpButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            control.UpButton(false);
        }

        //Обработчик нажатия кнопки "вниз"
        private void DownButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            control.DownButton(true);
        }
        private void DownButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            control.DownButton(state: false);
        }

        //Обработчик нажатия кнопки "платформа влево"
        private void ManPlatformToLeftButton_Checked(object sender, RoutedEventArgs e)
        {
            control.PlatformToLeft();   
            bt = sender as Button;
            bt.IsEnabled = false;
        }

        //Обработчик нажатия кнопки платформа "вправо вправо"
        private void ManPlatformToRightButton_Checked(object sender, RoutedEventArgs e)
        {
            control.PlatformToRight();
            bt = sender as Button;
            bt.IsEnabled = false;
        }

        //Обработчик нажатия кнопки STOP
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            control.StopButton();
        }

        //Обработчик нажатия кнопки "Перейти на координаты"
        private void GotoButton_Click(object sender, RoutedEventArgs e)
        {
            int x = Convert.ToUInt16(GotoXTextBox.Text);
            int y = Convert.ToUInt16(GotoYTextBox.Text);
            x = x > control.MaxX ? control.MaxX : x;
            y = y > control.MaxY ? control.MaxY : y;
            control.GotoXY(x, y);
            bt = sender as Button;
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
            control.NextLineFartherCommand();
            bt = sender as Button;
            bt.IsEnabled = false;
        }
        private void CloserButton_NextLine(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            control.NextLineCloserCommand();
            bt = sender as Button;
            bt.IsEnabled = false;
        }
        private void UpButton_NextLine(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            control.NextLineUpCommand();
            bt = sender as Button;
            bt.IsEnabled = false;
        }
        private void DownButton_NextLine(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            control.NextLineDownCommand();
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

        //обрабатывает нажатие кнопки "привезти"
        private void BringSemiAutoButton_Click(object sender, RoutedEventArgs e)
        {
            bool stacker = RightRackSemiAutoButton.IsChecked == true;
            int r = RowSemiAutoComboBox.SelectedIndex;
            int f = FloorSemiAutoCombobox.SelectedIndex;
            control.BringOrTakeAwayCommand(stacker,r,f,true);           
            r++;f++;
            bt = sender as Button;
            bt.IsEnabled = false;
        }

        //обрабатывает нажатие кнопки "увезти"
        private void TakeAwayManualButton_Click(object sender, RoutedEventArgs e)
        {
            bool stacker = RightRackSemiAutoButton.IsChecked == true;
            int r = RowSemiAutoComboBox.SelectedIndex;
            int f = FloorSemiAutoCombobox.SelectedIndex;
            control.BringOrTakeAwayCommand(stacker, r, f, false);
            r++; f++;
            bt = sender as Button;
            bt.IsEnabled = false;
        }
                

    }
}

