using System;
using Android.Graphics;
using OpenCV.ImgProc;
using Rect = OpenCV.Core.Rect;

namespace CameraX.Helpers
{
    public static class ImageTransformationHelper
    {
        
        /// <summary>
        /// Get cropped bitmap after analysing the final output image directly
        /// </summary>
        /// <param name="output"></param>
        /// <param name="boundingBox"></param>
        /// <param name="returnColorimage">pass optional parameter if you want the final image to be in color</param>
        /// <returns>Cropped Bitmap</returns>
        public static Bitmap CropOutputImageWoTransform(Bitmap output, Rect boundingBox, bool returnColorimage = false)
        {
            // Get the original bitmap
            var originalBitmap = output;
            
            // Padding values (adjust these as needed)
            var paddingXPercent = 0.01; // Left and right padding 0.005
            var paddingYPercent = 0.01; // Top and bottom padding 0.008

            // Calculate the width and height of the bounding box including scaling and padding
            var cropX = (boundingBox.X);
            var cropY = (boundingBox.Y);

            // Apply padding
            cropX -= (int)(paddingXPercent * output.Width);
            cropY -= (int)(paddingYPercent * output.Height);
            var cropHeight = boundingBox.Height + 2 * (paddingYPercent * originalBitmap.Height);
            var cropWidth = boundingBox.Width + 2 * (paddingXPercent * originalBitmap.Width);

            // Ensure that the cropping area is within the bounds of the original bitmap
            cropX = Math.Max(0, cropX);
            cropY = Math.Max(0, cropY);
            cropWidth = Math.Min(originalBitmap.Width - cropX, cropWidth);
            cropHeight = Math.Min(originalBitmap.Height - cropY, cropHeight);

            // Create a new bitmap with the dimensions of the bounding box
            var croppedBitmap = Bitmap.CreateBitmap(originalBitmap, (int)cropX, (int)cropY, (int)cropWidth, (int)cropHeight);

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

        /// <summary>
        /// This method is used to overlay the canny image detector's bounding box
        /// over the final output image. It transforms the final bounding box by scaling according to the final output resolution.
        /// However, this seems to work inconsistently across different devices
        /// so it is better to use the implementation that gets the bounding box directly using the output image.  
        /// </summary>
        /// <param name="output"></param>
        /// <param name="boundingBox"></param>
        /// <param name="analysisFrameHeight"></param>
        /// <param name="analysisFrameWidth"></param>
        /// <param name="returnColorimage">pass optional parameter if you want the final image to be in color</param>
        /// <returns>Cropped Bitmap</returns>
        public static Bitmap CropOutputImage(Bitmap output, Rect boundingBox, int analysisFrameHeight, int analysisFrameWidth, bool returnColorimage = false)
        {
            // Get the original bitmap
            var originalBitmap = output;

            //Calculate scaling factor
            var heightScaling = output.Height / analysisFrameHeight;
            var widthScaling = output.Width / analysisFrameWidth;
            
            // Padding values (adjust these as needed)
            var paddingXPercent = 0.02; // Left and right padding 0.005
            var paddingYPercent = 0.02; // Top and bottom padding 0.008

            // Calculate the width and height of the bounding box including scaling and padding
            var cropX = (boundingBox.X * widthScaling);
            var cropY = (boundingBox.Y * heightScaling);

            // Check if origin is at center
            if (boundingBox.X < 0 || boundingBox.X > analysisFrameWidth || boundingBox.Y < 0 || boundingBox.Y > analysisFrameHeight)
            {
                // Adjust for center origin
                cropX += output.Width / 2;
                cropY += output.Height / 2;
            }

            // Apply padding
            cropX -= (int)(paddingXPercent * output.Width);
            cropY -= (int)(paddingYPercent * output.Height);
            var cropHeight = (boundingBox.Height * heightScaling) + 3 * (paddingYPercent * originalBitmap.Height);
            var cropWidth = (boundingBox.Width * widthScaling) + 3 * (paddingXPercent * originalBitmap.Width);

            // Ensure that the cropping area is within the bounds of the original bitmap
            cropX = Math.Max(0, cropX);
            cropY = Math.Max(0, cropY);
            cropWidth = Math.Min(originalBitmap.Width - cropX, cropWidth);
            cropHeight = Math.Min(originalBitmap.Height - cropY, cropHeight);

            // Create a new bitmap with the dimensions of the bounding box
            var croppedBitmap = Bitmap.CreateBitmap(originalBitmap, (int)cropX, (int)cropY, (int)cropWidth, (int)cropHeight);

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