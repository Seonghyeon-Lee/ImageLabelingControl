using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ImageLabelingControl_OpenCV.Draw
{
    public abstract class DrawLabelBase
    {
        public abstract void OnMouseDown(Grid grid, Scalar color, int imageSize, int imageStride, int widthScale, MouseButtonEventArgs e);
        public abstract void OnMouseMove(Grid grid, Image image, WriteableBitmap writeableBitmap, Int32Rect roiRect, MouseEventArgs e);
        public abstract void OnMouseUp(Image image, Mat labelImage, WriteableBitmap writeableBitmap, Int32Rect roiRect, MouseButtonEventArgs e);
    }
}
