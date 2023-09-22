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
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Net;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.ConstraintLayout.Widget;
using AndroidX.ExifInterface.Media;
using CameraX.Handlers;
using CameraX.Helpers;
using Java.IO;
using OpenCV.Android;
using OpenCV.Core;
using Bitmap = Android.Graphics.Bitmap;
using Console = System.Console;
using File = Java.IO.File;
using Image = Android.Media.Image;
using Matrix = Android.Graphics.Matrix;

namespace CameraX
{
    /// <summary>
    /// https://codelabs.developers.google.com/codelabs/camerax-getting-started#0
    /// </summary>

    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private const string Tag = "Android Document Scanner";
        private const int RequestCodePermissions = 10;
        private const string FilenameFormat = "yyyy-MM-dd-HH-mm-ss-SSS";
        private bool _pauseAnalysis = false;
        private BaseLoaderCallback _callback;
        private int _width;
        private int _height;
        private bool _captureClicked;
        private Bitmap _croppedImage;
        private bool _flashOn = false;
        
        private ICamera _processCameraProvider;
        private ImageCapture _imageCapture;
        private File _outputDirectory;
        private IExecutorService _cameraExecutor;
        private IImageProxy _imageProxy;
        private ImageView _imageView;
        private ImageView _croppedImageView;
        private PreviewView _viewFinder;
        private TextView _fpsTextView;
        private Button _cameraCaptureButton;
        private ImageView _cameraWireIcon;
        private Button _clearButton;
        private ImageView _clearIcon;
        private Button _imageSaveButton;
        private ImageView _saveIcon;
        private Button _flashSwitch;
        
        private void UpdateFps(double fps)
        {
            _fpsTextView.Text = $"FPS: {fps:F2}";
        }
        private long LastTimestamp { get; set; } = 0;

        private int FrameCount { get; set; } = 0;

        private double Fps { get; set; } = 0;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            base.RequestedOrientation = ScreenOrientation.Portrait;
            
            _viewFinder = FindViewById<PreviewView>(Resource.Id.viewFinder);
            _fpsTextView = FindViewById<TextView>(Resource.Id.fpsTextView);
            _imageView = FindViewById<ImageView>(Resource.Id.imageView);
            _croppedImageView = FindViewById<ImageView>(Resource.Id.croppedImageView);
            
            var flashSwitch = FindViewById<Button>(Resource.Id.flashSwitch);
            var cameraCaptureButton = FindViewById<Button>(Resource.Id.camera_capture_button);
            var imageSaveButton = FindViewById<Button>(Resource.Id.save_button);
            var clearButton = FindViewById<Button>(Resource.Id.clear_button);
            var clearIcon = FindViewById<ImageView>(Resource.Id.clear_icon);
            var saveIcon = FindViewById<ImageView>(Resource.Id.save_icon);
            var cameraWireIcon = FindViewById<ImageView>(Resource.Id.camera_wire);

            _flashSwitch = flashSwitch;
            _cameraCaptureButton = cameraCaptureButton;
            _imageSaveButton = imageSaveButton;
            _clearButton = clearButton;
            _clearIcon = clearIcon;
            _saveIcon = saveIcon;
            _cameraWireIcon = cameraWireIcon;

            // Request camera permissions   
            string[] permissions = new string[] { Manifest.Permission.Camera, Manifest.Permission.WriteExternalStorage };
            if (permissions.FirstOrDefault(x => ContextCompat.CheckSelfPermission(this, x) != Android.Content.PM.Permission.Granted) != null) //   ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) == Android.Content.PM.Permission.Granted)
                ActivityCompat.RequestPermissions(this, permissions, RequestCodePermissions);
            else
                StartCamera();
            
            // Set up the listener for take photo button
            cameraCaptureButton.SetOnClickListener(new OnClickListener(TakePhoto));
            clearButton.SetOnClickListener(new OnClickListener(TakePhoto));
            imageSaveButton.SetOnClickListener(new OnClickListener(ScanButton_Click));
            flashSwitch.SetOnClickListener(new OnClickListener(flashToggle_click));
            
            // Set up the touch event handler for your Tap to Focus
            _viewFinder.Touch += ViewFinderOnTouch;
            
