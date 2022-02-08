using System;
using System.Windows;
using System.Windows.Media.Imaging;

using ControlCore.Model;
using OpenCvSharp;

namespace ImageLabelingControl_OpenCV.Draw
{
    public class DrawRectangle : DrawLabelBase
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

            if (!_IsFirstDraw)
            {
                Cv2.Rectangle(tempLabelImage, new OpenCvSharp.Point(_DrawingStartPos.X, _DrawingStartPos.Y),
                    new OpenCvSharp.Point(_DrawingLastPos.X, _DrawingLastPos.Y), eraserColor, -1, LineTypes.Link8);

                Cv2.Rectangle(tempLabelImage, new OpenCvSharp.Point(_DrawingStartPos.X, _DrawingStartPos.Y),
                    new OpenCvSharp.Point(curX, curY), color, -1, LineTypes.Link8);
                writeableBitmap.WritePixels(roiRect, tempLabelImage.Data, imageSize, imageStride, roiRect.X, roiRect.Y);
                UpdateWriteableBitmapRoi(ref roiRect, curX, curY);
            }
            else
            {
                Cv2.Rectangle(tempLabelImage, new OpenCvSharp.Point(_DrawingStartPos.X, _DrawingStartPos.Y),
                    new OpenCvSharp.Point(curX, curY), color, -1, LineTypes.Link8);

                UpdateWriteableBitmapRoi(ref roiRect, curX, curY);
                writeableBitmap.WritePixels(roiRect, tempLabelImage.Data, imageSize, imageStride, roiRect.X, roiRect.Y);
            }

            _IsFirstDraw = false;
            _DrawingLastPos.Set(mousePos);
        }

        private void UpdateWriteableBitmapRoi(ref Int32Rect roiRect, int posX, int posY)
        {
            int startX = Math.Min(_DrawingStartPos.X, posX);
            int startY = Math.Min(_DrawingStartPos.Y, posY);

            roiRect.X = startX - thickness / 2;
            roiRect.Y = startY - thickness / 2;
            roiRect.Width = Math.Abs(_DrawingStartPos.X - posX) + thickness + 1;
            roiRect.Height = Math.Abs(_DrawingStartPos.Y - posY) + thickness + 1;
        }

        public override void OnMouseUp(Mat labelImage, WriteableBitmap writeableBitmap, 
            WriteableBitmap TempWriteableBitmap, Int32Rect roiRect)
        {
            _IsFirstDraw = true;
            Cv2.Rectangle(tempLabelImage, new OpenCvSharp.Point(_DrawingStartPos.X, _DrawingStartPos.Y),
                new OpenCvSharp.Point(_DrawingLastPos.X, _DrawingLastPos.Y), eraserColor, -1, LineTypes.Link8);
            TempWriteableBitmap.WritePixels(roiRect, tempLabelImage.Data, imageSize, imageStride, roiRect.X, roiRect.Y);

            Cv2.Rectangle(labelImage, new OpenCvSharp.Point(_DrawingStartPos.X, _DrawingStartPos.Y),
                new OpenCvSharp.Point(_DrawingLastPos.X, _DrawingLastPos.Y), color, -1, LineTypes.Link8);
            writeableBitmap.WritePixels(roiRect, labelImage.Data, imageSize, imageStride, roiRect.X, roiRect.Y);
            tempLabelImage.Dispose();
        }
    }
}
