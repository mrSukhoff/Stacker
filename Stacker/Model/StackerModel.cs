using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Windows;


namespace Stacker.Model
{
    public class StackerModel : IDisposable
    {
        //видимые свойства объекта ****************************************************************************

        //хранилище настроек
        public SettingsKeeper Settings;
        //менеджер заявок
        public OrdersManager OrderManager;
        //команды управления краном
        public CraneCommands Crane;

        //События
        public delegate void StackerModelEventHandler();
        //появился флаг завершения выполнения команды
        public event StackerModelEventHandler CommandDone = (() => { });
        //флаг ошибки
        public event StackerModelEventHandler ErrorAppeared = (() => { });
        //происходит после очередного считывания текущих координат крана
        public event StackerModelEventHandler CoordinateReaded = (() => { });
        //изменилось слово состояния контроллера
        public event StackerModelEventHandler StateWordChanged = (() => { });

        //Актуальные координаты крана
        public int ActualX { get; private set; }
        public int ActualY { get; private set; }
        public int ActualRow { get; private set; }
        public int ActualFloor { get; private set; }

        //список ошибок контроллера
        public ObservableCollection<string> ErrorList { get; private set; } = new ObservableCollection<string>();

        //Слово состояния контроллера
        public int StateWord { get; private set; } = 0;

        //вес или что-то измеренное с частотника
        //мгновенное значение тока
        public int Weight { get; private set; }
        //вес на подъеме
        public int MeasuredWeight { get; private set; }
        //вес на спуске
        public int MeasuredWeight2 { get; private set; }

        //флаг наличия контейнера на кране
        public bool IsBinOnPlatform;

        //флаг нахождения крана на начальной позиции
        public bool IsStartPosiotion;

        //флаг нахождения крана на метке ряда
        public bool IsRowMark;
        
        //флаг нахождения крана на начальной позиции
        public bool IsFloorMark;

        //флаг успешности подключения
        public bool IsConnected = false;

        //Контроллер крана
        internal Controller PLC;

        //Координаты ячеек
        internal CellsGrid Stacker;

        //внутренние поля класса ******************************************************************************
        private char LeftRackName;
        private char RightRackName;

        //Таймер для чтения слова состояния контроллера
        private Timer PlcTimer;

        //флаг уничтожения объектов
        private bool disposed = false;
        
        //Конструктор класса **********************************************************************************
        public StackerModel()
        {
            //Инициализируем хранилище настроек
            Settings = new SettingsKeeper();
            
            //Создаем менеджер заявок
            OrderManager = new OrdersManager(this);

            LeftRackName = Settings.LeftRackName;
            RightRackName = Settings.RightRackName;

            //Загружаем таблицы координат ячеек
            string path = Environment.CurrentDirectory+"\\"+Settings.CellsFile;
            Stacker = File.Exists(path) ? new CellsGrid(path) : new CellsGrid(Settings.StackerDepth, Settings.StackerHight);
            
            try
            {
                //создаем контроллер
                PLC = new Controller(Settings.ComPort);
                //временно включаем ручной режим
                PLC.WriteDword(8, 1);
                //Записываем максимальные значения координат
                PLC.WriteDword(10, Settings.MaxX);
                PLC.WriteDword(12, Settings.MaxY);
                //и максимальные значения ячеек
                PLC.WriteDword(14, 29);
                PLC.WriteDword(16, 16);
                //записываем максимальный вес
                PLC.WriteDword(18, Settings.MaxWeight);
                //запускаем таймер на чтение сосотояния контроллера
                PlcTimer = new Timer(ReadStateWord, null, 0, 500);
                IsConnected = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "Ошибка открытия порта");
            }

            Crane = new CraneCommands(this);
        }

