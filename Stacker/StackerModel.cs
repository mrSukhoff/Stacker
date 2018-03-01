using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using Modbus.Device;


namespace Stacker
{
    class StackerModel : IDisposable
    {
        //видимые свойства объекта ****************************************************************************
    
        //Максимальные значения координат
        public int MaxX { get; } = 55000;
        public int MaxY { get; } = 14000;
        
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
        public List<string> ErrorList { get; private set; } = new List<string>();

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

        //внутренние поля класса ******************************************************************************

        OrdersManager OrdersManager;
        SettingsKeeper Settings;

        private char LeftRackName;
        private char RightRackName;

        //Таймер для чтения слова состояния контроллера
        private Timer PlcTimer;
        
        //Координаты ячеек
        private CellsGrid Stacker;
        
        //Com-порт к которому подсоединен контроллер
        private SerialPort ComPort = null;
        
        //интерфейс контроллера
        private IModbusMaster PLC;

        //флаг уничтожения объектов
        private bool disposed = false;
        
        //Конструктор класса **********************************************************************************
        public StackerModel(OrdersManager ordersManager, SettingsKeeper settingsKeeper)
        {
            OrdersManager = ordersManager;
            Settings = settingsKeeper; 

            LeftRackName = Settings.LeftRackName;
            RightRackName = Settings.RightRackName;

            //Загружаем таблицы координат ячеек
            string path = Environment.CurrentDirectory+"\\"+Settings.CellsFile;
            Stacker = File.Exists(path) ? new CellsGrid(path) : new CellsGrid(Settings.StackerDepth, Settings.StackerHight);
            
            //Открываем порт и создаем контроллер
            try
            {
                ComPort = new SerialPort(Settings.ComPort, 115200, Parity.Even, 7, StopBits.One);
                ComPort.Open();
                PLC = ModbusSerialMaster.CreateAscii(ComPort);
                //временно включаем ручной режим
                WriteDword(8, 1);
                //Записываем максимальные значения координат
                WriteDword(10, MaxX);
                WriteDword(12, MaxY);
                //и максимальные значения ячеек
                WriteDword(14, 29);
                WriteDword(16, 16);
                //записываем максимальный вес
                WriteDword(18, Settings.MaxWeight);
                //запускаем таймер на чтение сосотояния контроллера
                PlcTimer = new Timer(ReadStateWord, null, 0, 500);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "Ошибка открытия порта");
            }

            //if (!ComPort.IsOpen) throw new NullReferenceException("!");
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
                    if (PlcTimer != null) PlcTimer.Dispose();
                    if (PLC != null) PLC.Dispose();
                    if (ComPort != null) ComPort.Dispose();
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

        //Записывает 32-битное число в контроллер
        public bool WriteDword(int adr, int d)
        {
            try
            {
                ushort dlo = (ushort)(d % 0x10000);
                ushort dhi = (ushort)(d / 0x10000);
                UInt16 address = Convert.ToUInt16(adr);
                address += 0x1000;
                PLC.WriteSingleRegister(1, address, dlo);
                PLC.WriteSingleRegister(1, ++address, dhi);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "WriteDwordToPLC");
                return false;
            }
        }

