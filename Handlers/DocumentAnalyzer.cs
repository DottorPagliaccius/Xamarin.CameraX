using System;
using Android.Graphics;
using AndroidX.Camera.Core;

namespace CameraX.Handlers
{
    public class DocumentAnalyzer: Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        private readonly Action<IImageProxy> docListener;
        public Matrix TransformMatrix;

        public DocumentAnalyzer(Action<IImageProxy> callback) //LumaListener listener)
        {
            docListener = callback;
        }

        public void Analyze(IImageProxy imageProxy)
        {
            docListener.Invoke(imageProxy);
        }

        public void UpdateTransform(Matrix matrix)
        {
            TransformMatrix = matrix;
        }
    }
}