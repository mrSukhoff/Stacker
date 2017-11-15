using System;


namespace Stacker
{
    class Order
    {
        public readonly string OrderType;    //1-поступление, 2-отпуск
        public readonly string OrderNumber;  
        public readonly string LineNumberinOrder;
        public readonly string ProductCode;
        public readonly string ProductName;
        public readonly string BatchERPLN;
        public readonly string ManufacturersBatch;
        public readonly string Amount;
        public readonly string StackerNumber;
        public readonly string Rack;
        public readonly string Row;
        public readonly string Floor;
        public readonly string Cell; // уточнить что за сущность
        public string FileName;
        public string OriginalString;
                
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
            OrderType = strings[0];
            OrderNumber = strings[1];
            LineNumberinOrder = strings[2];
            ProductCode = strings[3];
            ProductName = strings[4];
            BatchERPLN = strings[5];
            ManufacturersBatch = strings[6];
            Amount = strings[7];
            StackerNumber = strings[8];
            Rack = strings[9];
            Row = strings[10];
            Floor = strings[11];
            Cell = strings[12].Trim('\r','\n'); //тут могут оказаться CRLF
        }
    }
}
