using OpenCvSharp;
using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageLabelingControl_OpenCV.Draw
{
    /// <summary>
    /// Image에 Label 그리는걸 도와주는 Class.
    /// </summary>
    public abstract class DrawShape
    {
        protected Mat tempLabelImage;
        protected int imageWidth;
        protected int imageHeight;
        protected int imageSize;
        protected int imageStride;
        protected int thickness;
        protected Scalar color;
        protected readonly Scalar eraserColor = new Scalar(0x00, 0x00, 0x00, 0x00);

        ~DrawShape()
        {
            if (tempLabelImage != null)
                tempLabelImage.Dispose();
        }

        public abstract void OnMouseDown(System.Windows.Point mousePos, int imageWidth, 
            int imageHeight, int imageSize, int imageStride, int thickness, Scalar color);

        public abstract void OnMouseMove(System.Windows.Point mousePos, WriteableBitmap writeableBitmap, ref Int32Rect roiRect);

        public abstract void OnMouseUp(Mat labelImage, WriteableBitmap writeableBitmap, 
            WriteableBitmap TempWriteableBitmap, Int32Rect roiRect);

        protected void UpdateRoiForRect(ref Int32Rect roiRect, int x1, int y1, int x2, int y2)
        {
            roiRect.X = Math.Min(x1, x2);
            roiRect.Y = Math.Min(y1, y2);
            roiRect.Width = Math.Abs(x1 - x2) + 2;
            roiRect.Height = Math.Abs(y1 - y2) + 2;
        }

        protected void UpdateRoiForLine(ref Int32Rect roiRect, int x1, int y1, int x2, int y2)
        {
            roiRect.X = Math.Min(x1, x2) - thickness / 2;
            roiRect.Y = Math.Min(y1, y2) - thickness / 2;
            roiRect.Width = Math.Abs(x1 - x2) + thickness + 1;
            roiRect.Height = Math.Abs(y1 - y2) + thickness + 1;
        }
    }
}