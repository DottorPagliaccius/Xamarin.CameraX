using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Android.Graphics;
using Android.Util;
using AndroidX.Camera.Core;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Java.Nio;
using Java.Util.Logging;

namespace CameraX.Handlers
{
    public class DocumentAnalyzer: Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        private const string TAG = "CameraXBasic";
        private readonly Action<IImageProxy> docListener;
        private byte[] depthPixelData;

        public DocumentAnalyzer(Action<IImageProxy> callback) //LumaListener listener)
        {
            docListener = callback;
        }

        public void Analyze(IImageProxy imageProxy)
        {
            docListener.Invoke(imageProxy);
        }
        
    }
}