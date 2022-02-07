using System;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ControlCore.Model
{
    public static class CustomCursors
    {
        public static Cursor Brush(double cursorWidth)
        {
            return GetEllipseShapeCursor(cursorWidth, Brushes.Transparent);
        }

        public static Cursor Eraser(double cursorWidth)
        {
            return GetEllipseShapeCursor(cursorWidth, Brushes.White);
        }

        private static Cursor GetEllipseShapeCursor(double cursorWidth, Brush brush)
        {
            if (cursorWidth == 0)
                cursorWidth = 2;

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                System.Windows.Rect r = new System.Windows.Rect(0, 0, cursorWidth, cursorWidth);
                System.Windows.Point center = new System.Windows.Point(
                     (r.Left + r.Right) / 2.0,
                     (r.Top + r.Bottom) / 2.0);

                double radiusX = (r.Right - r.Left) / 2.0;
                double radiusY = (r.Bottom - r.Top) / 2.0;

                dc.DrawEllipse(brush, new Pen(Brushes.Black, 0.5), center, radiusX, radiusY);
                dc.Close();
            }
            var rtb = new RenderTargetBitmap((int)Math.Ceiling(cursorWidth), (int)Math.Ceiling(cursorWidth), 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            return GetCursor(rtb, cursorWidth);
        }

        private static Cursor GetCursor(RenderTargetBitmap rtb, double cursorWidth)
        {
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
                    WritewMemoryStream(ms, size, cursorWidth);
                    ms.Write(pngBytes, 0, size);//write the png data.
                    ms.Seek(0, SeekOrigin.Begin);
                    return new Cursor(ms);
                }
            }
        }

        private static void WritewMemoryStream(MemoryStream ms, Int32 size, double cursorWidth)
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

                ms.Write(BitConverter.GetBytes((Int16)(cursorWidth / 2.0)), 0, 2);//2 bytes. In CUR format: Specifies the horizontal coordinates of the hotspot in number of pixels from the left.
                ms.Write(BitConverter.GetBytes((Int16)(cursorWidth / 2.0)), 0, 2);//2 bytes. In CUR format: Specifies the vertical coordinates of the hotspot in number of pixels from the top.

                ms.Write(BitConverter.GetBytes(size), 0, 4);//Specifies the size of the image's data in bytes
                ms.Write(BitConverter.GetBytes((Int32)22), 0, 4);//Specifies the offset of BMP or PNG data from the beginning of the ICO/CUR file
            }
        }
    }
}
