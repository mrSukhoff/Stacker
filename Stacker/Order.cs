using System;


namespace Stacker
{
    class Order : IEquatable<Order>
    {
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

        public Order(string str, char LeftStackerName, int LeftStackerNumber, char RightStackerName, int RightStackerNumber)
        {
            str = str.TrimEnd('\r', '\n');
            this.OriginalString = str;

            //проверяем количество разделителей
            int z = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i]=='~') { z++; }
            }
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

        public bool Equals(Order other)
        {
            return  (this.ProductCode == other.ProductCode)&&(this.Address == other.Address);
        }
    }
}
