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

        //номер выбранной заявки для автоматического режима
        public int SelectedOrderNumber
        {
            get => _selectedOrderNumber;
            set =>_selectedOrderNumber = (value >= 0) & (value < Orders.Count()) ? value : -1;
        }

        //События
        public delegate void OrdersManagerEvent();
        //появилась новая заявка
        public event OrdersManagerEvent NewOrderAppeared = (() => { });

        //внутрении поля --------------------------------------------------------------------------

        //места хранения файлов заявлок и архива
        private string OrdersFile;
        private string ArchiveFile;
        private string WrongOrdersFile;

        // переменная для контроля изменения файла заявок
        private DateTime LastOrdersFileAccessTime = DateTime.Now;

        // таймер для контроля изменения файла заявок
        private Timer FileTimer;

        //хранит номер выбранной заявки для автоматического режима
        private int _selectedOrderNumber = -1;

        //флаг уничтожения неуправляемых ресурсов
        bool disposed;

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
                    FileTimer?.Dispose();
                }
                disposed = true;
            }
        }

        //Запускаем таймер для проверки изменений списка заявок
        public void TimerStart(uint period)
        {
            uint p = period * 1000;
            FileTimer = new Timer(callback: ReadOrdersFile, state: null, dueTime: 0, period: p);
        }

        //завершение заявки с удалением ее из файла заявок и запись в файл архива с временем
        //и результатом выополнения
        public void FinishSelectedOrder(bool successfully)
        {
            if (_selectedOrderNumber == -1) throw new Exception("Не установлен номер заявки");
            string res = successfully ? " успешно" : " отменено";

            //удаляем строку из файла заявок и записываем в архив
            RemoveStringFromOrdersFile(Orders[_selectedOrderNumber].OriginalString, ArchiveFile, res);

            //удаляем заявку из коллекции
            Orders.RemoveAt(_selectedOrderNumber);

            //сбрасываем указатель
            _selectedOrderNumber = -1;
        }

        //private ---------------------------------------------------------------------------------
        //Проверки изменений файла с заданиями и чтения заявок из него
        private void ReadOrdersFile(object ob)
        {
            //проверяем не изменился ли файл с момента последнего чтения
            if (File.GetLastWriteTime(OrdersFile) != LastOrdersFileAccessTime)
            {
                bool newOrderAdded = false;
                try
                {
                    //и если изменился читаем его
                    List<string> lines = new List<string>();
                    using (FileStream fs = new FileStream(OrdersFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (StreamReader sr = new StreamReader(fs, System.Text.Encoding.Default))
                        {
                            while (sr.Peek()>=0)
                            {
                                lines.Add(sr.ReadLine());
                            }
                        }
                    }

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
                    lines = null;
                    if (newOrderAdded) NewOrderAppeared();
                }
                catch (Exception ex)
                {
                    FileTimer.Dispose();
                    MessageBox.Show(ex.Message, "ReadOrdersFile");
                }

            }
        }

        //метод удаляет строку из файла заявок и записывает в указаный файл с заданным результатом
        private void RemoveStringFromOrdersFile(string str, string filePath, string res)
        {
            try
            {
                //записываем в архив строку заявки, время и результат
                File.AppendAllText(filePath,
                    DateTime.Now.ToString() + " : " + str + " - " + res + '\r' + '\n',
                        System.Text.Encoding.Default);

                //читаем файл заявок в список строк
                List<string> lines = new List<string>();
                using (FileStream fs = new FileStream(OrdersFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader sr = new StreamReader(fs, System.Text.Encoding.Default))
                    {
                        while (sr.Peek() >= 0)
                        {
                            lines.Add(sr.ReadLine());
                        }
                    }
                }
                //и удаляем из списка строку с нашей заявкой
                lines.Remove(str);
                //записываем список обратно
                File.WriteAllLines(OrdersFile, lines, System.Text.Encoding.Default);
                //обнуляем список
                lines = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "RemoveStringFromOrdersFile");
            }
        }
    }
}
