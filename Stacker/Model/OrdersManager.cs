using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;

namespace Stacker.Model
{
    public class OrdersManager : IDisposable
    {
        //коллекция заявок
        public ObservableCollection<Order> Orders { get; private set; } = new ObservableCollection<Order>();

        //номер выбранной заявки для автоматического режима
        public int SelectedOrderNumber
        {
            get => _selectedOrderNumber;
            set =>_selectedOrderNumber = (value >= 0) & (value < Orders.Count) ? value : -1;
        }

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

        SettingsKeeper sk;

        //методы ----------------------------------------------------------------------------------
        //public
        public OrdersManager(StackerModel master)
        {
            sk = master.Settings;
            OrdersFile = sk.OrdersFile;
            ArchiveFile = sk.ArchiveFile;
            WrongOrdersFile = sk.WrongOrdersFile;
            Order.LeftStackerName = sk.LeftRackName;
            Order.RightStackerName = sk.RightRackName;
        }

        ~OrdersManager() => Dispose(false);
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

        public void StartTimer()
        {
            uint p = sk.ReadingInterval * (uint)1000;
            FileTimer = new Timer(callback: ReadOrdersFile, state: null, dueTime: 2000, period: p);
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

        //сортировка списка заявок пр заданному полю и в задданом направлении
        //dir=false по возрастанию
        public void SortList(string sortField, bool direction)
        {
            bool sorted = false;
            bool needsSorting;
            string str1, str2;
            while (!sorted)
            {
                sorted = true;
                for (int i = 1; i < Orders.Count; i++)
                {
                    PropertyDescriptor descr = TypeDescriptor.GetProperties(Orders[i])[sortField];
                    str1 = descr?.GetValue(Orders[i-1]).ToString();
                    str2 = descr?.GetValue(Orders[i]).ToString();
                    needsSorting = !direction & String.Compare(str1,str2) > 0 || direction & String.Compare(str1,str2) < 0;
                    if (needsSorting)
                    {
                        if (direction) Orders.Move(i-1, i);
                        else Orders.Move(i, i - 1);
                        sorted = false;
                    }
                }
            }
        }

        //private ---------------------------------------------------------------------------------
        //Проверки изменений файла с заданиями и чтения заявок из него
        private void ReadOrdersFile(object ob)
        {
            //проверяем не изменился ли файл с момента последнего чтения
            if (File.GetLastWriteTime(OrdersFile) != LastOrdersFileAccessTime)
            {
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
                                App.Current.Dispatcher.Invoke( () => Orders.Add(order));
                            }
                            order = null;
                        }
                    }
                    //и запоминаем время последнего чтения
                    LastOrdersFileAccessTime = File.GetLastWriteTime(OrdersFile);
                    lines = null;
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
