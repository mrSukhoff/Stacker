using System;


namespace Stacker
{
    class Order
    {
        public readonly string OrderType;    //1-поступление, 2-отпуск
        public readonly string OrderNumber;  
        public readonly string LineNumberInOrder;
        public readonly string ProductCode;
        public readonly string ProductDescription;
        public readonly string BatchERPLN;
        public readonly string ManufacturersBatchNumber;
        public readonly string Amount;
        public readonly string StackerNumber;
        public readonly string Row;
        public readonly string Floor;
        public readonly string Cell; // уточнить что за сущность
        public readonly string OriginalString;
                
        public Order(string str)
        {
            OriginalString = str;

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
            OrderType = strings[0];
            OrderNumber = strings[1];
            LineNumberInOrder = strings[2];
            ProductCode = strings[3];
            ProductDescription = strings[4];
            BatchERPLN = strings[5];
            ManufacturersBatchNumber = strings[6];
            Amount = strings[7];
            StackerNumber = strings[8];
            Row = strings[9];
            Floor = strings[10];
            Cell = strings[11].Trim('\r','\n'); //тут могут оказаться CRLF
        }
    }
}
