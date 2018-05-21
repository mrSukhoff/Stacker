using System;

namespace Stacker.Model
{
    public class CraneCommands
    {
        private Controller PLC;
        private SettingsKeeper Settings;
        private CellsGrid Stacker;
        private OrdersManager OrderManager;
        private CraneWatcher CraneState;

        public CraneCommands(StackerModel master)
        {
            PLC = master.PLC;
            Settings = master.Settings;
            Stacker = master.Stacker;
            OrderManager = master.OrderManager;
            CraneState = master.CraneState;
        }

        //*команда подтверждения ошибок в ПЛК и очистка списка ошибок
        public void SubmitError()
        {
            CraneState.ErrorList.Clear();
            if (PLC != null)
            {
                PLC.WriteDword(8, 0);
                PLC.SetMerker(101, true);
            }
            //System.Windows.MessageBox.Show("List must be cleared!");
        }

        //*команда дальше
        public void FartherButton(bool state)
        {
            if (PLC != null)
            {
                //устанавливаем ручной режим передвижения
                PLC.WriteDword(8, 1);
                //задаем команду "движение дальше"
                PLC.SetMerker(10, state);
            }
        }

        //*команда ближе
        public void CloserButton(bool state)
        {
            if (PLC != null)
            {
                //устанавливаем ручной режим передвижения
                PLC.WriteDword(8, 1);
                //задаем команду "движение ближе"
                PLC.SetMerker(11, state);
            }
        }

        //*команда вверх
        public void UpButton(bool state)
        {
            if (PLC != null)
            {
                //устанавливаем ручной режим передвижения
                PLC.WriteDword(8, 1);
                //задаем команду "движение вверх"
                PLC.SetMerker(12, state);
            }
        }

        //*команда вниз
        public void DownButton(bool state)
        {
            if (PLC != null)
            {
                //устанавливаем ручной режим передвижения
                PLC.WriteDword(8, 1);
                //задаем команду "движение вниз"
                PLC.SetMerker(13, state);
            }
        }

        //*Команда на движение дальше до следующего ряда
        public void NextLineFartherCommand()
        {
            if (PLC != null)
            {
                //устанавливаем режим движения по координатам
                PLC.WriteDword(8, 4);
                //задаем команду "движение дальше"
                PLC.SetMerker(10, true);
            }
        }

        //*Команда на движение ближе до следующего ряда
        public void NextLineCloserCommand()
        {
            if (PLC != null)
            {
                //устанавливаем режим движения по координатам
                PLC.WriteDword(8, 4);
                //задаем команду "движение ближе"
                PLC.SetMerker(11, true);
            }
        }

        //*Команда на движение вверх до следующего этажа
        public void NextLineUpCommand()
        {
            if (PLC != null)
            {
                //устанавливаем режим движения по координатам
                PLC.WriteDword(8, 4);
                //задаем команду "движение вверх"
                PLC.SetMerker(12, true);
            }
        }

        //*Команда на движение вниз до следующего этажа
        public void NextLineDownCommand()
        {
            if (PLC != null)
            {
                //устанавливаем режим движения по координатам
                PLC.WriteDword(8, 4);
                //задаем команду "движение вниз"
                PLC.SetMerker(13, true);
            }
        }

        //*команда "платформа влево"
        public void PlatformToLeft()
        {
            if (PLC != null)
            {
                //включаем ручной режим
                PLC.WriteDword(8, 1);
                //задаем команду ПЛК "платформа вправо"
                PLC.SetMerker(14, true);
            }
        }

        //*команда "платформа вправо"
        public void PlatformToRight()
        {
            if (PLC != null)
            {
                //включаем ручной режим
                PLC.WriteDword(8, 1);
                //задаем команду ПЛК "платформа влево"
                PLC.SetMerker(15, true);
            }
        }

        //*команда STOP
        public void StopButton()
        {
            if (PLC != null) PLC.SetMerker(0, true);
#if DEBUG
            CraneState.ErrorList.Add("Нажата кнопка аварийной остановки");
#endif
        }

        //*команда "Перейти на координаты"
        public void GotoXY(int x, int y)
        {
            //проверяем аргументы на допустимость
            if ((x < 0) || (y < 0) || (x > Settings.MaxX) || (y > Settings.MaxY)) throw new ArgumentException();
            if (PLC != null)
            {
                //Включаем режим перемещения по координатам
                PLC.WriteDword(8, 3);

                //Записываем координаты в ПЛК
                PLC.WriteDword(0, x);
                PLC.WriteDword(2, y);

                //даем команду на движение
                PLC.SetMerker(20, true);
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
                if (x == 0 || (y == 0 && floor != 1)) throw new ArgumentException("Неверные координаты ячеейки");

                //Включаем режим перемещения по координатам
                PLC.WriteDword(8, 2);
                //Пишем координаты
                PLC.WriteDword(0, x);
                PLC.WriteDword(2, y);
                //Пишем ряд и этаж
                PLC.WriteDword(4, row);
                PLC.WriteDword(6, floor);
                //Устанваливаем сторону
                PLC.SetMerker(2, rack);
                //Устанавливаем флаг в "привезти/увезти"
                PLC.SetMerker(3, bring);
                //Даем команду на старт
                PLC.SetMerker(1, true);
            }
        }

        //*Команда "привезти/увезти" по зараннее установленной заявке, bring = true - привезти
        public void BringOrTakeAway(bool bring)
        {
            Order order = OrderManager.Orders[OrderManager.SelectedOrderNumber];
            if (order != null)
            {
                bool rack = order.StackerName == Settings.RightRackName;
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
                PLC.WriteDword(8, 5);
                //задаем команду "взвесить"
                PLC.SetMerker(21, true);
            }
        }

        //*читает бит наличие ящика на платформе в слове состояния
        public bool ChekBinOnPlatform()
        {
            int word = 0;
            if (PLC != null) PLC.ReadDword(100, out word);
            return GetBitState(word, 10);
            
            //метод возвращает состояния указанного бита
            bool GetBitState(int b, int num)
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
        }
    }
}
