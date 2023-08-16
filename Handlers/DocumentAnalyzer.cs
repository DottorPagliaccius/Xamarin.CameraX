using System;
using System.Linq;
using AndroidX.Camera.Core;
using Java.Nio;
using OpenCvSharp;
using OpenCvSharp.Internal.Vectors;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace CameraX
{
    public class DocumentAnalyzer: Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        private const string TAG = "CameraXBasic";
        private readonly Action<VectorOfPoint> docListener;

        public DocumentAnalyzer(Action<VectorOfPoint> callback) //LumaListener listener)
        {
            this.docListener = callback;
        }

        public void Analyze(IImageProxy image)
        {
            ByteBuffer buffer = image.GetPlanes()[0].Buffer;
            var byteData = FromByteBuffer(buffer);
                    
            Mat bitmapImage = new Mat(image.Height, image.Width, MatType.CV_8UC4, byteData);

            Mat grayScaleImage = new Mat();
            Cv2.CvtColor(bitmapImage, grayScaleImage, ColorConversionCodes.BGR2GRAY);
            Mat blurredImage = new Mat();
            Cv2.GaussianBlur(grayScaleImage, blurredImage, new Size(5, 5), 0);
            Mat cannyImage = new Mat();
            Cv2.Canny(blurredImage, cannyImage, 50, 150);

            Point[][] contours = Cv2.FindContoursAsArray(cannyImage, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);
            if (contours.Length > 0)
            {
                Point[][] sortedContours = contours.OrderByDescending(contour => Cv2.ContourArea(contour)).ToArray();
                Point2f[] mainContour = Array.ConvertAll(sortedContours[0], p => new Point2f(p.X, p.Y));

                if (mainContour.Length == 4 && Cv2.IsContourConvex(mainContour))
                {
                    var contourList = new System.Collections.Generic.List<Point>();
                    foreach (Point2f point in mainContour)
                    {
                        contourList.Add(new Point((int)point.X, (int)point.Y));
                    }
                    ContourCoordinates = new VectorOfPoint(contourList.ToArray());
                }
            }
            
            image.Close();
            docListener.Invoke(ContourCoordinates);
        }
        
        private byte[] FromByteBuffer(ByteBuffer buffer)
        {
            byte[] bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes);
            return bytes;
        }

        // private VectorOfPoint[] SelectContours(VectorOfVectorOfPoint contours)
        // {
        //     VectorOfPoint[] top5Contours = new VectorOfPoint[5];
        //     for (int i = 0; i < 5; i++)
        //     {
        //         top5Contours[i] = contours[i];
        //     }
        //     return top5Contours;
        // }
        //
        // private VectorOfPoint SelectTopContour(VectorOfPoint[] contours)
        // {
        //     foreach (var contourVector in contours)
        //     {
        //         using (var contour = new VectorOfPoint())
        //         {
        //             var peri = CvInvoke.ArcLength(contourVector, true);
        //             CvInvoke.ApproxPolyDP(contourVector, contour, 0.1 * peri, true);
        //             if (contour.ToArray().Length == 4 && CvInvoke.IsContourConvex(contour))
        //                 ContourCoordinates = contour;
        //         }
        //     }
        //     return null;
        // }

        public VectorOfPoint ContourCoordinates { get; set; }
    }
}