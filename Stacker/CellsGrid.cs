using System;
using System.IO;
using System.Windows;

namespace Stacker
{
    class Cell
    {
        public Cell()
        {
            this.X = 0;
            this.Y = 0;
            this.IsNotAvailable = false;
        }

        public int X { get; set; }
        public int Y { get; set; }
        public bool IsNotAvailable { get; set; }
    }

    class CellsGrid
    {
        //индексатор
        public Cell this[int rowIndex,int floorIndex]
        {
            get
            {
                return grid[rowIndex,floorIndex];
            }
            set
            {
                grid[rowIndex,floorIndex] = value;
            }
        }

        public int RowSize { get { return rowSize; } }
        public int FloorSize { get { return floorSize; } }

        private int rowSize;
        private int floorSize;
        private Cell[,] grid;

        //конструктор создает массив и инициализирует его
        public CellsGrid(int RowSize, int FloorSize)
        {
            rowSize = RowSize > 0 ? RowSize : 1;
            floorSize = FloorSize > 0 ? FloorSize : 1;
            grid = new Cell[rowSize, floorSize];
            for (int r=0;r<rowSize;r++) 
                for (int f=0;f<floorSize;f++)
                    grid[r, f] = new Cell(); 
        }

        //конструктор считывает массив значений из файла
        public CellsGrid(string path)
        {
            if (File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path, System.Text.Encoding.Default);
                int rowSize = Convert.ToInt32(lines[0]);
                int floorSize = Convert.ToInt32(lines[1]);
                this.rowSize = rowSize > 0 ? rowSize : 1;
                this.floorSize = floorSize > 0 ? floorSize : 1;
                this.grid = new Cell[rowSize, floorSize];
                for (int r = 0; r < rowSize; r++)
                    for (int f = 0; f < floorSize; f++)
                        grid[r, f] = new Cell();
                for (int i = 2; i < lines.Length; i++)
                {
                    string[] line = lines[i].Split('~');
                    int r = Convert.ToInt32(line[0]);
                    int f = Convert.ToInt32(line[1]);
                    int x = Convert.ToInt32(line[2]);
                    int y = Convert.ToInt32(line[3]);
                    bool isNotAvailable = Convert.ToBoolean(line[4]);
                    this.grid[r, f].X = x;
                    this.grid[r, f].Y = y;
                    this.grid[r, f].IsNotAvailable = isNotAvailable;
                }
            }
        }

        public void SaveCellsGrid(string path)
        {
            string[] lines = new string[RowSize*FloorSize+2];
            lines[0] = this.RowSize.ToString();
            lines[1] = this.FloorSize.ToString();
            for (int r = 0; r < RowSize; r++)
            {
                for (int f = 0; f < FloorSize; f++)
                {
                    lines[2+f+r*FloorSize] = 
                        r.ToString()+'~'+
                        f.ToString() + '~' +
                        grid[r, f].X.ToString() + '~'+
                        grid[r, f].Y.ToString() + "~"+
                        grid[r, f].IsNotAvailable.ToString();
                }
            }
            //try
            {
                File.WriteAllLines(path, lines, System.Text.Encoding.Default);
            }
            //catch (Exception ex)
            {
               // MessageBox.Show(ex.Message, caption:"SaveCellGrid");
            }
        }
    }
}
