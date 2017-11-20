using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stacker
{
    class Cell
    {
        public int X { get => x; set => x = value; }
        public int Y { get => y; set => y = value; }
        public bool IsNotAvailable { get => isNotAvailable; set => isNotAvailable = value; }

        int x=0;
        int y=0;
        bool isNotAvailable = false;

        
    }

    class CellsGrid
    {

        public Cell this[int row,int floor]
        {
            get
            {
                return grid[row,floor];
            }
            set
            {
                grid[row,floor] = value;
            }
        }

        int maxRow;
        int maxFloor;
        Cell[,] grid;

        public CellsGrid(int RowSize, int FloorSize)
        {
            maxRow = RowSize > 1 ? RowSize - 1 : 0;
            maxFloor = FloorSize > 1 ? FloorSize - 1 : 0;
            grid = new Cell[RowSize, FloorSize];
        }

        public void SaveCellsGrid(string path)
        {
            
        }
    }
}
