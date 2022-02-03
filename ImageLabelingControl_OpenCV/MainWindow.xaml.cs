using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;
using OpenCvSharp.Blob;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Window = System.Windows.Window;

namespace ImageLabelingControl_OpenCV
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        int imageStride;
        int imageSize;
        Int32Rect roiRect;
        Mat _Image;
        Mat tempMat;
        ImageInfo _ImageInfo;
        System.Windows.Point? lastCenterPositionOnTarget;
        System.Windows.Point? lastMousePositionOnTarget;
        System.Windows.Point? lastDragPoint;

        System.Windows.Point? DrawlastDragPoint;

        int brushWidth = 10;
        double cursorScale;

        const double _INIT_SCALE = 0.96;
        const double _INCREASE_SCALE = 1.1;
        const double _DECREASE_SCALE = 0.9;

        Scalar color;
        Scalar drawColor = new Scalar(0xF0, 0xf8, 0xff, 0x64);
        Scalar eraserColor = new Scalar(0x00, 0x00, 0x00, 0x00);

        DrawType curDrawType;
        WriteableBitmap WriteableBitmapSource;
        WriteableBitmap drawWriteableBitmapSource;

        public MainWindow()
        {
            InitializeComponent();

            PART_ScrollViewer.Drop += PART_ScrollViewer_Drop;
            PART_ScrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
            PART_ScrollViewer.MouseRightButtonUp += OnMouseLeftButtonUp;
            PART_ScrollViewer.PreviewMouseRightButtonUp += OnMouseLeftButtonUp;
            PART_ScrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;
            PART_ScrollViewer.PreviewMouseRightButtonDown += OnMouseLeftButtonDown;
            PART_ScrollViewer.MouseMove += OnMouseMove;

            PART_Grid.PreviewMouseLeftButtonDown += PART_Image_PreviewMouseLeftButtonDown;
            PART_Grid.MouseMove += PART_Image_MouseMove;
            PART_Grid.PreviewMouseLeftButtonUp += PART_Grid_PreviewMouseLeftButtonUp;

            EraserBtn.Click += EraserBtn_Click;
            BrushBtn.Click += BrushBtn_Click;
            LineBtn.Click += LineBtn_Click;
            PenSlider.ValueChanged += PenSlider_ValueChanged;
            color = drawColor;
        }

        private void PenSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if ((int)e.NewValue == 0)
            {
                brushWidth = 1;
            }
            else
            {
                brushWidth = (int)e.NewValue;
            }
        }

        private void LineBtn_Click(object sender, RoutedEventArgs e)
        {
            curDrawType = DrawType.Line;
            PART_Viewbox.Cursor = Cursors.Cross;
        }

        private void BrushBtn_Click(object sender, RoutedEventArgs e)
        {
            curDrawType = DrawType.Cursor;
            color = drawColor;
            PART_Viewbox.Cursor = CreateBrushCursor(cursorScale, cursorScale, Brushes.White, null);
        }

        private void EraserBtn_Click(object sender, RoutedEventArgs e)
        {
            curDrawType = DrawType.Eraser;
            color = eraserColor;
            PART_Viewbox.Cursor = CreateEraserCursor(cursorScale, cursorScale, Brushes.White, null);
        }

        private void PART_ScrollViewer_Drop(object sender, DragEventArgs e)
        {
            var dropItems = (string[])e.Data.GetData(DataFormats.FileDrop);
            _ImageInfo = new ImageInfo(dropItems[0]);
            PART_Image2.Source = new BitmapImage(new Uri(_ImageInfo.FilePath));

            PART_Grid.Width = _ImageInfo.Width;
            PART_Grid.Height = _ImageInfo.Height;

            _Image = new Mat(new OpenCvSharp.Size(_ImageInfo.Width, _ImageInfo.Height), MatType.CV_8UC4, new Scalar(0, 0, 0, 0));
            WriteableBitmapSource = new WriteableBitmap((int)PART_Grid.Width, (int)PART_Grid.Height, 96, 96, PixelFormats.Bgra32, null);
            roiRect = new Int32Rect(0, 0, (int)PART_Grid.Width, (int)PART_Grid.Height);
            imageStride = (int)_Image.Step();
            imageSize = (int)_Image.Total() * 4;
            WriteableBitmapSource.WritePixels(roiRect, _Image.Data, imageSize, imageStride, roiRect.X, roiRect.Y);

            tempMat = new Mat(new OpenCvSharp.Size(_ImageInfo.Width, _ImageInfo.Height), MatType.CV_8UC4, new Scalar(0, 0, 0, 0));
            drawWriteableBitmapSource = WriteableBitmapSource.Clone();

            SetCanvasScale();
            UpdateScale();

            PART_Viewbox.Cursor = CreateBrushCursor(cursorScale, cursorScale, Brushes.White, null);
        }

        private void UpdateScale(double scale = 1.0)
        {
            scaleTransform.ScaleX = scale;
            scaleTransform.ScaleY = scale;
            cursorScale = brushWidth * _CanvasScale;
        }

        private void ChangeScale(double scale)
        {
            scaleTransform.ScaleX *= scale;
            scaleTransform.ScaleY *= scale;

            cursorScale = cursorScale * scale;

            if (curDrawType == DrawType.Line)
                return;

            PART_Viewbox.Cursor = CreateBrushCursor(cursorScale, cursorScale, Brushes.White, null);
        }

        double _CanvasScale;
        private void SetCanvasScale()
        {
            if (_ImageInfo.Width > _ImageInfo.Height)
            {
                _CanvasScale = PART_Viewbox.Width / _ImageInfo.Width;
            }
            else if (_ImageInfo.Width == _ImageInfo.Height)
            {
                if (PART_Viewbox.Width > PART_Viewbox.Height)
                    _CanvasScale = PART_Viewbox.Height / _ImageInfo.Width;
                else
                    _CanvasScale = PART_Viewbox.Width / _ImageInfo.Height;
            }
            else
            {
                _CanvasScale = PART_Viewbox.Height / _ImageInfo.Height;
            }
        }


        void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0)
            {
                System.Windows.Point? targetBefore = null;
                System.Windows.Point? targetNow = null;

                if (!lastMousePositionOnTarget.HasValue)
                {
                    if (lastCenterPositionOnTarget.HasValue)
                    {
                        var centerOfViewport = new System.Windows.Point(PART_ScrollViewer.ViewportWidth / 2,
                                                         PART_ScrollViewer.ViewportHeight / 2);
                        System.Windows.Point centerOfTargetNow =
                              PART_ScrollViewer.TranslatePoint(centerOfViewport, PART_Viewbox);

                        targetBefore = lastCenterPositionOnTarget;
                        targetNow = centerOfTargetNow;
                    }
                }
                else
                {
                    targetBefore = lastMousePositionOnTarget;
                    targetNow = Mouse.GetPosition(PART_Viewbox);

                    lastMousePositionOnTarget = null;
                }

                if (targetBefore.HasValue)
                {
                    double dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
                    double dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;

                    double multiplicatorX = e.ExtentWidth / PART_Viewbox.Width;
                    double multiplicatorY = e.ExtentHeight / PART_Viewbox.Height;

                    double newOffsetX = PART_ScrollViewer.HorizontalOffset -
                                        dXInTargetPixels * multiplicatorX;
                    double newOffsetY = PART_ScrollViewer.VerticalOffset -
                                        dYInTargetPixels * multiplicatorY;

                    if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY))
                    {
                        return;
                    }

                    PART_ScrollViewer.ScrollToHorizontalOffset(newOffsetX);
                    PART_ScrollViewer.ScrollToVerticalOffset(newOffsetY);
                }
            }
        }

        void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            PART_ScrollViewer.Cursor = Cursors.Arrow;
            PART_ScrollViewer.ReleaseMouseCapture();
            lastDragPoint = null;
        }

        void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            lastMousePositionOnTarget = Mouse.GetPosition(PART_Viewbox);

            if (e.Delta > 0)
            {
                ChangeScale(_INCREASE_SCALE);
            }
            if (e.Delta < 0)
            {
                ChangeScale(_DECREASE_SCALE);
            }

            var centerOfViewport = new System.Windows.Point(PART_ScrollViewer.ViewportWidth / 2, PART_ScrollViewer.ViewportHeight / 2);
            lastCenterPositionOnTarget = PART_ScrollViewer.TranslatePoint(centerOfViewport, PART_Viewbox);

            e.Handled = true;
        }

        void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(PART_ScrollViewer);
            if (mousePos.X <= PART_ScrollViewer.ViewportWidth &&
                mousePos.Y < PART_ScrollViewer.ViewportHeight) //make sure we still can use the scrollbars
            {
                PART_ScrollViewer.Cursor = Cursors.SizeAll;
                lastDragPoint = mousePos;
                Mouse.Capture(PART_ScrollViewer);
            }
        }

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(PART_Grid);
            if (lastDragPoint.HasValue)
            {
                System.Windows.Point posNow = e.GetPosition(PART_ScrollViewer);

                double dX = posNow.X - lastDragPoint.Value.X;
                double dY = posNow.Y - lastDragPoint.Value.Y;

                lastDragPoint = posNow;

                PART_ScrollViewer.ScrollToHorizontalOffset(PART_ScrollViewer.HorizontalOffset - dX);
                PART_ScrollViewer.ScrollToVerticalOffset(PART_ScrollViewer.VerticalOffset - dY);
            }
        }

        System.Windows.Point? drawStartPoint;
        private void PART_Image_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(PART_Grid);
            if (curDrawType == DrawType.Line)
            {
                drawStartPoint = mousePos;
            }
            else
            {
                Cv2.Line(_Image, (int)mousePos.X, (int)mousePos.Y,
                    (int)mousePos.X, (int)mousePos.Y, color, brushWidth, LineTypes.Link8);
                roiRect.X = (int)mousePos.X - brushWidth / 2;
                roiRect.Y = (int)mousePos.Y - brushWidth / 2;
                roiRect.Width = brushWidth + 1;
                roiRect.Height = brushWidth + 1;
                UpdateImage();

                DrawlastDragPoint = mousePos;
            }
        }

        System.Windows.Point? drawPrevPoint;
        private void PART_Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var mousePos = e.GetPosition(PART_Grid);
                if (curDrawType == DrawType.Line)
                {
                    if (drawStartPoint.HasValue)
                    {
                        int preX = (int)drawStartPoint.Value.X;
                        int preY = (int)drawStartPoint.Value.Y;
                        int curX = (int)mousePos.X;
                        int curY = (int)mousePos.Y;

                        if (drawPrevPoint.HasValue)
                        {
                            Cv2.Line(tempMat, preX, preY, (int)drawPrevPoint.Value.X, (int)drawPrevPoint.Value.Y, eraserColor, brushWidth, LineTypes.Link8);
                            Cv2.Line(tempMat, preX, preY, curX, curY, color, brushWidth, LineTypes.Link8);

                            drawWriteableBitmapSource.WritePixels(roiRect, tempMat.Data, imageSize, imageStride, roiRect.X, roiRect.Y);

                            int startX = Math.Min(preX, curX);
                            int startY = Math.Min(preY, curY);
                            roiRect.X = startX - brushWidth / 2;
                            roiRect.Y = startY - brushWidth / 2;
                            roiRect.Width = Math.Abs((curX - preX)) + brushWidth + 1;
                            roiRect.Height = Math.Abs((curY - preY)) + brushWidth + 1;
                        }
                        else
                        {
                            Cv2.Line(tempMat, preX, preY, curX, curY, color, brushWidth, LineTypes.Link8);

                            int startX = Math.Min(preX, curX);
                            int startY = Math.Min(preY, curY);
                            roiRect.X = startX - brushWidth / 2;
                            roiRect.Y = startY - brushWidth / 2;
                            roiRect.Width = Math.Abs((curX - preX)) + brushWidth + 1;
                            roiRect.Height = Math.Abs((curY - preY)) + brushWidth + 1;
                            drawWriteableBitmapSource.WritePixels(roiRect, tempMat.Data, imageSize, imageStride, roiRect.X, roiRect.Y);
                            
                        }
                        drawPrevPoint = mousePos;
                        PART_Image3.Source = drawWriteableBitmapSource;
                    }
                }
                else
                {
                    if (DrawlastDragPoint.HasValue)
                    {
                        int preX = (int)DrawlastDragPoint.Value.X;
                        int preY = (int)DrawlastDragPoint.Value.Y;
                        int curX = (int)mousePos.X;
                        int curY = (int)mousePos.Y;
                        Cv2.Line(_Image, (int)DrawlastDragPoint.Value.X, (int)DrawlastDragPoint.Value.Y,
                            (int)mousePos.X, curY, color, brushWidth, LineTypes.Link8);
                        int startX = Math.Min(preX, curX);
                        int startY = Math.Min(preY, curY);
                        roiRect.X = startX - brushWidth / 2;
                        roiRect.Y = startY - brushWidth / 2;
                        roiRect.Width = Math.Abs((curX - preX)) + brushWidth + 1;
                        roiRect.Height = Math.Abs((curY - preY)) + brushWidth + 1;

                        UpdateImage();
                        DrawlastDragPoint = mousePos;
                    }
                }
            }
        }

        private void PART_Grid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (curDrawType == DrawType.Line)
            {
                int preX = (int)drawStartPoint.Value.X;
                int preY = (int)drawStartPoint.Value.Y;
                int curX = (int)drawPrevPoint.Value.X;
                int curY = (int)drawPrevPoint.Value.Y;

                Cv2.Line(_Image, preX, preY, curX, curY, color, brushWidth, LineTypes.Link8);
                UpdateImage();
            }

            

            tempMat = new Mat(new OpenCvSharp.Size(_ImageInfo.Width, _ImageInfo.Height), MatType.CV_8UC4, new Scalar(0, 0, 0, 0));
            drawWriteableBitmapSource.WritePixels(roiRect, tempMat.Data, imageSize, imageStride, roiRect.X, roiRect.Y);
            PART_Image3.Source = drawWriteableBitmapSource;

            drawStartPoint = null;
            drawPrevPoint = null;
        }

        private void UpdateImage()
        {
            WriteableBitmapSource.WritePixels(roiRect, _Image.Data, imageSize, imageStride, roiRect.X, roiRect.Y);
            PART_Image.Source = WriteableBitmapSource;
        }
        

        private Cursor CreateEraserCursor(double rx, double ry, SolidColorBrush brush, System.Windows.Media.Pen pen)
        {
            var vis = new DrawingVisual();
            using (var dc = vis.RenderOpen())
            {

                System.Windows.Rect r = new System.Windows.Rect(0, 0, rx, ry);
                System.Windows.Point center = new System.Windows.Point(
                     (r.Left + r.Right) / 2.0,
                     (r.Top + r.Bottom) / 2.0);

                double radiusX = (r.Right - r.Left) / 2.0;
                double radiusY = (r.Bottom - r.Top) / 2.0;

                dc.DrawEllipse(Brushes.White, new System.Windows.Media.Pen(Brushes.Black, 0.5), center, radiusX, radiusY);
                dc.Close();
            }
            var rtb = new RenderTargetBitmap((int)Math.Ceiling(rx), (int)Math.Ceiling(ry), 96, 96, PixelFormats.Pbgra32);
            rtb.Render(vis);

            using (var ms1 = new MemoryStream())
            {
                var penc = new PngBitmapEncoder();
                penc.Frames.Add(BitmapFrame.Create(rtb));
                penc.Save(ms1);

                var pngBytes = ms1.ToArray();
                var size = pngBytes.GetLength(0);

                //.cur format spec http://en.wikipedia.org/wiki/ICO_(file_format)
                using (var ms = new MemoryStream())
                {
                    {//ICONDIR Structure
                        ms.Write(BitConverter.GetBytes((Int16)0), 0, 2);//Reserved must be zero; 2 bytes
                        ms.Write(BitConverter.GetBytes((Int16)2), 0, 2);//image type 1 = ico 2 = cur; 2 bytes
                        ms.Write(BitConverter.GetBytes((Int16)1), 0, 2);//number of images; 2 bytes
                    }

                    {//ICONDIRENTRY structure
                        ms.WriteByte(32); //image width in pixels
                        ms.WriteByte(32); //image height in pixels

                        ms.WriteByte(0); //Number of Colors in the color palette. Should be 0 if the image doesn't use a color palette
                        ms.WriteByte(0); //reserved must be 0

                        ms.Write(BitConverter.GetBytes((Int16)(rx / 2.0)), 0, 2);//2 bytes. In CUR format: Specifies the horizontal coordinates of the hotspot in number of pixels from the left.
                        ms.Write(BitConverter.GetBytes((Int16)(ry / 2.0)), 0, 2);//2 bytes. In CUR format: Specifies the vertical coordinates of the hotspot in number of pixels from the top.

                        ms.Write(BitConverter.GetBytes(size), 0, 4);//Specifies the size of the image's data in bytes
                        ms.Write(BitConverter.GetBytes((Int32)22), 0, 4);//Specifies the offset of BMP or PNG data from the beginning of the ICO/CUR file
                    }

                    ms.Write(pngBytes, 0, size);//write the png data.
                    ms.Seek(0, SeekOrigin.Begin);
                    return new Cursor(ms);
                }
            }
        }

        private Cursor CreateBrushCursor(double rx, double ry, SolidColorBrush brush, System.Windows.Media.Pen pen)
        {
            var vis = new DrawingVisual();
            using (var dc = vis.RenderOpen())
            {

                System.Windows.Rect r = new System.Windows.Rect(0, 0, rx, ry);
                System.Windows.Point center = new System.Windows.Point(
                     (r.Left + r.Right) / 2.0,
                     (r.Top + r.Bottom) / 2.0);

                double radiusX = (r.Right - r.Left) / 2.0;
                double radiusY = (r.Bottom - r.Top) / 2.0;

                dc.DrawEllipse(Brushes.Transparent, new System.Windows.Media.Pen(Brushes.Black, 0.5), center, radiusX, radiusY);
                dc.Close();
            }
            var rtb = new RenderTargetBitmap((int)Math.Ceiling(rx), (int)Math.Ceiling(ry), 96, 96, PixelFormats.Pbgra32);
            rtb.Render(vis);

            using (var ms1 = new MemoryStream())
            {
                var penc = new PngBitmapEncoder();
                penc.Frames.Add(BitmapFrame.Create(rtb));
                penc.Save(ms1);

                var pngBytes = ms1.ToArray();
                var size = pngBytes.GetLength(0);

                //.cur format spec http://en.wikipedia.org/wiki/ICO_(file_format)
                using (var ms = new MemoryStream())
                {
                    {//ICONDIR Structure
                        ms.Write(BitConverter.GetBytes((Int16)0), 0, 2);//Reserved must be zero; 2 bytes
                        ms.Write(BitConverter.GetBytes((Int16)2), 0, 2);//image type 1 = ico 2 = cur; 2 bytes
                        ms.Write(BitConverter.GetBytes((Int16)1), 0, 2);//number of images; 2 bytes
                    }

                    {//ICONDIRENTRY structure
                        ms.WriteByte(32); //image width in pixels
                        ms.WriteByte(32); //image height in pixels

                        ms.WriteByte(0); //Number of Colors in the color palette. Should be 0 if the image doesn't use a color palette
                        ms.WriteByte(0); //reserved must be 0

                        ms.Write(BitConverter.GetBytes((Int16)(rx / 2.0)), 0, 2);//2 bytes. In CUR format: Specifies the horizontal coordinates of the hotspot in number of pixels from the left.
                        ms.Write(BitConverter.GetBytes((Int16)(ry / 2.0)), 0, 2);//2 bytes. In CUR format: Specifies the vertical coordinates of the hotspot in number of pixels from the top.

                        ms.Write(BitConverter.GetBytes(size), 0, 4);//Specifies the size of the image's data in bytes
                        ms.Write(BitConverter.GetBytes((Int32)22), 0, 4);//Specifies the offset of BMP or PNG data from the beginning of the ICO/CUR file
                    }

                    ms.Write(pngBytes, 0, size);//write the png data.
                    ms.Seek(0, SeekOrigin.Begin);
                    return new Cursor(ms);
                }
            }
        }
    }

    public enum DrawType
    {
        Cursor,
        Eraser,
        Line
    }

    public static class PointExtensionMethod
    {
        public static void Truncate(this System.Windows.Point point)
        {
            point.X = (int)point.X;
            point.Y = (int)point.Y;
        }
    }
}
