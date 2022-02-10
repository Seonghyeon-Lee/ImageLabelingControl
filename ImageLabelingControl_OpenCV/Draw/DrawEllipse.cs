using System;
using System.Windows;
using System.Windows.Media.Imaging;

using ControlCore.Model;
using OpenCvSharp;

namespace ImageLabelingControl_OpenCV.Draw
{
    public class DrawEllipse : DrawShape
    {
        private bool _IsFirstDraw;
        private IntPoint _DrawingStartPos;
        private IntPoint _DrawingLastPos;

        public override void OnMouseDown(System.Windows.Point mousePos, int imageWidth, 
            int imageHeight, int imageSize, int imageStride, int thickness, Scalar color)
        {
            this.imageWidth = imageWidth;
            this.imageHeight = imageHeight;
            this.imageSize = imageSize;
            this.imageStride = imageStride;
            this.thickness = thickness;
            this.color = color;

            _IsFirstDraw = true;
            _DrawingStartPos.Set(mousePos);
            tempLabelImage = new Mat(new OpenCvSharp.Size(imageWidth, imageHeight), MatType.CV_8UC4, new Scalar(0, 0, 0, 0));
        }

        public override void OnMouseMove(System.Windows.Point mousePos, WriteableBitmap writeableBitmap, ref Int32Rect roiRect)
        {
            int curX = (int)mousePos.X;
            int curY = (int)mousePos.Y;

            int centerX = (_DrawingStartPos.X + curX) / 2;
            int centerY = (_DrawingStartPos.Y + curY) / 2;
            int width = Math.Abs(_DrawingStartPos.X - curX);
            int height = Math.Abs(_DrawingStartPos.Y - curY);

            if (!_IsFirstDraw)
            {
                Cv2.Rectangle(tempLabelImage, new OpenCvSharp.Point(_DrawingStartPos.X, _DrawingStartPos.Y),
                    new OpenCvSharp.Point(_DrawingLastPos.X, _DrawingLastPos.Y), eraserColor, -1, LineTypes.Link8);


                Cv2.Ellipse(tempLabelImage, new RotatedRect(new Point2f(centerX, centerY),
                    new Size2f(width, height), 0), color, -1, LineTypes.Link8);

                writeableBitmap.WritePixels(roiRect, tempLabelImage.Data, imageSize, imageStride, roiRect.X, roiRect.Y);
                UpdateRoiForRect(ref roiRect, _DrawingStartPos.X, _DrawingStartPos.Y, curX, curY);
            }
            else
            {
                Cv2.Ellipse(tempLabelImage, new RotatedRect(new Point2f(centerX, centerY),
                    new Size2f(width, height), 0), color, -1, LineTypes.Link8);

                UpdateRoiForRect(ref roiRect, _DrawingStartPos.X, _DrawingStartPos.Y, curX, curY);
                writeableBitmap.WritePixels(roiRect, tempLabelImage.Data, imageSize, imageStride, roiRect.X, roiRect.Y);
            }

            _IsFirstDraw = false;
            _DrawingLastPos.Set(mousePos);
        }

        public override void OnMouseUp(Mat labelImage, WriteableBitmap writeableBitmap, 
            WriteableBitmap TempWriteableBitmap, Int32Rect roiRect)
        {
            if (!_IsFirstDraw)
            {
                Cv2.Rectangle(tempLabelImage, new OpenCvSharp.Point(_DrawingStartPos.X, _DrawingStartPos.Y),
                    new OpenCvSharp.Point(_DrawingLastPos.X, _DrawingLastPos.Y), eraserColor, -1, LineTypes.Link8);
                TempWriteableBitmap.WritePixels(roiRect, tempLabelImage.Data, imageSize, imageStride, roiRect.X, roiRect.Y);

                Cv2.Ellipse(labelImage, new RotatedRect(new OpenCvSharp.Point(roiRect.X + (roiRect.Width - 2) / 2,
                    roiRect.Y + (roiRect.Height -2) / 2), new Size2f(roiRect.Width -2, roiRect.Height -2), 0), color, -1, LineTypes.Link8);
                writeableBitmap.WritePixels(roiRect, labelImage.Data, imageSize, imageStride, roiRect.X, roiRect.Y);
            }

            tempLabelImage.Dispose();
        }
    }
}