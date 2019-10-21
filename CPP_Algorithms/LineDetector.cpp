#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include "opencv2/ximgproc.hpp"
#include "opencv2/imgcodecs.hpp"
#include "opencv2/highgui.hpp"

using namespace std;
using namespace cv;
using namespace cv::ximgproc;

// why allocate that here?  Because the intptr returned will allow VB.Net to marshal the contents back to managed code.
vector<Vec4f> lines;

extern "C" __declspec(dllexport)
int lineDetectorFast_Run(int *grayInput, int rows, int cols, int length_threshold, float distance_threshold, int canny_th1, int canny_th2, int canny_aperture_size, bool do_merge)
{
	Ptr<FastLineDetector> fld = createFastLineDetector(length_threshold, distance_threshold, canny_th1, canny_th2, canny_aperture_size, do_merge);
	Mat image = Mat(rows, cols, CV_8UC1, grayInput);

	lines.clear();
	fld->detect(image, lines);

	return (int)lines.size();
}

extern "C" __declspec(dllexport)
int lineDetector_Run(int *grayInput, int rows, int cols)
{
	Ptr<LineSegmentDetector> lsd = createLineSegmentDetector();
	Mat image = Mat(rows, cols, CV_8UC1, grayInput);

	lines.clear();
	lsd->detect(image, lines);

	return (int)lines.size();
}

extern "C" __declspec(dllexport)
int *lineDetector_Lines()
{
	if (lines.size() == 0) return 0;
	return (int *)&lines[0];
}