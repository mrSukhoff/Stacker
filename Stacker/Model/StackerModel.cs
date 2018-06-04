using System;
using System.IO;
using System.Windows;


namespace Stacker.Model
{
    public class StackerModel : IDisposable
    {
        //хранилище настроек
        public SettingsKeeper Settings;
        //менеджер заявок
        public OrdersManager OrderManager;
        //команды управления краном
        public CraneCommands Crane;
        //состояние крана
        public CraneWatcher CraneState;

        //флаг успешности подключения
        public bool IsConnected = false;

        //Контроллер крана
        internal Controller PLC;
        //Координаты ячеек
        internal CellsGrid Stacker;

        //флаг уничтожения объектов
        private bool disposed = false;
        
        //Конструктор класса **********************************************************************************
        public StackerModel()
        {
            //Инициализируем хранилище настроек
            Settings = new SettingsKeeper();

            //Создаем менеджер заявок
            OrderManager = new OrdersManager(this);

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
                IsConnected = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "Ошибка открытия порта");
            }

            //включаем мониторинг состояния крана
            CraneState = new CraneWatcher(PLC);
            //и его управление
            Crane = new CraneCommands(this);
        }

        //деструктор
        ~StackerModel()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    CraneState.Dispose();
                    PLC?.Dispose();
                    OrderManager.Dispose();
                }
                disposed = true;
            }
        }
        
        //Сохранение массивов координат ячеек в файлы
        public void SaveCells()
        {
            string path = Environment.CurrentDirectory + "\\" + Settings.CellsFile;
            Stacker.SaveCellsGrid(path);
        }

        //выдает по адресу ячейки её координаты и доступность
        public void GetCell(char r, int row, int floor, out int x, out int y, out bool isNotAvailable)
        {
            //if (r != LeftRackName & r != RightRackName) throw new ArgumentException("Неправильное имя стойки");
            if (r == '\0') r = Settings.LeftRackName;
            if (row < 1) row = 1;
            if (floor < 1) floor = 1;
            x = Stacker[row, floor].X;
            y = Stacker[row, floor].Y;
            isNotAvailable = r == Settings.LeftRackName ? Stacker[row, floor].LeftSideIsNotAvailable : Stacker[row, floor].RightSideIsNotAvailable;
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
