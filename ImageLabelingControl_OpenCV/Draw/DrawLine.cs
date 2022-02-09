using System;
using System.Windows;
using System.Windows.Media.Imaging;

using ControlCore.Model;
using OpenCvSharp;

namespace ImageLabelingControl_OpenCV.Draw
{
    public class DrawLine : DrawLabelBase
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
                Cv2.Line(tempLabelImage, _DrawingStartPos.X, _DrawingStartPos.Y,
                    _DrawingLastPos.X, _DrawingLastPos.Y, eraserColor, thickness, LineTypes.Link8);

                Cv2.Line(tempLabelImage, _DrawingStartPos.X, _DrawingStartPos.Y, curX, curY, color, thickness, LineTypes.Link8);
                writeableBitmap.WritePixels(roiRect, tempLabelImage.Data, imageSize, imageStride, roiRect.X, roiRect.Y);
                UpdateWriteableBitmapRoi(ref roiRect, _DrawingStartPos.X, _DrawingStartPos.Y, curX, curY);
            }
            else
            {
                Cv2.Line(tempLabelImage, _DrawingStartPos.X, _DrawingStartPos.Y, curX, curY, color, thickness, LineTypes.Link8);

                UpdateWriteableBitmapRoi(ref roiRect, _DrawingStartPos.X, _DrawingStartPos.Y, curX, curY);
                writeableBitmap.WritePixels(roiRect, tempLabelImage.Data, imageSize, imageStride, roiRect.X, roiRect.Y);
            }

            _IsFirstDraw = false;
            _DrawingLastPos.Set(mousePos);
        }

        protected override void UpdateWriteableBitmapRoi(ref Int32Rect roiRect, int x1, int y1, int x2, int y2)
        {
            int startX = Math.Min(x1, x2);
            int startY = Math.Min(y1, y2);

            roiRect.X = startX - thickness / 2;
            roiRect.Y = startY - thickness / 2;
            roiRect.Width = Math.Abs(x1 - x2) + thickness + 1;
            roiRect.Height = Math.Abs(y1 - y2) + thickness + 1;
        }

        public override void OnMouseUp(Mat labelImage, WriteableBitmap writeableBitmap, 
            WriteableBitmap TempWriteableBitmap, Int32Rect roiRect)
        {
            if (!_IsFirstDraw)
            {
                Cv2.Line(tempLabelImage, _DrawingStartPos.X, _DrawingStartPos.Y,
                    _DrawingLastPos.X, _DrawingLastPos.Y, eraserColor, thickness, LineTypes.Link8);
                TempWriteableBitmap.WritePixels(roiRect, tempLabelImage.Data, imageSize, imageStride, roiRect.X, roiRect.Y);

                Cv2.Line(labelImage, _DrawingStartPos.X, _DrawingStartPos.Y,
                    _DrawingLastPos.X, _DrawingLastPos.Y, color, thickness, LineTypes.Link8);
                writeableBitmap.WritePixels(roiRect, labelImage.Data, imageSize, imageStride, roiRect.X, roiRect.Y);
            }

            _IsFirstDraw = true;
            tempLabelImage.Dispose();
        }
    }
}
