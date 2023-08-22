using System;
using System.Linq;
using System.IO;
using System.Reflection;
using Android.Media;
using System.Runtime.InteropServices;
using Android.Graphics;
using Java.Nio;
using Lennox.LibYuvSharp;
using OpenCV.Core;
using MAT = OpenCV.Core.Mat;
using Stream = System.IO.Stream;

public static unsafe class YUUVHelper
{
    public static MAT Mat { get; private set; }
    public static void ProcessImage(Image image)
    {
        byte[] bytes;
        
        using (var buffer = image.GetPlanes()[0].Buffer)
        {
            bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes);
        }
        
        int width = image.Width;
        int height = image.Height;
        
        int dstStrideARGB = width * 4; // Assuming 4 bytes per ARGB pixel
        
        var result = ConvertYUV2RGB(
            bytes,
            width,
            height);
        
        ByteBuffer argbByteBuffer = ByteBuffer.Wrap(result, dstStrideARGB, height);
        // Create a Mat from the ByteBuffer
        Mat = new Mat(height, width, 16, argbByteBuffer);
    }
    
    static byte[] ConvertYUV2RGB(byte[] YUVFrame, int width, int height)
    {
        int numOfPixel = width * height;
        int positionOfV = numOfPixel;
        int positionOfU = numOfPixel / 4 + numOfPixel;
        byte[] rgb = new byte[numOfPixel * 3];

        int R = 0;
        int G = 1;
        int B = 2;

        for (int i = 0; i < height; i++)
        {
            int startY = i * width;
            int step = (i / 2) * (width / 2);
            int startU = positionOfU + step - 1;
            int startV = positionOfV + step - 1;

            for (int j = 0; j < width; j++)
            {
                int Y = startY + j;
                int U = startU + j / 2;
                int V = startV + j / 2;
                int index = Y * 3;

                double r = ((YUVFrame[Y] & 0xff) + 1.4075 * ((YUVFrame[V] & 0xff) - 128));
                double g = ((YUVFrame[Y] & 0xff) - 0.3455 * ((YUVFrame[U] & 0xff) - 128) - 0.7169 * ((YUVFrame[V] & 0xff) - 128));
                double b = ((YUVFrame[Y] & 0xff) + 1.779 * ((YUVFrame[U] & 0xff) - 128));

                r = (r < 0 ? 0 : r > 255 ? 255 : r);
                g = (g < 0 ? 0 : g > 255 ? 255 : g);
                b = (b < 0 ? 0 : b > 255 ? 255 : b);

                rgb[index + R] = (byte)r;
                rgb[index + G] = (byte)g;
                rgb[index + B] = (byte)b;
            }
        }

        return rgb;
    }
    
    public static void CleanUp()
    {
        // Release the Mat to avoid memory leaks
        Mat.Release();
    }
}