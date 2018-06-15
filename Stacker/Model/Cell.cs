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

        internal uint X { get; set; }
        internal uint Y { get; set; }
        internal bool LeftSideIsNotAvailable { get; set; }
        internal bool RightSideIsNotAvailable { get; set; }
    }
}
