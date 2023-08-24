using System;
using System.Collections.Generic;
using System.Linq;
using Android.Runtime;
using Java.Nio;
using OpenCV.Android;
using OpenCV.Core;
using OpenCV.ImgCodecs;
using OpenCV.ImgProc;
using Size = System.Drawing.Size;

namespace CameraX.Handlers
{
    public class CannyImageDetector
    {
        public Mat Update(ByteBuffer imageData, int height, int width)
        {
            // Create a MAT object from the byte array
            var imageDataArray = new byte[imageData.Remaining()];
            imageData.Get(imageDataArray);
            var mat = new Mat(height, width, 0);
            mat.Put(0, 0, imageDataArray);
            
            var blurredMat = new Mat();
            Imgproc.GaussianBlur(mat, blurredMat, new OpenCV.Core.Size(11, 11), 0, 0);

            var edges = new Mat();
            Imgproc.Canny(blurredMat, edges, 30, 90);
            Imgproc.Dilate(edges, edges, Imgproc.GetStructuringElement(Imgproc.MorphEllipse, new OpenCV.Core.Size(5,5)));

            var contours = new JavaList<MatOfPoint>();
            var hierarchy = new Mat();
            Imgproc.FindContours(edges, contours, hierarchy, Imgproc.RetrList, Imgproc.ChainApproxNone);
            
            // Now the 'contours' list should contain the detected contours
            var largestContour = contours?.OrderByDescending(Imgproc.ContourArea).FirstOrDefault();
            
            // Create a black canvas to draw the contour
            var blackColor = new Scalar(0, 0, 0);
            var whiteColor = new Scalar(255, 255, 255);
            var yellowColor = new Scalar(255, 255, 0);
            
            var canvas = new Mat(new OpenCV.Core.Size(width, height), 24, blackColor);
            
            if (largestContour != null)
            {
                // Convert MatOfPoint to MatOfPoint2f for ApproxPolyDP
                var corners2f = new MatOfPoint2f(largestContour.ToArray());
                
                // Approximate the contour
                var epsilon = 0.02 * Imgproc.ArcLength(corners2f, true);
                var corners = new MatOfPoint2f();
                Imgproc.ApproxPolyDP(corners2f, corners, epsilon, true);

                // If our approximated contour has four points
                if (corners.Rows() == 4)
                {
                    var approx = new MatOfPoint();
                    corners.ConvertTo(approx, 4);
                    // Draw the contour and corners on the canvas
                    Imgproc.DrawContours(canvas, new JavaList<MatOfPoint> { largestContour }, -1, new Scalar(255, 0, 0), 1);
                    Imgproc.DrawContours(canvas, new JavaList<MatOfPoint> { approx }, -1, yellowColor, 2);

                    // Sorting the corners and converting them to desired shape
                    var cornerArray = approx.ToArray();
                    Array.Sort(cornerArray, (p1, p2) => p1.X.CompareTo(p2.X));

                    // Displaying the corners
                    for (var index = 0; index < cornerArray.Length; index++)
                    {
                        var character = ((char)(65 + index)).ToString();
                        Imgproc.PutText(canvas, character, cornerArray[index], Imgproc.FontHersheySimplex, 0.5, new Scalar(0, 255, 0), 1, Imgproc.LineAa);
                    }
                }
            }
            
            return canvas; // Return the canvas with the bounding boxes overlaid
        }
    }
}