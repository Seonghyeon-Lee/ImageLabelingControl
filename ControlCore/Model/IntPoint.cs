namespace ControlCore.Model
{
    /// <summary>
    /// 2차원 공간에서 정수 x 및 y 좌표 쌍을 나타냅니다.
    /// </summary>
    public struct IntPoint
    {
        public int X { get; set; }
        public int Y { get; set; }

        public IntPoint(System.Windows.Point point)
        {
            X = (int)point.X;
            Y = (int)point.Y;
        }

        public IntPoint(double x, double y)
        {
            X = (int)x;
            Y = (int)y;
        }

        public void Set(System.Windows.Point point)
        {
            X = (int)point.X;
            Y = (int)point.Y;
        }

        public void Set(double x, double y)
        {
            X = (int)x;
            Y = (int)y;
        }
    }
}
