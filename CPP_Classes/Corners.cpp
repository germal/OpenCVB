#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>

using namespace std;
using namespace cv;

extern "C" __declspec(dllexport)
void Corners_ShiTomasi(int *grayPtr, int *dstPtr, int rows, int cols, int blockSize, int apertureSize)
{
	Mat gray = Mat(rows, cols, CV_8UC1, grayPtr);
	Mat dstVB = Mat(gray.size(), CV_32FC1, dstPtr);
	Mat output = Mat::zeros(gray.size(), CV_32FC1);
	/// My Shi-Tomasi -- Using cornerMinEigenVal - can't access this from opencvSharp...
	cornerMinEigenVal(gray, output, blockSize, apertureSize, BORDER_DEFAULT);
	output.copyTo(dstVB);
}