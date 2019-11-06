#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "VideoStab.h"

using namespace std;
using namespace cv;

class Stabilizer_Basics_CPP
{
private:
public:
	VideoStab stab;
	Mat rgb;
	Mat smoothedFrame;
    Stabilizer_Basics_CPP()
	{
		smoothedFrame = Mat(2, 3, CV_64F);
	}
    void Run()
    {
		smoothedFrame = stab.stabilize(rgb);
	}
};

extern "C" __declspec(dllexport)
Stabilizer_Basics_CPP *Stabilizer_Basics_Open()
{
    Stabilizer_Basics_CPP *sPtr = new Stabilizer_Basics_CPP();
    return sPtr;
}

extern "C" __declspec(dllexport)
void Stabilizer_Basics_Close(Stabilizer_Basics_CPP *sPtr)
{
    delete sPtr;
}

// https://github.com/Lakshya-Kejriwal/Real-Time-Video-Stabilization
extern "C" __declspec(dllexport)
int *Stabilizer_Basics_Run(Stabilizer_Basics_CPP *sPtr, int *rgbPtr, int rows, int cols)
{
	sPtr->rgb = Mat(rows, cols, CV_8UC3, rgbPtr);
	cvtColor(sPtr->rgb, sPtr->stab.gray, COLOR_BGR2GRAY);
	if (sPtr->stab.lastFrame.rows > 0) sPtr->Run(); // skips the first pass while the frames get loaded.
	sPtr->stab.gray.copyTo(sPtr->stab.lastFrame);
	return (int *)sPtr->stab.smoothedFrame.data; // return this C++ allocated data to managed code where it will be used in the marshal.copy
}
