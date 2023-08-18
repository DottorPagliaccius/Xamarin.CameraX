using Android;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Concurrent;
using System.Linq;
using Android.Graphics;
using Android.Views;
using CameraX.Handlers;
using CameraX.Helpers;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Java.Nio;
using Org.Apache.Http.Util;
using SkiaSharp.Views.Android;

namespace CameraX
{
    /// <summary>
    /// https://codelabs.developers.google.com/codelabs/camerax-getting-started#0
    /// </summary>

    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private const string Tag = "CameraXBasic";
        private const int RequestCodePermissions = 10;
        private const string FilenameFormat = "yyyy-MM-dd-HH-mm-ss-SSS";
        private Bitmap _bitmapBuffer;
        private bool _pauseAnalysis = false;
        private int _imageRotationDegrees = 0;

        ImageCapture _imageCapture;
        File _outputDirectory;
        IExecutorService _cameraExecutor;
        VectorOfPoint _contourCoordinates;

        PreviewView _viewFinder;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            _viewFinder = FindViewById<PreviewView>(Resource.Id.viewFinder);
            //surfaceView = FindViewById<SurfaceView>(Resource.Id.surfaceView);
            var cameraCaptureButton = FindViewById<Button>(Resource.Id.camera_capture_button);

            // Request camera permissions   
            string[] permissions = new string[] { Manifest.Permission.Camera, Manifest.Permission.WriteExternalStorage };
            if (permissions.FirstOrDefault(x => ContextCompat.CheckSelfPermission(this, x) != Android.Content.PM.Permission.Granted) != null) //   ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) == Android.Content.PM.Permission.Granted)
                ActivityCompat.RequestPermissions(this, permissions, RequestCodePermissions);
            else
                StartCamera();

            // Set up the listener for take photo button
            cameraCaptureButton.SetOnClickListener(new OnClickListener(() => TakePhoto()));

            _outputDirectory = GetOutputDirectory();

            _cameraExecutor = Executors.NewSingleThreadExecutor();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _cameraExecutor.Shutdown();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            if (requestCode == RequestCodePermissions)
            {
                //if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) == Android.Content.PM.Permission.Granted)
                if (permissions.FirstOrDefault(x => ContextCompat.CheckSelfPermission(this, x) != Android.Content.PM.Permission.Granted) == null)
                {
                    StartCamera();
                }
                else
                {
                    Toast.MakeText(this, "Permissions not granted by the user.", ToastLength.Short).Show();
                    this.Finish();
                    return;
                }
            }

