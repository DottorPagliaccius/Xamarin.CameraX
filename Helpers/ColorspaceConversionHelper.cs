using System;
using Android.Graphics;
using Android.Media;
using Java.Nio;
using OpenCV.Core;
using OpenCV.ImgProc;

namespace CameraX.Helpers
{
    public static class ColorspaceConversionHelper
    {
        public static Mat Rgba8888ToMat(Image oImage)
        {
            var buffer = oImage.GetPlanes()[0].Buffer;
            int width = oImage.Width, height = oImage.Height;
            
            // Create a byte buffer with the same size as the original buffer
            byte[] rgba8888Data = new byte[buffer.Remaining()]; // Create a byte array to hold the data
            
            // Get the byte data from the ByteBuffer
            buffer.Get(rgba8888Data);
            Mat mat = new Mat(height, width, CvType.Cv8uc4);

            int offset = 0;
            byte[] matData = new byte[width * height * 4]; // 4 channels (RGBA)

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int rgbaOffset = y * width * 4 + x * 4;
            
                    // Copy RGBA values from rgba8888Data to Mat
                    matData[offset++] = rgba8888Data[rgbaOffset + 2]; // Red channel
                    matData[offset++] = rgba8888Data[rgbaOffset + 1]; // Green channel
                    matData[offset++] = rgba8888Data[rgbaOffset];     // Blue channel
                    matData[offset++] = rgba8888Data[rgbaOffset + 3]; // Alpha channel
                }
            }

            mat.Put(0, 0, matData);
            return mat;
        }
        
        [Obsolete]
        //This method was used to convert YUV to BGRA MAT
        //And suffered from a huge performance hit if the camera image had padding.
        public static Mat YuvImageToMat(Image oImage)
        {
            ByteBuffer buffer;
            int rowStride, pixelStride, width = oImage.Width, height = oImage.Height, offset = 0;
            Image.Plane[] planes = oImage.GetPlanes();
            
            var data = new byte[width * height * ImageFormat.GetBitsPerPixel(ImageFormatType.Yuv420888) / 8];
            var rowData = new byte[planes[0].RowStride];
            
            buffer = planes[0].Buffer;
            rowStride = planes[0].RowStride;
            pixelStride = planes[0].PixelStride;
            
            int bytesPerPixel = ImageFormat.GetBitsPerPixel(ImageFormatType.Yuv420888) / 8;
            int pixelStrideBytes = pixelStride * bytesPerPixel;
            int lastRow = height - 1;
            int lastCol = width - 1;
            
            if (rowStride == width)
            {
                buffer.Get(data, 0, rowStride * height);
            }
            else
            {
                for (int row = 0; row < height; row++)
                {
                    if (pixelStrideBytes == bytesPerPixel)
                    {
                        int length = width * bytesPerPixel;
                        buffer.Get(data, offset, length);
        
                        // Advance buffer the remainder of the row stride, unless on the last row.
                        if (row != lastRow)
                            buffer.Position(buffer.Position() + rowStride - length);
        
                        offset += length;
                    }
                    else
                    {
                        // On the last row, only read the width of the image minus the pixel stride plus one.
                        if (row == lastRow)
                        {
                            buffer.Get(rowData, 0, width - pixelStride + 1);
                            for (int col = 0; col < width; col++)
                                data[offset++] = rowData[col * pixelStrideBytes];
                        }
                        else
                        {
                            buffer.Get(rowData, 0, rowStride);
                            for (int col = 0; col < lastCol; col++)
                                data[offset++] = rowData[col * pixelStrideBytes];
                            // Handle the last pixel in the row separately (without pixelStride)
                            data[offset++] = rowData[lastCol * pixelStrideBytes];
                        }
                    }
                }
            }
        
            return GetYUV2Mat(data, oImage, planes[0].RowStride * height);
        }
        
        [Obsolete]
        private static Mat GetYUV2Mat(byte[] data, Image image, int area, bool isColorSpaceRequired = false)
        {
            int width = image.Width;
            int height = image.Height;
            
            if (isColorSpaceRequired)
            {
                Mat mYuv = new Mat(image.Height , image.Width, CvType.Cv8uc1);
                mYuv.Put(0, 0, data);
                Mat mRGB = new Mat();
                Imgproc.CvtColor(mYuv, mRGB, Imgproc.ColorRgba2gray, 3);
                return mRGB;
            }
            
            // Extract the Y channel (luminance) as grayscale
            Mat mGray = new Mat(height, width, CvType.Cv8uc1);
            mGray.Put(0, 0, data, 0, area);

            return mGray;
        }
    }

}