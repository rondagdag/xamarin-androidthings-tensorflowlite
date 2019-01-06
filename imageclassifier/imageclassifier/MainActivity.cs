using Android.App;
using Android.Widget;
using Android.OS;
using Google.Android.Things.Contrib.Driver.Button;
using Xamarin.TensorFlow.Lite;
using System.Collections.Generic;
using Java.IO;
using Android.Util;
using Android.Graphics;
using Java.Nio;
using Android.Media;
using Google.Android.Things.Contrib.Driver.Rainbowhat;
using Android.Views;
using static Android.Resource;
using Java.Lang;
using System.Linq;
using Android.Runtime;
using static Android.Media.ImageReader;
using System;
using Java.Util;

namespace imageclassifier
{

    [Activity(Label = "imageclassifier", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        static string TAG = "ImageClassifierActivity";

        /** Camera image capture size */
        static int PREVIEW_IMAGE_WIDTH = 640;
        static int PREVIEW_IMAGE_HEIGHT = 480;
        /** Image dimensions required by TF model */
        static int TF_INPUT_IMAGE_WIDTH = 224;
        static int TF_INPUT_IMAGE_HEIGHT = 224;

        //static int TF_INPUT_IMAGE_WIDTH = 227;
        //static int TF_INPUT_IMAGE_HEIGHT = 227;
        /** Dimensions of model inputs. */
        static int DIM_BATCH_SIZE = 1;
        static int DIM_PIXEL_SIZE = 3;
        /** TF model asset files */
        static string LABELS_FILE = "labels.txt";
        static string MODEL_FILE = "mobilenet_quant_v1_224.tflite";

        //static string LABELS_FILE = "optimizedgraphlabels.txt";
        //static string MODEL_FILE = "optimized_graph_flowers.lite";

        //static string LABELS_FILE = "optimizedgraphlabels.txt";
        //static string MODEL_FILE = "optimized_graph.lite";

        ButtonInputDriver mButtonDriver;
        bool mProcessing;

        ImageView mImage;
        TextView mResultText;

        Interpreter mTensorFlowLite;
        List<string> mLabels;
        CameraHandler mCameraHandler;
        ImagePreprocessor mImagePreprocessor;


        /*
         * Initialize the classifier that will be used to process images.
         */
        void InitClassifier()
        {
            try
            {
                mTensorFlowLite = new Interpreter(TensorFlowHelper.LoadModelFile(this, MODEL_FILE),2);
                mLabels = TensorFlowHelper.ReadLabels(this, LABELS_FILE);
            }
            catch (IOException e)
            {
                Log.Warn(TAG, "Unable to initialize TensorFlow Lite.", e);
            }
        }

        /*
         * Clean up the resources used by the classifier.
         */
        void DestroyClassifier()
        {
            mTensorFlowLite.Close();
        }


        /*
     * Process an image and identify what is in it. When done, the method
     * {@link #onPhotoRecognitionReady(Collection)} must be called with the results of
     * the image recognition process.
     *
     * @param image Bitmap containing the image to be classified. The image can be
     *              of any size, but preprocessing might occur to resize it to the
     *              format expected by the classification process, which can be time
     *              and power consuming.
     */
        void DoRecognize(Bitmap image)
        {
            // Allocate space for the inference results
            var count = mLabels.Count;

            // Allocate buffer for image pixels.
            int[] intValues = new int[TF_INPUT_IMAGE_WIDTH * TF_INPUT_IMAGE_HEIGHT];
            //ByteBuffer imgData = ByteBuffer.AllocateDirect(
            //        4 * DIM_BATCH_SIZE * TF_INPUT_IMAGE_WIDTH * TF_INPUT_IMAGE_HEIGHT * DIM_PIXEL_SIZE);
            ByteBuffer imgData = ByteBuffer.AllocateDirect(
                    DIM_BATCH_SIZE * TF_INPUT_IMAGE_WIDTH * TF_INPUT_IMAGE_HEIGHT * DIM_PIXEL_SIZE);
            imgData.Order(ByteOrder.NativeOrder());

            // Read image data into buffer formatted for the TensorFlow model
            TensorFlowHelper.ConvertBitmapToByteBuffer(image, intValues, imgData);

            // Run inference on the network with the image bytes in imgData as input,
            // storing results on the confidencePerLabel array.
            //ByteBuffer confidenceByteBuffer = ByteBuffer.Allocate(count);
            //mTensorFlowLite.Run(imgData, confidenceByteBuffer);
            //byte[] confidenceByteArray = ConvertResults(confidenceByteBuffer);

            //var confidenceBuffer = FloatBuffer.Allocate(4 * count);


            byte[][] confidence = new byte[1][];
            confidence[0] = new byte[count];

            var conf = Java.Lang.Object.FromArray<byte[]>(confidence);
            mTensorFlowLite.Run(imgData, conf);

            confidence = conf.ToArray<byte[]>();
            List<Recognition> results = TensorFlowHelper.GetBestResults(confidence[0], mLabels);


            /*float[][] confidence = new float[1][];
            confidence[0] = new float[count];

            var conf = Java.Lang.Object.FromArray<float[]>(confidence);
            mTensorFlowLite.Run(imgData, conf);

            confidence = conf.ToArray<float[]>();
            List<Recognition> results = TensorFlowHelper.GetBestResults(confidence[0], mLabels);
            */
            //float[] confidenceArray = ConvertResults(confidenceBuffer.AsFloatBuffer());
            //float[] confidenceByteArray = ConvertResultsFloat(confidenceBuffer, count);

            // Get the results with the highest confidence and map them to their labels
            //List<Recognition> results = TensorFlowHelper.GetBestResults(confidenceArray, mLabels);
            //List<Recognition> results = TensorFlowHelper.GetBestResults(confidenceByteArray, mLabels);
            //List<Recognition> results = TensorFlowHelper.GetBestResults(confidencePerLabel, mLabels);

            // Report the results with the highest confidence
            OnPhotoRecognitionReady(results);
        }

        private static byte[] ConvertResults(ByteBuffer outputData)
        {
            outputData.Rewind();
            byte[] arr = new byte[outputData.Remaining()];
            outputData.Get(arr);
            return arr;
        }

        private static float[] ConvertResultsFloat(ByteBuffer outputData, int count)
        {
            outputData.Rewind();
            float[] arr = new float[count];
            outputData.AsFloatBuffer().Get(arr);
            return arr;
        }

        class CameraOnImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
        {
            private MainActivity mainActivity;

            public CameraOnImageAvailableListener(MainActivity mainActivity)
            {
                this.mainActivity = mainActivity;
            }

            public void OnImageAvailable(ImageReader reader)
            {
                Bitmap bitmap = mainActivity.mImagePreprocessor.preprocessImage(reader.AcquireNextImage());
                mainActivity.OnPhotoReady(bitmap);
            }
        }
        /*
     * Initialize the camera that will be used to capture images.
     */
        private void InitCamera()
        {
            mImagePreprocessor = new ImagePreprocessor(PREVIEW_IMAGE_WIDTH, PREVIEW_IMAGE_HEIGHT,
                    TF_INPUT_IMAGE_WIDTH, TF_INPUT_IMAGE_HEIGHT);
            mCameraHandler = CameraHandler.GetInstance();

            mCameraHandler.InitializeCamera(this,
                    PREVIEW_IMAGE_WIDTH, PREVIEW_IMAGE_HEIGHT, null, new CameraOnImageAvailableListener(this));

        }

        /*
     * Clean up resources used by the camera.
     */
        private void CloseCamera()
        {
            mCameraHandler.ShutDown();
        }

        /*
         * Load the image that will be used in the classification process.
         * When done, the method {@link #onPhotoReady(Bitmap)} must be called with the image.
         */
        private void LoadPhoto()
        {
            mCameraHandler.TakePicture();
        }


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Window.AddFlags(Android.Views.WindowManagerFlags.KeepScreenOn);

            SetContentView(Resource.Layout.activity_camera);
            mImage = (ImageView)FindViewById(Resource.Id.imageView);
            mResultText = (TextView)FindViewById(Resource.Id.resultText);

            UpdateStatus(GetString(Resource.String.initializing));
            InitCamera();
            InitClassifier();
            InitButton();
            UpdateStatus(GetString(Resource.String.help_message));
        }

        /*
    * Register a GPIO button that, when clicked, will generate the {@link KeyEvent#KEYCODE_ENTER}
    * key, to be handled by {@link #onKeyUp(int, KeyEvent)} just like any regular keyboard
    * event.
    *
    * If there's no button connected to the board, the doRecognize can still be triggered by
    * sending key events using a USB keyboard or `adb shell input keyevent 66`.
    */
        private void InitButton()
        {
            try
            {
                mButtonDriver = RainbowHat.CreateButtonCInputDriver((int)Keycode.Enter);
                mButtonDriver.Register();
            }
            catch (IOException e)
            {
                Log.Warn(TAG, "Cannot find button. Ignoring push button. Use a keyboard instead.", e);
            }
        }

        public override bool OnKeyUp([GeneratedEnum] Keycode keyCode, KeyEvent keyEvent)
        {
            if (keyCode == Keycode.Enter)
            {
                if (mProcessing)
                {
                    UpdateStatus("Still processing, please wait");
                    return true;
                }
                UpdateStatus("Running photo recognition");
                mProcessing = true;
                LoadPhoto();
                return true;
            }
            return base.OnKeyUp(keyCode, keyEvent);
        }


        /*
         * Image capture process complete
         */
        void OnPhotoReady(Bitmap bitmap)
        {
            mImage.SetImageBitmap(bitmap);
            DoRecognize(bitmap);
        }

        /*
     * Image classification process complete
     */
        void OnPhotoRecognitionReady(List<Recognition> results)
        {
            UpdateStatus(FormatResults(results));
            mProcessing = false;
        }

        /*
     * Format results list for display
     */
        string FormatResults(List<Recognition> results)
        {
            if (results == null || !results.Any())
            {
                return GetString(Resource.String.empty_result);
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                int counter = 0;
                foreach (var r in results)
                {
                    sb.Append(r.GetTitle());
                    counter++;
                    if (counter < results.Count - 1)
                    {
                        sb.Append(", ");
                    }
                    else if (counter == results.Count - 1)
                    {
                        sb.Append(" or ");
                    }
                }

                return sb.ToString();
            }
        }

        /**
         * Report updates to the display and log output
         */
        void UpdateStatus(string status)
        {
            Log.Debug(TAG, status);
            mResultText.Text = status;
        }

        ~MainActivity()
        {
            try
            {
                DestroyClassifier();
            }
            catch (Throwable t)
            {
                // close quietly
            }
            try
            {
                CloseCamera();
            }
            catch (Throwable t)
            {
                // close quietly
            }
            try
            {
                if (mButtonDriver != null) mButtonDriver.Close();
            }
            catch (Throwable t)
            {
                // close quietly
            }
        }
    }
}