            //base.OnRequestPermissionsResult(requestCode, permissions, grantResults);              
        }

        /// <summary>
        /// https://codelabs.developers.google.com/codelabs/camerax-getting-started#3
        /// </summary>
        private void StartCamera()
        {
            var cameraProviderFuture = ProcessCameraProvider.GetInstance(this);

            cameraProviderFuture.AddListener(new Runnable(() =>
            {
                // Used to bind the lifecycle of cameras to the lifecycle owner
                var cameraProvider = (ProcessCameraProvider)cameraProviderFuture.Get();

                // Preview
                var preview = new Preview.Builder().Build();
                preview.SetSurfaceProvider(_viewFinder.CreateSurfaceProvider());

                // Take Photo
                this._imageCapture = new ImageCapture.Builder().Build();

                // Frame by frame analyze
                var imageAnalyzer = new ImageAnalysis.Builder()
                    .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                    .Build();
                // imageAnalyzer.SetAnalyzer(cameraExecutor, new LuminosityAnalyzer(luma =>
                //     Log.Debug(TAG, $"Average luminosity: {luma}")
                //     ));

                // imageAnalyzer.SetAnalyzer(cameraExecutor, new DocumentAnalyzer(docContour =>
                // {
                //     Log.Debug(TAG, $"Current Pixel data: {docContour.Length}");
                //     var boundingBox = new OverlayGenerator(docContour);
                //     viewFinder.Overlay?.Clear();
                //     viewFinder.Overlay?.Add(boundingBox);
                //     
                // }));
                
                imageAnalyzer.SetAnalyzer(_cameraExecutor, new DocumentAnalyzer( image=>
                {
                    try
                    {
                        // Initialize the bitmapBuffer if it's not already initialized
                        if (_bitmapBuffer == null)
                        {
                            // The image rotation and RGB image buffer are initialized only once
                            // the analyzer has started running
                            _imageRotationDegrees = image.ImageInfo.RotationDegrees;
                            _bitmapBuffer = Bitmap.CreateBitmap(
                                image.Width, image.Height, Bitmap.Config.Argb8888);
                        }

                        ProcessEmguCV(image);

                        // Compute the FPS and report prediction here

                        // ...
                    }
                    catch (Exception exception)
                    {
                        Log.Error(exception.Source, exception.Message);
                    }
                }));

                
                // Select back camera as a default, or front camera otherwise
                CameraSelector cameraSelector = null;
                
                if (cameraProvider.HasCamera(CameraSelector.DefaultBackCamera) == true)
                    cameraSelector = CameraSelector.DefaultBackCamera;
                else if (cameraProvider.HasCamera(CameraSelector.DefaultFrontCamera) == true)
                    cameraSelector = CameraSelector.DefaultFrontCamera;
                else
                    throw new System.Exception("Camera not found");

                try
                {
                    // Unbind use cases before rebinding
                    cameraProvider.UnbindAll();

                    // Bind use cases to camera
                    cameraProvider.BindToLifecycle(this, cameraSelector, preview, _imageCapture, imageAnalyzer);
                }
                catch (Exception exc)
                {
                    Log.Debug(Tag, "Use case binding failed", exc);
                    Toast.MakeText(this, $"Use case binding failed: {exc.Message}", ToastLength.Short).Show();
                }

            }), ContextCompat.GetMainExecutor(this)); //GetMainExecutor: returns an Executor that runs on the main thread.
        }

        private void ProcessEmguCV(IImageProxy imageProxy)
        {
            try
            {
                var image = imageProxy.Image;

                byte[] bytes;

                // Copy out RGB bits to our shared buffer
                using (var buffer = image.GetPlanes()[0].Buffer)
                {
                    bytes = new byte[buffer.Remaining()];
                    buffer.Get(bytes);
                }

                // Perform the EmguCV processing
                using (var bitmapImage = new Image<Gray, byte>(image.Width, image.Height))
                {
                    bitmapImage.Bytes = bytes;

                    using (var blurredImage = bitmapImage.SmoothGaussian(5, 5, 0, 0))
                    {
                        using (var cannyImage = new UMat())
                        {
                            CvInvoke.Canny(blurredImage, cannyImage, 50, 150);

                            using (var contours = new VectorOfVectorOfPoint())
                            {
                                CvInvoke.FindContours(cannyImage, contours, null, RetrType.Tree,
                                    ChainApproxMethod.ChainApproxSimple);
                                if (contours.Size > 0)
                                {
                                    var emguHandle = new EmguHelper();
                                    var top5Contours = emguHandle.SelectContours(contours);
                                    emguHandle.SelectTopContour(top5Contours);
                                    _viewFinder.Overlay?.Clear();
                                    if (emguHandle.ContourCoordinates != null)
                                    {
                                        var boundingBox = new OverlayGenerator(emguHandle.ContourCoordinates);
                                        _viewFinder.Overlay?.Add(boundingBox);
                                    }
                                }
                            }
                        }
                    }
                }

                // Close the imageProxy after processing
                //imageProxy.Close();
            }
            catch (Exception exception)
            {
                Log.Error(exception.Source, exception.Message);
            }
        }


        // private void Canvas_PaintSurface()
        // {
        //     Canvas canvas = surfaceView.get;
        //
        //     var canvasWidth = e.Info.Width;
        //     var canvasHeight = e.Info.Height;
        //     var points = new System.Drawing.Point[4];
        //
        //     var boxData = ContourCoordinates.ToArray();
        //
        //     canvas.Clear();
        //
        //     var recHeight = 250;
        //     DrawingHelper.DrawBackgroundRectangle(
        //         canvas,
        //         canvasWidth,
        //         recHeight,
        //         0,
        //         canvasHeight - recHeight);
        //
        //     // Calculate the bounding box coordinates
        //     int left = int.MaxValue;
        //     int top = int.MaxValue;
        //     int right = int.MinValue;
        //     int bottom = int.MinValue;
        //
        //     for (var i = 0; i < boxData.Count(); i++)
        //     {
        //         System.Drawing.Point point = boxData[i];
        //         left = Math.Min(left, point.X);
        //         top = Math.Min(top, point.Y);
        //         right = Math.Max(right, point.X);
        //         bottom = Math.Max(bottom, point.Y);
        //     }
        //
        //     DrawingHelper.DrawBoundingBox(
        //         canvas,
        //         left,
        //         top,
        //         right,
        //         bottom);
        // }

        private void TakePhoto()
        {
            // Get a stable reference of the modifiable image capture use case   
            var imageCapture = this._imageCapture;
            if (imageCapture == null)
                return;

            // Create time-stamped output file to hold the image
            var photoFile = new File(_outputDirectory, new Java.Text.SimpleDateFormat(FilenameFormat, Locale.Us).Format(JavaSystem.CurrentTimeMillis()) + ".jpg");

            // Create output options object which contains file + metadata
            var outputOptions = new ImageCapture.OutputFileOptions.Builder(photoFile).Build();

            // Set up image capture listener, which is triggered after photo has been taken
            imageCapture.TakePicture(outputOptions, ContextCompat.GetMainExecutor(this), new ImageSaveCallback(

                onErrorCallback: (exc) =>
                {
                    var msg = $"Photo capture failed: {exc.Message}";
                    Log.Error(Tag, msg, exc);
                    Toast.MakeText(this.BaseContext, msg, ToastLength.Short).Show();
                },

                onImageSaveCallback: (output) =>
                {
                    var savedUri = output.SavedUri;
                    var msg = $"Photo capture succeeded: {savedUri}";
                    Log.Debug(Tag, msg);
                    Toast.MakeText(this.BaseContext, msg, ToastLength.Short).Show();
                }
            ));
        }

        // Save photos to => /Pictures/CameraX/
        private File GetOutputDirectory()
        {
            //var mediaDir = GetExternalMediaDirs().FirstOrDefault();  
            var mediaDir = Environment.GetExternalStoragePublicDirectory(System.IO.Path.Combine(Environment.DirectoryPictures, Resources.GetString(Resource.String.app_name)));

            if (mediaDir != null && mediaDir.Exists())
                return mediaDir;

            var file = new File(mediaDir, string.Empty); 
            file.Mkdirs();
            return file;
        }
    }
}