        //Читает 32-битное число из контроллера
        public bool ReadDword(ushort address, out int d)
        {
            try
            {
                d = 0;
                address += 0x1000;
                ushort[] x = PLC.ReadHoldingRegisters(1, address, 2);
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
        public bool ReadMerker(ushort address, out bool m)
        {
            try
            {
                bool[] ms;
                address += 0x800;
                ms = PLC.ReadCoils(1, address, 1);
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
        public bool SetMerker(ushort address, bool m)
        {
            try
            {
                address += 0x800;
                PLC.WriteSingleCoil(1, address, m);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "SetMerker");
                return false;
            }
        }

        //По таймеру читаем слово состояния контроллера
        private void ReadStateWord(object ob)
        {
            if (PLC == null) return;
            try
            {
                //читаем оптом из ПЛК актуальные координаты крана
                ushort[] word = PLC.ReadHoldingRegisters(1, 0x1198, 8);

                //и записываем их значения
                ActualX = word[0] + 0x10000 * word[1];
                ActualY = word[2] + 0x10000 * word[3];
                ActualRow = word[4];
                ActualFloor = word[6];

                //читаем слово состояния ПЛК
                word = PLC.ReadHoldingRegisters(1, 0x1064, 8);

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
            ReadDword(110, out int ErrorWord);
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
                ErrorList.Add(str);
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
            if (r != LeftRackName & r != RightRackName) throw new ArgumentException("Неправильное имя стойки");
            x = Stacker[row, floor].X;
            y = Stacker[row, floor].Y;
            isNotAvailable = r == LeftRackName ? Stacker[row, floor].LeftSideIsNotAvailable : Stacker[row, floor].RightSideIsNotAvailable;
        }
        
        //устанавливает для ячейки её координаты и доступность
        //левый стеллаж r = false
        public void SetCell(bool r, int row, int floor, int x, int y, bool isNotAvailable)
        {
            if ((x < 0) || (y < 0) || (x > MaxX) || (y > MaxY)) throw new ArgumentException();
            
            Stacker[row, floor].X = x;
            Stacker[row, floor].Y = y;
            if (!r) Stacker[row, floor].LeftSideIsNotAvailable  = isNotAvailable;
            else Stacker[row, floor].RightSideIsNotAvailable = isNotAvailable;
        }

        //*команда подтверждения ошибок в ПЛК и очистка списка ошибок
        public void SubmitError()
        {
            ErrorList.Clear();
            if (PLC != null)
            {
                WriteDword(8, 0);
                SetMerker(101, true);
            }
        }

        //*команда дальше
        public void FartherButton(bool state)
        {
            if (PLC != null)
            {
                //устанавливаем ручной режим передвижения
                WriteDword(8, 1);
                //задаем команду "движение дальше"
                SetMerker(10, state);
            }
        }

        //*команда ближе
        public void CloserButton(bool state)
        {
            if (PLC != null)
            {
                //устанавливаем ручной режим передвижения
                WriteDword(8, 1);
                //задаем команду "движение ближе"
                SetMerker(11, state);
            }
        }

        //*команда вверх
        public void UpButton(bool state)
        {
            if (PLC != null)
            {
                //устанавливаем ручной режим передвижения
                WriteDword(8, 1);
                //задаем команду "движение вверх"
                SetMerker(12, state);
            }
        }

        //*команда вниз
        public void DownButton(bool state)
        {
            if (PLC != null)
            {
                //устанавливаем ручной режим передвижения
                WriteDword(8, 1);
                //задаем команду "движение вниз"
                SetMerker(13, state);
            }
        }

        //*Команда на движение дальше до следующего ряда
        public void NextLineFartherCommand()
        {
            if (PLC != null)
            {
                //устанавливаем режим движения по координатам
                WriteDword(8, 4);
                //задаем команду "движение дальше"
                SetMerker(10, true);
            }
        }

        //*Команда на движение ближе до следующего ряда
        public void NextLineCloserCommand()
        {
            if (PLC != null)
            {
                //устанавливаем режим движения по координатам
                WriteDword(8, 4);
                //задаем команду "движение ближе"
                SetMerker(11, true);
            }
        }

        //*Команда на движение вверх до следующего этажа
        public void NextLineUpCommand()
        {
            if (PLC != null)
            {
                //устанавливаем режим движения по координатам
                WriteDword(8, 4);
                //задаем команду "движение вверх"
                SetMerker(12, true);
            }
        }

        //*Команда на движение вниз до следующего этажа
        public void NextLineDownCommand()
        {
            if (PLC != null)
            {
                //устанавливаем режим движения по координатам
                WriteDword(8, 4);
                //задаем команду "движение вниз"
                SetMerker(13, true);
            }
        }

        //*команда "платформа влево"
        public void PlatformToLeft()
        {
            if (PLC != null)
            {
                //включаем ручной режим
                WriteDword(8, 1);
                //задаем команду ПЛК "платформа вправо"
                SetMerker(14, true);
            }
        }

        //*команда "платформа вправо"
        public void PlatformToRight()
        {
            if (PLC != null)
            {
                //включаем ручной режим
                WriteDword(8, 1);
                //задаем команду ПЛК "платформа влево"
                SetMerker(15, true);
            }
        }

        //*команда STOP
        public void StopButton()
        {
            if (PLC != null) SetMerker(0, true);
        }

        //*команда "Перейти на координаты"
        public void GotoXY(int x, int y)
        {
            //проверяем аргументы на допустимость
            if ((x < 0) || (y < 0) || (x > MaxX) || (y > MaxY)) throw new ArgumentException();
            if (PLC != null)
            {
                //Включаем режим перемещения по координатам
                WriteDword(8, 3);

                //Записываем координаты в ПЛК
                WriteDword(0, x);
                WriteDword(2, y);

                //даем команду на движение
                SetMerker(20, true);
            }
        }

        //*Команда "привезти/увезти" из/в конкретную ячейку. bring = true - привезти
        public void BringOrTakeAway(bool rack, int row, int floor, bool bring)
        {
            if (PLC != null)
            {
                if (rack ? Stacker[row, floor].RightSideIsNotAvailable : Stacker[row, floor].LeftSideIsNotAvailable)
                    throw new ArgumentException("Ячейка недоступна!");
                int x = Stacker[row, floor].X;
                int y = Stacker[row, floor].Y;
                //требуемая ячейка не может находится в начале штабелера или иметь вертикальную координату 0
                if ( x == 0 || (y==0 && floor!=1) ) throw new ArgumentException("Неверные координаты ячеейки");

                //Включаем режим перемещения по координатам
                WriteDword(8, 2);
                //Пишем координаты
                WriteDword(0, x);
                WriteDword(2, y);
                //Пишем ряд и этаж
                WriteDword(4, row);
                WriteDword(6, floor);
                //Устанваливаем сторону
                SetMerker(2, rack);
                //Устанавливаем флаг в "привезти/увезти"
                SetMerker(3, bring);
                //Даем команду на старт
                SetMerker(1, true);
            }
        }
        
        //*Команда "привезти/увезти" по зараннее установленной заявке, bring = true - привезти
        public void BringOrTakeAway(bool bring)
        {
            Order order = OrdersManager.Orders[OrdersManager.SelectedOrderNumber];
            if (order != null)
            {
                bool rack = order.StackerName == RightRackName;
                int row = order.Row;
                int floor = order.Floor;
                BringOrTakeAway(rack, row, floor, bring);
            }
        }

        //*Команда взвесить
        public void Weigh()
        {
            if (PLC != null)
            {
                //включаем режим взвешивания
                WriteDword(8, 5);
                //задаем команду "взвесить"
                SetMerker(21,true);
            }
        }

        //*читает бит наличие ящика на платформе в слове состояния
        public bool ChekBinOnPlatform()
        {
            int word = 0;
            if (PLC != null) ReadDword(100, out word);
            return GetBitState(word, 10);
        }
    }
}
