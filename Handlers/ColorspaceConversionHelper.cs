using System;
using Android.Graphics;
using Android.Media;
using Android.Util;
using Java.Nio;
using OpenCV.Core;
using OpenCV.ImgProc;

namespace CameraX.Handlers
{
    public static class ColorspaceConversionHelper
    {
        // public Mat YuvToGrayscale(Image oImage)
        // {
        //     if (oImage == null) return new Mat();
        //     try
        //     {
        //         var width = oImage.Width;
        //         var height = oImage.Height;
        //         ByteBuffer yBuffer = oImage.GetPlanes()[0].Buffer;
        //         ByteBuffer uBuffer = oImage.GetPlanes()[1].Buffer;
        //             
        //         var rowStride = oImage.GetPlanes()[0].RowStride;
        //         var uRowStride = oImage.GetPlanes()[1].RowStride;
        //
        //         int ySize = rowStride * height;
        //         int uvSize = oImage.Width * oImage.Height/4;
        //
        //         var nv21 = new byte[ySize * rowStride];
        //         
        //         int pos = 0;
        //
        //         // if (rowStride == oImage.Width) { // likely
        //         //     yBuffer.Get(nv21, 0, ySize);
        //         //     pos += ySize;
        //         // }
        //         // else 
        //         // {
        //         //     int limit = height/2 - 1;
        //         //
        //         //     // not an actual position
        //         //     for (int yBufferPos = 0; yBufferPos<ySize; pos =+ width) 
        //         //     {
        //         //         yBuffer.Position(yBufferPos);
        //         //         yBuffer.Get(nv21, pos, width);
        //         //         yBufferPos += rowStride;
        //         //     }
        //         // }
        //
        //         yBuffer.Get(nv21, 0, ySize);
        //
        //         Mat mGray = GetYUV2Mat(nv21, oImage, ySize);
        //
        //         return mGray;
        //     }
        //     catch (Exception e)
        //     {
        //         Log.Warn("Warning", e.Message);
        //         throw;
        //     }
        // }
        
        public static Mat ImageToMat(Image oImage)
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
        
        public static Mat Rgba8888ToMat(Image oImage)
        {
            var buffer = oImage.GetPlanes()[0].Buffer;
            int width = oImage.Width, height = oImage.Height;
            
            // Assuming you have a ByteBuffer named "buffer"
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
        
        public static Mat YuvToRgb(Image oImage)
        {
            try
            {
                var image = oImage;
                if (image != null)
                {
                    byte[] nv21;
                    ByteBuffer yBuffer = image.GetPlanes()[0].Buffer;
                    ByteBuffer uBuffer = image.GetPlanes()[1].Buffer;
                    ByteBuffer vBuffer = image.GetPlanes()[2].Buffer;
                    
                    var yRowStride = image.GetPlanes()[0].RowStride;
                    var uRowStride = image.GetPlanes()[1].RowStride;
                    var vRowStride = image.GetPlanes()[2].RowStride;

                    int ySize = yRowStride * image.Height;
                    var uSize = uRowStride * (image.Height / 2); // Height is halved for U and V
                    var vSize = vRowStride * (image.Height / 2);

                    nv21 = new byte[ySize + uSize + vSize];

                    // U and V are swapped
                    yBuffer.Get(nv21, 0, ySize);
                    vBuffer.Get(nv21, ySize, vSize);
                    uBuffer.Get(nv21, ySize + vSize, uSize);

                    Mat mBGRA = GetYUV2Mat(nv21, image, ySize, true);
                    return mBGRA;
                }
            }
            catch (Exception e)
            {
                Log.Warn("Warning", e.Message);
            }

            return new Mat();
        }

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