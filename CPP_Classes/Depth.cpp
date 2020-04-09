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
class DepthXYZ
{
private:
public:
	Mat depth, depthxyz;
    float ppx, ppy, fx, fy;
    DepthXYZ(float _ppx, float _ppy, float _fx, float _fy)
	{
		ppx = _ppx; ppy = _ppy; fx = _fx; fy = _fy;
	}
	void GetImageCoordinates()
	{
		depthxyz = Mat_<Vec3f>(depth.rows, depth.cols);
#ifdef _DEBUG
#pragma omp parallel for  // doubles performance in debug mode but is much worse in Release mode.
#endif
		for (int y = 0; y < depth.rows; y++)
		{
			for (int x = 0; x < depth.cols; x++)
			{
				float d = float(depth.at<unsigned short>(y, x)) / 1000;
				depthxyz.at<Vec3f>(y,x) = Vec3f(float(x), float(y), d);
			}
		}
	}
	void Run()
	{
		depthxyz = Mat_<Vec3f>(1, depth.rows * depth.cols);
		for (int y = 0, nbPix = 0; y < depth.rows; y++)
		{
			for (int x = 0; x < depth.cols; x++, nbPix++)
			{
				float d = float(depth.at<unsigned short>(y, x)) / 1000;
				if (d > 0) depthxyz.at< Vec3f >(0, nbPix) = Vec3f(float((x - ppx) / fx), float((y - ppy) / fy), d);
			}
		}
	}
};

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
	DepthXYZPtr->GetImageCoordinates();
	return (int *)DepthXYZPtr->depthxyz.data; // return this C++ allocated data to managed code where it will be used in the marshal.copy
}







extern "C" __declspec(dllexport)
Depth_Colorizer32s * Depth_Colorizer32s_Open()
{
	Depth_Colorizer32s* Depth_ColorizerPtr = new Depth_Colorizer32s();
	return Depth_ColorizerPtr;
}

extern "C" __declspec(dllexport)
void Depth_Colorizer32s_Close(Depth_Colorizer32s * Depth_ColorizerPtr)
{
	delete Depth_ColorizerPtr;
}

extern "C" __declspec(dllexport)
int* Depth_Colorizer32s_Run(Depth_Colorizer32s * Depth_ColorizerPtr, int* depthPtr, int rows, int cols)
{
	Depth_ColorizerPtr->depth32fzz = Mat(rows, cols, CV_32F, depthPtr);
	Depth_ColorizerPtr->dst = Mat(rows, cols, CV_8UC3);
	Depth_ColorizerPtr->Run();
	return (int*)Depth_ColorizerPtr->dst.data; // return this C++ allocated data to managed code where it will be used in the marshal.copy
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
	Depth_ColorizerPtr->depth16 = Mat(rows, cols, CV_16U, depthPtr);
	Depth_ColorizerPtr->dst = Mat(rows, cols, CV_8UC3);
	Depth_ColorizerPtr->Run();
	return (int*)Depth_ColorizerPtr->dst.data; // return this C++ allocated data to managed code where it will be used in the marshal.copy
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
	Depth_ColorizerPtr->depth16 = Mat(rows, cols, CV_16U, depthPtr);
	Depth_ColorizerPtr->dst = Mat(rows, cols, CV_8UC3);
	Depth_ColorizerPtr->Run();
	return (int*)Depth_ColorizerPtr->dst.data; // return this C++ allocated data to managed code where it will be used in the marshal.copy
}