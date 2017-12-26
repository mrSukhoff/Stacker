using System;


namespace Stacker
{
    class Order : IEquatable<Order>
    {
        //поля хранят имена и номера штабелеров
        public static char LeftStackerName;
        public static int LeftStackerNumber;
        public static char RightStackerName;
        public static int RightStackerNumber;

        public string OrderType { get;}    //1-поступление, 2-отпуск
        public string OrderNumber { get;}
        public string LineNumberInOrder { get;}
        public string ProductCode { get; }
        public string ProductDescription { get; }
        public string BatchERPLN { get; }
        public string ManufacturersBatchNumber { get; }
        public string Amount { get; }
        public string StackerNumber { get; }
        public string Row { get; }
        public string Floor { get; }
        public string Cell { get; } // уточнить что за сущность
        public string OriginalString { get; }
        public string Address { get; }
        public char StackerName { get; }

        
        
        public Order(string str)
        {
            str = str.TrimEnd('\r', '\n');
            this.OriginalString = str;

            //проверяем количество разделителей
            int z = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i]=='~') { z++; }
            }
            //если количество полей в строке отличается от 11 кидаем исключение
            if (z!=11)
            {
                throw new ArgumentException("Неправильный формат заявки");
            }
            
            //разбиваем строку и заносим данные в соответсвтующие поля
            string[] strings = str.Split('~');
            this.OrderType = strings[0];
            this.OrderNumber = strings[1];
            this.LineNumberInOrder = strings[2];
            this.ProductCode = strings[3];
            this.ProductDescription = strings[4];
            this.BatchERPLN = strings[5];
            this.ManufacturersBatchNumber = strings[6];
            this.Amount = strings[7];
            this.StackerNumber = strings[8];
            this.Row = strings[9];
            this.Floor = strings[10];
            this.Cell = strings[11];
            int sn = Convert.ToInt32(StackerNumber);
            if ((sn == LeftStackerNumber) || (sn == RightStackerNumber))
                StackerName = sn == LeftStackerNumber ? LeftStackerName : RightStackerName;
            else StackerName = '?';
            this.Address = StackerName +"-"+ Row + "-" + Floor;
        }

        //для интерфейса IEquatable сравнение двух заявок
        public bool Equals(Order other) => (ProductCode == other.ProductCode) && (Address == other.Address);
    }
}
