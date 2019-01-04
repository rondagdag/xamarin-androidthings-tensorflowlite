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

        static int RESULTS_TO_SHOW = 3;

        /*
         * Memory-map the model file in Assets.
         */
        public static MappedByteBuffer LoadModelFile(Context context, string modelFile)
        {
            //var res = context.Resources.OpenRawResource(Resource.Raw.mobilenet_quant_v1_224);

            //AssetFileDescriptor fileDescriptor = context.Resources.OpenRawResourceFd(Resource.Raw.mobilenet_quant_v1_224);

            AssetFileDescriptor fileDescriptor = context.Assets.OpenFd(modelFile);
            FileInputStream inputStream = new FileInputStream(fileDescriptor.FileDescriptor);
            FileChannel fileChannel = inputStream.Channel;
            long startOffset = fileDescriptor.StartOffset;
            long declaredLength = fileDescriptor.DeclaredLength;
            return fileChannel.Map(FileChannel.MapMode.ReadOnly, startOffset, declaredLength);
        }

        /*public static ByteBuffer LoadModelFile(Context context, string modelFile)
        {
            var assets = context.Assets;
            Java.Nio.ByteBuffer b;

            using (BufferedStream sourceStream = new BufferedStream(assets.Open(modelFile)))
            {
                using (var memoryStream = new MemoryStream())
                {
                    sourceStream.CopyTo(memoryStream);
                    byte[] a = memoryStream.ToArray();
                    b = Java.Nio.ByteBuffer.Wrap(a);
                    b.Order(ByteOrder.NativeOrder());

                }
            }
            return b;
        }*/


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
        /*public static List<Recognition> GetBestResults(byte[][] labelProbArray,
                                                             List<string> labelList)
        {
            SortedList<float, Recognition> sortedLabels = new SortedList<float, Recognition>(new DescComparer<float>());
            for (int i = 0; i < labelList.Count; ++i)
            {
                Recognition r = new Recognition(i.ToString(),
                        labelList[i], (labelProbArray[0][i] & 0xff) / 255.0f);
                sortedLabels.Add(r.GetConfidence(), r);
            }

            return sortedLabels.Values.Take(RESULTS_TO_SHOW).ToList();
        }*/

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
            int pixel = 0;
            for (int i = 0; i < bitmap.Width; ++i)
            {
                for (int j = 0; j < bitmap.Height; ++j)
                {
                    int val = intValues[pixel++];
                    imgData.Put((sbyte)((val >> 16) & 0xFF));
                    imgData.Put((sbyte)((val >> 8) & 0xFF));
                    imgData.Put((sbyte)(val & 0xFF));
                }
            }
        }
    }
}