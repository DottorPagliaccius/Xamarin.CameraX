using System;
using Android.Graphics;
using Rect = OpenCV.Core.Rect;

namespace CameraX.Helpers
{
    public static class ImageTransformationHelper
    {
        /// <summary>
        /// This method is used to overlay the canny image detectors bounding box
        /// over the final output image.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="boundingBox"></param>
        /// <param name="analysisFrameHeight"></param>
        /// <param name="analysisFrameWidth"></param>
        /// <returns></returns>
        public static Bitmap CropOutputImage(Bitmap output, Rect boundingBox, int analysisFrameHeight,int analysisFrameWidth)
        {
            // Get the original bitmap
            Bitmap originalBitmap = output;

            //Calculate scaling factor
            var heightScaling = output.Height / analysisFrameHeight;
            var widthScaling = output.Width / analysisFrameWidth;

            // Calculate the width and height of the bounding box
            int cropX = (boundingBox.X * heightScaling) - (50 / heightScaling);
            int cropY = (boundingBox.Y * widthScaling) - (50/ widthScaling);
            int cropHeight = (boundingBox.Height * heightScaling) + (100/heightScaling);
            int cropWidth = (boundingBox.Width * widthScaling) + (100/widthScaling);

            // Ensure that the cropping area is within the bounds of the original bitmap
            cropX = Math.Max(0, cropX);
            cropY = Math.Max(0, cropY);
            cropWidth = Math.Min(originalBitmap.Width - cropX, cropWidth);
            cropHeight = Math.Min(originalBitmap.Height - cropY, cropHeight);

            // Create a new bitmap with the dimensions of the bounding box
            Bitmap croppedBitmap = Bitmap.CreateBitmap(originalBitmap, cropX, cropY, cropWidth, cropHeight);

            Matrix matrix = new Matrix();
            matrix.PostRotate(90); // Rotate by 90 degrees
           
            return Bitmap.CreateBitmap(croppedBitmap, 0, 0, croppedBitmap.Width, croppedBitmap.Height, matrix, true);
        }
        
        /// <summary>
        /// Use this method to transform an image according to the view_finder aspect ratio
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="previewWidth"></param>
        /// <param name="previewHeight"></param>
        /// <returns></returns>
        private static Bitmap TransformImage(Bitmap bitmap, int previewWidth, int previewHeight)
        {
            // Calculate the aspect ratio of the original processedBitmap
            float aspectRatio = (float)bitmap.Width / (float)bitmap.Height;

            // Calculate the new dimensions for the scaled image while maintaining the aspect ratio
            int newWidth, newHeight;
            if (aspectRatio > 1)
            {
                // Landscape orientation
                newWidth = previewWidth;
                newHeight = (int)(previewWidth / aspectRatio);
            }
            else
            {
                // Portrait orientation or square
                newHeight = previewHeight;
                newWidth = (int)(previewHeight * aspectRatio);
            }

            // Create a scaled bitmap using the new dimensions
            return Bitmap.CreateScaledBitmap(bitmap, newWidth, newHeight, true);
        }
    }
}