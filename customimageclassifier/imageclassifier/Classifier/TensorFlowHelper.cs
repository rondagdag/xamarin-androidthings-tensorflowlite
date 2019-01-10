using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Util;
using Java.IO;
using Java.Lang;
using Java.Nio;
using Java.Nio.Channels;

namespace imageclassifier
{
    internal partial class TensorFlowHelper
    {

        static int RESULTS_TO_SHOW = 1;

        /*
         * Memory-map the model file in Assets.
         */
        public static MappedByteBuffer LoadModelFile(Context context, string modelFile)
        {
            AssetFileDescriptor fileDescriptor = context.Assets.OpenFd(modelFile);
            FileInputStream inputStream = new FileInputStream(fileDescriptor.FileDescriptor);
            FileChannel fileChannel = inputStream.Channel;
            long startOffset = fileDescriptor.StartOffset;
            long declaredLength = fileDescriptor.DeclaredLength;
            return fileChannel.Map(FileChannel.MapMode.ReadOnly, startOffset, declaredLength);
        }

        public static List<string> ReadLabels(Context context, string labelsFile)
        {
            AssetManager assetManager = context.Assets;
            List<string> result = new List<string>();
            try
            {
                using (var input = assetManager.Open(labelsFile))
                {
                    using (var sr = new StreamReader(input))
                    {
                        while (sr.Peek() >= 0)
                        {
                            result.Add(sr.ReadLine());
                        }
                    }
                }
                return result;
            }
            catch (System.IO.IOException)
            {
                throw new IllegalStateException("Cannot read labels from " + labelsFile);
            }
        }
        /*
         * Find the best classifications.
          */
        public static List<Recognition> GetBestResults(byte[] labelProbArray,
                                                           List<string> labelList)
        {
            SortedList<float, Recognition> sortedLabels = new SortedList<float, Recognition>(new DescComparer<float>());

            for (int i = 0; i < labelList.Count; ++i)
            {
                var confidence = (labelProbArray[i] & 0xff) / 255.0f;
                Recognition r = new Recognition(i.ToString(),
                        labelList[i], confidence);
                sortedLabels.Add(r.GetConfidence(), r);
            }

            return sortedLabels.Values.Take(RESULTS_TO_SHOW).ToList();
        }
        /*
        * Find the best classifications for float values
         */
        public static List<Recognition> GetBestResults(float[] labelProbArray,
                                                           List<string> labelList)
        {
            SortedList<float, Recognition> sortedLabels = new SortedList<float, Recognition>(new DescComparer<float>());

            for (int i = 0; i < labelList.Count; ++i)
            {
                var confidence = (labelProbArray[i]);
                Recognition r = new Recognition(i.ToString(),
                        labelList[i], confidence);
                sortedLabels.Add(r.GetConfidence(), r);
            }

            return sortedLabels.Values.Take(RESULTS_TO_SHOW).Where(v => v.GetConfidence() >= 0.75f).ToList();
        }

        /* Writes Image data into a {@code ByteBuffer}. */
        public static void ConvertBitmapToByteBuffer(Bitmap bitmap, int[] intValues, ByteBuffer imgData)
        {
            if (imgData == null)
            {
                return;
            }
            imgData.Rewind();
            bitmap.GetPixels(intValues, 0, bitmap.Width, 0, 0,
                    bitmap.Width, bitmap.Height);
            // Encode the image pixels into a byte buffer representation matching the expected
            // input of the Tensorflow model

            for (int i = 0; i < intValues.Length; ++i)
            {
                var val = intValues[i];

                /*imgData.PutFloat(((val & 0xFF) - 104));
                imgData.PutFloat((((val >> 8) & 0xFF) - 117));
                imgData.PutFloat((((val >> 16) & 0xFF) - 123));*/
                imgData.PutFloat(((val & 0xFF) - 104));
                imgData.PutFloat((((val >> 8) & 0xFF) - 117));
                imgData.PutFloat((((val >> 16) & 0xFF) - 123));
            }

        }
    }
}