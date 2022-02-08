using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCore.Model
{
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
