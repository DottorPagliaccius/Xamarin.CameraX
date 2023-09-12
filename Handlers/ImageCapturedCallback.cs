using System;
using Android.Graphics;
using AndroidX.Camera.Core;
using AndroidX.Camera.Core.Impl;
using AndroidX.Camera.Core.Internal.Utils;
using static AndroidX.Camera.Core.ImageCapture;

namespace CameraX.Handlers
{
    class ImageCapturedCallback : OnImageCapturedCallback
    {
        private readonly Action<ImageCaptureException> onErrorCallback;
        private readonly Action<Bitmap> onCapturedSuccessCallback;

        public ImageCapturedCallback(Action<Bitmap> onCapturedSuccessCallback, Action<ImageCaptureException> onErrorCallback)
        {
            this.onCapturedSuccessCallback = onCapturedSuccessCallback;
            this.onErrorCallback = onErrorCallback;
        }

        public override void OnError(ImageCaptureException exc)
        {
            this.onErrorCallback?.Invoke(exc);
        }

        public override void OnCaptureSuccess(IImageProxy image)
        {
            var data = ImageUtil.JpegImageToJpegByteArray(image);
            var imageBitmap = BitmapFactory.DecodeByteArray(data, 0, data.Length);

            onCapturedSuccessCallback?.Invoke(imageBitmap);

            base.OnCaptureSuccess(image);
            image.Close();
        }
    } 
}