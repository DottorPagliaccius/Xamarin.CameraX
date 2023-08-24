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
using Android.Media;
using Android.Opengl;
using Android.Views;
using AndroidX.AppCompat.Widget;
using CameraX.Handlers;
using CameraX.Helpers;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Java.Nio;
using Lennox.LibYuvSharp;
using OpenCV.Android;
using Mat = OpenCV.Core.Mat;
using Matrix = Android.Graphics.Matrix;

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
        private BaseLoaderCallback _callback;
        private int width;
        private int height;



        private CannyImageDetector cannyImageDetector = new CannyImageDetector();
        ImageCapture _imageCapture;
        File _outputDirectory;
        IExecutorService _cameraExecutor;
        VectorOfPoint _contourCoordinates;
        ImageView imageView;
        PreviewView _viewFinder;
        SwitchCompat cannySwitch;
        TextView fpsTextView;
        

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            _viewFinder = FindViewById<PreviewView>(Resource.Id.viewFinder);
            fpsTextView = FindViewById<TextView>(Resource.Id.fpsTextView);
            cannySwitch = FindViewById<SwitchCompat>(Resource.Id.cannySwitch);
            cannySwitch.CheckedChange += (sender, e) =>
            {
                if (e.IsChecked)
                {
                    imageView.Visibility = ViewStates.Visible;
                }
                else
                {
                    imageView.Visibility = ViewStates.Invisible;
                }
            };
            imageView = FindViewById<ImageView>(Resource.Id.imageView);
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

                imageAnalyzer.SetAnalyzer(_cameraExecutor, new DocumentAnalyzer(imageProxy =>
                {
                    long currentTimestamp = JavaSystem.CurrentTimeMillis();
                    frameCount++;

                    if (currentTimestamp - lastTimestamp >= 1000) // Calculate FPS every second
                    {
                        fps = frameCount / ((currentTimestamp - lastTimestamp) / 1000.0);
                        frameCount = 0;
                        lastTimestamp = currentTimestamp;
                        Log.Debug("Debug", $"Current FPS data: {fps}");
                    }
                    
                    var image = imageProxy.Image;
                    height = image.Height;
                    width = image.Width;
                    ByteBuffer imageData = image.GetPlanes()[0].Buffer;
                    var bitmap = OpenCVHelper(imageData);
                    
                    // Calculate the aspect ratio of the original processedBitmap
                    float aspectRatio = (float)bitmap.Width / (float)bitmap.Height;

                    // Get the dimensions of the PreviewView
                    int previewWidth = _viewFinder.Width;
                    int previewHeight = _viewFinder.Height;
                    
                    imageProxy.Close();
                    
                    // Calculate the new dimensions for the scaled image while maintaining the aspect ratio
                    int newWidth, newHeight;
                    if (aspectRatio > 1) {
                        // Landscape orientation
                        newWidth = previewWidth;
                        newHeight = (int)(previewWidth / aspectRatio);
                    } 
                    else 
                    {
                        // Portrait orientation or square
                        newHeight = previewHeight ;
                        newWidth = (int)(previewHeight * aspectRatio);
                    }

                    //bitmap = ZoomInBitmap(bitmap, previewWidth, previewHeight);

                    // Create a scaled bitmap using the new dimensions
                    Bitmap scaledBitmap = Bitmap.CreateScaledBitmap(bitmap, newWidth, newHeight, true);
                    //scaledBitmap = Bitmap.CreateScaledBitmap(scaledBitmap, previewWidth, previewHeight, false);
                    RunOnUiThread(() =>
                    {
                        imageView.SetImageBitmap(scaledBitmap);
                        UpdateFPS(fps);
                    });
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
        private void UpdateFPS(double fps)
        {
            fpsTextView.Text = $"FPS: {fps:F2}";
        }
        public long lastTimestamp { get; set; } = 0;

        public int frameCount { get; set; } = 0;

        public double fps { get; set; } = 0;
        
        public Bitmap ZoomInBitmap(Bitmap bitmap, int targetWidth, int targetHeight)
        {
            // Calculate the scaling factors for width and height
            float scaleX = (float)bitmap.Width / targetWidth;
            float scaleY = (float)bitmap.Height / targetHeight;

            // Calculate the scaling factor that maintains the aspect ratio
            float scaleFactor = Math.Max(scaleX, scaleY);

            // Calculate the dimensions of the cropped image
            int scaledWidth = (int)(bitmap.Width / scaleFactor);
            int scaledHeight = (int)(bitmap.Height / scaleFactor);

            // Ensure that the scaled dimensions are not larger than the original dimensions
            scaledWidth = Math.Min(scaledWidth, bitmap.Width);
            scaledHeight = Math.Min(scaledHeight, bitmap.Height);

            // Calculate the cropping coordinates
            int left = Math.Max((bitmap.Width - scaledWidth) / 2, 0);
            int top = Math.Max((bitmap.Height - scaledHeight) / 2, 0);
            int right = Math.Min(left + scaledWidth, bitmap.Width);
            int bottom = Math.Min(top + scaledHeight, bitmap.Height);

            // Create a cropped bitmap
            Bitmap croppedBitmap = Bitmap.CreateBitmap(bitmap, left, top, right - left, bottom - top);

            return croppedBitmap;
        }
        
        private Bitmap OpenCVHelper(ByteBuffer imageData)
        {
            var filteredMat = cannyImageDetector.Update(imageData, height, width);

            var originalOrientation = 0;

            var bitmapFiltered = 
                Bitmap.CreateBitmap(
                    width, height,
                    Bitmap.Config.Argb8888
                );

             Utils.MatToBitmap(filteredMat, bitmapFiltered);
             
             // Apply the original orientation transformation to the bitmap
             Matrix matrix = new Matrix();
             matrix.PostRotate(90);

             Bitmap rotatedBitmap = Bitmap.CreateBitmap(bitmapFiltered, 0, 0, filteredMat.Width(), filteredMat.Height(), matrix, true);
             
             return rotatedBitmap;
             //var boundingBox = new OverlayGenerator(imageProxy);
             // _viewFinder.Overlay?.Clear();
             // _viewFinder.Overlay?.Add(boundingBox);
        }
        
        protected override void OnResume()
        {
            base.OnResume();

            if(!OpenCVLoader.InitDebug())
            {
                Log.Debug("CameraPreview", "Internal OpenCV library not found. Using OpenCV Manager for initialization");
                OpenCVLoader.InitAsync(OpenCVLoader.OpencvVersion300, this, _callback);
            }
            else
            {
                Log.Debug("CameraPreview", "OpenCV library found inside package. Using it!");
            }
        }
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