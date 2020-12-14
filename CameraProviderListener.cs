using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Lifecycle;
using Google.Common.Util.Concurrent;
using Java.Interop;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CameraX
{
    class CameraProviderListener : Java.Lang.Object, ILifecycleOwner, IRunnable
    {
        private readonly IListenableFuture cameraProviderFuture;
        private readonly PreviewView viewFinder;

        public IntPtr Handle { get; }// throw new NotImplementedException();

        public int JniIdentityHashCode { get; }// throw new NotImplementedException();

        public JniObjectReference PeerReference { get; }// throw new NotImplementedException();

        public JniPeerMembers JniPeerMembers { get; }// throw new NotImplementedException();

        public JniManagedPeerStates JniManagedPeerState { get; }// throw new NotImplementedException();

        public Lifecycle Lifecycle { get; }// throw new NotImplementedException();



        public CameraProviderListener(IListenableFuture cameraProviderFuture, PreviewView viewFinder)
        {
            this.cameraProviderFuture = cameraProviderFuture;
            this.viewFinder = viewFinder;
        }


        public void Dispose()
        {
            // throw new NotImplementedException();
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

        public void Run()
        {

            //// Used to bind the lifecycle of cameras to the lifecycle owner
            //ProcessCameraProvider cameraProvider = (ProcessCameraProvider)cameraProviderFuture.Get();

            //// Preview
            //var preview = (new Preview.Builder()).Build(); //.Also {
            //preview.SetSurfaceProvider(viewFinder.CreateSurfaceProvider());


            //// Select back camera as a default
            //var cameraSelector = CameraSelector.DefaultBackCamera;

            //try
            //{
            //    // Unbind use cases before rebinding
            //    cameraProvider.UnbindAll();

            //    // Bind use cases to camera
            //    cameraProvider.BindToLifecycle(this, cameraSelector, preview);

            //}
            //catch (System.Exception ex)
            //{
            //    Log.Debug("", "Use case binding failed", ex);
            //}

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