using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using OpenCvSharp;
using OpenCvSharp.XFeatures2D;

/// http://www.prism.gatech.edu/~ahuaman3/docs/OpenCV_Docs/tutorials/nonfree_1/nonfree_1.html
namespace CS_Classes
{
    public class CS_SurfBasics
    {
        public void New(){}
        public void Run(Mat gray1, Mat gray2, Mat dst, int hessianThreshold, bool useBFMatcher)
        {
            var surf = SURF.Create(hessianThreshold, 4, 2, true);

            KeyPoint[] keypoints1, keypoints2;
            var descriptors1 = new MatOfFloat();
            var descriptors2 = new MatOfFloat();
            surf.DetectAndCompute(gray1, null, out keypoints1, descriptors1);
            surf.DetectAndCompute(gray2, null, out keypoints2, descriptors2);

            if (useBFMatcher)
            {
                if (descriptors1.Rows > 0 && descriptors2.Rows > 0) // occasionally there is nothing to match!
                {
                    var bfMatcher = new BFMatcher(NormTypes.L2, false);
                    DMatch[] bfMatches = bfMatcher.Match(descriptors1, descriptors2);
                    Cv2.DrawMatches(gray1, keypoints1, gray2, keypoints2, bfMatches, dst);
                }
            }
            else
            {
                var flannMatcher = new FlannBasedMatcher();
                if (descriptors1.Width > 0 && descriptors2.Width > 0)
                {
                    DMatch[] flannMatches = flannMatcher.Match(descriptors1, descriptors2);
                    Cv2.DrawMatches(gray1, keypoints1, gray2, keypoints2, flannMatches, dst);
                }
            }
        }
    }

    public class CS_SiftBasics
    {
        public void New(){}
        public void Run(Mat gray1, Mat gray2, Mat dst, bool useBFMatcher, int pointsToMatch)
        {
            var sift = SIFT.Create(pointsToMatch);

            KeyPoint[] keypoints1, keypoints2;
            var descriptors1 = new MatOfFloat();
            var descriptors2 = new MatOfFloat();
            sift.DetectAndCompute(gray1, null, out keypoints1, descriptors1);
            sift.DetectAndCompute(gray2, null, out keypoints2, descriptors2);

            if (useBFMatcher)
            {
                var bfMatcher = new BFMatcher(NormTypes.L2, false);
                DMatch[] bfMatches = bfMatcher.Match(descriptors1, descriptors2);
                Cv2.DrawMatches(gray1, keypoints1, gray2, keypoints2, bfMatches, dst);
            }
            else
            {
                if (descriptors1.Count > 0 && descriptors2.Count > 0)
                {
                    var flannMatcher = new FlannBasedMatcher();
                    DMatch[] flannMatches = flannMatcher.Match(descriptors1, descriptors2);
                    Cv2.DrawMatches(gray1, keypoints1, gray2, keypoints2, flannMatches, dst);
                }
            }
        }
    }
}
