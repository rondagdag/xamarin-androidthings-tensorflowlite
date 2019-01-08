using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Java.Lang;
using Java.Util;

namespace imageclassifier
{

    internal class CameraHandler
    {
        static string TAG = nameof(CameraHandler);

        static int MAX_IMAGES = 1;
        CameraDevice _cameraDevice;
        CameraCaptureSession _captureSession;
        bool _initialized;

        /*
         * An {@link ImageReader} that handles still image capture.
         */
        private ImageReader mImageReader;

        private static class InstanceHolder
        {
            public static readonly CameraHandler _camera = new CameraHandler();
        }

        public static CameraHandler GetInstance()
        {
            return InstanceHolder._camera;
        }

        /*
         * Initialize the camera device
         */
        public void InitializeCamera(Context context, int previewWidth, int previewHeight,
                                 Handler backgroundHandler,
                                 ImageReader.IOnImageAvailableListener imageAvailableListener)
        {
            if (_initialized)
            {
                throw new IllegalStateException(
                        "CameraHandler is already initialized or is initializing");
            }
            _initialized = true;

            // Discover the camera instance
            CameraManager manager = (CameraManager)context.GetSystemService(Context.CameraService);
            string[] camIds = null;
            try
            {
                camIds = manager.GetCameraIdList();
            }
            catch (CameraAccessException e)
            {
                Log.Warn(TAG, "Cannot get the list of available cameras", e);
            }
            if (camIds == null || camIds.Length < 1)
            {
                Log.Debug(TAG, "No cameras found");
                return;
            }
            Log.Debug(TAG, "Using camera id " + camIds[0]);

            // Initialize the image processor
            mImageReader = ImageReader.NewInstance(previewWidth, previewHeight, ImageFormatType.Jpeg,
                    MAX_IMAGES);
            mImageReader.SetOnImageAvailableListener(imageAvailableListener, backgroundHandler);

            // Open the camera resource
            try
            {
                manager.OpenCamera(camIds[0], new CameraStateCallBack(this), backgroundHandler);
            }
            catch (CameraAccessException cae)
            {
                Log.Debug(TAG, "Camera access exception", cae);
            }
        }


        /*
         * Begin a still image capture
         */
        public void TakePicture()
        {
            if (_cameraDevice == null)
            {
                Log.Warn(TAG, "Cannot capture image. Camera not initialized.");
                return;
            }
            // Create a CameraCaptureSession for capturing still images.
            try
            {

                List<Surface> list = new List<Surface>() { mImageReader.Surface };
                //SessionConfiguration config = new SessionConfiguration()
                _cameraDevice.CreateCaptureSession(list, new CameraSessionCallback(this),
                        null);
            }
            catch (CameraAccessException cae)
            {
                Log.Error(TAG, "Cannot create camera capture session", cae);
            }
        }

        /**
         * Execute a new capture request within the active session
         */
        private void TriggerImageCapture()
        {
            try
            {
                CaptureRequest.Builder captureBuilder =
                        _cameraDevice.CreateCaptureRequest(CameraTemplate.StillCapture);
                captureBuilder.AddTarget(mImageReader.Surface);
                captureBuilder.Set(CaptureRequest.ControlAeMode, new Integer((int)ControlAEMode.On));
                Log.Debug(TAG, "Capture request created.");
                _captureSession.Capture(captureBuilder.Build(), new CameraCaptureCallback(this), null);
            }
            catch (CameraAccessException)
            {
                Log.Error(TAG, "Cannot trigger a capture request");
            }
        }

        private void CloseCaptureSession()
        {
            if (_captureSession != null)
            {
                try
                {
                    _captureSession.Close();
                }
                catch (Java.Lang.Exception ex)
                {
                    Log.Warn(TAG, "Could not close capture session", ex);
                }
                _captureSession = null;
            }
        }

        /*
         * Close the camera resources
         */
        public void ShutDown()
        {
            try
            {
                CloseCaptureSession();
                if (_cameraDevice != null)
                {
                    _cameraDevice.Close();
                }
                mImageReader.Close();
            }
            finally
            {
                _initialized = false;
            }
        }

