using OpenCvSharp;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ImageLabelingControl_OpenCV.Draw
{
    public abstract class DrawLabelBase
    {
        protected Mat tempLabelImage;
        protected int imageWidth;
        protected int imageHeight;
        protected int imageSize;
        protected int imageStride;
        protected int thickness;
        protected Scalar color;
        protected readonly Scalar eraserColor = new Scalar(0x00, 0x00, 0x00, 0x00);

        public abstract void OnMouseDown(System.Windows.Point mousePos, int imageWidth, 
            int imageHeight, int imageSize, int imageStride, int thickness, Scalar color);

        public abstract void OnMouseMove(System.Windows.Point mousePos, WriteableBitmap writeableBitmap, ref Int32Rect roiRect);

        public abstract void OnMouseUp(Mat labelImage, WriteableBitmap writeableBitmap, 
            WriteableBitmap TempWriteableBitmap, Int32Rect roiRect);
    }
}