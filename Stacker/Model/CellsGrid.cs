using System;
using System.IO;
using System.Windows;

namespace Stacker.Model
{
    //класс-обертка для массива ячеек
    internal class CellsGrid
    {
        //индексатор
        internal Cell this[int rowIndex,int floorIndex]
        {
            //так как стеллажи нумируются с единицы вычитаем 1
            get
            {
                int row = rowIndex - 1;
                int floor = floorIndex - 1;
                return grid[row, floor];
            }

            set
            {
                int row = rowIndex - 1;
                int floor = floorIndex - 1;
                grid[row, floor] = value;
            }
        }

        //массив с координатами ячеек
        private Cell[,] grid;

        //конструктор создает массив
        internal CellsGrid(ushort RowSize, ushort FloorSize)
        {
            //проверяем не слишком малы ли аргументы
            if (RowSize < 1 || FloorSize < 1) throw new ArgumentException("Размры массива слишком малы");
            grid = InitGrid(RowSize, FloorSize);
        }

        //конструктор считывает массив координат из файла
        internal CellsGrid(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException();

            //читаем файл с координатами  в массив строк
            string[] lines = File.ReadAllLines(path, System.Text.Encoding.Default);

            //первые две строки хранят размер массива
            if (!UInt16.TryParse(lines[0], out ushort rowSize) | !UInt16.TryParse(lines[1], out ushort floorSize))
                throw new ArgumentException("Неверный размер массива ячеек");

            //создаем массив координат
            grid = InitGrid(rowSize, floorSize);
        
            //разбираем все строки и заносим значения в массив
            for (int i = 2; i < lines.Length; i++)
            {
                string[] line = lines[i].Split('~');

                int r = Convert.ToInt32(line[0]) - 1;
                int f = Convert.ToInt32(line[1]) - 1;
                int x = Convert.ToInt32(line[2]);
                int y = Convert.ToInt32(line[3]);
                bool leftSideIsNotAvailable = Convert.ToBoolean(line[4]);
                bool rightSideIsNotAvailable = Convert.ToBoolean(line[5]);

                grid[r, f].X = x;
                grid[r, f].Y = y;
                grid[r, f].LeftSideIsNotAvailable = leftSideIsNotAvailable;
                grid[r, f].RightSideIsNotAvailable = rightSideIsNotAvailable;
            }

        }

        //создаем массив ячеек и инициализируем все его элементы
        private Cell[,] InitGrid(ushort rowSize, ushort floorSize)
        {
            //создаем массив координат
            Cell[,] grid = new Cell[rowSize, floorSize];

            //и инициализируем каждый элемент
            for (int r = 0; r < rowSize; r++)
                for (int f = 0; f < floorSize; f++)
                    grid[r, f] = new Cell();

            return grid;
        }

        //сохраняет массив координат в файл
        internal void SaveCellsGrid(string path)
        {
            //создаем массив строк размером равным количеству ячеек + 2
            int rowSize = grid.GetLength(0);
            int floorSize = grid.GetLength(1);

            string[] lines = new string[rowSize*floorSize+2];

            //первые две строки хранят размер массива
            lines[0] = rowSize.ToString();
            lines[1] = floorSize.ToString();

            //для каждой ячейки формируем строку с координатами
            for (int r = 0; r < rowSize; r++)
            {
                for (int f = 0; f < floorSize; f++)
                {
                    lines[2+f+r*floorSize] = 
                        (r+1).ToString()+'~'+
                        (f+1).ToString() + '~' +
                        grid[r, f].X.ToString() + '~'+
                        grid[r, f].Y.ToString() + "~"+
                        grid[r, f].LeftSideIsNotAvailable.ToString() + "~" +
                        grid[r, f].RightSideIsNotAvailable.ToString();
                }
            }
            //пытаемся сохранить получееные строки в файл
            try
            {
                File.WriteAllLines(path, lines, System.Text.Encoding.Default);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption:"SaveCellGrid");
            }
        }
    }
}
