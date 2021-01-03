using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using cv = OpenCvSharp;
using sk = SkiaSharp;

// https://docs.microsoft.com/en-us/samples/xamarin/xamarin-forms-samples/skiasharpforms-demos/
namespace CS_Classes
{
    public class SkiaSharp_Hello
    {
        const string TEXT = "Hello, Bitmap!";
        SKBitmap helloBitmap;
        public void New()
        {
        }
        public cv.Mat Run(cv.Mat src)
        {
            // Create bitmap and draw on it
            using (SKPaint textPaint = new SKPaint { TextSize = 48 })
            {
                SKRect bounds = new SKRect();
                textPaint.MeasureText(TEXT, ref bounds);

                helloBitmap = new SKBitmap((int)bounds.Right, (int)bounds.Height);

                using (SKCanvas bitmapCanvas = new SKCanvas(helloBitmap))
                {
                    bitmapCanvas.Clear();
                    bitmapCanvas.DrawText(TEXT, 0, -bounds.Top, textPaint);
                }
            }
            return src;
        }
    }
}
