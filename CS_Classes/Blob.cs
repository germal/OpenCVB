using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cv = OpenCvSharp;

namespace CS_Classes
{
    public class Blob_Basics
    {
        public void New() { }

        public void Start(cv.Mat input, cv.Mat dst, cv.SimpleBlobDetector.Params detectorParams)
        { 
            var binaryImage = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            cv.Cv2.Threshold(binaryImage, binaryImage, thresh: 100, maxval: 255, type: cv.ThresholdTypes.Binary);

            var simpleBlobDetector = cv.SimpleBlobDetector.Create(detectorParams);
            var keyPoints = simpleBlobDetector.Detect(binaryImage);

            cv.Cv2.DrawKeypoints(
                    image: binaryImage,
                    keypoints: keyPoints,
                    outImage: dst,
                    color: cv.Scalar.FromRgb(255, 0, 0),
                    flags: cv.DrawMatchesFlags.DrawRichKeypoints);
        }
    }
}
