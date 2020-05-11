#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "DepthColorizer.hpp"

using namespace std;
using namespace cv;


extern "C" __declspec(dllexport)
DepthXYZ *Depth_XYZ_OpenMP_Open(float ppx, float ppy, float fx, float fy)
{
	DepthXYZ *DepthXYZPtr = new DepthXYZ(ppx, ppy, fx, fy);
	return DepthXYZPtr;
}

extern "C" __declspec(dllexport)
void Depth_XYZ_OpenMP_Close(DepthXYZ *DepthXYZPtr)
{
	delete DepthXYZPtr;
}

extern "C" __declspec(dllexport)
int *Depth_XYZ_OpenMP_Run(DepthXYZ *DepthXYZPtr, int *depthPtr, int rows, int cols)
{
	DepthXYZPtr->depth = Mat(rows, cols, CV_16U, depthPtr);
	DepthXYZPtr->Run();
	return (int *)DepthXYZPtr->depthxyz.data; // return this C++ allocated data to managed code where it will be used in the marshal.copy
}







extern "C" __declspec(dllexport)
Depth_Colorizer * Depth_Colorizer_Open()
{
	Depth_Colorizer* Depth_ColorizerPtr = new Depth_Colorizer();
	return Depth_ColorizerPtr;
}

extern "C" __declspec(dllexport)
void Depth_Colorizer_Close(Depth_Colorizer * Depth_ColorizerPtr)
{
	delete Depth_ColorizerPtr;
}

extern "C" __declspec(dllexport)
int* Depth_Colorizer_Run(Depth_Colorizer * Depth_ColorizerPtr, int* depthPtr, int rows, int cols)
{
	Depth_ColorizerPtr->depth32f = Mat(rows, cols, CV_32F, depthPtr);
	Depth_ColorizerPtr->output = Mat(rows, cols, CV_8UC3);
	Depth_ColorizerPtr->Run();
	return (int*)Depth_ColorizerPtr->output.data; // return this C++ allocated data to managed code where it will be used in the marshal.copy
}







extern "C" __declspec(dllexport)
Depth_Colorizer2 * Depth_Colorizer2_Open()
{
	Depth_Colorizer2* Depth_ColorizerPtr = new Depth_Colorizer2();
	return Depth_ColorizerPtr;
}

extern "C" __declspec(dllexport)
void Depth_Colorizer2_Close(Depth_Colorizer2 * Depth_ColorizerPtr)
{
	delete Depth_ColorizerPtr;
}

extern "C" __declspec(dllexport)
int* Depth_Colorizer2_Run(Depth_Colorizer2 * Depth_ColorizerPtr, int* depthPtr, int rows, int cols, int _histSize)
{
	Depth_ColorizerPtr->histSize = _histSize;
	Depth_ColorizerPtr->depth32f = Mat(rows, cols, CV_32F, depthPtr);
	Depth_ColorizerPtr->output = Mat(rows, cols, CV_8UC3);
	Depth_ColorizerPtr->Run();
	return (int*)Depth_ColorizerPtr->output.data; // return this C++ allocated data to managed code where it will be used in the marshal.copy
}