        /*
         * Helpful debugging method:  Dump all supported camera formats to log.  You don't need to run
         * this for normal operation, but it's very helpful when porting this code to different
         * hardware.
         */
        public static void DumpFormatInfo(Context context)
        {
            // Discover the camera instance
            CameraManager manager = (CameraManager)context.GetSystemService(Context.CameraService);
            string[] camIds = null;
            try
            {
                camIds = manager.GetCameraIdList();
            }
            catch (CameraAccessException e)
            {
                Log.Warn(TAG, "Cannot get the list of available cameras", e);
            }
            if (camIds == null || camIds.Length < 1)
            {
                Log.Debug(TAG, "No cameras found");
                return;
            }
            Log.Debug(TAG, "Using camera id " + camIds[0]);
            try
            {
                CameraCharacteristics characteristics = manager.GetCameraCharacteristics(camIds[0]);
                StreamConfigurationMap configs = (StreamConfigurationMap)characteristics.Get(
                        CameraCharacteristics.ScalerStreamConfigurationMap);
                foreach (var format in configs.GetOutputFormats())
                {
                    Log.Debug(TAG, "Getting sizes for format: " + format);
                    foreach (var s in configs.GetOutputSizes(format))
                    {
                        Log.Debug(TAG, "\t" + s.ToString());
                    }
                }

                int[] effects = (int[])characteristics.Get(CameraCharacteristics.ControlAvailableEffects);
                foreach (var effect in effects)
                {
                    Log.Debug(TAG, "Effect available: " + effect);
                }
            }
            catch (CameraAccessException)
            {
                Log.Debug(TAG, "Cam access exception getting characteristics.");
            }
        }


        /*
         * Callback handling device state changes
         */
        //private readonly CameraDevice.StateCallback _stateCallback;

        private class CameraStateCallBack : CameraDevice.StateCallback
        {

            private CameraHandler cameraHandler;

            public CameraStateCallBack(CameraHandler cameraHandler)
            {
                this.cameraHandler = cameraHandler;
            }

            public override void OnDisconnected(CameraDevice camera)
            {
                Log.Debug(TAG, "Camera disconnected, closing.");
                cameraHandler.CloseCaptureSession();
                cameraHandler._cameraDevice.Close();
            }

            public override void OnError(CameraDevice camera, [GeneratedEnum] CameraError error)
            {
                throw new NotImplementedException();
            }

            public override void OnOpened(CameraDevice camera)
            {
                Log.Debug(TAG, "Opened camera.");
                cameraHandler._cameraDevice = camera;
            }
        }

        /*
         * Callback handling session state changes
         */
        //private readonly CameraCaptureSession.StateCallback _sessionCallback;

        private class CameraSessionCallback : CameraCaptureSession.StateCallback
        {
            private CameraHandler cameraHandler;

            public CameraSessionCallback(CameraHandler cameraHandler)
            {
                this.cameraHandler = cameraHandler;
            }

            public override void OnConfigured(CameraCaptureSession session)
            {
                if (cameraHandler._cameraDevice == null)
                {
                    return;
                }
                // When the session is ready, we start capture.
                cameraHandler._captureSession = session;
                cameraHandler.TriggerImageCapture();
            }

            public override void OnConfigureFailed(CameraCaptureSession session)
            {
                Log.Warn(TAG, "Failed to configure camera");
            }

        };

        /*
         * Callback handling capture session events
         */
       //private readonly CameraCaptureSession.CaptureCallback _captureCallback;

        private class CameraCaptureCallback : CameraCaptureSession.CaptureCallback
        {
            private CameraHandler cameraHandler;

            public CameraCaptureCallback(CameraHandler cameraHandler)
            {
                this.cameraHandler = cameraHandler;
            }

            public override void OnCaptureProgressed(CameraCaptureSession session, CaptureRequest request, CaptureResult partialResult)
            {
                //base.OnCaptureProgressed(session, request, partialResult);
                Log.Debug(TAG, "Partial result");
            }
            public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
            {
                //base.OnCaptureCompleted(session, request, result);
                session.Close();
                cameraHandler._captureSession = null;
                Log.Debug(TAG, "CaptureSession closed");
            }
        }
    }
}