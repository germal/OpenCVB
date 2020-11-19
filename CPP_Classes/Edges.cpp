#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/ximgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/core/utility.hpp"

using namespace std;
using namespace cv;
using namespace ximgproc;

// https://docs.opencv.org/3.1.0/d0/da5/tutorial_ximgproc_prediction.html
class Edges_RandomForest
{
private:
	Ptr<StructuredEdgeDetection> pDollar;
public:
	Mat dst32f, src32f, gray8u;
	Edges_RandomForest(char *modelFileName) { pDollar = createStructuredEdgeDetection(modelFileName); }

	void Run(Mat src)
	{
		src.convertTo(src32f, CV_32FC3, 1.0 / 255.0); 
		pDollar->detectEdges(src32f, dst32f);
		dst32f.convertTo(gray8u, CV_8U, 255);
	}
 };

extern "C" __declspec(dllexport)
Edges_RandomForest *Edges_RandomForest_Open(char *modelFileName)
{
  return new Edges_RandomForest(modelFileName);
}

extern "C" __declspec(dllexport)
void Edges_RandomForest_Close(Edges_RandomForest *Edges_RandomForestPtr)
{
  delete Edges_RandomForestPtr;
}

extern "C" __declspec(dllexport)
int *Edges_RandomForest_Run(Edges_RandomForest *Edges_RandomForestPtr, int *rgbPtr, int rows, int cols)
{
	Edges_RandomForestPtr->Run(Mat(rows, cols, CV_8UC3, rgbPtr));
	return (int *) Edges_RandomForestPtr->gray8u.data; // return this C++ allocated data to managed code where it will be used in the marshal.copy
}





// https://github.com/opencv/opencv_contrib/blob/master/modules/ximgproc/samples/dericheSample.py
class Edges_Deriche
{
private:
public:
	Mat src, dst;
	Edges_Deriche() {}
	void Run(float alpha, float omega) {
		Mat xdst, ydst;
		ximgproc::GradientDericheX(src, xdst, alpha, omega);
		ximgproc::GradientDericheY(src, ydst, alpha, omega);
		Mat dx2 = xdst.mul(xdst);
		Mat dy2 = ydst.mul(ydst);
		Mat d2 = dx2 + dy2;
		sqrt(d2, d2);
		normalize(d2, d2, 255, NormTypes::NORM_MINMAX);
		d2.convertTo(dst, CV_8UC3, 255, 0);
	}
};

extern "C" __declspec(dllexport)
Edges_Deriche * Edges_Deriche_Open()
{
	Edges_Deriche* dPtr = new Edges_Deriche();
	return dPtr;
}

extern "C" __declspec(dllexport)
void Edges_Deriche_Close(Edges_Deriche * dPtr)
{
	delete dPtr;
}

extern "C" __declspec(dllexport)
int* Edges_Deriche_Run(Edges_Deriche * dPtr, int* rgbPtr, int rows, int cols, float alpha, float omega)
{
	dPtr->src = Mat(rows, cols, CV_8UC3, rgbPtr);
	dPtr->Run(alpha, omega);
	return (int*)dPtr->dst.data; // return this C++ allocated data to managed code
}