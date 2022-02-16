using ControlCore.Model;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageLabelingControl_OpenCV.Draw
{
    public class DrawPolygon : DrawShape
    {
        private bool _IsFirstDraw;
        private bool _IsStartPoly;
        private bool _IsEarlyStop;
        private IntPoint _DrawingStartPos;
        private IntPoint _DrawingLastPos;
        private List<List<OpenCvSharp.Point>> _Points = new List<List<OpenCvSharp.Point>>();

        public override void OnMouseDown(System.Windows.Point mousePos, int imageWidth,
            int imageHeight, int imageSize, int imageStride, int thickness, Scalar color)
        {
            this.imageWidth = imageWidth;
            this.imageHeight = imageHeight;
            this.imageSize = imageSize;
            this.imageStride = imageStride;
            this.thickness = 2;
            this.color = color;

            _IsFirstDraw = true;
            _IsStartPoly = !_IsEarlyStop;
            _IsEarlyStop = false;
            _DrawingStartPos.Set(mousePos);
            if (_Points.Count == 0)
                _Points.Add(new List<OpenCvSharp.Point>());
            
            if (IsNearStartPoint(mousePos))
            {
                OnDrawPolygonEvent();
                return;
            }

            _Points[0].Add(new OpenCvSharp.Point(_DrawingStartPos.X, _DrawingStartPos.Y));
            tempLabelImage = new Mat(new OpenCvSharp.Size(imageWidth, imageHeight), MatType.CV_8UC4, new Scalar(0, 0, 0, 0));
        }

        private bool IsNearStartPoint(System.Windows.Point point)
        {
            if (_Points[0].Count < 1)
                return false;

            var x = (int)point.X - _Points[0][0].X;
            var y = (int)point.Y - _Points[0][0].Y;
            Console.WriteLine(Math.Sqrt(x * x + y * y));
            if (Math.Sqrt(x * x + y * y) <= 20)
                return true;

            return false;
        }
        
        protected override void OnDrawPolygonEvent()
        {
            base.OnDrawPolygonEvent();
        }

        public override void OnMouseMove(System.Windows.Point mousePos, WriteableBitmap writeableBitmap, ref Int32Rect roiRect)
        {
            if (!_IsStartPoly)
                return;

            int curX = (int)mousePos.X;
            int curY = (int)mousePos.Y;

            if (!_IsFirstDraw)
            {
                // 직전 Move 때 그려진 도형 삭제 코드
                Cv2.Line(tempLabelImage, _DrawingStartPos.X, _DrawingStartPos.Y,
                    _DrawingLastPos.X, _DrawingLastPos.Y, eraserColor, thickness, LineTypes.Link8);

                // 도형 생성 코드
                Cv2.Line(tempLabelImage, _DrawingStartPos.X, _DrawingStartPos.Y, curX, curY, color, thickness, LineTypes.Link8);
                writeableBitmap.WritePixels(roiRect, tempLabelImage.Data, imageSize, imageStride, roiRect.X, roiRect.Y);
                UpdateRoiForLine(ref roiRect, _DrawingStartPos.X, _DrawingStartPos.Y, curX, curY);
            }
            else
            {
                Cv2.Line(tempLabelImage, _DrawingStartPos.X, _DrawingStartPos.Y, curX, curY, color, thickness, LineTypes.Link8);

                UpdateRoiForLine(ref roiRect, _DrawingStartPos.X, _DrawingStartPos.Y, curX, curY);
                writeableBitmap.WritePixels(roiRect, tempLabelImage.Data, imageSize, imageStride, roiRect.X, roiRect.Y);
            }

            _IsFirstDraw = false;
            _DrawingLastPos.Set(mousePos);
        }

        public override void OnMouseUp(Mat labelImage, WriteableBitmap writeableBitmap,
            WriteableBitmap TempWriteableBitmap, Int32Rect roiRect, bool isRightClick = false)
        {
            if (tempLabelImage == null)
                return;

            if (!_IsStartPoly)
                return;

            if (isRightClick)
            {
                // 기존 Move 때 그려진 도형 삭제 코드
                Cv2.Line(tempLabelImage, _DrawingStartPos.X, _DrawingStartPos.Y,
                    _DrawingLastPos.X, _DrawingLastPos.Y, eraserColor, thickness, LineTypes.Link8);
                TempWriteableBitmap.WritePixels(roiRect, tempLabelImage.Data, imageSize, imageStride, roiRect.X, roiRect.Y);

                // Polygon 생성 코드
                Cv2.FillPoly(labelImage, _Points, color);
                UpdatePolygonRoi(ref roiRect, Cv2.BoundingRect(_Points[0]));

                writeableBitmap.WritePixels(roiRect, labelImage.Data, imageSize, imageStride, roiRect.X, roiRect.Y);

                _IsStartPoly = false;
                _Points = new List<List<OpenCvSharp.Point>>();
                return;
            }

            if (!_IsFirstDraw)
            {
                // 기존 Move 때 그려진 도형 삭제 코드
                Cv2.Line(tempLabelImage, _DrawingStartPos.X, _DrawingStartPos.Y,
                    _DrawingLastPos.X, _DrawingLastPos.Y, eraserColor, thickness, LineTypes.Link8);
                TempWriteableBitmap.WritePixels(roiRect, tempLabelImage.Data, imageSize, imageStride, roiRect.X, roiRect.Y);

                // 얼리 스탑 부분을 추가해야함
                // 현재 문제는 마우스 이벤트에서 폴리 시작과 끝을 알려줘야하는것임 
                // 이부분을 참고해서 코드를 수정해야할듯 함.

                // 기존 Move 때 그려진 도형 생성 코드
                Cv2.Line(labelImage, _DrawingStartPos.X, _DrawingStartPos.Y,
                    _DrawingLastPos.X, _DrawingLastPos.Y, color, thickness, LineTypes.Link8);
                writeableBitmap.WritePixels(roiRect, labelImage.Data, imageSize, imageStride, roiRect.X, roiRect.Y);
            }
        }

        private void UpdatePolygonRoi(ref Int32Rect roiRect, OpenCvSharp.Rect roi)
        {
            roiRect.X = roi.X;
            roiRect.Y = roi.Y;
            roiRect.Width = roi.Width;
            roiRect.Height = roi.Height;
        }
    }
}
