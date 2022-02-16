using ControlCore.Model;
using ImageLabelingControl_OpenCV.Draw;
using OpenCvSharp;
using System;
using System.Collections.Generic;
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
        private Mat _LabelImage;
        private Int32Rect _RoiRect;
        private ImageInfo _ImageInfo;
        private WriteableBitmap _WriteableBitmapSource;
        private WriteableBitmap _DrawWriteableBitmapSource;

        private int _ImageSize;
        private int _ImageStride;
        private int _Thickness = 10;

        private Scalar _Curcolor;
        private Scalar _DrawColor = new Scalar(0x45, 0x00, 0xFF, 0x64);
        private Scalar _EraserColor = new Scalar(0x00, 0x00, 0x00, 0x00);

        private System.Windows.Point? _LastDragPoint;
        private System.Windows.Point? _LastCenterPositionOnTarget;
        private System.Windows.Point? _LastMousePositionOnTarget;
        private IntPoint _DrawingLastPos;

        private double _CanvasScale;
        private double _CurCursorScale;
        private const double _INIT_SCALE = 0.96;
        private const double _INCREASE_SCALE = 1.1;
        private const double _DECREASE_SCALE = 0.9;
        
        private DrawType _CurDrawType;
        private DrawShape _DrawingLabel;
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
            PART_Grid.PreviewMouseRightButtonDown += PART_Grid_PreviewMouseRightButtonDown;

            EraserBtn.Click += EraserBtn_Click;
            BrushBtn.Click += BrushBtn_Click;
            LineBtn.Click += LineBtn_Click;
            RectBtn.Click += RectBtn_Click;
            EllipseBtn.Click += EllipseBtn_Click;
            PolylineBtn.Click += PolylineBtn_Click;
            PolygonBtn.Click += PolygonBtn_Click;
            PenSlider.ValueChanged += PenSlider_ValueChanged;

            PART_Viewbox.Cursor = CustomCursors.Brush(_Thickness * _CurCursorScale);

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
            _DrawWriteableBitmapSource = _WriteableBitmapSource.Clone();
        }

        #endregion

        #region Method

        #region Drawing Method
        private void DrawBrush(System.Windows.Point mousePos)
        {
            int curX = (int)mousePos.X;
            int curY = (int)mousePos.Y;

            Cv2.Line(_LabelImage, _DrawingLastPos.X, _DrawingLastPos.Y, curX, curY, _Curcolor, _Thickness, LineTypes.Link8);
            UpdateWriteableBitmapRoi(curX, curY);
            UpdateLabelLayer();

            _DrawingLastPos.Set(mousePos);
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

            ChangeBrushScale();
        }

        private void ChangeBrushScale()
        {
            if (_DrawingLabel != null)
                return;

            if (_CurDrawType == DrawType.Brush)
            {
                PART_Viewbox.Cursor = CustomCursors.Brush(_Thickness * _CurCursorScale);
            }
            else
            {
                PART_Viewbox.Cursor = CustomCursors.Eraser(_Thickness * _CurCursorScale);
            }
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

        private void UpdateWriteableBitmapRoi(int posX, int posY)
        {
            _RoiRect.X = Math.Min(_DrawingLastPos.X, posX) - _Thickness / 2;
            _RoiRect.Y = Math.Min(_DrawingLastPos.Y, posY) - _Thickness / 2;
            _RoiRect.Width = Math.Abs(_DrawingLastPos.X - posX) + _Thickness + 1;
            _RoiRect.Height = Math.Abs(_DrawingLastPos.Y - posY) + _Thickness + 1;
        }

        private void UpdateLabelLayer()
        {
            _WriteableBitmapSource.WritePixels(_RoiRect, _LabelImage.Data, _ImageSize, _ImageStride, _RoiRect.X, _RoiRect.Y);
            PART_LabelImageLayer.Source = _WriteableBitmapSource;
        }
        #endregion

        #region Eventhandler

        #region Test Tools

        private void PenSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if ((int)e.NewValue == 0)
            {
                _Thickness = 1;
            }
            else
            {
                _Thickness = (int)e.NewValue;
            }

            ChangeBrushScale();
        }

        private void LineBtn_Click(object sender, RoutedEventArgs e)
        {
            _CurDrawType = DrawType.Line;
            PART_Viewbox.Cursor = Cursors.Cross;
            _DrawingLabel = new DrawLine();
        }

        private void BrushBtn_Click(object sender, RoutedEventArgs e)
        {
            _CurDrawType = DrawType.Brush;
            _Curcolor = _DrawColor;
            PART_Viewbox.Cursor = CustomCursors.Brush(_Thickness * _CurCursorScale);
            _DrawingLabel = null;
        }

        private void EraserBtn_Click(object sender, RoutedEventArgs e)
        {
            _CurDrawType = DrawType.Eraser;
            _Curcolor = _EraserColor;
            PART_Viewbox.Cursor = CustomCursors.Eraser(_Thickness * _CurCursorScale);
            _DrawingLabel = null;
        }

        private void RectBtn_Click(object sender, RoutedEventArgs e)
        {
            _CurDrawType = DrawType.Rect;
            PART_Viewbox.Cursor = Cursors.Cross;
            _DrawingLabel = new DrawRectangle();
        }

        private void EllipseBtn_Click(object sender, RoutedEventArgs e)
        {
            _CurDrawType = DrawType.Ellipse;
            PART_Viewbox.Cursor = Cursors.Cross;
            _DrawingLabel = new DrawEllipse();
        }


        private void PolylineBtn_Click(object sender, RoutedEventArgs e)
        {
            _CurDrawType = DrawType.Polyline;
            PART_Viewbox.Cursor = Cursors.Cross;
            _DrawingLabel = new DrawPolyline();
        }

        private void PolygonBtn_Click(object sender, RoutedEventArgs e)
        {
            _CurDrawType = DrawType.Polygon;
            PART_Viewbox.Cursor = Cursors.Cross;
            _DrawingLabel = new DrawPolygon();
            _DrawingLabel.OnDrawPolygon += _DrawingLabel_OnDrawPolygon;
        }

        private void _DrawingLabel_OnDrawPolygon()
        {
            _DrawingLabel.OnMouseUp(_LabelImage, _WriteableBitmapSource, _DrawWriteableBitmapSource, _RoiRect, true);
            UpdateLabelLayer();
            PART_TemplabelImageLayer.Source = _DrawWriteableBitmapSource;
        }

        #endregion

        #region ScrollViewer Method

        private void PART_ScrollViewer_Drop(object sender, DragEventArgs e)
        {
            var dropItems = (string[])e.Data.GetData(DataFormats.FileDrop);
            _ImageInfo = new ImageInfo(dropItems[0]);
            PART_BackgroundImageLayer.Source = new BitmapImage(new Uri(_ImageInfo.FilePath));

            PART_Grid.Width = _ImageInfo.Width;
            PART_Grid.Height = _ImageInfo.Height;

            InitImage();
            SetCanvasScale();
            UpdateScale();

            PART_Viewbox.Cursor = CustomCursors.Brush(_Thickness * _CurCursorScale);
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
            _DrawingLastPos.Set(mousePos);
            if (_CurDrawType == DrawType.Brush || _CurDrawType == DrawType.Eraser)
            {
                Cv2.Circle(_LabelImage, _DrawingLastPos.X, _DrawingLastPos.Y, _Thickness / 2, _Curcolor, -1, LineTypes.Link8);
                UpdateWriteableBitmapRoi(_DrawingLastPos.X, _DrawingLastPos.Y);
                UpdateLabelLayer();
            }
            else
            {
                if (_CurDrawType == DrawType.Polyline || _CurDrawType == DrawType.Polygon)
                {
                    _DrawingLabel.OnMouseUp(_LabelImage, _WriteableBitmapSource, _DrawWriteableBitmapSource, _RoiRect);
                    UpdateLabelLayer();
                }
                _DrawingLabel.OnMouseDown(mousePos, _LabelImage.Width, _LabelImage.Height, _ImageSize, _ImageStride, _Thickness, _Curcolor);
            }
        }

        private void PART_Grid_MouseMove(object sender, MouseEventArgs e)
        {
            var mousePos = e.GetPosition(PART_Grid);
            if (_CurDrawType == DrawType.Polyline || _CurDrawType == DrawType.Polygon)
            {
                _DrawingLabel.OnMouseMove(mousePos, _DrawWriteableBitmapSource, ref _RoiRect);
                PART_TemplabelImageLayer.Source = _DrawWriteableBitmapSource;
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            if (_DrawingLabel == null)
            {
                DrawBrush(mousePos);
            }
            else
            {
                _DrawingLabel.OnMouseMove(mousePos, _DrawWriteableBitmapSource, ref _RoiRect);
                PART_TemplabelImageLayer.Source = _DrawWriteableBitmapSource;
            }
        }

        private void PART_Grid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_DrawingLabel == null || _CurDrawType == DrawType.Polyline)
                return;

            _DrawingLabel.OnMouseUp(_LabelImage, _WriteableBitmapSource, _DrawWriteableBitmapSource, _RoiRect);
            UpdateLabelLayer();
            PART_TemplabelImageLayer.Source = _DrawWriteableBitmapSource;
        }

        private void PART_Grid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_CurDrawType == DrawType.Polyline || _CurDrawType == DrawType.Polygon)
            {
                _DrawingLabel.OnMouseUp(_LabelImage, _WriteableBitmapSource, _DrawWriteableBitmapSource, _RoiRect, true);
                UpdateLabelLayer();
                PART_TemplabelImageLayer.Source = _DrawWriteableBitmapSource;
            }
        }
         
        #endregion

        #endregion
    }
}