        //завершение работы программы
        ~StackerModel()
        {
            Dispose(false);
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
                    // Освобождаем управляемые ресурсы
                    PlcTimer?.Dispose();
                    PLC?.Dispose();
                    OrderManager.Dispose();
                }
                // освобождаем неуправляемые объекты
                disposed = true;
            }
        }
        
        //Сохранение массивов координат ячеек в файлы
        public void SaveCells()
        {
            string path = Environment.CurrentDirectory + "\\" + Settings.CellsFile;
            Stacker.SaveCellsGrid(path);
        }
        
        //По таймеру читаем слово состояния контроллера
        private void ReadStateWord(object ob)
        {
            if (PLC == null) return;
            try
            {
                //читаем оптом из ПЛК актуальные координаты крана
                ushort[] word = PLC.ReadHoldingRegisters(0x1198, 8);

                //и записываем их значения
                ActualX = word[0] + 0x10000 * word[1];
                ActualY = word[2] + 0x10000 * word[3];
                ActualRow = word[4];
                ActualFloor = word[6];

                //читаем слово состояния ПЛК
                word = PLC.ReadHoldingRegisters(0x1064, 8);

                int stateWord = word[0];
                Weight = word[2] > 32767 ? 0 : word[2];
                MeasuredWeight = word[4];
                MeasuredWeight2 = word[6];

                //вызываем событие по чтению координат
                CoordinateReaded();

                //если поменялось слово состояния
                if (stateWord != StateWord)
                {
                    IsStartPosiotion = GetBitState(stateWord, 0);
                    IsRowMark = GetBitState(stateWord, 8);
                    IsFloorMark = GetBitState(stateWord, 9);
                    IsBinOnPlatform = GetBitState(stateWord, 10);
                    StateWordChanged();
                }

                //если появился флаг завершения
                if (GetBitState(stateWord, 15) && !GetBitState(StateWord, 15)) CommandDone();

                //если появился флаг ошибки вызываем обрабочик ошибок
                if (GetBitState(stateWord, 13) && !GetBitState(StateWord, 13)) ErrorHandler();

                StateWord = stateWord;
            }
            catch (Exception ex)
            {
                PlcTimer.Dispose();
                MessageBox.Show(ex.Message, caption: "ReadStateWord");
            }
        }

        //вызывается при появления флага ошибки в слове состояния
        private void ErrorHandler()
        {
            PLC.ReadDword(110, out int ErrorWord);
            if (GetBitState(ErrorWord, 0)) addAlarm("Нажата кнопка аварийной остановки");
            if (GetBitState(ErrorWord, 1)) addAlarm("Одновременное включение контакторов");
            if (GetBitState(ErrorWord, 2)) addAlarm("Попытка загрузки на занятый кран");
            if (GetBitState(ErrorWord, 3)) addAlarm("Ячейка для установки ящика занята");
            if (GetBitState(ErrorWord, 4)) addAlarm("Обнаружена помеха вертикальному перемещению крана");
            if (GetBitState(ErrorWord, 5)) addAlarm("Ошибка преобразователя частоты №1");
            if (GetBitState(ErrorWord, 6)) addAlarm("Ошибка преобразователя частоты №2");
            if (GetBitState(ErrorWord, 7)) addAlarm("Попытка установить большой ящик не на первый этаж");
            if (GetBitState(ErrorWord, 8)) addAlarm("Попытка установить средний ящик выше седьмого этажа");
            if (GetBitState(ErrorWord, 9)) addAlarm("Ошибка перемещения платформы");
            if (GetBitState(ErrorWord, 10)) addAlarm("Ошибка позиционирования крана");
            if (GetBitState(ErrorWord, 11)) addAlarm("Помеха движению по горизонтали");
            if (GetBitState(ErrorWord, 12)) addAlarm("Превышен максимальный вес груза");
            ErrorAppeared?.Invoke();

            void addAlarm(string alarmText)
            {
                string str = DateTime.Now.ToString() + " : " + alarmText;
                App.Current.Dispatcher.Invoke(() => ErrorList.Add(str));
                try
                {
                    //записываем в лог 
                    File.AppendAllText("Errors.log",str+'\r'+'\n', System.Text.Encoding.Default);
                }
                catch (Exception ex)
                { MessageBox.Show(ex.Message, caption: "ErrorHandler"); }
            }
        }
       
        //метод возвращает состояния указанного бита
        private bool GetBitState(int b, int num)
        {
            bool[] bits = new bool[16];
            int z = 1;
            for (int i = 0; i < 16; i++)
            {
                bits[i] = ((b & z) == z);
                z *= 2;
            }
            return bits[num];
        }

        //выдает по адресу ячейки её координаты и доступность
        public void GetCell(char r, int row, int floor, out int x, out int y, out bool isNotAvailable)
        {
            //if (r != LeftRackName & r != RightRackName) throw new ArgumentException("Неправильное имя стойки");
            if (r == '\0') r = LeftRackName;
            if (row < 1) row = 1;
            if (floor < 1) floor = 1;
            x = Stacker[row, floor].X;
            y = Stacker[row, floor].Y;
            isNotAvailable = r == LeftRackName ? Stacker[row, floor].LeftSideIsNotAvailable : Stacker[row, floor].RightSideIsNotAvailable;
        }
        
        //устанавливает для ячейки её координаты и доступность
        //левый стеллаж r = false
        public void SetCell(bool r, int row, int floor, int x, int y, bool isNotAvailable)
        {
            if ((x < 0) || (y < 0) || (x > Settings.MaxX) || (y > Settings.MaxY)) throw new ArgumentException();
            
            Stacker[row, floor].X = x;
            Stacker[row, floor].Y = y;
            if (!r) Stacker[row, floor].LeftSideIsNotAvailable  = isNotAvailable;
            else Stacker[row, floor].RightSideIsNotAvailable = isNotAvailable;
        }
    }
}
