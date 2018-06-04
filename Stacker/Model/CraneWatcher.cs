using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Windows;

namespace Stacker.Model
{
    public class CraneWatcher : IDisposable
    {
        //список ошибок контроллера
        public ObservableCollection<string> ErrorList { get; private set; } = new ObservableCollection<string>();

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

        //Контроллер крана
        private Controller PLC;

        //Таймер для чтения слова состояния контроллера
        private Timer PlcTimer;
        
        //Слово состояния контроллера
        private ushort StateWord = 0;
        
        //флаг уничтожения объектов
        private bool disposed = false;

        //конструктор
        internal CraneWatcher(Controller plc)
        {
            PLC = plc;
            if (plc != null) PlcTimer = new Timer(ReadStateWord, null, 0, 500);
        }

        //деструктор
        ~CraneWatcher()
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
                    PlcTimer.Dispose();
                }
                disposed = true;
            }
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

                ushort stateWord = word[0];
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
            PLC.ReadDword(110, out uint ErrorWord);
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
                    File.AppendAllText("Errors.log", str + '\r' + '\n', System.Text.Encoding.Default);
                }
                catch (Exception ex)
                { MessageBox.Show(ex.Message, caption: "ErrorHandler"); }
            }
        }

        //метод возвращает состояния указанного бита
        private bool GetBitState(uint b, byte num)
        {
            return (b & (ushort)Math.Pow(2,num)) > 0 ;
        }
    }
}
