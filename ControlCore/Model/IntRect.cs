using System;

namespace ControlCore.Model
{
    /// <summary>
    /// 정수 사각형의 너비, 높이, 위치, 중심을 설명합니다.
    /// </summary>
    public struct IntRect
    {
        /// <summary>사각형 왼쪽 위 모퉁이의 X 좌표를 가져오거나 설정합니다.<br/><br/></summary>
        /// <returns>사각형의 왼쪽 위 모퉁이의 X 좌표입니다. 기본값은 0입니다.</returns>
        public int X { get; set; }

        /// <summary>사각형 왼쪽 위 모퉁이의 Y 좌표를 가져오거나 설정합니다.<br/><br/></summary>
        /// <returns>사각형의 왼쪽 위 모퉁이의 Y 좌표입니다. 기본값은 0입니다.</returns>
        public int Y { get; set; }

        /// <summary>사각형의 너비를 가져오거나 설정합니다.<br/><br/></summary>
        /// <returns>사각형의 너비입니다. 기본값은 0입니다.</returns>
        public int Width { get; set; }

        /// <summary>사각형의 높이를 가져오거나 설정합니다.<br/><br/></summary>
        /// <returns>사각형의 높이입니다. 기본값은 0입니다.</returns>
        public int Height { get; set; }

        /// <summary>사각형의 중심 X 좌표를 가져오거나 설정합니다.<br/><br/></summary>
        /// <returns>사각형의 중심 X 좌표입니다. 기본값은 0입니다.</returns>
        public int CenterX { get; set; }

        /// <summary>사각형의 중심 Y 좌표를 가져오거나 설정합니다.<br/><br/></summary>
        /// <returns>사각형의 중심 Y 좌표입니다. 기본값은 0입니다.</returns>
        public int CenterY { get; set; }

        public void Set(int x1, int y1, int x2, int y2)
        {
            X = Math.Min(x1, x2);
            Y = Math.Min(y1, y2);

            Width = Math.Abs(x1 - x2);
            Height = Math.Abs(y1 - y2);

            CenterX = X + Width / 2;
            CenterY = Y + Height / 2;
        }
    }
}
