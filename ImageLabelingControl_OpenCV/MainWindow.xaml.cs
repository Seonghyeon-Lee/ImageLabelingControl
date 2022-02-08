using ControlCore.Model;
using ImageLabelingControl_OpenCV.Draw;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        #region Fields
        private int _ImageStride;
        private int _ImageSize;
        private Int32Rect _RoiRect;
        private Mat _LabelImage;
        private Mat _TempLabelImage;
        private ImageInfo _ImageInfo;
        private System.Windows.Point? _LastCenterPositionOnTarget;
        private System.Windows.Point? _LastMousePositionOnTarget;
        private System.Windows.Point? _LastDragPoint;
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

        private IntPoint _DrawingStartPos = new IntPoint();
        private IntPoint _DrawingLastPos = new IntPoint();
        private bool _IsFirstDraw = true;

        #endregion


        #region Contructors
        public MainWindow()
        {
            InitializeComponent();

            PART_ScrollViewer.Drop += PART_ScrollViewer_Drop;
            PART_ScrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
            PART_ScrollViewer.MouseRightButtonUp += OnScrollViewerMouseLeftButtonUp;
            PART_ScrollViewer.PreviewMouseWheel += OnScrollViewerPreviewMouseWheel;
            PART_ScrollViewer.PreviewMouseRightButtonDown += OnScrollViewerMouseLeftButtonDown;
            PART_ScrollViewer.MouseMove += OnScrollViewerMouseMove;

            PART_Grid.PreviewMouseLeftButtonDown += PART_Grid_PreviewMouseLeftButtonDown;
            PART_Grid.MouseMove += PART_Grid_MouseMove;
            PART_Grid.PreviewMouseLeftButtonUp += PART_Grid_PreviewMouseLeftButtonUp;

            EraserBtn.Click += EraserBtn_Click;
            BrushBtn.Click += BrushBtn_Click;
            LineBtn.Click += LineBtn_Click;
            RectBtn.Click += RectBtn_Click;
            PenSlider.ValueChanged += PenSlider_ValueChanged;

            PART_Viewbox.Cursor = CustomCursors.Brush(_BrushWidth * _CurCursorScale);
            _Curcolor = _DrawColor;
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

        #endregion


        #region Method

        #region Draw Action
        private void DrawBrush(System.Windows.Point pos)
        {
            int curX = (int)pos.X;
            int curY = (int)pos.Y;

            Cv2.Line(_LabelImage, _DrawingStartPos.X, _DrawingStartPos.Y, curX, curY, _Curcolor, _BrushWidth, LineTypes.Link8);
            UpdateWriteableBitmapRoi(curX, curY);
            UpdateLabelLayer();

            _DrawingStartPos.Set(pos);
        }

        private void DrawLine(System.Windows.Point pos)
        {
            int curX = (int)pos.X;
            int curY = (int)pos.Y;

            if (!_IsFirstDraw)
            {
                Cv2.Line(_TempLabelImage, _DrawingStartPos.X, _DrawingStartPos.Y,
                    _DrawingLastPos.X, _DrawingLastPos.Y, _EraserColor, _BrushWidth, LineTypes.Link8);

                Cv2.Line(_TempLabelImage, _DrawingStartPos.X, _DrawingStartPos.Y, curX, curY, _Curcolor, _BrushWidth, LineTypes.Link8);
                _DrawWriteableBitmapSource.WritePixels(_RoiRect, _TempLabelImage.Data, _ImageSize, _ImageStride, _RoiRect.X, _RoiRect.Y);
                UpdateWriteableBitmapRoi(curX, curY);
            }
            else
            {
                Cv2.Line(_TempLabelImage, _DrawingStartPos.X, _DrawingStartPos.Y, curX, curY, _Curcolor, _BrushWidth, LineTypes.Link8);

                UpdateWriteableBitmapRoi(curX, curY);
                _DrawWriteableBitmapSource.WritePixels(_RoiRect, _TempLabelImage.Data, _ImageSize, _ImageStride, _RoiRect.X, _RoiRect.Y);
            }

            _DrawingLastPos.Set(pos);
            PART_TemplabelImage.Source = _DrawWriteableBitmapSource;
            _IsFirstDraw = false;
        }

        private void DrawRect(System.Windows.Point pos)
        {
            int curX = (int)pos.X;
            int curY = (int)pos.Y;

            if (!_IsFirstDraw)
            {
                Cv2.Rectangle(_TempLabelImage, new OpenCvSharp.Point(_DrawingStartPos.X, _DrawingStartPos.Y),
                    new OpenCvSharp.Point(_DrawingLastPos.X, _DrawingLastPos.Y), _EraserColor, -1, LineTypes.Link8);

                Cv2.Rectangle(_TempLabelImage, new OpenCvSharp.Point(_DrawingStartPos.X, _DrawingStartPos.Y),
                    new OpenCvSharp.Point(curX, curY), _Curcolor, -1, LineTypes.Link8);
                _DrawWriteableBitmapSource.WritePixels(_RoiRect, _TempLabelImage.Data, _ImageSize, _ImageStride, _RoiRect.X, _RoiRect.Y);
                UpdateWriteableBitmapRoi(curX, curY);
            }
            else
            {
                Cv2.Rectangle(_TempLabelImage, new OpenCvSharp.Point(_DrawingStartPos.X, _DrawingStartPos.Y), 
                    new OpenCvSharp.Point(curX, curY), _Curcolor, -1, LineTypes.Link8);

                UpdateWriteableBitmapRoi(curX, curY);
                _DrawWriteableBitmapSource.WritePixels(_RoiRect, _TempLabelImage.Data, _ImageSize, _ImageStride, _RoiRect.X, _RoiRect.Y);
            }

            _DrawingLastPos.Set(pos);
            PART_TemplabelImage.Source = _DrawWriteableBitmapSource;
            _IsFirstDraw = false;
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

        private void UpdateWriteableBitmapRoi(System.Windows.Point pos)
        {
            _RoiRect.X = (int)pos.X - _BrushWidth / 2;
            _RoiRect.Y = (int)pos.Y - _BrushWidth / 2;
            _RoiRect.Width = _BrushWidth + 1;
            _RoiRect.Height = _BrushWidth + 1;
        }

        private void UpdateWriteableBitmapRoi(int posX, int posY)
        {
            int startX = Math.Min(_DrawingStartPos.X, posX);
            int startY = Math.Min(_DrawingStartPos.Y, posY);

            _RoiRect.X = startX - _BrushWidth / 2;
            _RoiRect.Y = startY - _BrushWidth / 2;
            _RoiRect.Width = Math.Abs(_DrawingStartPos.X - posX) + _BrushWidth + 1;
            _RoiRect.Height = Math.Abs(_DrawingStartPos.Y - posY) + _BrushWidth + 1;
        }

        private void UpdateLabelLayer()
        {
            _WriteableBitmapSource.WritePixels(_RoiRect, _LabelImage.Data, _ImageSize, _ImageStride, _RoiRect.X, _RoiRect.Y);
            PART_LabelImage.Source = _WriteableBitmapSource;
        }

        private void UpdateTempLabelLayer()
        {

        }

        #endregion

        #region Eventhandler

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
            _CurDrawType = DrawType.Brush;
            _Curcolor = _DrawColor;
            PART_Viewbox.Cursor = CustomCursors.Brush(_BrushWidth * _CurCursorScale);
            _DrawLabel = null;
        }

        private void EraserBtn_Click(object sender, RoutedEventArgs e)
        {
            _CurDrawType = DrawType.Eraser;
            _Curcolor = _EraserColor;
            PART_Viewbox.Cursor = CustomCursors.Eraser(_BrushWidth * _CurCursorScale);
            _DrawLabel = null;
        }

        private void RectBtn_Click(object sender, RoutedEventArgs e)
        {
            _CurDrawType = DrawType.Rect;
            PART_Viewbox.Cursor = Cursors.Cross;
            _DrawLabel = new DrawRectangle();
        }
        #endregion

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

        #region Grid

        private void PART_Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_LabelImage == null)
                return;

            var mousePos = e.GetPosition(PART_Grid);
            if (_CurDrawType == DrawType.Brush ||
                _CurDrawType == DrawType.Eraser)
            {
                Cv2.Circle(_LabelImage, _DrawingStartPos.X, _DrawingStartPos.Y, _BrushWidth / 2, _Curcolor, -1, LineTypes.Link4);
                UpdateWriteableBitmapRoi(mousePos);
                UpdateLabelLayer();
            }
            else
            {
                _DrawLabel.OnMouseDown(mousePos, _LabelImage.Width, _LabelImage.Height, _ImageSize, _ImageStride, _BrushWidth, _Curcolor);
            }

            _DrawingStartPos.Set(mousePos);
        }

        private void PART_Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            var mousePos = e.GetPosition(PART_Grid);
            switch (_CurDrawType)
            {
                case DrawType.Brush:
                case DrawType.Eraser:
                    DrawBrush(mousePos);
                    break;
                case DrawType.Line:
                case DrawType.Rect:
                    // DrawLine(mousePos);
                    _DrawLabel.OnMouseMove(mousePos, _DrawWriteableBitmapSource, ref _RoiRect);
                    PART_TemplabelImage.Source = _DrawWriteableBitmapSource;
                    // DrawRect(mousePos);
                    break;
                default:
                    break;
            }
        }

        private void PART_Grid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_DrawLabel != null)
            {
                _DrawLabel.OnMouseUp(_LabelImage, _WriteableBitmapSource, _DrawWriteableBitmapSource, _RoiRect);
            }
            UpdateLabelLayer();
            PART_TemplabelImage.Source = _DrawWriteableBitmapSource;
        }

        #endregion

        #endregion
    }

    
}
