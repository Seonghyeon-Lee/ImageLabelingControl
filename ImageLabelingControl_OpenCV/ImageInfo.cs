using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLabelingControl_OpenCV
{
    public class ImageInfo
    {
        public int Index { get; set; }

        public string FilenameWithoutExtension { get; set; }

        public string Filename { get; set; }

        public string FilePath { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int Channel { get; set; }

        public ImageInfo(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            FilePath = path;
            Filename = Path.GetFileName(path);
            FilenameWithoutExtension = Path.GetFileNameWithoutExtension(path);

            SetImageInfo(path);
        }

        private void SetImageInfo(string path)
        {
            try
            {
                byte[] header = new byte[48];
                byte[] buffer = new byte[2];
                bool isETCFormat = false;
                int bitPerPixel = 0;

                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        br.Read(header, 0, header.Length);
                        StringBuilder sb = new StringBuilder();

                        foreach (byte b in header)
                        {
                            sb.Append(b.ToString("X2"));
                        }

                        // JPG (JFIF)
                        // FF D8 FF E0 Graphics - JPEG/JFIF Format
                        // FF D8 FF E1 Graphics - JPEG/Exif Format - Digital Camera (Exchangeable Image File Format (EXIF))
                        // FF D8 FF E8 Graphics - Still Picture Interchange File Format (SPIFF)
                        if (sb.ToString().StartsWith("FFD8FFE0"))
                        {
                            int readSize = 0;
                            while (true)
                            {
                                readSize = br.Read(buffer, 0, buffer.Length);
                                if (readSize == 0)
                                {
                                    isETCFormat = true;
                                    break;
                                }

                                if (buffer[0] == 255)
                                {
                                    if (buffer[1] == 192 || buffer[1] == 193 || buffer[1] == 194)
                                        break;
                                }
                            }

                            br.Read(header, 0, header.Length);
                            bitPerPixel = header[2];
                            Height = (header[3] << 8) + header[4];
                            Width = (header[5] << 8) + header[6];
                            Channel = header[7];
                            br.Close();
                        }
                        // BMP
                        else if (sb.ToString().StartsWith("424D"))
                        {
                            Width = header[18] + (header[19] << 8) + (header[20] << 16) + (header[21] << 32);
                            Height = header[22] + (header[23] << 8) + (header[24] << 16) + (header[25] << 32);
                            bitPerPixel = header[28];
                            Channel = header[28] / 8;
                            br.Read(header, 0, header.Length);
                            br.Close();
                        }
                        // PNG
                        else if (sb.ToString().StartsWith("89504E470D0A1A0A"))
                        {
                            Width = (header[18] << 8) + header[19];
                            Height = (header[22] << 8) + header[23];

                            bitPerPixel = header[24];
                            switch (header[25])
                            {
                                // Grayscale
                                case 0:
                                    Channel = 1;
                                    break;
                                // TrueColor
                                case 2:
                                    Channel = 3;
                                    break;
                                // Indexed-color
                                case 3:
                                    Channel = 3;
                                    break;
                                // Grayscale with alpha
                                case 4:
                                    Channel = 4;
                                    break;
                                // TrueColor with alpha
                                case 6:
                                    Channel = 4;
                                    break;
                            }
                            br.Close();
                        }
                        //기타 포멧일 경우 이미지 파일 읽어서 시도
                        else
                        {
                            isETCFormat = true;
                            br.Close();
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
