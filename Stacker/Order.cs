using System;


namespace Stacker
{
    class Order : IEquatable<Order>
    {
        public string OrderType { get;}    //1-поступление, 2-отпуск
        public  string OrderNumber { get;}
        public  string LineNumberInOrder { get;}
        public  string ProductCode { get; }
        public  string ProductDescription { get; }
        public  string BatchERPLN { get; }
        public  string ManufacturersBatchNumber { get; }
        public  string Amount { get; }
        public  string StackerNumber { get; }
        public  string Row { get; }
        public  string Floor { get; }
        public  string Cell { get; } // уточнить что за сущность
        public  string OriginalString { get; }

        public Order(string str)
        {
            this.OriginalString = str;

            //проверяем количество разделителей
            int z = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i]=='~') { z++; }
            }
            if (z!=11)
            {
                throw new ArgumentException("Invalid order string");
            }
            
            //разбиваем строку и заносим данные в соответсвтующие поля
            string[] strings = str.Split('~');
            if (strings.Length == 1)
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
            this.Cell = strings[11].Trim('\r','\n'); //тут могут оказаться CRLF
        }

        public bool Equals(Order other)
        {
            return  (this.ProductCode == other.ProductCode)&&
                    (this.StackerNumber == other.StackerNumber)&&
                    (this.Row == other.Row)&&
                    (this.Floor==other.Floor);
        }
    }
}
