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
    public class DrawLine : DrawLabelBase
    {
        private System.Windows.Point? _StartPoint;
        private System.Windows.Point? _LastPoint;
        private Scalar _Color;
        private Scalar _EraserColor = new Scalar(0x00, 0x00, 0x00, 0x00);
        private int _WidthScale;
        private int _ImageSize;
        private int _ImageStride;
        private Mat _TempLabelImage;

        private int _startX;
        private int _startY;
        private int _CurX;
        private int _CurY;

        public override void OnMouseDown(Grid grid, Scalar color, int imageSize, int imageStride, int widthScale, MouseButtonEventArgs e)
        {
            _StartPoint = e.GetPosition(grid);
            _Color = color;
            _WidthScale = widthScale;
            _ImageSize = imageSize;
            _ImageStride = imageStride;
            _TempLabelImage = new Mat(new OpenCvSharp.Size((int)grid.Width, (int)grid.Height), MatType.CV_8UC4, new Scalar(0, 0, 0, 0));
        }

        public override void OnMouseMove(Grid grid, Image image, WriteableBitmap writeableBitmapSource, Int32Rect roiRect, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            var mousePos = e.GetPosition(grid);
            if (!_StartPoint.HasValue)
                return;

            _startX = (int)_StartPoint.Value.X;
            _startY = (int)_StartPoint.Value.Y;
            _CurX = (int)mousePos.X;
            _CurY = (int)mousePos.Y;

            if (!_LastPoint.HasValue)
            {
                DrawFirstLine(writeableBitmapSource, roiRect);
            }
            else
            {
                DrawOtherLine(writeableBitmapSource, roiRect);
            }

            _LastPoint = mousePos;
            image.Source = writeableBitmapSource;
        }

        private void DrawFirstLine(WriteableBitmap writeableBitmapSource, Int32Rect roiRect)
        {
            Cv2.Line(_TempLabelImage, _startX, _startY, _CurX, _CurY, _Color, _WidthScale, LineTypes.Link8);

            UpdateRoiRect(ref roiRect, _startX, _startY, _CurX, _CurY);
            writeableBitmapSource.WritePixels(roiRect, _TempLabelImage.Data, _ImageSize, _ImageStride, roiRect.X, roiRect.Y);
        }

        private void DrawOtherLine(WriteableBitmap writeableBitmapSource, Int32Rect roiRect)
        {
            Cv2.Line(_TempLabelImage, _startX, _startY, (int)_LastPoint.Value.X, (int)_LastPoint.Value.Y, _EraserColor, _WidthScale, LineTypes.Link8);
            Cv2.Line(_TempLabelImage, _startX, _startY, _CurX, _CurY, _Color, _WidthScale, LineTypes.Link8);

            writeableBitmapSource.WritePixels(roiRect, _TempLabelImage.Data, _ImageSize, _ImageStride, roiRect.X, roiRect.Y);
            UpdateRoiRect(ref roiRect, _startX, _startY, _CurX, _CurY);
        }

        private void UpdateRoiRect(ref Int32Rect roiRect, int prevX, int prevY, int curX, int curY)
        {
            int startX = Math.Min(prevX, curX);
            int startY = Math.Min(prevY, curY);

            roiRect.X = startX - _WidthScale / 2;
            roiRect.Y = startY - _WidthScale / 2;
            roiRect.Width = Math.Abs((curX - prevX)) + _WidthScale + 1;
            roiRect.Height = Math.Abs((curY - prevY)) + _WidthScale + 1;
        }

        public override void OnMouseUp(Image image, Mat labelImage, WriteableBitmap writeableBitmapSource, Int32Rect roiRect, MouseButtonEventArgs e)
        {
            _TempLabelImage.Dispose();
            if (!_LastPoint.HasValue)
            {
                Cv2.Line(labelImage, (int)_StartPoint.Value.X, (int)_StartPoint.Value.Y, (int)_StartPoint.Value.X, (int)_StartPoint.Value.Y, _Color, _WidthScale, LineTypes.Link8);
            }
            else
            {
                Cv2.Line(labelImage, (int)_StartPoint.Value.X, (int)_StartPoint.Value.Y, (int)_LastPoint.Value.X, (int)_LastPoint.Value.Y, _Color, _WidthScale, LineTypes.Link8);
            }
            

            writeableBitmapSource.WritePixels(roiRect, labelImage.Data, _ImageSize, _ImageStride, roiRect.X, roiRect.Y);
            image.Source = writeableBitmapSource;
            _LastPoint = null;
        }
    }
}
