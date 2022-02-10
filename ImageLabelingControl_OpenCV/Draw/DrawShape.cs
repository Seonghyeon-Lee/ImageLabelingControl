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

        /// <summary>
        /// Draw Mouse Down Method.
        /// </summary>
        /// <param name="mousePos">마우스 좌표</param>
        /// <param name="imageWidth">Image Width</param>
        /// <param name="imageHeight">Image Height</param>
        /// <param name="imageSize">Image Size</param>
        /// <param name="imageStride">Image Stride</param>
        /// <param name="thickness">두께</param>
        /// <param name="color">색상</param>
        public abstract void OnMouseDown(System.Windows.Point mousePos, int imageWidth, 
            int imageHeight, int imageSize, int imageStride, int thickness, Scalar color);

        /// <summary>
        /// Draw Mouse Move Method.
        /// </summary>
        /// <param name="mousePos">마우스 좌표</param>
        /// <param name="writeableBitmap">WriteableBitmap</param>
        /// <param name="roiRect">ROI</param>
        public abstract void OnMouseMove(System.Windows.Point mousePos, WriteableBitmap writeableBitmap, ref Int32Rect roiRect);

        /// <summary>
        /// Draw Mouse Up Method.
        /// </summary>
        /// <param name="labelImage">Mat형식 라벨 이미지</param>
        /// <param name="writeableBitmap">WriteableBitmap </param>
        /// <param name="TempWriteableBitmap">Temp WriteableBitmap</param>
        /// <param name="roiRect">ROI</param>
        /// <param name="isRightClick">Polygon, Polyline 관련 우측 마우스 클릭 체크 변수</param>
        public abstract void OnMouseUp(Mat labelImage, WriteableBitmap writeableBitmap, 
            WriteableBitmap TempWriteableBitmap, Int32Rect roiRect, bool isRightClick = false);

        /// <summary>
        /// 사각형 내부에 들어오는 도형에 대한 Roi 업데이트 함수
        /// </summary>
        /// <param name="roiRect">Rect</param>
        /// <param name="x1">X1</param>
        /// <param name="y1">Y1</param>
        /// <param name="x2">X2</param>
        /// <param name="y2">Y2</param>
        protected void UpdateRoiForRect(ref Int32Rect roiRect, int x1, int y1, int x2, int y2)
        {
            roiRect.X = Math.Min(x1, x2);
            roiRect.Y = Math.Min(y1, y2);
            roiRect.Width = Math.Abs(x1 - x2) + 2;
            roiRect.Height = Math.Abs(y1 - y2) + 2;
        }

        /// <summary>
        /// 직선에 대한 Roi 업데이트 함수
        /// </summary>
        /// <param name="roiRect">Rect</param>
        /// <param name="x1">X1</param>
        /// <param name="y1">Y1</param>
        /// <param name="x2">X2</param>
        /// <param name="y2">Y2</param>
        protected void UpdateRoiForLine(ref Int32Rect roiRect, int x1, int y1, int x2, int y2)
        {
            roiRect.X = Math.Min(x1, x2) - thickness / 2;
            roiRect.Y = Math.Min(y1, y2) - thickness / 2;
            roiRect.Width = Math.Abs(x1 - x2) + thickness + 1;
            roiRect.Height = Math.Abs(y1 - y2) + thickness + 1;
        }
    }
}