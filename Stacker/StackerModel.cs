using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Windows;
using Modbus.Device;


namespace Stacker
{
    class StackerModel:IDisposable
    {
        //видимые свойства объекта ****************************************************************************

        //места хранения файлов заявлок и архива
        private string OrdersFile;
        private string ArchiveFile;

        //размеры, имена и номера штабелеров
        //нулевая позиция по горизонтали - место погрузки
        public int StackerDepth { get; } = 29;
        public int StackerHight { get; } = 16;
        public char LeftRackName { get; private set; }
        public int LeftRackNumber { get; private set; }
        public char RightRackName { get; private set; }
        public int RightRackNumber { get; private set; }
        
        //Максимальные значения координат
        public int MaxX { get; } = 55000;
        public int MaxY { get; } = 14000;

        //коллекция заявок
        public List<Order> Orders { get; private set; } = new List<Order>();
        
        //События
        public delegate void StackerModelEventHandler();
        //появился флаг завершения выполнения команды
        public event StackerModelEventHandler CommandDone = (() => { });
        //флаг ошибки
        public event StackerModelEventHandler ErrorAppeared = (() => { });
        //происходит после очередного считывания текущих координат крана
        public event StackerModelEventHandler CoordinateReaded = (() => { });
        //изменилось слово состояния контроллера
        public event StackerModelEventHandler SomethingChanged = (() => { });

        //Актуальные координаты крана
        public int ActualX { get; private set; }
        public int ActualY { get; private set; }
        public int ActualRow { get; private set; }
        public int ActualFloor { get; private set; }

        //список ошибок контроллера
        public List<string> ErrorList { get; private set; } = new List<string>();

        //Слово состояния контроллера
        public int StateWord { get; private set; }

        //вес или что-то измеренное с частотника
        //мгновенное значение тока
        public int Weight { get; private set; }
        //вес на подъеме
        public int MeasuredWeight { get; private set; }
        //вес на спуске
        public int MeasuredWeight2 { get; private set; }

        //флаг наличия контейнера на кране
        public bool IsBinOnPlatform;

        //внутренние поля класса ******************************************************************************

        // переменная для контроля изменения файла заявок
        private DateTime LastOrdersFileAccessTime = DateTime.Now;

        // таймер для контроля изменения файла заявок
        private Timer FileTimer;
        
        //Таймер для чтения слова состояния контроллера
        private Timer PlcTimer;
        
        //Координаты ячеек
        private CellsGrid LeftStacker;
        private CellsGrid RightStacker;

        //Com-порт к которому подсоединен контроллер
        private SerialPort ComPort = null;
        
        //интерфейс контроллера
        private IModbusMaster PLC;

        //хранит номер выбранной заявки в автоматическом режиме
        int SelectedOrderNumber = -1;

