using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Linq;

namespace Emgu_Classes
{
    public static class FaceDetection
    {
        public static void Detect(string inputName, string haarData)
        {
            Mat img = CvInvoke.Imread(inputName);
            var faceDetector = new CascadeClassifier(haarData);

            UMat imgGray = new UMat();
            CvInvoke.CvtColor(img, imgGray, ColorConversion.Bgr2Gray);
            foreach (Rectangle face in faceDetector.DetectMultiScale(imgGray, 1.1, 10, new Size(20, 20), Size.Empty))
                CvInvoke.Rectangle(img, face, new MCvScalar(255, 255, 255));
            CvInvoke.Imshow("img", img);
        }
    }
}
