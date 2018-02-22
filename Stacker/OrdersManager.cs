using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Stacker
{
    public class OrdersManager : IDisposable
    {
        //коллекция заявок
        public List<Order> Orders { get; private set; } = new List<Order>();

        //События
        public delegate void OrdersManagerEventHandler();
        //появилась новая заявка
        public event OrdersManagerEventHandler NewOrderAppeared = (() => { });

        //внутрении поля --------------------------------------------------------------------------

        //места хранения файлов заявлок и архива
        private string OrdersFile;
        private string ArchiveFile;
        private string WrongOrdersFile;

        // переменная для контроля изменения файла заявок
        private DateTime LastOrdersFileAccessTime = DateTime.Now;

        // таймер для контроля изменения файла заявок
        private Timer FileTimer;

        //хранит номер выбранной заявки в автоматическом режиме
        int SelectedOrderNumber = -1;

        //методы ----------------------------------------------------------------------------------
        //public
        public OrdersManager(string ordersFile, string archiveFile, string wrongOrdersFile, char leftRackName, char rightRackName)
        {
            OrdersFile = ordersFile;
            ArchiveFile = archiveFile;
            WrongOrdersFile = wrongOrdersFile;
            Order.LeftStackerName = leftRackName;
            Order.RightStackerName = rightRackName;
        }

        public void Dispose()
        {
            FileTimer?.Dispose();
        }

        //Запускаем таймер для проверки изменений списка заявок
        public void TimerStart()
        {
            FileTimer = new Timer(ReadOrdersFile, null, 0, 10000);
        }

        //*выбор заявки для последующей работы с ней
        public bool SelectOrder(int orderNumber)
        {
            if (orderNumber < 0 || orderNumber >= Orders.Count) return false;
            else
            {
                SelectedOrderNumber = orderNumber;
                return true;
            }
        }

        //*завершение заявки с удалением ее из файла заявок и запись в файл архива с временем
        //и результатом выополнения
        public void FinishSelectedOrder(bool successfully)
        {
            if (SelectedOrderNumber == -1) throw new Exception("Не установлен номер заявки");
            string res = successfully ? " успешно" : " отменено";

            //удаляем строку из файла заявок и записываем в архив
            RemoveStringFromOrdersFile(Orders[SelectedOrderNumber].OriginalString, ArchiveFile, res);

            //удаляем заявку из коллекции
            Orders.RemoveAt(SelectedOrderNumber);

            //сбрасываем указатель
            SelectedOrderNumber = -1;
        }

        public Order GetSelectedOrder()
        {
            if (SelectedOrderNumber >= 0 & SelectedOrderNumber < Orders.Count)
                return Orders[SelectedOrderNumber];
            else return null;
        }

        //private ---------------------------------------------------------------------------------
        //*Проверки изменений файла с заданиями и чтения заявок из него
        private void ReadOrdersFile(object ob)
        {
            //проверяем не изменился ли файл с момента последнего чтения
            if (File.GetLastWriteTime(OrdersFile) != LastOrdersFileAccessTime)
            {
                bool newOrderAdded = false;
                try
                {
                    //и если изменился читаем его
                    string[] lines;
                    lines = File.ReadAllLines(OrdersFile, System.Text.Encoding.Default);
                    Order order = null;
                    foreach (string str in lines)
                    {
                        //пытаемся преобразовать каждую строку в заявку
                        try
                        {
                            order = new Order(str);
                        }
                        //в случае ошибки строку переносим в файл с ошибками
                        catch (ArgumentException ae)
                        {
                            RemoveStringFromOrdersFile(str, WrongOrdersFile, ae.Message);
                        }
                        //в зависимости от результата добавляем строку или не добавляем
                        finally
                        {
                            if (order != null && order.StackerName != '?' && !Orders.Contains(order))
                            {
                                Orders.Add(order);
                                newOrderAdded = true;
                            }
                            order = null;
                        }
                    }
                    //и запоминаем время последнего чтения
                    LastOrdersFileAccessTime = File.GetLastWriteTime(OrdersFile);
                    if (newOrderAdded) NewOrderAppeared();
                }
                catch (Exception ex)
                {
                    FileTimer.Dispose();
                    MessageBox.Show(ex.Message, "ReadOrdersFile");
                }

            }
        }

        //*метод удаляет строку из файла заявок и записывает в указаный файл с заданным результатом
        private void RemoveStringFromOrdersFile(string str, string filePath, string res)
        {
            try
            {
                //записываем в архив строку заявки, время и результат
                File.AppendAllText(filePath,
                    DateTime.Now.ToString() + " : " + str + " - " + res + '\r' + '\n',
                        System.Text.Encoding.Default);

                //читаем файл заявок и удаляем из него строку с нашей заявкой
                string[] strings = File.ReadAllLines(OrdersFile, System.Text.Encoding.Default).
                    Where(v => v.TrimEnd('\r', '\n').IndexOf(str) == -1).ToArray();

                //записываем его обратно
                File.WriteAllLines(OrdersFile, strings, System.Text.Encoding.Default);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "RemoveStringFromOrdersFile");
            }
        }

    }
}