        //Конструктор класса **********************************************************************************
        public StackerModel()
        {
            //Читаем первоначальные настройки
            ReadINISettings();

            //Загружаем таблицы координат ячеек
            LoadCellGrid();
            
            //Записываем в класс заявок имена и названия штабелеров для идентификации заявок
            Order.LeftStackerName = LeftRackName;
            Order.LeftStackerNumber = LeftRackNumber;
            Order.RightStackerName = RightRackName;
            Order.RightStackerNumber = RightRackNumber;

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
                //запускаем таймер на чтение сосотояния контроллера
                PlcTimer = new Timer(ReadStateWord, null, 0, 500);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "Ошибка открытия порта");
                //throw new Exception("No com port");
            }
        }

        //завершение работы программы  - нужно поправить оп Микрософту!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
         ~StackerModel()
        {
            Dispose();
        }

        //закрываем неуправляемые ресурсы
        public void Dispose()
        {
            if (FileTimer != null) FileTimer.Dispose();
            if (PLC != null) PLC.Dispose();
            if (ComPort != null) ComPort.Dispose();
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

        //Проверки изменений файла с заданиями и чтения заявок из него
        private void ReadOrdersFile(object ob)
        {
            try
            {
                //проверяем не изменился ли файл с момента последнего чтения
                if (File.GetLastWriteTime(OrdersFile) != LastOrdersFileAccessTime)
                {

                    //и если изменился читаем его, находим новые приказы и добавляем их в список
                    string[] lines = File.ReadAllLines(OrdersFile, System.Text.Encoding.Default);
                    foreach (string str in lines)
                    {
                        Order o = new Order(str);
                        if ((!Orders.Contains(o)) && (o.StackerName != '?')) Orders.Add(o);
                    }
                    //и запоминаем время последнего чтения
                    LastOrdersFileAccessTime = File.GetLastWriteTime(OrdersFile);
                    
                }
            }
            catch (ArgumentException ae)
            {
                FileTimer.Dispose();
                MessageBox.Show(messageBoxText: "Найдена некорректная заявка! Чтения заявок прекращено"
                    + ae.Message, caption: "ReadOrdersFile");
            }
            catch (Exception ex)
            {
                FileTimer.Dispose();
                MessageBox.Show(ex.Message, caption: "ReadOrdersFile");
            }
        }

        //завершение заявки с удалением ее из файла заявок и запись в файл архива с временем
        //и результатом выополнения
        public void FinishOrder(bool succesed)
        {
            if (SelectedOrderNumber == -1) throw new Exception("Не установлен номер заявки");
            string res = succesed ? " succeeded" : " canceled";
            
            //получаем оригинальную строку из файла
            string orderString = Orders[SelectedOrderNumber].OriginalString;
            try
            {
                //записываем в архив строку заявки, время и результат
                File.AppendAllText(ArchiveFile,
                    DateTime.Now.ToString() + " : " + orderString + " - " + res + '\r' + '\n',
                        System.Text.Encoding.Default);

                //читаем файл заявок и удаляем из него строку с нашей заявкой
                string[] strings = File.ReadAllLines(OrdersFile, System.Text.Encoding.Default).
                    Where(v => v.TrimEnd('\r', '\n').IndexOf(orderString) == -1).ToArray();

                //записываем его обратно
                File.WriteAllLines(OrdersFile, strings, System.Text.Encoding.Default);

                //удаляем заявку из коллекции
                Orders.RemoveAt(SelectedOrderNumber);
                //сбрасываем указатель
                SelectedOrderNumber = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "FinishOrder");
            }
        }

        //выбор заявки для последующей работы с ней
        public bool SelectOrder(int orderNumber)
        {
            if (orderNumber < 0 || orderNumber >= Orders.Count) return false;
            else
            {
                SelectedOrderNumber = orderNumber;
                return true;
            }
        }

        //Сохранение массивов координат ячеек в файлы
        public void SaveCells()
        {
            try
            {
                LeftStacker.SaveCellsGrid("LeftStack.cell");
                RightStacker.SaveCellsGrid("RightStack.cell");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "SaveCells");
            }
        }

        //Записывает 32-битное число в контроллер
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

        //Читает 32-битное число из контроллера
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

        //По таймеру читаем слово состояния контроллера
        private void ReadStateWord(object ob)
        {
            if (PLC != null)
            {
                try
                {
                    
                    //читаем оптом из ПЛК актуальные координаты крана
                    ushort[] word = PLC.ReadHoldingRegisters(1, 0x1198, 8);
                    
                    //и записываем их значения
                    ActualX = word[0] + 0x10000 * word[1];
                    ActualY = word[2] + 0x10000 * word[3];
                    ActualRow = word[4];
                    ActualFloor = word[6];

                    word = PLC.ReadHoldingRegisters(1, 0x1064, 8);

                    int stateWord = word[0];
                    Weight = word[2] > 32767 ? 0 : word[2];
                    MeasuredWeight = word[4];
                    MeasuredWeight2 = word[6];

                    /*
                    //читаем слово состояния ПЛК
                    ReadDword(PLC, 100, out int stateWord);
                    ReadDword(PLC, 102, out int weight);
                    weight = weight > 33000 ? 0: weight;
                    Weight = weight;
                    ReadDword(PLC, 104, out weight);
                    MeasuredWeight = weight;
                    ReadDword(PLC, 106, out weight);
                    MeasuredWeight2 = weight;
                    */

                    //вызываем событие по чтению координат
                    CoordinateReaded();

                    //если поменялось слово состояния
                    if (stateWord != StateWord) SomethingChanged();

                    //если появился флаг завершения
                    if (GetBitState(stateWord, 15) && !GetBitState(StateWord, 15))
                    {
                        IsBinOnPlatform = GetBitState(stateWord, 1);
                        CommandDone();
                    }

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
        }

        //вызывается при появления флага ошибки в слове состояния
        private void ErrorHandler()
        {
            ReadDword(PLC, 110, out int ErrorWord);
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
        //левый стеллаж - false
        public void GetCell(bool r, int row, int floor, out int x, out int y, out bool isNotAvailable)
        {
            CellsGrid rack = !r ? LeftStacker: RightStacker;
            x = rack[row, floor].X;
            y = rack[row, floor].Y;
            isNotAvailable = rack[row, floor].IsNotAvailable;
        }

        //устанавливает для ячейки её координаты и доступность
        //левый стеллаж r = false
        public void SetCell(bool r, int row, int floor, int x, int y, bool isNotAvailable)
        {
            if ((x < 0) || (y < 0) || (x > MaxX) || (y > MaxY)) throw new ArgumentException();
            CellsGrid rack = r ? RightStacker : LeftStacker;
            rack[row, floor].X = x;
            rack[row, floor].Y = y;
            rack[row, floor].IsNotAvailable = isNotAvailable;
        }

        //команда подтверждения ошибок в ПЛК и очистка списка ошибок
        public void SubmitError()
        {
            ErrorList.Clear();
            if (PLC != null) SetMerker(PLC, 101, true);
        }

        //команда дальше
        public void FartherButton(bool state)
        {
            if (PLC != null)
            {
                //устанавливаем ручной режим передвижения
                WriteDword(PLC, 8, 1);
                //задаем команду "движение дальше"
                SetMerker(PLC, 10, state);
            }
        }

        //команда ближе
        public void CloserButton(bool state)
        {
            if (PLC != null)
            {
                //устанавливаем ручной режим передвижения
                WriteDword(PLC, 8, 1);
                //задаем команду "движение ближе"
                SetMerker(PLC, 11, state);
            }
        }

        //команда вверх
        public void UpButton(bool state)
        {
            if (PLC != null)
            {
                //устанавливаем ручной режим передвижения
                WriteDword(PLC, 8, 1);
                //задаем команду "движение вверх"
                SetMerker(PLC, 12, state);
            }
        }

        //команда вниз
        public void DownButton(bool state)
        {
            if (PLC != null)
            {
                //устанавливаем ручной режим передвижения
                WriteDword(PLC, 8, 1);
                //задаем команду "движение вниз"
                SetMerker(PLC, 13, state);
            }
        }

        //Команда на движение дальше до следующего ряда
        public void NextLineFartherCommand()
        {
            if (PLC != null)
            {
                //устанавливаем режим движения по координатам
                WriteDword(PLC, 8, 4);
                //задаем команду "движение дальше"
                SetMerker(PLC, 10, true);
            }
        }

        //Команда на движение ближе до следующего ряда
        public void NextLineCloserCommand()
        {
            if (PLC != null)
            {
                //устанавливаем режим движения по координатам
                WriteDword(PLC, 8, 4);
                //задаем команду "движение ближе"
                SetMerker(PLC, 11, true);
            }
        }

        //Команда на движение вверх до следующего этажа
        public void NextLineUpCommand()
        {
            if (PLC != null)
            {
                //устанавливаем режим движения по координатам
                WriteDword(PLC, 8, 4);
                //задаем команду "движение вверх"
                SetMerker(PLC, 12, true);
            }
        }

        //Команда на движение вниз до следующего этажа
        public void NextLineDownCommand()
        {
            if (PLC != null)
            {
                //устанавливаем режим движения по координатам
                WriteDword(PLC, 8, 4);
                //задаем команду "движение вниз"
                SetMerker(PLC, 13, true);
            }
        }

        //команда "платформа влево"
        public void PlatformToLeft()
        {
            if (PLC != null)
            {
                //включаем ручной режим
                WriteDword(PLC, 8, 1);
                //задаем команду ПЛК "платформа вправо"
                SetMerker(PLC, 14, true);
            }
        }

        //команда "платформа вправо"
        public void PlatformToRight()
        {
            if (PLC != null)
            {
                //включаем ручной режим
                WriteDword(PLC, 8, 1);
                //задаем команду ПЛК "платформа влево"
                SetMerker(PLC, 15, true);
            }
        }

        //команда STOP
        public void StopButton()
        {
            if (PLC != null) SetMerker(PLC, 0, true);
        }

        //команда "Перейти на координаты"
        public void GotoXY(int x, int y)
        {
            //проверяем аргументы на допустимость
            if ((x < 0) || (y < 0) || (x > MaxX) || (y > MaxY)) throw new ArgumentException();
            if (PLC != null)
            {
                //Включаем режим перемещения по координатам
                WriteDword(PLC, 8, 3);

                //Записываем координаты в ПЛК
                WriteDword(PLC, 0, x);
                WriteDword(PLC, 2, y);

                //даем команду на движение
                SetMerker(PLC, 20, true);
            }
        }

        //Команда "привезти/увезти" bring = true - привезти
        public void BringOrTakeAwayCommand(bool rack, int row, int floor, bool bring)
        {
            if (PLC != null)
            {
                CellsGrid stacker = rack ?  RightStacker: LeftStacker;
                int x = stacker[row, floor].X;
                int y = stacker[row, floor].Y;
                //требуемая ячейка не может находится в начале штабелера или иметь вертикальную координату 0
                if ( x == 0 || (y==0 && floor!=1) ) throw new ArgumentException("Неверные координаты ячеейки");

                //Включаем режим перемещения по координатам
                WriteDword(PLC, 8, 2);
                //Пишем координаты
                WriteDword(PLC, 0, x);
                WriteDword(PLC, 2, y);
                //Пишем ряд и этаж
                WriteDword(PLC, 4, row);
                WriteDword(PLC, 6, floor);
                //Устанваливаем сторону
                SetMerker(PLC, 2, rack);
                //Устанавливаем флаг в "привезти/увезти"
                SetMerker(PLC, 3, bring);
                //Даем команду на старт
                SetMerker(PLC, 1, true);
            }
        }
        
        //Команда "привезти/увезти" bring = true - привезти
        public void BringOrTakeAwayOrder(bool bring)
        {
            if (SelectedOrderNumber == -1) throw new Exception("Не установлен номер заявки");
            if (PLC != null)
            {
                bool rack = Orders[SelectedOrderNumber].StackerNumber == LeftRackNumber;
                int row = Orders[SelectedOrderNumber].Row;
                int floor = Orders[SelectedOrderNumber].Floor;

                CellsGrid stacker = rack ? RightStacker : LeftStacker;
                int x = stacker[row, floor].X;
                int y = stacker[row, floor].Y;

                //Включаем режим перемещения по координатам
                WriteDword(PLC, 8, 2);
                //Пишем координаты
                WriteDword(PLC, 0, x);
                WriteDword(PLC, 2, y);
                //Пишем ряд и этаж
                WriteDword(PLC, 4, row);
                WriteDword(PLC, 6, floor);
                //Устанваливаем сторону
                SetMerker(PLC, 2, rack);
                //Устанавливаем флаг в "привезти/увезти"
                SetMerker(PLC, 3, bring);
                //Даем команду на старт
                SetMerker(PLC, 1, true);
            }
        }

        public void Weigh()
        {
            if (PLC != null)
            {
                //включаем режим взвешивания
                WriteDword(PLC, 8, 5);
                //задаем команду "взвесить"
                SetMerker(PLC,21,true);
            }
        }


    }
}
