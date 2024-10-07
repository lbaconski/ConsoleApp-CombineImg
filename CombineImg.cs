using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace CombineImages
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 5) 
            {
                Console.WriteLine("CombinarImagenes.exe <path1> <path2> <outputfile.jpg> <rotation1> <rotation2>");
                return;
            }

            string imagePath1 = args[0];
            string imagePath2 = args[1];
            string outputImagePath = args[2];
            int rotation1 = int.Parse(args[3]); // Rotacion de la imagen 1
            int rotation2 = int.Parse(args[4]); // Rotacion de la imagen 2
            int pixelesSeparacion = 15;
            long maxFileSizeBytes = 499 * 1024; // 499KB

            try
            {
   
                using (Bitmap image1 = new Bitmap(imagePath1))
                using (Bitmap image2 = new Bitmap(imagePath2))
                {

                    RotateImage(image1, rotation1);
                    RotateImage(image2, rotation2);

                    Bitmap largerImage, smallerImage;

        
                    if (image1.Width * image1.Height >= image2.Width * image2.Height)
                    {
                        largerImage = image1;
                        smallerImage = image2;
                    }
                    else
                    {
                        largerImage = image2;
                        smallerImage = image1;
                    }

                    // Reescalo la imagen mas chica al tamaño de la mas grande sin alterar el ratio
                    Size newSize = GetScaledSize(smallerImage.Width, smallerImage.Height, largerImage.Width, largerImage.Height);
                    using (Bitmap resizedSmallerImage = ResizeImage(smallerImage, newSize.Width, newSize.Height))
                    {
                        using (Bitmap outputImage = new Bitmap(largerImage.Width, largerImage.Height + resizedSmallerImage.Height + pixelesSeparacion))
                        {
                            using (Graphics g = Graphics.FromImage(outputImage))
                            {
                                g.Clear(Color.White);
                                g.DrawImage(largerImage, 0, 0);
                                g.DrawImage(resizedSmallerImage, (largerImage.Width - resizedSmallerImage.Width) / 2, largerImage.Height + pixelesSeparacion);
                            }

                            //tope 500kb
                            SaveJpegWithMaxSize(outputImagePath, outputImage, maxFileSizeBytes);
                        }
                    }
                }

                Console.WriteLine("OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
        static void RotateImage(Bitmap image, int degrees)
        {
            if (degrees != 0)
            {
                switch (degrees)
                {
                    case 90:
                        image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        break;
                    case 180:
                        image.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        break;
                    case 270:
                        image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        break;
                    default:
                        Console.WriteLine("Angulo de rotacion valido: 90, 180, 270");
                        break;
                }
            }
        }
        static Size GetScaledSize(int originalWidth, int originalHeight, int targetWidth, int targetHeight)
        {
            float ratio = Math.Min((float)targetWidth / originalWidth, (float)targetHeight / originalHeight);

            int newWidth = (int)(originalWidth * ratio);
            int newHeight = (int)(originalHeight * ratio);

            return new Size(newWidth, newHeight);
        }

        static Bitmap ResizeImage(Bitmap image, int targetWidth, int targetHeight)
        {
            Bitmap resizedImage = new Bitmap(targetWidth, targetHeight);
            using (Graphics g = Graphics.FromImage(resizedImage))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, 0, 0, targetWidth, targetHeight);
            }
            return resizedImage;
        }

        /*Guarda un archivo temp y chequea tamaño.
         * Si es muy grande va bajando la calidad del encoding de a 5 
         * y chequea tamaño nuevamente hasta que este dentro del limite   */
        static void SaveJpegWithMaxSize(string path, Bitmap image, long maxSizeBytes)
        {
            long quality = 100;
            using (MemoryStream ms = new MemoryStream())
            {
                ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                EncoderParameters encoderParams = new EncoderParameters(1);

                do
                {
                    ms.SetLength(0);
                    encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                    image.Save(ms, jpgEncoder, encoderParams);

                    if (ms.Length > maxSizeBytes && quality > 5)
                    {
                        quality -= 5; 
                    }
                    else
                    {
                        break; 
                }
                while (ms.Length > maxSizeBytes);

                File.WriteAllBytes(path, ms.ToArray());
            }
        }

        static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}

