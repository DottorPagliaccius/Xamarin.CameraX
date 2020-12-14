using AndroidX.Camera.Core;
using Java.Nio;
using System;
using System.Linq;

namespace CameraX
{
    //https://codelabs.developers.google.com/codelabs/camerax-getting-started#5

    public class LuminosityAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        private const string TAG = "CameraXBasic";
        private readonly Action<double> lumaListerner;

        public LuminosityAnalyzer(Action<double> callback) //LumaListener listener)
        {
            this.lumaListerner = callback;
        }

        public void Analyze(IImageProxy image)
        {
            var buffer = image.GetPlanes()[0].Buffer;
            var data = ToByteArray(buffer);

            var pixels = data.ToList();
            pixels.ForEach(x => x = (byte)((int)x & 0xFF));
            var luma = pixels.Average(x => x);
            //Log.Debug(TAG, $"Average luminosity: {luma}");

            image.Close();

            lumaListerner.Invoke(luma);
        }

        private byte[] ToByteArray(ByteBuffer buff)
        {
            buff.Rewind();    // Rewind the buffer to zero
            var data = new byte[buff.Remaining()];
            buff.Get(data);   // Copy the buffer into a byte array
            return data;      // Return the byte array
        }

    }
}
