using AndroidX.Camera.Core;
using System;
using static AndroidX.Camera.Core.ImageCapture;

namespace CameraX
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
            this.onImageSaveCallback.Invoke(photoFile);
        }
    }
}