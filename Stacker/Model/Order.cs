﻿using System;


namespace Stacker.Model
{
    public class Order : IEquatable<Order>
    {
        //поля хранят имена штабелеров
        public static char LeftStackerName;
        public static char RightStackerName;

        public string WarehouseNumber { get; }
        public string OrderNumber { get; }
        public string LineNumberInOrder { get; }
        public string OrderType { get;}    //1-поступление, 2-отпуск
        public string ProductCode { get; }
        public string ProductDescription { get; }
        public string BatchERPLN { get; }
        public string ManufacturersBatchNumber { get; }
        public string Amount { get; }
        public string Unit { get; }
        public char   StackerName { get; }
        public UInt16 Row { get; }
        public UInt16 Floor { get; }
        public string Cell { get; } // уточнить что за сущность
        public string OriginalString { get; }
        public string Address { get; }

        public Order(string str)
        {
            str = str.TrimEnd('\r', '\n');
            OriginalString = str;

            //проверяем количество разделителей
            int z = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i]=='~')  z++; 
            }
            //если количество полей в строке отличается от 13 кидаем исключение
            if (z!=13) throw new ArgumentException("Неверное количество полей");
                        
            //разбиваем строку и заносим данные в соответсвтующие поля
            string[] strings = str.Split('~');

            WarehouseNumber = strings[0];
            OrderNumber = strings[1];
            LineNumberInOrder = strings[2];

            if (UInt16.TryParse(strings[3], out ushort r))
            {
                string oType;
                switch (r)
                {
                    case 1:
                        oType = "Поступление";
                        break;
                    case 2:
                        oType = "Отпуск";
                        break;
                    default:
                        throw new ArgumentException("Неверный тип заявки");
                }
                OrderType = oType;
            }
            else throw new ArgumentException("Неверный тип заявки");

            ProductCode = strings[4];
            ProductDescription = strings[5];
            BatchERPLN = strings[6];
            ManufacturersBatchNumber = strings[7];
            Amount = strings[8];
            Unit = strings[9];
            if (strings[10].Length==0) throw new ArgumentException("Пустое поле имени стеллажа");
            char sn = strings[10][0];
            if (sn == LeftStackerName || sn == RightStackerName) StackerName = sn;
            else StackerName='?';
            if (UInt16.TryParse(strings[11], out UInt16 row)) Row = row;
            else throw new ArgumentException("Некорректный номер ряда");
            if (UInt16.TryParse(strings[12], out UInt16 floor)) Floor = floor;
            else throw new ArgumentException("Некорректный номер этажа");
            Cell = strings[13];
            Address = StackerName +"-"+ Row + "-" + Floor;
        }

        //для интерфейса IEquatable сравнение двух заявок
        public bool Equals(Order other) => OriginalString == other.OriginalString;
    }
}
