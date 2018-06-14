namespace Stacker.Model
{
    //класс ячейки массива для хранения координат и доступности 
    internal class Cell
    {
        internal Cell()
        {
            X = 0;
            Y = 0;
            LeftSideIsNotAvailable = false;
            RightSideIsNotAvailable = false;
        }

        internal int X { get; set; }
        internal int Y { get; set; }
        internal bool LeftSideIsNotAvailable { get; set; }
        internal bool RightSideIsNotAvailable { get; set; }
    }
}