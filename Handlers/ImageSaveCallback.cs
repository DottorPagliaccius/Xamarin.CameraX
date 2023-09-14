using System;
using AndroidX.Camera.Core;
using static AndroidX.Camera.Core.ImageCapture;

namespace CameraX.Handlers
{
    class ImageSaveCallback : Java.Lang.Object, IOnImageSavedCallback
    {
        private const string TAG = "CameraXBasic";

        private readonly Action<ImageCaptureException> onErrorCallback;
        private readonly Action<OutputFileResults> onImageSaveCallback;

        public ImageSaveCallback(Action<OutputFileResults> onImageSaveCallback, Action<ImageCaptureException> onErrorCallback)
        {
            this.onImageSaveCallback = onImageSaveCallback;
            this.onErrorCallback = onErrorCallback;
        }

        public void OnError(ImageCaptureException exc)
        {
            this.onErrorCallback.Invoke(exc);
        }

        public void OnImageSaved(OutputFileResults photoFile)
        {
            // Get the saved file path as a string
            string savedFilePath = photoFile.SavedUri?.Path;

            if (!string.IsNullOrEmpty(savedFilePath))
            {
                // If you want to append a timestamp to the filename:
                savedFilePath = AppendTimestampToFileName(savedFilePath);

                // Convert the string path to an Android.Net.Uri
                Android.Net.Uri savedUri = Android.Net.Uri.FromFile(new Java.IO.File(savedFilePath));

                // Invoke the callback with the modified Android.Net.Uri
                this.onImageSaveCallback.Invoke(new OutputFileResults(savedUri));
            }
            else
            {
                // Handle the case where the saved file path is empty or null
            }
        }

        private string AppendTimestampToFileName(string filePath)
        {
            string directory = System.IO.Path.GetDirectoryName(filePath);
            string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(filePath);
            string fileExtension = System.IO.Path.GetExtension(filePath);
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            string newFileName = $"{fileNameWithoutExtension}_{timestamp}{fileExtension}";

            return System.IO.Path.Combine(directory, newFileName);
        }
    }
}