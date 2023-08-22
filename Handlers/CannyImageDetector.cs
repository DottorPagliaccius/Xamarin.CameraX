using System.Collections.Generic;
using Java.Nio;
using OpenCV.Android;
using OpenCV.Core;
using OpenCV.ImgProc;

namespace CameraX.Handlers
{
    public class CannyImageDetector
    {
        public Mat Update(ByteBuffer imageData, int height, int width)
        {
            // Create a MAT object from the byte array
            byte[] imageDataArray = new byte[imageData.Remaining()];
            imageData.Get(imageDataArray);
            Mat mat = new Mat(height, width, 0);
            mat.Put(0, 0, imageDataArray);

            Mat cannyImage = new Mat();
            Imgproc.Canny(mat, cannyImage, 80, 90);
            Imgproc.CvtColor(cannyImage, mat, Imgproc.ColorGray2bgra, 4);
            return mat;
        }
    }
}