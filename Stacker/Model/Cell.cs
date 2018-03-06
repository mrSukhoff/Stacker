using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stacker.Model
{
    //класс ячейки массива для хранения координат и доступности 
    internal class Cell
    {
        public Cell()
        {
            X = 0;
            Y = 0;
            LeftSideIsNotAvailable = false;
            RightSideIsNotAvailable = false;
        }

        public int X { get; set; }
        public int Y { get; set; }
        public bool LeftSideIsNotAvailable { get; set; }
        public bool RightSideIsNotAvailable { get; set; }
    }
}
