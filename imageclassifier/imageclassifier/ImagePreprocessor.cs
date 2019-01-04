using System;
using System.Diagnostics;
using System.IO;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Java.IO;
using Java.Lang;
using Java.Nio;
using static Android.Graphics.Bitmap;
using Environment = Android.OS.Environment;
using Exception = Java.Lang.Exception;
using Math = Java.Lang.Math;

namespace imageclassifier
{
    internal class ImagePreprocessor
    {

        static bool SAVE_PREVIEW_BITMAP = false;

        Bitmap rgbFrameBitmap;
        Bitmap croppedBitmap;

        public ImagePreprocessor(int previewWidth, int previewHeight,
                                 int croppedwidth, int croppedHeight)
        {
            this.croppedBitmap = Bitmap.CreateBitmap(croppedwidth, croppedHeight, Bitmap.Config.Argb8888);
            this.rgbFrameBitmap = Bitmap.CreateBitmap(previewWidth, previewHeight, Bitmap.Config.Argb8888);
        }

        public Bitmap preprocessImage(Image image)
        {
            if (image == null)
            {
                return null;
            }

            System.Diagnostics.Debug.Assert(rgbFrameBitmap.Width == image.Width, "Invalid size width");
            System.Diagnostics.Debug.Assert(rgbFrameBitmap.Height == image.Height, "Invalid size height");

            if (croppedBitmap != null && rgbFrameBitmap != null)
            {
                ByteBuffer bb = image.GetPlanes()[0].Buffer;

                byte[] byteArray = new byte[bb.Remaining()];
                bb.Get(byteArray);
                rgbFrameBitmap = BitmapFactory.DecodeStream(new MemoryStream(byteArray));
                CropAndRescaleBitmap(rgbFrameBitmap, croppedBitmap, 0);
            }

            image.Close();

            // For debugging
            if (SAVE_PREVIEW_BITMAP)
            {
                SaveBitmap(croppedBitmap);
            }
            return croppedBitmap;
        }

        static void CropAndRescaleBitmap(Bitmap src, Bitmap dst,
                                             int sensorOrientation)
        {
            System.Diagnostics.Debug.Assert(dst.Width == dst.Height);

            float minDim = Math.Min(src.Width, src.Height);

            Matrix matrix = new Matrix();

            // We only want the center square out of the original rectangle.
            float translateX = -Math.Max(0, (src.Width - minDim) / 2);
            float translateY = -Math.Max(0, (src.Height - minDim) / 2);
            matrix.PreTranslate(translateX, translateY);

            float scaleFactor = dst.Height / minDim;
            matrix.PostScale(scaleFactor, scaleFactor);

            // Rotate around the center if necessary.
            if (sensorOrientation != 0)
            {
                matrix.PostTranslate(-dst.Width / 2.0f, -dst.Height / 2.0f);
                matrix.PostRotate(sensorOrientation);
                matrix.PostTranslate(dst.Width / 2.0f, dst.Height / 2.0f);
            }

            Canvas canvas = new Canvas(dst);
            canvas.DrawBitmap(src, matrix, null);
        }

        /*
       * Saves a Bitmap object to disk for analysis.
       *
       * @param bitmap The bitmap to save.
       */
        static void SaveBitmap(Bitmap bitmap)
        {
            string path = System.IO.Path.Combine(
            Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures).AbsolutePath, "tensorflow_preview.png");

            Log.Debug("ImageHelper", string.Format("Saving {0}x{1} bitmap to {2}.",
                bitmap.Width, bitmap.Height, path));

            System.IO.File.Delete(path);

            try
            {
                var fs = new FileStream(path, FileMode.OpenOrCreate);
                bitmap.Compress(Bitmap.CompressFormat.Png, 99, fs);
                fs.Close();

            }
            catch (Exception e)
            {
                Log.Warn("ImageHelper", "Could not save image for debugging", e);
            }
        }
    }
}
