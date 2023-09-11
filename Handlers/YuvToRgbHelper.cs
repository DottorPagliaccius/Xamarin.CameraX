using System;
using Android.Graphics;
using Android.Media;
using Java.Nio;

namespace CameraX.Handlers
{
    public class YuvToRgbHelper
    {
        private int pixelCount = -1;

        public void ImageToByteArray(Image image, byte[] outputBuffer)
        {
            if (image.Format != ImageFormatType.Yuv420888)
            {
                throw new ArgumentException("Image format must be YUV_420_888");
            }

            Rect imageCrop = new Rect(0, 0, image.Width, image.Height);
            Image.Plane[] imagePlanes = image.GetPlanes();

            foreach (var plane in imagePlanes)
            {
                int outputStride;
                int outputOffset;

                int planeIndex = Array.IndexOf(imagePlanes, plane);

                switch (planeIndex)
                {
                    case 0:
                        outputStride = 1;
                        outputOffset = 0;
                        break;
                    case 1:
                        outputStride = 2;
                        outputOffset = pixelCount + 1;
                        break;
                    case 2:
                        outputStride = 2;
                        outputOffset = pixelCount;
                        break;
                    default:
                        // Image contains more than 3 planes, something strange is going on
                        return;
                }

                ByteBuffer planeBuffer = plane.Buffer;
                int rowStride = plane.RowStride;
                int pixelStride = plane.PixelStride;

                Rect planeCrop = (planeIndex == 0) ? imageCrop : new Rect(
                    imageCrop.Left / 2,
                    imageCrop.Top / 2,
                    imageCrop.Right / 2,
                    imageCrop.Bottom / 2
                );

                int planeWidth = planeCrop.Width();
                int planeHeight = planeCrop.Height();

                byte[] rowBuffer = new byte[plane.RowStride];
                int rowLength = (pixelStride == 1 && outputStride == 1)
                    ? planeWidth
                    : (planeWidth - 1) * pixelStride + 1;

                for (int row = 0; row < planeHeight; row++)
                {
                    planeBuffer.Position((row + planeCrop.Top) * rowStride + planeCrop.Left * pixelStride);

                    if (pixelStride == 1 && outputStride == 1)
                    {
                        planeBuffer.Get(outputBuffer, outputOffset, rowLength);
                        outputOffset += rowLength;
                    }
                    else
                    {
                        planeBuffer.Get(rowBuffer, 0, rowLength);
                        for (int col = 0; col < planeWidth; col++)
                        {
                            outputBuffer[outputOffset] = rowBuffer[col * pixelStride];
                            outputOffset += outputStride;
                        }
                    }
                }
            }
        }
    }

    public class YuvToArgbConverter
    {
        public static Bitmap ConvertNv21ToArgb(byte[] nv21Data, int width, int height, Image image)
        {
            // Convert NV21 data to YUV_420_888 format
            ByteBuffer[] yuvPlanes = new ByteBuffer[3];
            yuvPlanes[0] = ByteBuffer.Wrap(nv21Data, 0, width * height);
            yuvPlanes[1] = ByteBuffer.Wrap(nv21Data, width * height, width * height / 4);
            yuvPlanes[2] = ByteBuffer.Wrap(nv21Data, width * height * 5 / 4, width * height / 4);
            

            // Create a buffer for the ARGB data
            byte[] argbData = new byte[width * height * 4]; // Assuming 4 bytes per pixel (ARGB)

            // Convert YUV to ARGB
            YuvToRgbHelper yuvConverter = new YuvToRgbHelper();
            yuvConverter.ImageToByteArray(image, argbData);

            // Create a Bitmap from the ARGB data
            Bitmap argbBitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);
            ByteBuffer buffer = ByteBuffer.Wrap(argbData);
            argbBitmap.CopyPixelsFromBuffer(buffer);

            return argbBitmap;
        }
    }
}