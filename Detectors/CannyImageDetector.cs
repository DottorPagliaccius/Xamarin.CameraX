using System;
using System.Linq;
using Android.Graphics;
using Android.Runtime;
using OpenCV.Android;
using OpenCV.Core;
using OpenCV.ImgProc;
using OpenCV.Utils;
using Point = OpenCV.Core.Point;
using Rect = OpenCV.Core.Rect;

namespace CameraX.Handlers
{
    public static class CannyImageDetector
    {
        private static Point[] _cornersArray;
        private static MatOfPoint2f _corners;
        private static MatOfPoint _contour;
        private static Mat _mat;
        private static Mat _colorMat;

        public static Mat Update(Mat oMat, bool colorSpaceRequired = false)
        {
            var width = oMat.Width();
            var height = oMat.Height();
            _mat = oMat;
            if (colorSpaceRequired)
            {
                try
                {
                    _mat = new Mat(oMat.Rows(), oMat.Cols(), 0);
                    _colorMat = oMat.Clone();
                    Imgproc.CvtColor(oMat, _mat, Imgproc.ColorBgra2gray);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            var largestContour = PrepareImage(width, height, out var yellowColor, out var canvas);

            if (largestContour != null)
            {
                ContourHelper(largestContour, canvas, yellowColor);
            }
            return canvas; // Return the canvas with the bounding boxes overlaid
        }

        private static void ContourHelper(MatOfPoint largestContour, Mat canvas, Scalar yellowColor)
        {
            // Convert MatOfPoint to MatOfPoint2f for ApproxPolyDP
            var corners2f = new MatOfPoint2f(largestContour.ToArray());

            // Approximate the contour
            var epsilon = 0.1 * Imgproc.ArcLength(corners2f, true);
            _corners = new MatOfPoint2f();
            Imgproc.ApproxPolyDP(corners2f, _corners, epsilon, true);

            // If our approximated contour has four points
            if (_corners.Rows() == 4)
            {
                var approx = new MatOfPoint();
                _corners.ConvertTo(approx, 4);

                Imgproc.DrawContours(canvas, new JavaList<MatOfPoint> { approx }, -1, yellowColor, 1);

                _contour = approx;
                // Sorting the corners and converting them to desired shape
                _cornersArray = approx.ToArray();
                Array.Sort(_cornersArray, (p1, p2) => p1.X.CompareTo(p2.X));

                // Displaying the corners - Enable for development/perspective transform
                
                // Draw the contour and corners on the canvas
                //Imgproc.DrawContours(canvas, new JavaList<MatOfPoint> { largestContour }, -1, new Scalar(255, 0, 0), 1);
                // for (var index = 0; index < _cornersArray.Length; index++)
                // {
                //     var character = ((char)(65 + index)).ToString();
                //     Imgproc.PutText(canvas, character, _cornersArray[index], Imgproc.FontHersheySimplex, 0.5,
                //         new Scalar(0, 255, 0), 1, Imgproc.LineAa);
                // }
            }
        }

        private static MatOfPoint PrepareImage(int width, int height, out Scalar yellowColor, out Mat canvas)
        {
            var blurredMat = new Mat();
            Imgproc.GaussianBlur(_mat, blurredMat, new OpenCV.Core.Size(11, 11), 0, 0);

            var edges = new Mat();
            Imgproc.Canny(blurredMat, edges, 30, 90);
            Imgproc.Dilate(edges, edges,
                Imgproc.GetStructuringElement(Imgproc.MorphEllipse, new OpenCV.Core.Size(5, 5)));

            var contours = new JavaList<MatOfPoint>();
            var hierarchy = new Mat();
            Imgproc.FindContours(edges, contours, hierarchy, Imgproc.RetrList, Imgproc.ChainApproxNone);

            // Now the 'contours' list should contain the detected contours
            var largestContour = contours?.OrderByDescending(Imgproc.ContourArea).FirstOrDefault();

            // Create a black canvas to draw the contour
            var blackColor = new Scalar(0, 0, 0);
            yellowColor = new Scalar(255, 255, 0);

            canvas = new Mat(new OpenCV.Core.Size(width, height), 24, blackColor);
            return largestContour;
        }

        public static Rect GetCroppingBoundingBox()
        {
            // If our approximated contour has four points
            if (_corners.Rows() == 4)
            {
                // Calculate the bounding rectangle of the largest contour
                return Imgproc.BoundingRect(_contour);
            }
            
            return null;
        }
        
                
        [Obsolete]
        public static Mat CropImage()
        {
            // If our approximated contour has four points
            if (_corners.Rows() == 4)
            {
                // Calculate the bounding rectangle of the largest contour
                Rect boundingRect = Imgproc.BoundingRect(_contour);

                // Create an output Mat without an Alpha channel - identical type to _colorMat (BGR)
                var result = new Mat(_colorMat.Size(), CvType.Cv8uc3);

                // Fill the entire result Mat with black color (0, 0, 0, 255)
                result.SetTo(new Scalar(0, 0, 0, 255));

                // Copy the region inside the bounding rectangle from the original image to the result
                _colorMat.Submat(boundingRect).CopyTo(result.Submat(boundingRect));

                // Return the cropped region with the area outside the bounding rectangle painted black
                return result;
            }

            return null;
        }

        //Tabling this for now
        //Need to find a way to get the coordinates to be ordered correctly
        //I think the issue might be due to the 90 degree rotation mismatch
        //Trying different combinations of dstPoints helps but the best result i've gotten so far
        //is a cropped in image of the rectangle contour data.
        //May need to try rearranging with the Y and X flipped to account for the 90 degree rotation
        [Obsolete]
        public static Bitmap PerformPerspectiveTransform(Bitmap inputImage)
        {
            Point[] srcPoints = _cornersArray;
            
            //rotate -90 degrees
            foreach (var t in srcPoints)
            {
                (t.X, t.Y) = (t.Y, -t.X);
            }
            
            Array.Sort(srcPoints, Vector2Compare);

            double dstHeight = Math.Max(
                srcPoints[0].DistanceFrom(srcPoints[1]),
                srcPoints[2].DistanceFrom(srcPoints[3])
            );

            double dstWidth = Math.Max(
                srcPoints[0].DistanceFrom(srcPoints[2]),
                srcPoints[1].DistanceFrom(srcPoints[3])
            );

            Point[] dstPoints =
            {
                new Point(0.0, 0.0),
                new Point(dstWidth-1.00, 0.0),
                new Point(dstWidth-1.00, dstHeight-1.00),
                new Point(0.0, dstHeight),
            };

            try
            {
                Mat srcMat = Converters.Vector_Point2d_to_Mat(srcPoints);
                srcMat.ConvertTo(srcMat, CvType.Cv32fc2); // Convert to CV_32F

                Mat dstMat = Converters.Vector_Point2d_to_Mat(dstPoints);
                dstMat.ConvertTo(dstMat, CvType.Cv32fc2); // Convert to CV_32F

                Mat perspectiveTransformation = Imgproc.GetPerspectiveTransform(srcMat, dstMat);

                Mat inputMat = _mat;

                Mat outputMat = new Mat(new OpenCV.Core.Size(dstWidth, dstHeight), CvType.Cv8uc1);
                //Console.WriteLine("Input Mat: " + inputMat.Dump());

                Imgproc.WarpPerspective(inputMat, outputMat, perspectiveTransformation,
                    new OpenCV.Core.Size(dstWidth, dstHeight));
                //Console.WriteLine("Transformed Mat: " + outputMat.Dump());

                Bitmap outputBitmap = Bitmap.CreateBitmap((int)dstWidth, (int)dstHeight, Bitmap.Config.Argb8888);

                Utils.MatToBitmap(outputMat, outputBitmap);
                return outputBitmap;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                return inputImage; // Return an empty Mat or handle the error case accordingly
            }
        }

        [Obsolete] //Not implemented fully
        private static float[,] OrderPoints(float[,] pts)
        {
            // Rearrange coordinates to order: top-left, top-right, bottom-right, bottom-left
            float[,] rect = new float[4, 2];
            float[,] ptsArray = new float[pts.GetLength(0), pts.GetLength(1)];

            for (int i = 0; i < pts.GetLength(0); i++)
            {
                for (int j = 0; j < pts.GetLength(1); j++)
                {
                    ptsArray[i, j] = pts[i, j];
                }
            }

            float[] s = new float[ptsArray.GetLength(0)];

            for (int i = 0; i < ptsArray.GetLength(0); i++)
            {
                float sum = 0;
                for (int j = 0; j < ptsArray.GetLength(1); j++)
                {
                    sum += ptsArray[i, j];
                }

                s[i] = sum;
            }

            // Top-left point will have the smallest sum.
            Array.Copy(ptsArray, s.ToList().IndexOf(s.Min()) * ptsArray.GetLength(1), rect, 0, ptsArray.GetLength(1));

            // Bottom-right point will have the largest sum.
            Array.Copy(ptsArray, s.ToList().IndexOf(s.Max()) * ptsArray.GetLength(1), rect, 2 * ptsArray.GetLength(1),
                ptsArray.GetLength(1));

            float[,] diff = new float[ptsArray.GetLength(0), ptsArray.GetLength(1) - 1];

            for (int i = 0; i < ptsArray.GetLength(0); i++)
            {
                for (int j = 0; j < ptsArray.GetLength(1) - 1; j++)
                {
                    diff[i, j] = ptsArray[i, j + 1] - ptsArray[i, j];
                }
            }

            // Top-right point will have the smallest difference.
            Array.Copy(ptsArray, GetIndexOfMinDiff(diff) * ptsArray.GetLength(1), rect, ptsArray.GetLength(1),
                ptsArray.GetLength(1));

            // Bottom-left will have the largest difference.
            Array.Copy(ptsArray, GetIndexOfMaxDiff(diff) * ptsArray.GetLength(1), rect, 3 * ptsArray.GetLength(1),
                ptsArray.GetLength(1));

            return rect;
        }

        [Obsolete] //Not implemented fully
        private static int GetIndexOfMinDiff(float[,] diff)
        {
            float minValue = float.MaxValue;
            int minIndex = 0;

            for (int i = 0; i < diff.GetLength(0); i++)
            {
                float sum = 0;
                for (int j = 0; j < diff.GetLength(1); j++)
                {
                    sum += diff[i, j];
                }

                if (sum < minValue)
                {
                    minValue = sum;
                    minIndex = i;
                }
            }

            return minIndex;
        }
        
        [Obsolete] //Not implemented fully
        private static int GetIndexOfMaxDiff(float[,] diff)
        {
            float maxValue = float.MinValue;
            int maxIndex = 0;

            for (int i = 0; i < diff.GetLength(0); i++)
            {
                float sum = 0;
                for (int j = 0; j < diff.GetLength(1); j++)
                {
                    sum += diff[i, j];
                }

                if (sum > maxValue)
                {
                    maxValue = sum;
                    maxIndex = i;
                }
            }

            return maxIndex;
        }
        
        [Obsolete] //Not implemented fully
        private static int Vector2Compare(Point value1, Point value2)
        {
            // NOTE: THESE DEPEND ON HOW YOU EVALUATE TOP/LEFT/RIGHT/BOTTOM,
            // BUT ASSUMING X AND Y COORDINATES ARE ROTATED 90 DEGREES
            if (value1.X < value2.X)
            {
                return -1;
            }
            else if (value1.X == value2.X)
            {
                if (value1.Y < value2.Y)
                {
                    return -1;
                }
                else if (value1.Y == value2.Y)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
            else
            {
                return 1;
            }
        }
    }

    [Obsolete] //Not implemented fully
    public static class PointExtensions
    {
        public static double DistanceFrom(this Point point, Point srcPoint)
        {
            double w1 = point.X - srcPoint.X;
            double h1 = point.Y - srcPoint.Y;
            double distance = Math.Pow(w1, 2) + Math.Pow(h1, 2);
            return Math.Sqrt(distance);
        }
    }
}

