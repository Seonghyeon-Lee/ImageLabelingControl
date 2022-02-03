using MahApps.Metro.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace ImageLabelingControl
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
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
            PART_ScrollViewer.Loaded += PART_ScrollViewer_Loaded;

            this.SizeChanged += MainWindow_SizeChanged;
            PART_AutoFitBtn.Click += PART_AutoFitBtn_Click;
            Eraser.Click += Eraser_Click;

            //PART_InkCanvas.EditingMode = InkCanvasEditingMode.None;
            //PART_InkCanvas.Cursor = Cursors.Arrow;
            PART_InkCanvas.DefaultDrawingAttributes.Height = PART_InkCanvas.DefaultDrawingAttributes.Width = 1;
        }

        private void PART_ScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            PART_ImageWidthTextBlock = (TextBlock)PART_ScrollViewer.Template.FindName("PART_ImageWidthTextBlock", PART_ScrollViewer);
            PART_ImageHeightTextBlock = (TextBlock)PART_ScrollViewer.Template.FindName("PART_ImageHeightTextBlock", PART_ScrollViewer);
            PART_ScaleComboBox = (ComboBox)PART_ScrollViewer.Template.FindName("PART_ScaleComboBox", PART_ScrollViewer);
            PART_ScaleComboBox.SelectionChanged += PART_SacleComboBox_SelectionChanged;
            PART_CursorCoordinateTextBox = (TextBlock)PART_ScrollViewer.Template.FindName("PART_CursorCoordinateTextBox", PART_ScrollViewer);
            PART_ColorSpaceTextBox = (TextBlock)PART_ScrollViewer.Template.FindName("PART_ColorSpaceTextBox", PART_ScrollViewer);
            PART_ImageFormatTextBox = (TextBlock)PART_ScrollViewer.Template.FindName("PART_ImageFormatTextBox", PART_ScrollViewer);
        }

        const double _INIT_SCALE = 0.96;
        const double _INCREASE_SCALE = 1.1;
        const double _DECREASE_SCALE = 0.9;
        double _CanvasScale;

        ImageInfo _Image;
        Point? lastCenterPositionOnTarget;
        Point? lastMousePositionOnTarget;
        Point? lastDragPoint;

        #region StatusBar Elements
        TextBlock PART_ImageWidthTextBlock;
        TextBlock PART_ImageHeightTextBlock;
        ComboBox PART_ScaleComboBox;
        TextBlock PART_CursorCoordinateTextBox;
        TextBlock PART_ColorSpaceTextBox;
        TextBlock PART_ImageFormatTextBox;
        #endregion

        private void PART_SacleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var typeItem = ((ComboBoxItem)PART_ScaleComboBox.SelectedItem).Content.ToString();
            UpdateScale(double.Parse(typeItem) / 100);

            var centerOfViewport = new Point(PART_ScrollViewer.ViewportWidth / 2, PART_ScrollViewer.ViewportHeight / 2);
            lastCenterPositionOnTarget = PART_ScrollViewer.TranslatePoint(centerOfViewport, PART_Viewbox);
        }

        private void UpdateScale(double scale = 1.0)
        {
            scaleTransform.ScaleX = scale * _INIT_SCALE;
            scaleTransform.ScaleY = scale * _INIT_SCALE;

            scaleTransform2.ScaleX = _CanvasScale * scale * _INIT_SCALE;
            scaleTransform2.ScaleY = _CanvasScale * scale * _INIT_SCALE;

            //scaleTransform3.ScaleY = _CanvasScale * scale * _INIT_SCALE;
            //scaleTransform3.ScaleY = _CanvasScale * scale * _INIT_SCALE;
        }

        private void ChangeScale(double scale)
        {
            scaleTransform.ScaleX *= scale;
            scaleTransform.ScaleY *= scale;

            scaleTransform2.ScaleX *= scale;
            scaleTransform2.ScaleY *= scale;

            //scaleTransform3.ScaleX *= scale;
            //scaleTransform3.ScaleY *= scale;
        }

        private void PART_AutoFitBtn_Click(object sender, RoutedEventArgs e)
        {
            UpdateScale();
        }

        private void Eraser_Click(object sender, RoutedEventArgs e)
        {
            if (PART_InkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint)
                PART_InkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            else
                PART_InkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
        }


        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_Image == null)
                return;

            SetCanvasScale();
            UpdateScale();
        }

        private void PART_ScrollViewer_Drop(object sender, DragEventArgs e)
        {
            var dropItems = (string[])e.Data.GetData(DataFormats.FileDrop);
            _Image = new ImageInfo(dropItems[0]);
            PART_Image.Source = new BitmapImage(new Uri(_Image.FilePath));
            //PART_Image.Height = _Image.Width;
            //PART_Image.Height = _Image.Height;
            PART_InkCanvas.Width = _Image.Width;
            PART_InkCanvas.Height = _Image.Height;

            //PART_CanvasGrid.Width = _Image.Width;
            //PART_CanvasGrid.Height = _Image.Height;


            SetCanvasScale();
            UpdateScale();

            PART_ImageWidthTextBlock.Text = _Image.Width.ToString();
            PART_ImageHeightTextBlock.Text = _Image.Height.ToString();

            PART_ScaleComboBox.SelectedItem = 50;
        }

        private void SetCanvasScale()
        {
            if (_Image.Width > _Image.Height)
            {
                _CanvasScale = PART_Viewbox.Width / _Image.Width;
            }
            else if (_Image.Width == _Image.Height)
            {
                if (PART_Viewbox.Width > PART_Viewbox.Height)
                    _CanvasScale = PART_Viewbox.Height / _Image.Width;
                else
                    _CanvasScale = PART_Viewbox.Width / _Image.Height;
            }
            else
            {
                _CanvasScale = PART_Viewbox.Height / _Image.Height;
            }
        }

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(PART_InkCanvas);
            UpdateCoordinate(point);

            if (lastDragPoint.HasValue)
            {
                Point posNow = e.GetPosition(PART_ScrollViewer);

                double dX = posNow.X - lastDragPoint.Value.X;
                double dY = posNow.Y - lastDragPoint.Value.Y;

                lastDragPoint = posNow;

                PART_ScrollViewer.ScrollToHorizontalOffset(PART_ScrollViewer.HorizontalOffset - dX);
                PART_ScrollViewer.ScrollToVerticalOffset(PART_ScrollViewer.VerticalOffset - dY);
            }
        }

        private void UpdateCoordinate(Point point)
        {
            if (point.X < 0 | PART_InkCanvas.Width < point.X)
            {
                PART_CursorCoordinateTextBox.Text = "X : 0, Y : 0";
                return;
            }

            if (point.Y < 0 | PART_InkCanvas.Height < point.Y)
            {
                PART_CursorCoordinateTextBox.Text = "X : 0, Y : 0";
                return;
            }
            PART_CursorCoordinateTextBox.Text = "X : " + (int)point.X + ", Y : " + (int)point.Y;
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

            var centerOfViewport = new Point(PART_ScrollViewer.ViewportWidth / 2, PART_ScrollViewer.ViewportHeight / 2);
            lastCenterPositionOnTarget = PART_ScrollViewer.TranslatePoint(centerOfViewport, PART_Viewbox);

            e.Handled = true;
        }

        void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            PART_ScrollViewer.Cursor = Cursors.Arrow;
            PART_ScrollViewer.ReleaseMouseCapture();
            lastDragPoint = null;
        }

        void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0)
            {
                Point? targetBefore = null;
                Point? targetNow = null;

                if (!lastMousePositionOnTarget.HasValue)
                {
                    if (lastCenterPositionOnTarget.HasValue)
                    {
                        var centerOfViewport = new Point(PART_ScrollViewer.ViewportWidth / 2,
                                                         PART_ScrollViewer.ViewportHeight / 2);
                        Point centerOfTargetNow =
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
    }
}
