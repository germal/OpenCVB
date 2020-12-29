using System;
using cv = OpenCvSharp;

namespace CS_Classes
{
    public class OilPaintManual
    {
        private static byte ClipByte(double colour)
        {
            return (byte)(colour > 255 ? 255 : (colour < 0 ? 0 : colour));
        }

        public void Start(cv.Mat color, cv.Mat result1, int filterSize, int levels)
        {
            int[] intensityBin = new int[levels];

            int filterOffset = (filterSize - 1) / 2;
            int currentIntensity = 0, maxIntensity = 0, maxIndex = 0;

            for (int offsetY = filterOffset; offsetY < color.Height - filterOffset; offsetY++)
            {
                for (int offsetX = filterOffset; offsetX < color.Width - filterOffset; offsetX++)
                {
                    maxIntensity = maxIndex = 0;

                    intensityBin = new int[levels];
                    cv.Vec3i[] bins = new cv.Vec3i[levels];

                    for (int y = offsetY - filterOffset; y < offsetY + filterOffset; y++)
                    {
                        for (int x = offsetX - filterOffset; x < offsetX + filterOffset; x++)
                        {
                            cv.Vec3b rgb = color.Get<cv.Vec3b>(y, x);
                            currentIntensity = (int)(Math.Round((Double)(rgb.Item0 + rgb.Item1 + rgb.Item2) / 3.0 * (levels - 1)) / 255.0);

                            intensityBin[currentIntensity] += 1;
                            bins[currentIntensity].Item0 += rgb.Item0;
                            bins[currentIntensity].Item1 += rgb.Item1;
                            bins[currentIntensity].Item2 += rgb.Item2;

                            if (intensityBin[currentIntensity] > maxIntensity)
                            {
                                maxIntensity = intensityBin[currentIntensity];
                                maxIndex = currentIntensity;
                            }
                        }
                    }

                    if (maxIntensity == 0) maxIntensity = 1;
                    double blue = bins[maxIndex].Item0 / maxIntensity;
                    double green = bins[maxIndex].Item1 / maxIntensity;
                    double red = bins[maxIndex].Item2 / maxIntensity;

                    result1.Set<cv.Vec3b>(offsetY, offsetX, new cv.Vec3b(ClipByte(blue), ClipByte(green), ClipByte(red)));
                }
            }
        }
    }
}
