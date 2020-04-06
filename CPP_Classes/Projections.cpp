#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "cvHmm.h"

using namespace std;
using namespace cv;

class SimpleProjection
{
private:
public:
	Mat depth32f, mask, viewTop, viewSide;
	SimpleProjection() {}

	void Run(float desiredMin, float desiredMax, int w, int h)
	{
		float range = float(desiredMax - desiredMin);
		float hRange = (float)h;
		float wRange = (float)w;
#pragma omp parallel for
		for (int y = 0; y < depth32f.rows; ++y)
		{
			for (int x = 0; x < depth32f.cols; ++x)
			{
				uchar m = mask.at<uchar>(y, x);
				if (m == 255)
				{
					viewSide.at<uchar>(y, x) = m;
					float d = depth32f.at<float>(y, x);
					float dy = hRange * (d - desiredMin) / range;
					if ((hRange - dy) > 0 && dy < hRange && dy > 0) viewTop.at<uchar>((int)(hRange - dy), x) = 0;
					float dx = wRange * (d - desiredMin) / range;
					if (dx < wRange && dx > 0) viewSide.at<uchar>(y, (int)dx) = 0;
				}
			}
		}
	}
};

extern "C" __declspec(dllexport)
SimpleProjection * SimpleProjectionOpen() {
	SimpleProjection* cPtr = new SimpleProjection();
	return cPtr;
}

extern "C" __declspec(dllexport)
void SimpleProjectionClose(SimpleProjection * cPtr)
{
	delete cPtr;
}

extern "C" __declspec(dllexport)
int* SimpleProjectionSide(SimpleProjection * cPtr)
{
	return (int*)cPtr->viewSide.data;
}

extern "C" __declspec(dllexport)
int* SimpleProjectionRun(SimpleProjection * cPtr, int* depthPtr, float desiredMin, float desiredMax, int rows, int cols)
{
	Mat depth16 = Mat(rows, cols, CV_16U, depthPtr);
	threshold(depth16, cPtr->mask, 0, 255, ThresholdTypes::THRESH_BINARY);
	convertScaleAbs(cPtr->mask, cPtr->mask);
	depth16.convertTo(cPtr->depth32f, CV_32F);
	cPtr->viewTop = Mat(rows, cols, CV_8U).setTo(255);
	cPtr->viewSide = Mat(rows, cols, CV_8U).setTo(255);
	cPtr->Run(desiredMin, desiredMax, cols, rows);
	return (int*)cPtr->viewTop.data;
}
