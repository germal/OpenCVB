#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>

using namespace std;
using namespace cv;

// https://docs.opencv.org/3.4/d1/d1d/tutorial_histo3D.html
// https://docs.opencv.org/trunk/d1/d1d/tutorial_histo3D.html
// this reinterpretation of the opencv_contrib example is due to the way the layout of a 3D histogram data.
// The rows and cols are both -1 so I had assumed there was a bug but the data is accessible with the at method.
// If you attempt the same access to the data in managed code, it does not work (AFAIK).
extern "C" __declspec(dllexport)
float *Histogram_3D_RGB(int *rgbPtr, int rows, int cols, int bins)
{
	float hRange[] = { 0, 256 }; // ranges are exclusive in the top of the range, hence 256
	const float *range[] = { hRange, hRange, hRange };
	int hbins[] = { bins, bins, bins };
	int channel[] = { 0, 1, 2 };
	Mat src = Mat(rows, cols, CV_8UC3, rgbPtr);

	static Mat histogram;
	calcHist(&src, 1, channel, Mat(), histogram, 3, hbins, range, true, false); // for 3D histograms, all 3 bins must be equal.

	int planeSize = (int)histogram.step1(0);
	int hCols = (int)histogram.step1(1);
	int hRows = (int)(planeSize / hCols);
	int planes = (int)(histogram.total() / planeSize);
	return (float *)histogram.data;
}