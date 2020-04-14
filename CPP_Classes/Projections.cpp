#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "cvHmm.h"
#include "DepthColorizer.hpp"

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
					float d = depth32f.at<float>(y, x);
					float dy = hRange * (d - desiredMin) / range;
					if (dy > 0 && dy < hRange) viewTop.at<uchar>((int)(hRange - dy), x) = 0;
					float dx = wRange * (d - desiredMin) / range;
					if (dx < wRange && dx > 0) viewSide.at<uchar>(y, (int)dx) = 0;
				}
			}
		}
	}
};

extern "C" __declspec(dllexport)
SimpleProjection * SimpleProjectionOpen() 
{
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
	cPtr->depth32f = Mat(rows, cols, CV_32F, depthPtr);
	threshold(cPtr->depth32f, cPtr->mask, 0, 255, ThresholdTypes::THRESH_BINARY);
	convertScaleAbs(cPtr->mask, cPtr->mask);
	cPtr->viewTop = Mat(rows, cols, CV_8U).setTo(255);
	cPtr->viewSide = Mat(rows, cols, CV_8U).setTo(255);
	cPtr->Run(desiredMin, desiredMax, cols, rows);
	return (int*)cPtr->viewTop.data;
}




class Projections_Gravity
{
private:
public:
	Mat xDistance, yDistance, zDistance, viewTop, viewSide, top16u, side16u;
	Depth_Colorizer2OLD* colorizePtr;
	Projections_Gravity() : colorizePtr{ NULL } {}

	void Run(float maxZ, int w, int h)
	{
		float zHalf = maxZ / 2;
		float range = (float)h;

#pragma omp parallel for
		for (int y = 0; y < zDistance.rows; ++y)
		{
			for (int x = 0; x < zDistance.cols; ++x)
			{
				float d = zDistance.at<float>(y, x);
				if (d > 0 and d < maxZ)
				{
					float fx = xDistance.at<float>(y, x);
					float dpixel = range * d / maxZ;
					if (fx > -zHalf && fx < zHalf)
					{
						float dx = range * (zHalf + fx) / maxZ; // maintain a 1:1 aspect ratio
						if (dx > 0 && dx < range) top16u.at<ushort>((int)(range - dpixel), (int)dx) = int(255 * d / maxZ);
					}

					float fy = yDistance.at<float>(y, x);
					if (fy > -zHalf && fy < zHalf)
					{
						float dy = range * (zHalf + fy) / maxZ; // maintain a 1:1 aspect ratio
						if (dy < range && dy > 0) side16u.at<ushort>(int(dy), (int)dpixel) = int(255 * d / maxZ);
					}
				}
			}
		}

		viewTop = Mat(top16u.rows, top16u.cols, CV_8UC3);
		viewSide = Mat(side16u.rows, side16u.cols, CV_8UC3);
		colorizePtr->histSize = 256;

		colorizePtr->depth16 = Mat(top16u.rows, top16u.cols, CV_16U, top16u.data);
		colorizePtr->dst = viewTop;
		colorizePtr->Run();

		colorizePtr->depth16 = Mat(side16u.rows, side16u.cols, CV_16U, side16u.data);
		colorizePtr->dst = viewSide;
		colorizePtr->Run();
	}
};

extern "C" __declspec(dllexport)
Projections_Gravity * Projections_Gravity_Open(LPSTR fileName) 
{
	Projections_Gravity* cPtr = new Projections_Gravity();
	cPtr->colorizePtr = new Depth_Colorizer2OLD();
	return cPtr;
}

extern "C" __declspec(dllexport)
void Projections_Gravity_Close(Projections_Gravity * cPtr)
{
	delete cPtr->colorizePtr;
	delete cPtr;
}

extern "C" __declspec(dllexport)
int* Projections_Gravity_Side(Projections_Gravity * cPtr)
{
	return (int*)cPtr->viewSide.data;
}

extern "C" __declspec(dllexport)
int* Projections_Gravity_Run(Projections_Gravity * cPtr, int* xPtr, int* yPtr, int* zPtr, float maxZ, int rows, int cols)
{
	cPtr->xDistance = Mat(rows, cols, CV_32F, xPtr);
	cPtr->yDistance = Mat(rows, cols, CV_32F, yPtr);
	cPtr->zDistance = Mat(rows, cols, CV_32F, zPtr);
	cPtr->top16u = Mat(rows, cols, CV_16U).setTo(0);
	cPtr->side16u = Mat(rows, cols, CV_16U).setTo(0);
	cPtr->Run(maxZ, cols, rows);
	return (int*)cPtr->viewTop.data;
}







class Projections_GravityHistogram
{
private:
public:
	Mat xDistance, yDistance, zDistance, histTop, histSide;
	Projections_GravityHistogram() {}

	void Run(float maxZ, int w, int h)
	{
		float zHalf = maxZ / 2;
		float range = (float)h;

		//#pragma omp parallel for
		for (int y = 0; y < zDistance.rows; ++y)
		{
			for (int x = 0; x < zDistance.cols; ++x)
			{
				float d = zDistance.at<float>(y, x);
				if (d > 0 and d < maxZ)
				{
					float fx = xDistance.at<float>(y, x);
					float dpixel = range * d / maxZ;
					if (fx > -zHalf && fx < zHalf)
					{
						float dx = range * (zHalf + fx) / maxZ; // maintain a 1:1 aspect ratio
						if (dx > 0 && dx < range) histTop.at<float>((int)(range - dpixel), (int)dx) += 1;
					}

					float fy = yDistance.at<float>(y, x);
					if (fy > -zHalf && fy < zHalf)
					{
						float dy = range * (zHalf + fy) / maxZ; // maintain a 1:1 aspect ratio
						if (dy < range && dy > 0) histSide.at<float>(int(dy), (int)dpixel) += 1;
					}
				}
			}
		}
	}
};

extern "C" __declspec(dllexport)
Projections_GravityHistogram * Projections_GravityHist_Open()
{
	Projections_GravityHistogram* cPtr = new Projections_GravityHistogram();
	return cPtr;
}

extern "C" __declspec(dllexport)
void Projections_GravityHist_Close(Projections_GravityHistogram * cPtr)
{
	delete cPtr;
}

extern "C" __declspec(dllexport)
int* Projections_GravityHist_Side(Projections_GravityHistogram * cPtr)
{
	return (int*)cPtr->histSide.data;
}

extern "C" __declspec(dllexport)
int* Projections_GravityHist_Run(Projections_GravityHistogram * cPtr, int* xPtr, int* yPtr, int* zPtr, float maxZ, int rows, int cols)
{
	cPtr->xDistance = Mat(rows, cols, CV_32F, xPtr);
	cPtr->yDistance = Mat(rows, cols, CV_32F, yPtr);
	cPtr->zDistance = Mat(rows, cols, CV_32F, zPtr);
	cPtr->histTop = Mat(rows, cols, CV_32F).setTo(0);
	cPtr->histSide = Mat(rows, cols, CV_32F).setTo(0);
	cPtr->Run(maxZ, cols, rows);
	return (int*)cPtr->histTop.data;
}