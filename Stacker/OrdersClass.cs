using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace Stacker
{
    class OrdersClass
    {
        //коллекция заявок
        public List<Order> Orders { get; private set; } = new List<Order>();

        //места хранения файлов заявлок и архива
        private string OrdersFile;
        private string ArchiveFile;

        // переменная для контроля изменения файла заявок
        private DateTime LastOrdersFileAccessTime = DateTime.Now;

        // таймер для контроля изменения файла заявок
        private Timer FileTimer;

        //хранит номер выбранной заявки в автоматическом режиме
        int SelectedOrderNumber = -1;

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
            string res;
            if (succesed) res = " - succeeded";
            else res = " - canceled";
            string orderString = Orders[SelectedOrderNumber].OriginalString;
            try
            {
                File.AppendAllText(ArchiveFile,
                    DateTime.Now.ToString() + " : " + orderString + " - " + res + '\r' + '\n',
                        System.Text.Encoding.Default);

                string[] strings = File.ReadAllLines(OrdersFile, System.Text.Encoding.Default).
                    Where(v => v.TrimEnd('\r', '\n').IndexOf(orderString) == -1).ToArray();

                File.WriteAllLines(OrdersFile, strings, System.Text.Encoding.Default);

                Orders.RemoveAt(SelectedOrderNumber);
                SelectedOrderNumber = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "FinishOrder");
            }
        }

        //выбор заявки для последующей работы с ней
        public void SelectOrder(int orderNumber)
        {
            if (orderNumber < 0 || orderNumber > Orders.Count) throw new ArgumentException("Номер заявки за прелами списка");
            SelectedOrderNumber = orderNumber;
        }
    }
}
