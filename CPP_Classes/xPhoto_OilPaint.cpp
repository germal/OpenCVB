#include <opencv2/core.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/xphoto.hpp>
#include "opencv2/xphoto/oilpainting.hpp"
#include <iostream>
#include <cstdlib>
#include <cstdio>

using namespace std;
using namespace cv;
class xPhoto_OilPaint
{
private:
public:
    Mat src, dst;
    xPhoto_OilPaint(){}
    void Run(int size, int dynRatio, int colorCode)
    {
		xphoto::oilPainting(src, dst, size, dynRatio, colorCode);
    }
};

extern "C" __declspec(dllexport)
xPhoto_OilPaint *xPhoto_OilPaint_Open()
{
    xPhoto_OilPaint *xPhoto_OilPaint_Ptr = new xPhoto_OilPaint();
    return xPhoto_OilPaint_Ptr;
}

extern "C" __declspec(dllexport)
void xPhoto_OilPaint_Close(xPhoto_OilPaint *xPhoto_OilPaint_Ptr)
{
    delete xPhoto_OilPaint_Ptr;
} 

extern "C" __declspec(dllexport)
int *xPhoto_OilPaint_Run(xPhoto_OilPaint *xPhoto_OilPaint_Ptr, int *imagePtr, int rows, int cols, int size, int dynRatio, int colorCode)
{
	xPhoto_OilPaint_Ptr->src = Mat(rows, cols, CV_8UC3, imagePtr);
	xPhoto_OilPaint_Ptr->Run(size, dynRatio, colorCode);
    return (int *) xPhoto_OilPaint_Ptr->dst.data; // return this C++ allocated data to managed code where it will be used in the marshal.copy
}