            _outputDirectory = GetOutputDirectory();
            _cameraExecutor = Executors.NewSingleThreadExecutor();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _cameraExecutor.Shutdown();
        }

        private void ViewFinderOnTouch(object sender, View.TouchEventArgs e)
        {
            if (e.Event.Action == MotionEventActions.Down)
            {
                float x = e.Event.GetX();
                float y = e.Event.GetY();

                OnTouch(x, y);
            }
        }
        
        //Tap-to-Focus
        private void OnTouch(float x, float y)
        {
            DisplayOrientedMeteringPointFactory factory = new DisplayOrientedMeteringPointFactory(
                _viewFinder.Display,
                _processCameraProvider.CameraInfo,
                _viewFinder.Width,
                _viewFinder.Height
            );
    
            MeteringPoint meteringPoint = factory.CreatePoint(x, y);

            // Prepare focus action to be triggered.
            FocusMeteringAction action = new FocusMeteringAction.Builder(meteringPoint).Build();

            // Execute focus action.
            _processCameraProvider.CameraControl.StartFocusAndMetering(action);
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
                preview.SetSurfaceProvider(_viewFinder.SurfaceProvider);
                
                // Take Photo
                _imageCapture = new ImageCapture.Builder()
                    .Build();

                // Frame by frame analyze
                var imageAnalyzer = new ImageAnalysis.Builder()
                    .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                    .SetOutputImageFormat(ImageAnalysis.OutputImageFormatRgba8888)
                    .Build();
                
                imageAnalyzer.SetAnalyzer(_cameraExecutor, new DocumentAnalyzer(imageProxy =>
                {
                    if (!_pauseAnalysis)
                    {
                        // Get the dimensions of the PreviewView
                        var image = imageProxy.Image;
                        _imageProxy = imageProxy;
                        _height = image.Height;
                        _width = image.Width;

                        var currentTimestamp = JavaSystem.CurrentTimeMillis();
                        FrameCount++;

                        if (currentTimestamp - LastTimestamp >= 1000) // Calculate FPS every second
                        {
                            Fps = FrameCount / ((currentTimestamp - LastTimestamp) / 1000.0);
                            FrameCount = 0;
                            LastTimestamp = currentTimestamp;
                            Log.Debug("Debug", $"Current FPS data: {Fps}");
                        }
                        
                        var bitmap = OpenCvHelper(image);
                        imageProxy.Close();
     
                        RunOnUiThread(() =>
                        {
                            _cameraCaptureButton.Visibility = ViewStates.Visible;
                            _cameraWireIcon.Visibility = ViewStates.Visible;
                            
                            _imageSaveButton.Visibility = ViewStates.Gone;
                            _saveIcon.Visibility = ViewStates.Gone;
                                
                            _clearButton.Visibility = ViewStates.Gone;
                            _clearIcon.Visibility = ViewStates.Gone;
                            
                            _flashSwitch.Visibility = ViewStates.Visible;
                            _imageView.Visibility = ViewStates.Visible;
                            _viewFinder.Visibility = ViewStates.Visible;
                            _croppedImageView.Visibility = ViewStates.Invisible;
                            _imageView.SetImageBitmap(bitmap);

                            UpdateFps(Fps);
                        });
                    }
                    else
                    {
                        imageProxy.Close();
                        
                        // We need this timeout for the TakePhoto() to complete processing
                        // and assign the latest cropped bitmap to _croppedImage
                        SystemClock.Sleep(150);
                        
                        RunOnUiThread(() =>
                        {
                            if (!_captureClicked) return;
                            
                            //Turn off flash in-case its on
                            _imageCapture.Camera.CameraControl.EnableTorch(false);
                            _flashSwitch.SetBackgroundResource(Resource.Drawable.flash_off_24px);
                            
                            _cameraCaptureButton.Visibility = ViewStates.Gone;
                            _cameraWireIcon.Visibility = ViewStates.Gone;
                            
                            _imageSaveButton.Visibility = ViewStates.Visible;
                            _saveIcon.Visibility = ViewStates.Visible;
                                
                            _clearButton.Visibility = ViewStates.Visible;
                            _clearIcon.Visibility = ViewStates.Visible;
                                
                            _flashSwitch.Visibility = ViewStates.Invisible;
                            
                            _croppedImageView.Visibility = ViewStates.Visible;
            
                            _imageView.Visibility = ViewStates.Invisible;
                            _viewFinder.Visibility = ViewStates.Invisible;
                            _croppedImageView.SetImageBitmap(_croppedImage);
                        });
                    }
                }));
                
                // Select back camera as a default, or front camera otherwise
                CameraSelector cameraSelector = CameraSelector.DefaultBackCamera;
                
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
                    _processCameraProvider = cameraProvider.BindToLifecycle(this, cameraSelector, preview, _imageCapture, imageAnalyzer);
                }
                catch (Exception exc)
                {
                    Log.Debug(Tag, "Use case binding failed", exc);
                    Toast.MakeText(this, $"Use case binding failed: {exc.Message}", ToastLength.Short).Show();
                }

            }), ContextCompat.GetMainExecutor(this)); //GetMainExecutor: returns an Executor that runs on the main thread.
        }

        private Bitmap OpenCvHelper(Image image)
        {
            var oMat = ColorspaceConversionHelper.Rgba8888ToMat(image);
            
            //close image proxy to release analysis frame
            _imageProxy?.Close();
            
            var filteredMat = CannyImageDetector.Update(oMat);

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
        
        private void OpenCvAnalyzeFullResOutputImage(Bitmap finalImage)
        {
            var oMat = new Mat();
            Utils.BitmapToMat(finalImage, oMat);
            
            CannyImageDetector.Update(oMat);
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
            
            //Access ImageCapture memory buffer
            imageCapture?.TakePicture(ContextCompat.GetMainExecutor(this), new ImageCapturedCallback(
                
                onErrorCallback: (exc) =>
                {
                    var msg = $"Photo capture failed: {exc.Message}";
                    Log.Error(Tag, msg, exc);
                    Toast.MakeText(this.BaseContext, msg, ToastLength.Short).Show();
                },
                
                onCapturedSuccessCallback: (output) =>
                {

                    OpenCvAnalyzeFullResOutputImage(output);
                    var boundingBox = CannyImageDetector.GetCroppingBoundingBox();
                    if (boundingBox != null)
                    {
                        // 'croppedBitmap' now contains the cropped portion of the original bitmap
                        _croppedImage = ImageTransformationHelper.CropOutputImageWoTransform(output, boundingBox);
                    }
                }
            ));
        }
        
        private void ScanButton_Click()
        {
            Uri originalUri;
            Bitmap capturedBitmap = _croppedImage;
            // Get a stable reference of the modifiable image capture use case   
            var imageCapture = _imageCapture;
            
            if (capturedBitmap != null)
            {
                // Create time-stamped output file to hold the image
                var croppedPhotoFile = 
                    new File(_outputDirectory, new Java.Text.SimpleDateFormat(FilenameFormat, Locale.Us).Format(JavaSystem.CurrentTimeMillis()) + ".jpg");
                
                var originalPhotoFile = 
                    new File(_outputDirectory, new Java.Text.SimpleDateFormat(FilenameFormat, Locale.Us).Format(JavaSystem.CurrentTimeMillis()) + "_Org.jpg");

                try
                {
                    // Create a FileOutputStream from the photoFile
                    var fileOutputStream = new System.IO.FileStream(croppedPhotoFile.Path, System.IO.FileMode.Create);

                    // Save the custom Bitmap to the specified file
                    capturedBitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, fileOutputStream);

                    // Close the stream
                    fileOutputStream.Close();
                    
                    // Create output options object which contains file + metadata
                    var outputOptions = new ImageCapture.OutputFileOptions.Builder(originalPhotoFile).Build();
                    
                    // Now, you can pass the outputOptions to the ImageCapture
                    imageCapture.TakePicture(outputOptions, ContextCompat.GetMainExecutor(this), new ImageSaveCallback(
                    
                        onErrorCallback: (exc) =>
                        {
                            var msg = $"Photo capture failed: {exc.Message}";
                            Log.Error(Tag, msg, exc);
                            Toast.MakeText(this.BaseContext, msg, ToastLength.Short).Show();
                        },
                    
                        onImageSaveCallback: (output) =>
                        {
                            originalUri = output.SavedUri;
                            var msg = $"Photo capture succeeded: {croppedPhotoFile}";
                            CloneExifData(originalPhotoFile, croppedPhotoFile, originalUri);
                            Log.Debug(Tag, msg);
                            Toast.MakeText(BaseContext, msg, ToastLength.Short).Show();
                        }
                    ));
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur while saving the file
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            else
            {
                // Handle the case where capturedBitmap is null
                Toast.MakeText(BaseContext, "No Saved Bitmap in Memory", ToastLength.Short).Show();
            }
        }

        private static void CloneExifData(File originalPhotoFile, File croppedPhotoFile, Uri originalUri)
        {
            // Open the image file with ExifInterface
            var exif = new ExifInterface(croppedPhotoFile.Path);
            var originalExif = new ExifInterface(originalPhotoFile.Path);
            
            // Define an array of common EXIF tags you want to copy
            string[] commonExifTags = {
                ExifInterface.TagDatetimeOriginal,
                ExifInterface.TagPixelXDimension,
                ExifInterface.TagPixelYDimension,
                ExifInterface.TagApertureValue,
                ExifInterface.TagBrightnessValue,
                ExifInterface.TagLensModel,
                ExifInterface.TagMake,
                ExifInterface.TagGpsTrack,
                ExifInterface.TagShutterSpeedValue,
                ExifInterface.TagDeviceSettingDescription,
                ExifInterface.TagLensMake,
                ExifInterface.TagExposureTime
                // Add other tags you want to copy here
            };

            // Loop through the common EXIF tags and copy their values
            foreach (var tag in commonExifTags)
            {
                var originalValue = originalExif.GetAttribute(tag);

                if (originalValue != null)
                {
                    // Copy the original EXIF value to the new EXIF object
                    exif.SetAttribute(tag, originalValue);
                }
            }
            // Save the updated EXIF data
            exif.SaveAttributes();
        }

        private void flashToggle_click()
        {
            if (!_flashOn)
            {
                _flashOn = true;
                _imageCapture.Camera.CameraControl.EnableTorch(true);
                
                RunOnUiThread(() =>
                {
                    _flashSwitch.SetBackgroundResource(Resource.Drawable.flash_on_24px);
                });
            }
            else
            {
                _flashOn = false;
                _imageCapture.Camera.CameraControl.EnableTorch(false);
                
                RunOnUiThread(() =>
                {
                    _flashSwitch.SetBackgroundResource(Resource.Drawable.flash_off_24px);
                });
            };
        }
        
        // Save photos to => /Pictures/AndroidCameraXScanner/
        //TODO - Need to modify this to save to gallery or scoped storage since external storage is deprecated
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