using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace Stacker
{
    class OrdersManager
    {
        //коллекция заявок
        public List<Order> Orders { get; private set; } = new List<Order>();

        //События
        public delegate void StackerModelEventHandler();
        //появилась новая заявка
        public event StackerModelEventHandler NewOrderAppeared = (() => { });

        private string OrdersFile;
        private string ArchiveFile;
        private string WrongOrdersFile;

        // переменная для контроля изменения файла заявок
        private DateTime LastOrdersFileAccessTime = DateTime.Now;

        // таймер для контроля изменения файла заявок
        private Timer FileTimer;

        //хранит номер выбранной заявки в автоматическом режиме
        int SelectedOrderNumber = -1;

        //Проверки изменений файла с заданиями и чтения заявок из него
        private void ReadOrdersFile(object ob)
        {
            //проверяем не изменился ли файл с момента последнего чтения
            if (File.GetLastWriteTime(OrdersFile) != LastOrdersFileAccessTime)
            {
                bool newOrderAdded = false;
                try
                {
                    //MessageBox.Show(File.GetLastWriteTime(OrdersFile) + " " + LastOrdersFileAccessTime);
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

        //метод удаляет строку из файла заявок и записывает в указаный файл с заданным результатом
        public void RemoveStringFromOrdersFile(string str, string filePath, string res)
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

        //завершение заявки с удалением ее из файла заявок и запись в файл архива с временем
        //и результатом выополнения
        public void FinishOrder(bool succesed)
        {
            if (SelectedOrderNumber == -1) throw new Exception("Не установлен номер заявки");
            string res = succesed ? " succeeded" : " canceled";

            //удаляем строку из файла заявок и записываем в архив
            RemoveStringFromOrdersFile(Orders[SelectedOrderNumber].OriginalString, ArchiveFile, res);

            //удаляем заявку из коллекции
            Orders.RemoveAt(SelectedOrderNumber);

            //сбрасываем указатель
            SelectedOrderNumber = -1;
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
        
    }
}
