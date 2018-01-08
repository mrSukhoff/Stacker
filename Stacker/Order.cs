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
        public int StackerNumber { get; }
        public int Row { get; }
        public int Floor { get; }
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
            OrderType = strings[0];
            OrderNumber = strings[1];
            LineNumberInOrder = strings[2];
            ProductCode = strings[3];
            ProductDescription = strings[4];
            BatchERPLN = strings[5];
            ManufacturersBatchNumber = strings[6];
            Amount = strings[7];
            StackerNumber = Convert.ToInt32(strings[8]);
            Row = Convert.ToInt32(strings[9]);
            Floor = Convert.ToInt32(strings[10]);
            Cell = strings[11];
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
