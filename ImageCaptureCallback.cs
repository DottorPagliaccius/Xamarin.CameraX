using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Camera.Core;
using Java.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static AndroidX.Camera.Core.ImageCapture;

namespace CameraX
{
    class ImageCaptureCallback : IOnImageSavedCallback
    {
        public IntPtr Handle { get; } // => // throw new NotImplementedException();

        public int JniIdentityHashCode { get; } // => // throw new NotImplementedException();

        public JniObjectReference PeerReference { get; } // => // throw new NotImplementedException();

        public JniPeerMembers JniPeerMembers { get; } // => // throw new NotImplementedException();

        public JniManagedPeerStates JniManagedPeerState { get; } // => // throw new NotImplementedException();

        public void Dispose()
        {
            // // throw new NotImplementedException();
        }

        public void Disposed()
        {
            // throw new NotImplementedException();
        }

        public void DisposeUnlessReferenced()
        {
            // throw new NotImplementedException();
        }

        public void Finalized()
        {
            // throw new NotImplementedException();
        }

        public void OnError(ImageCaptureException ex)
        {
            throw new Exception(ex.Message);
        }

        public void OnImageSaved(OutputFileResults p0)
        {
            var savedUri = Uri.FromFile(photoFile)
               val msg = "Photo capture succeeded: $savedUri"
               Toast.makeText(baseContext, msg, Toast.LENGTH_SHORT).show()
               Log.d(TAG, msg)
        }

        public void SetJniIdentityHashCode(int value)
        {
            // throw new NotImplementedException();
        }

        public void SetJniManagedPeerState(JniManagedPeerStates value)
        {
            // throw new NotImplementedException();
        }

        public void SetPeerReference(JniObjectReference reference)
        {
            // throw new NotImplementedException();
        }

        public void UnregisterFromRuntime()
        {
            // throw new NotImplementedException();
        }
    }
}