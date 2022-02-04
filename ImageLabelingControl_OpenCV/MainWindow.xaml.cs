using ImageLabelingControl_OpenCV.Draw;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;
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
        #region Field
        private int _ImageStride;
        private int _ImageSize;
        private Int32Rect _RoiRect;
        private Mat _LabelImage;
        private Mat _TempLabelImage;
        private ImageInfo _ImageInfo;
        private System.Windows.Point? _LastCenterPositionOnTarget;
        private System.Windows.Point? _LastMousePositionOnTarget;
        private System.Windows.Point? _LastDragPoint;
        private System.Windows.Point? _DrawlastDragPoint;
        private System.Windows.Point? _DrawStartPoint;
        private System.Windows.Point? _DrawPrevPoint;
        private int _BrushWidth = 10;

        private const double _INIT_SCALE = 0.96;
        private const double _INCREASE_SCALE = 1.1;
        private const double _DECREASE_SCALE = 0.9;
        private double _CanvasScale;
        private double _CurCursorScale;

        private Scalar _Curcolor;
        private Scalar _DrawColor = new Scalar(0x45, 0x00, 0xFF, 0x64);
        private Scalar _EraserColor = new Scalar(0x00, 0x00, 0x00, 0x00);

        private DrawType _CurDrawType;

        private WriteableBitmap _WriteableBitmapSource;
        private WriteableBitmap _DrawWriteableBitmapSource;

        private DrawLabelBase _DrawLabel;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            PART_ScrollViewer.Drop += PART_ScrollViewer_Drop;
            PART_ScrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
            PART_ScrollViewer.MouseRightButtonUp += OnScrollViewerMouseLeftButtonUp;
            PART_ScrollViewer.PreviewMouseWheel += OnScrollViewerPreviewMouseWheel;
            PART_ScrollViewer.PreviewMouseRightButtonDown += OnScrollViewerMouseLeftButtonDown;
            PART_ScrollViewer.MouseMove += OnScrollViewerMouseMove;

            PART_Grid.PreviewMouseLeftButtonDown += PART_Image_PreviewMouseLeftButtonDown;
            PART_Grid.MouseMove += PART_Image_MouseMove;
            PART_Grid.PreviewMouseLeftButtonUp += PART_Grid_PreviewMouseLeftButtonUp;

            EraserBtn.Click += EraserBtn_Click;
            BrushBtn.Click += BrushBtn_Click;
            LineBtn.Click += LineBtn_Click;
            PenSlider.ValueChanged += PenSlider_ValueChanged;

            PART_Viewbox.Cursor = CustomCursors.Brush(_BrushWidth * _CurCursorScale);
            _Curcolor = _DrawColor;
        }

        #region ScrollViewer Method
        private void PART_ScrollViewer_Drop(object sender, DragEventArgs e)
        {
            var dropItems = (string[])e.Data.GetData(DataFormats.FileDrop);
            _ImageInfo = new ImageInfo(dropItems[0]);
            PART_BackgroundImage.Source = new BitmapImage(new Uri(_ImageInfo.FilePath));

            PART_Grid.Width = _ImageInfo.Width;
            PART_Grid.Height = _ImageInfo.Height;

            InitImage();
            SetCanvasScale();
            UpdateScale();

            PART_Viewbox.Cursor = CustomCursors.Brush(_BrushWidth * _CurCursorScale);
        }

        private void InitImage()
        {
            _LabelImage = new Mat(new OpenCvSharp.Size(_ImageInfo.Width, _ImageInfo.Height), MatType.CV_8UC4, new Scalar(0, 0, 0, 0));
            _WriteableBitmapSource = new WriteableBitmap(_ImageInfo.Width, _ImageInfo.Height, 96, 96, PixelFormats.Bgra32, null);
            _RoiRect = new Int32Rect(0, 0, _ImageInfo.Width, _ImageInfo.Height);
            _ImageStride = (int)_LabelImage.Step();
            _ImageSize = (int)_LabelImage.Total() * 4;
            _WriteableBitmapSource.WritePixels(_RoiRect, _LabelImage.Data, _ImageSize, _ImageStride, _RoiRect.X, _RoiRect.Y);

            _TempLabelImage = new Mat(new OpenCvSharp.Size(_ImageInfo.Width, _ImageInfo.Height), MatType.CV_8UC4, new Scalar(0, 0, 0, 0));
            _DrawWriteableBitmapSource = _WriteableBitmapSource.Clone();
        }

        void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0)
            {
                System.Windows.Point? targetBefore = null;
                System.Windows.Point? targetNow = null;

                if (!_LastMousePositionOnTarget.HasValue)
                {
                    if (_LastCenterPositionOnTarget.HasValue)
                    {
                        var centerOfViewport = new System.Windows.Point(PART_ScrollViewer.ViewportWidth / 2,
                                                        PART_ScrollViewer.ViewportHeight / 2);
                        System.Windows.Point centerOfTargetNow = PART_ScrollViewer.TranslatePoint(centerOfViewport, PART_Viewbox);
                        targetBefore = _LastCenterPositionOnTarget;
                        targetNow = centerOfTargetNow;
                    }
                }
                else
                {
                    targetBefore = _LastMousePositionOnTarget;
                    targetNow = Mouse.GetPosition(PART_Viewbox);
                    _LastMousePositionOnTarget = null;
                }

                if (targetBefore.HasValue)
                {
                    SetScrollBarOffset(e, targetNow, targetBefore);
                }
            }
        }

        private void SetScrollBarOffset(ScrollChangedEventArgs e, System.Windows.Point? targetNow, System.Windows.Point? targetBefore)
        {
            double dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
            double dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;

            double multiplicatorX = e.ExtentWidth / PART_Viewbox.Width;
            double multiplicatorY = e.ExtentHeight / PART_Viewbox.Height;

            double newOffsetX = PART_ScrollViewer.HorizontalOffset - dXInTargetPixels * multiplicatorX;
            double newOffsetY = PART_ScrollViewer.VerticalOffset - dYInTargetPixels * multiplicatorY;

            if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY))
                return;

            PART_ScrollViewer.ScrollToHorizontalOffset(newOffsetX);
            PART_ScrollViewer.ScrollToVerticalOffset(newOffsetY);
        }

        void OnScrollViewerMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            PART_ScrollViewer.Cursor = Cursors.Arrow;
            PART_ScrollViewer.ReleaseMouseCapture();
            _LastDragPoint = null;
        }

        void OnScrollViewerPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            _LastMousePositionOnTarget = Mouse.GetPosition(PART_Viewbox);

            if (e.Delta > 0)
            {
                ChangeScale(_INCREASE_SCALE);
            }
            if (e.Delta < 0)
            {
                ChangeScale(_DECREASE_SCALE);
            }

            var centerOfViewport = new System.Windows.Point(PART_ScrollViewer.ViewportWidth / 2, PART_ScrollViewer.ViewportHeight / 2);
            _LastCenterPositionOnTarget = PART_ScrollViewer.TranslatePoint(centerOfViewport, PART_Viewbox);

            e.Handled = true;
        }

        void OnScrollViewerMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(PART_ScrollViewer);
            if (mousePos.X <= PART_ScrollViewer.ViewportWidth &&
                mousePos.Y < PART_ScrollViewer.ViewportHeight) //make sure we still can use the scrollbars
            {
                PART_ScrollViewer.Cursor = Cursors.SizeAll;
                _LastDragPoint = mousePos;
                Mouse.Capture(PART_ScrollViewer);
            }
        }

        void OnScrollViewerMouseMove(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(PART_Grid);
            if (_LastDragPoint.HasValue)
            {
                System.Windows.Point posNow = e.GetPosition(PART_ScrollViewer);

                double dX = posNow.X - _LastDragPoint.Value.X;
                double dY = posNow.Y - _LastDragPoint.Value.Y;

                _LastDragPoint = posNow;

                PART_ScrollViewer.ScrollToHorizontalOffset(PART_ScrollViewer.HorizontalOffset - dX);
                PART_ScrollViewer.ScrollToVerticalOffset(PART_ScrollViewer.VerticalOffset - dY);
            }
        }
        #endregion

        #region Test Tools
        private void PenSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if ((int)e.NewValue == 0)
            {
                _BrushWidth = 1;
            }
            else
            {
                _BrushWidth = (int)e.NewValue;
            }
            PART_Viewbox.Cursor = CustomCursors.Brush(_BrushWidth * _CurCursorScale);
        }

        private void LineBtn_Click(object sender, RoutedEventArgs e)
        {
            _CurDrawType = DrawType.Line;
            PART_Viewbox.Cursor = Cursors.Cross;
            _DrawLabel = new DrawLine();
        }

        private void BrushBtn_Click(object sender, RoutedEventArgs e)
        {
            _CurDrawType = DrawType.Cursor;
            _Curcolor = _DrawColor;
            PART_Viewbox.Cursor = CustomCursors.Brush(_BrushWidth * _CurCursorScale);
        }

        private void EraserBtn_Click(object sender, RoutedEventArgs e)
        {
            _CurDrawType = DrawType.Eraser;
            _Curcolor = _EraserColor;
            PART_Viewbox.Cursor = CustomCursors.Eraser(_BrushWidth * _CurCursorScale);
        }
        #endregion

        #region Scale Method
        private void UpdateScale(double scale = 1.0)
        {
            scaleTransform.ScaleX = scale * _INIT_SCALE;
            scaleTransform.ScaleY = scale * _INIT_SCALE;
            _CurCursorScale = _CanvasScale * _INIT_SCALE;
        }

        private void ChangeScale(double scale)
        {
            scaleTransform.ScaleX *= scale;
            scaleTransform.ScaleY *= scale;
            _CurCursorScale *= scale;

            if (_CurDrawType == DrawType.Line)
                return;

            PART_Viewbox.Cursor = CustomCursors.Brush(_BrushWidth * _CurCursorScale);
        }

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
        #endregion

        private void PART_Image_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(PART_Grid);
            if (_CurDrawType == DrawType.Line)
            {
                //_DrawStartPoint = mousePos;
                _DrawLabel.OnMouseDown(PART_Grid, _Curcolor, _ImageSize, _ImageStride, _BrushWidth, e);
            }
            else
            {
                Cv2.Circle(_LabelImage, (int)mousePos.X, (int)mousePos.Y, _BrushWidth/2, _Curcolor, -1, LineTypes.Link4);

                _RoiRect.X = (int)mousePos.X - _BrushWidth / 2;
                _RoiRect.Y = (int)mousePos.Y - _BrushWidth / 2;
                _RoiRect.Width = _BrushWidth + 1;
                _RoiRect.Height = _BrushWidth + 1;
                UpdateLabelImage();

                _DrawlastDragPoint = mousePos;
            }
        }

        private void PART_Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var mousePos = e.GetPosition(PART_Grid);
                if (_CurDrawType == DrawType.Line)
                {
                    _DrawLabel.OnMouseMove(PART_Grid, PART_TemplabelImage, _DrawWriteableBitmapSource, _RoiRect, e);
                    //if (_DrawStartPoint.HasValue)
                    //{
                    //    int preX = (int)_DrawStartPoint.Value.X;
                    //    int preY = (int)_DrawStartPoint.Value.Y;
                    //    int curX = (int)mousePos.X;
                    //    int curY = (int)mousePos.Y;

                    //    if (_DrawPrevPoint.HasValue)
                    //    {
                    //        Cv2.Line(_TempLabelImage, preX, preY, (int)_DrawPrevPoint.Value.X, (int)_DrawPrevPoint.Value.Y, _EraserColor, _BrushWidth, LineTypes.Link8);
                    //        Cv2.Line(_TempLabelImage, preX, preY, curX, curY, _Curcolor, _BrushWidth, LineTypes.Link8);

                    //        _DrawWriteableBitmapSource.WritePixels(_RoiRect, _TempLabelImage.Data, _ImageSize, _ImageStride, _RoiRect.X, _RoiRect.Y);

                    //        int startX = Math.Min(preX, curX);
                    //        int startY = Math.Min(preY, curY);
                    //        _RoiRect.X = startX - _BrushWidth / 2;
                    //        _RoiRect.Y = startY - _BrushWidth / 2;
                    //        _RoiRect.Width = Math.Abs((curX - preX)) + _BrushWidth + 1;
                    //        _RoiRect.Height = Math.Abs((curY - preY)) + _BrushWidth + 1;
                    //    }
                    //    else
                    //    {
                    //        Cv2.Line(_TempLabelImage, preX, preY, curX, curY, _Curcolor, _BrushWidth, LineTypes.Link8);

                    //        int startX = Math.Min(preX, curX);
                    //        int startY = Math.Min(preY, curY);
                    //        _RoiRect.X = startX - _BrushWidth / 2;
                    //        _RoiRect.Y = startY - _BrushWidth / 2;
                    //        _RoiRect.Width = Math.Abs((curX - preX)) + _BrushWidth + 1;
                    //        _RoiRect.Height = Math.Abs((curY - preY)) + _BrushWidth + 1;
                    //        _DrawWriteableBitmapSource.WritePixels(_RoiRect, _TempLabelImage.Data, _ImageSize, _ImageStride, _RoiRect.X, _RoiRect.Y);

                    //    }
                    //    _DrawPrevPoint = mousePos;
                    //    PART_TemplabelImage.Source = _DrawWriteableBitmapSource;
                    //}
                }
                else
                {
                    if (_DrawlastDragPoint.HasValue)
                    {
                        int preX = (int)_DrawlastDragPoint.Value.X;
                        int preY = (int)_DrawlastDragPoint.Value.Y;
                        int curX = (int)mousePos.X;
                        int curY = (int)mousePos.Y;
                        Cv2.Line(_LabelImage, (int)_DrawlastDragPoint.Value.X, (int)_DrawlastDragPoint.Value.Y,
                            (int)mousePos.X, curY, _Curcolor, _BrushWidth, LineTypes.Link8);
                        int startX = Math.Min(preX, curX);
                        int startY = Math.Min(preY, curY);
                        _RoiRect.X = startX - _BrushWidth / 2;
                        _RoiRect.Y = startY - _BrushWidth / 2;
                        _RoiRect.Width = Math.Abs((curX - preX)) + _BrushWidth + 1;
                        _RoiRect.Height = Math.Abs((curY - preY)) + _BrushWidth + 1;

                        UpdateLabelImage();
                        _DrawlastDragPoint = mousePos;
                    }
                }
            }
        }

        private void PART_Grid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_CurDrawType == DrawType.Line)
            {
                _DrawLabel.OnMouseUp(PART_LabelImage, _LabelImage, _WriteableBitmapSource, _RoiRect, e);
                //int preX = (int)_DrawStartPoint.Value.X;
                //int preY = (int)_DrawStartPoint.Value.Y;
                //int curX = (int)_DrawPrevPoint.Value.X;
                //int curY = (int)_DrawPrevPoint.Value.Y;

                //Cv2.Line(_LabelImage, preX, preY, curX, curY, _Curcolor, _BrushWidth, LineTypes.Link8);
                //UpdateLabelImage();
            }

            _TempLabelImage = new Mat(new OpenCvSharp.Size(_ImageInfo.Width, _ImageInfo.Height), MatType.CV_8UC4, new Scalar(0, 0, 0, 0));
            _DrawWriteableBitmapSource.WritePixels(_RoiRect, _TempLabelImage.Data, _ImageSize, _ImageStride, _RoiRect.X, _RoiRect.Y);
            PART_TemplabelImage.Source = _DrawWriteableBitmapSource;

            _DrawStartPoint = null;
            _DrawPrevPoint = null;
        }

        private void UpdateLabelImage()
        {
            _WriteableBitmapSource.WritePixels(_RoiRect, _LabelImage.Data, _ImageSize, _ImageStride, _RoiRect.X, _RoiRect.Y);
            PART_LabelImage.Source = _WriteableBitmapSource;
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
