#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>

#include <winsock2.h>

using namespace std;
using namespace cv;

extern "C" __declspec(dllexport)
void FitEllipse_AMS(float *inputPoints, int count, float *outputTriangle)
{
	Mat input(count, 1, CV_32FC2, inputPoints);
	RotatedRect box = fitEllipseAMS(input);
	outputTriangle[0] = box.angle;
	outputTriangle[1] = box.center.x;
	outputTriangle[2] = box.center.y;
	outputTriangle[3] = box.size.width;
	outputTriangle[4] = box.size.height;
}



extern "C" __declspec(dllexport)
void FitEllipse_Direct(float *inputPoints, int count, float *outputTriangle)
{
	Mat input(count, 1, CV_32FC2, inputPoints);
	RotatedRect box = fitEllipseDirect(input);
	outputTriangle[0] = box.angle;
	outputTriangle[1] = box.center.x;
	outputTriangle[2] = box.center.y;
	outputTriangle[3] = box.size.width;
	outputTriangle[4] = box.size.height;
}