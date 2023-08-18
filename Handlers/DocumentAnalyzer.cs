using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Android.Graphics;
using Android.Util;
using AndroidX.Camera.Core;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Java.Nio;
using Java.Util.Logging;

namespace CameraX.Handlers
{
    public class DocumentAnalyzer: Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        private const string TAG = "CameraXBasic";
        private readonly Action<IImageProxy> docListener;
        private byte[] depthPixelData;

        public DocumentAnalyzer(Action<IImageProxy> callback) //LumaListener listener)
        {
            docListener = callback;
        }

        public void Analyze(IImageProxy imageProxy)
        {
            // try
            // {
            //     var image = imageProxy.Image;
            //     
            //     ByteBuffer buffer = image.GetPlanes()[0].Buffer;
            //     //var bitmap = FromByteBuffer(buffer);
            //     FromByteBuffer(buffer);
            //     imageProxy.Close();
            //     //byte[] depthPixelData = new byte[bitmap.Height * bitmap.Width]; // your data
            //     Image<Gray, byte> bitmapImage = new Image<Gray, byte>(image.Width, image.Height);
            //     bitmapImage.Bytes = depthPixelData;
            //     //var bitmapImage = new Image<Bgr, byte>(bitmap);
            //     //using var grayScaleImage = bitmapImage.Convert<Gray, byte>();
            //     
            //     using var blurredImage = bitmapImage.SmoothGaussian(5, 5, 0, 0);
            //     using var cannyImage = new UMat();
            //     CvInvoke.Canny(blurredImage, cannyImage, 50, 150);
            //
            //     using var contours = new VectorOfVectorOfPoint();
            //     CvInvoke.FindContours(cannyImage, contours, null, RetrType.Tree, ChainApproxMethod.ChainApproxSimple);
            //     if (contours.Length > 0)
            //     {
            //         var top5Contours = SelectContours(contours);
            //         SelectTopContour(top5Contours);
            //         //CvInvoke.DrawContours(bitmapImage, mainContour, 1, new           MCvScalar(0, 0, 255));
            //     }
            // }
            // catch (Exception exception)
            // {
            //     Log.Error(exception.Source, exception.Message);
            // }
            //
            // //image.Close();
            docListener.Invoke(imageProxy);
        }
        
        private void FromByteBuffer(ByteBuffer buffer)
        {
            byte[] bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes);
            depthPixelData = bytes;
            //return BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length);
            //return Base64.EncodeToString(bytes, Base64Flags.Default);
        }

        private VectorOfPoint[] SelectContours(VectorOfVectorOfPoint contours)
        {
            VectorOfPoint[] top5Contours = new VectorOfPoint[5];
            for (int i = 0; i < 5; i++)
            {
                top5Contours[i] = contours[i];
            }
            return top5Contours;
        }

        private void SelectTopContour(VectorOfPoint[] contours)
        {
            foreach (var contourVector in contours)
            {
                using (var contour = new VectorOfPoint())
                {
                    var peri = CvInvoke.ArcLength(contourVector, true);
                    CvInvoke.ApproxPolyDP(contourVector, contour, 0.1 * peri, true);
                    if (contour.ToArray().Length == 4 && CvInvoke.IsContourConvex(contour))
                        ContourCoordinates = contour;
                }
            }
        }
        private VectorOfPoint ContourCoordinates { get; set; } = new VectorOfPoint();
    }
}