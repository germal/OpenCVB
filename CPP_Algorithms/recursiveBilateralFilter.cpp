#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "rbf.hpp"
 
using namespace std;
using namespace cv;
class recursiveBilateralFilter
{
private:
public:
	Mat src;
	int recursions = 2;
	float sigma_spatial = 0.03f;
	float sigma_range = 0.01f;
	recursiveBilateralFilter() {}
	void RecursiveBilateralFilter_Run()
	{
		for (int i = 0; i < recursions; ++i)
			_recursive_bf(src.data, sigma_spatial, sigma_range, src.cols, src.rows, 3);
	}
};

extern "C" __declspec(dllexport)
recursiveBilateralFilter *RecursiveBilateralFilter_Open()
{
	recursiveBilateralFilter *rbf = new recursiveBilateralFilter();
  return rbf;
}

extern "C" __declspec(dllexport)
void RecursiveBilateralFilter_Close(recursiveBilateralFilter *rbf)
{
  delete rbf;
}

extern "C" __declspec(dllexport)
int *RecursiveBilateralFilter_Run(recursiveBilateralFilter *rbf, int *rgbPtr, int rows, int cols, int recursions)
{
	rbf->src = Mat(rows, cols, CV_8U, rgbPtr);
	rbf->recursions = recursions;
	rbf->RecursiveBilateralFilter_Run();
	return (int *)rbf->src.data; // return this C++ allocated data to managed code where it will be in the marshal.copy
}