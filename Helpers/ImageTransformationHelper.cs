using System;
using Android.Graphics;
using Rect = OpenCV.Core.Rect;

namespace CameraX.Helpers
{
    public static class ImageTransformationHelper
    {
        /// <summary>
        /// This method is used to overlay the canny image detector's bounding box
        /// over the final output image.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="boundingBox"></param>
        /// <param name="analysisFrameHeight"></param>
        /// <param name="analysisFrameWidth"></param>
        /// <param name="returnColorimage">pass optional parameter if you want the final image to be in color</param>
        /// <returns></returns>
        public static Bitmap CropOutputImage(Bitmap output, Rect boundingBox, int analysisFrameHeight, int analysisFrameWidth, bool returnColorimage = false)
        {
            // Get the original bitmap
            var originalBitmap = output;

            //Calculate scaling factor
            var heightScaling = output.Height / analysisFrameHeight;
            var widthScaling = output.Width / analysisFrameWidth;

            // Padding values (adjust these as needed)
            var paddingX = 75; // Left and right padding
            var paddingY = 50; // Top and bottom padding

            // Calculate the width and height of the bounding box including scaling and padding
            var cropX = (boundingBox.X * heightScaling) + paddingX;
            var cropY = (boundingBox.Y * widthScaling) + paddingY;
            var cropHeight = (boundingBox.Height * heightScaling) + 2 * paddingY;
            var cropWidth = (boundingBox.Width * widthScaling) + 2 * paddingX;

            // Ensure that the cropping area is within the bounds of the original bitmap
            cropX = Math.Max(0, cropX);
            cropY = Math.Max(0, cropY);
            cropWidth = Math.Min(originalBitmap.Width - cropX, cropWidth);
            cropHeight = Math.Min(originalBitmap.Height - cropY, cropHeight);

            // Create a new bitmap with the dimensions of the bounding box
            var croppedBitmap = Bitmap.CreateBitmap(originalBitmap, cropX, cropY, cropWidth, cropHeight);

            var matrix = new Matrix();
            matrix.PostRotate(90); // Rotate by 90 degrees
           
            var rgbBitmap = Bitmap.CreateBitmap(croppedBitmap, 0, 0, croppedBitmap.Width, croppedBitmap.Height, matrix, true);
            
            if (returnColorimage)
            {
                return rgbBitmap;
            }
            
            var grayscaleBitmap = ConvertToGrayscale(rgbBitmap);

            return grayscaleBitmap;
        }

        private static Bitmap ConvertToGrayscale(Bitmap rgbBitmap)
        {
            var grayscaleBitmap = Bitmap.CreateBitmap(
                rgbBitmap.Width, rgbBitmap.Height,
                Bitmap.Config.Rgb565);

            var c = new Canvas(grayscaleBitmap);
            var p = new Paint();
            var cm = new ColorMatrix();

            cm.SetSaturation(0);
            var filter = new ColorMatrixColorFilter(cm);
            p.SetColorFilter(filter);
            c.DrawBitmap(rgbBitmap, 0, 0, p);
            return grayscaleBitmap;
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
            var aspectRatio = (float)bitmap.Width / (float)bitmap.Height;

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