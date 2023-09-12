﻿using System.Drawing;
using System.IO;
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
using Java.Lang;
using Java.Util;
using Java.Util.Concurrent;
using System.Linq;
using Android.Graphics;
using Android.Views;
using AndroidX.AppCompat.Widget;
using CameraX.Handlers;
using Java.Nio;
using OpenCV.Android;
using OpenCV.Core;
using OpenCV.ImgProc;
using Bitmap = Android.Graphics.Bitmap;
using Console = System.Console;
using File = Java.IO.File;
using Image = Android.Media.Image;
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
        private bool _pauseAnalysis = false;
        private BaseLoaderCallback _callback;
        private int _width;
        private int _height;
        private bool _captureClicked;
        private Bitmap _croppedImage;

        private CannyImageDetector _cannyImageDetector = new CannyImageDetector();
        private ImageCapture _imageCapture;
        private File _outputDirectory;
        private IExecutorService _cameraExecutor;
        private ImageView _imageView;
        private ImageView _croppedImageView;
        private PreviewView _viewFinder;
        private TextView _fpsTextView;
        

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            _viewFinder = FindViewById<PreviewView>(Resource.Id.viewFinder);
            _fpsTextView = FindViewById<TextView>(Resource.Id.fpsTextView);
            _imageView = FindViewById<ImageView>(Resource.Id.imageView);
            _croppedImageView = FindViewById<ImageView>(Resource.Id.croppedImageView);
            
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
                _imageCapture = new ImageCapture.Builder().Build();

                // Frame by frame analyze
                var imageAnalyzer = new ImageAnalysis.Builder()
                    .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                    .Build();
                
                imageAnalyzer.SetAnalyzer(_cameraExecutor, new DocumentAnalyzer(imageProxy =>
                {
                    if (!_pauseAnalysis)
                    {
                        // Get the dimensions of the PreviewView
                        var image = imageProxy.Image;
                        _height = image.Height;
                        _width = image.Width;
                        
                        long currentTimestamp = JavaSystem.CurrentTimeMillis();
                        FrameCount++;

                        if (currentTimestamp - LastTimestamp >= 1000) // Calculate FPS every second
                        {
                            Fps = FrameCount / ((currentTimestamp - LastTimestamp) / 1000.0);
                            FrameCount = 0;
                            LastTimestamp = currentTimestamp;
                            Log.Debug("Debug", $"Current FPS data: {Fps}");
                        }
                        
                        var bitmap = OpenCvHelper(image, imageProxy);
                        imageProxy.Close();
                        //bitmap = TransformImage(bitmap, _viewFinder.Width, _viewFinder.Height);
                        RunOnUiThread(() =>
                        {
                            if (_captureClicked)
                            {
                                _croppedImageView.Visibility = ViewStates.Visible;
                                _imageView.Visibility = ViewStates.Invisible;
                                _viewFinder.Visibility = ViewStates.Invisible;
                                _croppedImageView.SetImageBitmap(_croppedImage);
                            }
                            else
                            {
                                _imageView.Visibility = ViewStates.Visible;
                                _viewFinder.Visibility = ViewStates.Visible;
                                _croppedImageView.Visibility = ViewStates.Invisible;
                                _imageView.SetImageBitmap(bitmap);
                            }

                            UpdateFps(Fps);
                        });
                    }
                    else
                    {
                        imageProxy.Close();
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

        private static Bitmap TransformImage(Bitmap bitmap, int previewWidth, int previewHeight)
        {
            // Calculate the aspect ratio of the original processedBitmap
            float aspectRatio = (float)bitmap.Width / (float)bitmap.Height;

            // Calculate the new dimensions for the scaled image while maintaining the aspect ratio
            int newWidth, newHeight;
            if (aspectRatio > 1)
            {
                // Landscape orientation
                newWidth = previewWidth;
                newHeight = (int)(previewWidth / aspectRatio);
            }
            else
            {
                // Portrait orientation or square
                newHeight = previewHeight;
                newWidth = (int)(previewHeight * aspectRatio);
            }

            // Create a scaled bitmap using the new dimensions
            return Bitmap.CreateScaledBitmap(bitmap, newWidth, newHeight, true);
        }

        private void UpdateFps(double fps)
        {
            _fpsTextView.Text = $"FPS: {fps:F2}";
        }

        private long LastTimestamp { get; set; } = 0;

        private int FrameCount { get; set; } = 0;

        private double Fps { get; set; } = 0;
        
        //TODO - 1) Move Image To Mat Converter logic to DocumentAnalyzer class
        private Bitmap OpenCvHelper(Image image, IImageProxy imageProxy)
        {
            var oMat = ColorspaceConversionHelper.ImageToMat(image);
            imageProxy.Close();
            //var filteredMat = oMat;
            var filteredMat = _cannyImageDetector.Update(oMat);

            if (_captureClicked)
            {
                _pauseAnalysis = true;
            }

            var bitmapFiltered = 
                Bitmap.CreateBitmap(
                    filteredMat.Width(), filteredMat.Height(),
                    Bitmap.Config.Argb8888
                );
            
            //if our bounding box is unstable, we can get an exception here
            try
            {
                Utils.MatToBitmap(filteredMat, bitmapFiltered);
            }
            catch (Exception e)
            {
                _pauseAnalysis = false;
                _captureClicked = false;
                Console.WriteLine(e);
            }
            
             // Apply the original orientation transformation to the bitmap
             Matrix matrix = new Matrix();
             matrix.PostRotate(90);
             
             Bitmap rotatedBitmap = Bitmap.CreateBitmap(bitmapFiltered, 0, 0, filteredMat.Width(), filteredMat.Height(), matrix, true);
             
             return rotatedBitmap;
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
            if (_captureClicked)
            {
                _captureClicked = false;
                _pauseAnalysis = false;
            }
            else _captureClicked = true;

            // Get a stable reference of the modifiable image capture use case   
            var imageCapture = _imageCapture;
            if (imageCapture == null)
                return;

            // Create time-stamped output file to hold the image
            var photoFile = new File(_outputDirectory, new Java.Text.SimpleDateFormat(FilenameFormat, Locale.Us).Format(JavaSystem.CurrentTimeMillis()) + ".jpg");

            // Create output options object which contains file + metadata
            var outputOptions = new ImageCapture.OutputFileOptions.Builder(photoFile).Build();

            //Access ImageCapture memory buffer
            imageCapture.TakePicture(ContextCompat.GetMainExecutor(this), new ImageCapturedCallback(
                
                onErrorCallback: (exc) =>
                {
                    var msg = $"Photo capture failed: {exc.Message}";
                    Log.Error(Tag, msg, exc);
                    Toast.MakeText(this.BaseContext, msg, ToastLength.Short).Show();
                },
                
                onCapturedSuccessCallback: (output) =>
                {
                    var boundingBox = _cannyImageDetector.GetCroppingBoundingBox();
                    if (boundingBox != null)
                    {
                        // Get the original bitmap
                        Bitmap originalBitmap = output;
                        
                        //Calculate scaling factor
                        var heightScaling = output.Height / _height;
                        var widthScaling = output.Width / _width;
                        
                        // Calculate the width and height of the bounding box
                        int cropX = boundingBox.X * heightScaling;
                        int cropY = boundingBox.Y * widthScaling;
                        int cropWidth = boundingBox.Width * widthScaling;
                        int cropHeight = boundingBox.Height * heightScaling;

                        // Ensure that the cropping area is within the bounds of the original bitmap
                        cropX = Math.Max(0, cropX);
                        cropY = Math.Max(0, cropY);
                        cropWidth = Math.Min(originalBitmap.Width - cropX, cropWidth);
                        cropHeight = Math.Min(originalBitmap.Height - cropY, cropHeight);

                        // Create a new bitmap with the dimensions of the bounding box
                        Bitmap croppedBitmap = Bitmap.CreateBitmap(originalBitmap, cropX, cropY, cropWidth, cropHeight);
                        _croppedImage = croppedBitmap;
                        Matrix matrix = new Matrix();
                        matrix.PostRotate(90); // Rotate by 90 degrees

                        Bitmap rotatedBitmap = Bitmap.CreateBitmap(_croppedImage, 0, 0, _croppedImage.Width, _croppedImage.Height, matrix, true);
    
                        // Update the reference to the rotated bitmap
                        _croppedImage = rotatedBitmap;
                        
                        //Implement logic to save picture within this method or use the imagesavedcallback
                        // 'croppedBitmap' now contains the cropped portion of the original bitmap
                    }
                }
            ));
            
            // Set up image capture listener, which is triggered after photo has been taken
            // imageCapture.TakePicture(outputOptions, ContextCompat.GetMainExecutor(this), new ImageSaveCallback(
            //
            //     onErrorCallback: (exc) =>
            //     {
            //         var msg = $"Photo capture failed: {exc.Message}";
            //         Log.Error(Tag, msg, exc);
            //         Toast.MakeText(this.BaseContext, msg, ToastLength.Short).Show();
            //     },
            //
            //     onImageSaveCallback: (output) =>
            //     {
            //         var savedUri = output.SavedUri;
            //         var msg = $"Photo capture succeeded: {savedUri}";
            //         Log.Debug(Tag, msg);
            //         Toast.MakeText(this.BaseContext, msg, ToastLength.Short).Show();
            //     }
            // ));
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