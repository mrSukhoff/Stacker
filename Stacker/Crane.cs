using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stacker
{
    class Crane
    {

        //*команда подтверждения ошибок в ПЛК и очистка списка ошибок
        public void SubmitError()
        {
            ErrorList.Clear();
            if (PLC != null) SetMerker(PLC, 101, true);
        }

        //*команда дальше
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

        //*команда ближе
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

        //*команда вверх
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

        //*команда вниз
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

        //*Команда на движение дальше до следующего ряда
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

        //*Команда на движение ближе до следующего ряда
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

        //*Команда на движение вверх до следующего этажа
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

        //*Команда на движение вниз до следующего этажа
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

        //*команда "платформа влево"
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

        //*команда "платформа вправо"
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

        //*команда STOP
        public void StopButton()
        {
            if (PLC != null) SetMerker(PLC, 0, true);
        }

        //*команда "Перейти на координаты"
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

        //*Команда "привезти/увезти" по зараннее установленной заявке, bring = true - привезти
        public void BringOrTakeAway(bool bring)
        {
            Order order = OrdersManager.GetSelectedOrder();
            bool rack = order.StackerName == RightRackName;
            int row = order.Row;
            int floor = order.Floor;
            BringOrTakeAway(rack, row, floor, bring);
        }

        //*Команда взвесить
        public void Weigh()
        {
            if (PLC != null)
            {
                //включаем режим взвешивания
                WriteDword(PLC, 8, 5);
                //задаем команду "взвесить"
                SetMerker(PLC, 21, true);
            }
        }

        //*читает бит наличие ящика на платформе в слове состояния
        public bool ChekBinOnPlatform()
        {
            int word = 0;
            if (PLC != null) ReadDword(PLC, 100, out word);
            return GetBitState(word, 10);
        }
    }
}
