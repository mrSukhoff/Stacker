using System;
using System.IO;
using System.Windows;

namespace Stacker
{
    //класс ячейки массива для хранения координат и доступности 
    class Cell
    {
        public Cell()
        {
            X = 0;
            Y = 0;
            IsNotAvailable = false;
        }

        public int X { get; set; }
        public int Y { get; set; }
        public bool IsNotAvailable { get; set; }
    }

    //класс-обертка для массива ячеек
    class CellsGrid
    {
        //индексатор
        public Cell this[int rowIndex,int floorIndex]
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

        //конструктор создает массив и инициализирует его
        public CellsGrid(int RowSize, int FloorSize)
        {
            //проверяем не слишком малы ли аргументы
            if (RowSize < 1 || FloorSize < 1) throw new ArgumentException("Размры массива слишком малы");
            
            //создаем массив координат
            grid = new Cell[RowSize, FloorSize];
            
            //и инициализируем каждый элемент
            for (int r=0;r<RowSize;r++) 
                for (int f=0;f<FloorSize;f++)
                    grid[r, f] = new Cell(); 
        }

        //конструктор считывает массив координат из файла
        public CellsGrid(string path)
        {
            if (File.Exists(path))
            {
                //читаем файл с координатами  в массив строк
                string[] lines = File.ReadAllLines(path, System.Text.Encoding.Default);

                //первые две строки хранят размер массива
                int rowSize = Convert.ToInt32(lines[0]);
                int floorSize = Convert.ToInt32(lines[1]);
                
                //создаем массив координат
                grid = new Cell[rowSize, floorSize];

                //и инициализируем каждый элемент
                for (int r = 0; r < rowSize; r++)
                    for (int f = 0; f < floorSize; f++)
                        grid[r, f] = new Cell();

                //разбираем все строки и заносим значения в массив
                for (int i = 2; i < lines.Length; i++)
                {
                    string[] line = lines[i].Split('~');
                    int r = Convert.ToInt32(line[0])-1;
                    int f = Convert.ToInt32(line[1])-1;
                    int x = Convert.ToInt32(line[2]);
                    int y = Convert.ToInt32(line[3]);
                    bool isNotAvailable = Convert.ToBoolean(line[4]);
                    grid[r, f].X = x;
                    grid[r, f].Y = y;
                    grid[r, f].IsNotAvailable = isNotAvailable;
                }
            }
        }

        //сохраняет массив координат в файл
        public void SaveCellsGrid(string path)
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
                        grid[r, f].IsNotAvailable.ToString();
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
