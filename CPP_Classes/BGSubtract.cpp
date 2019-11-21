#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include "opencv2/bgsegm.hpp"
#include <opencv2/core.hpp>
#include <opencv2/highgui.hpp>

using namespace std;
using namespace cv;
using namespace cv::bgsegm;
class BGSubtract_BGFG
{
private:
public:
	Ptr<BackgroundSubtractor> algo;
	Mat src, fgMask;
    BGSubtract_BGFG(){}
    void Run() {
		algo->apply(src, fgMask);
    }
};

extern "C" __declspec(dllexport)
BGSubtract_BGFG *BGSubtract_BGFG_Open(int currMethod) {
    BGSubtract_BGFG *bgfs = new BGSubtract_BGFG();
	if (currMethod == 0) bgfs->algo = createBackgroundSubtractorGMG(20, 0.7);
	else if (currMethod == 1) bgfs->algo = createBackgroundSubtractorCNT();
	else if (currMethod == 2) bgfs->algo = createBackgroundSubtractorKNN();
	else if (currMethod == 3) bgfs->algo = createBackgroundSubtractorMOG();
	else if (currMethod == 4) bgfs->algo = createBackgroundSubtractorMOG2();
	else if (currMethod == 5) bgfs->algo = createBackgroundSubtractorGSOC();
	else if (currMethod == 6) bgfs->algo = createBackgroundSubtractorLSBP();
	return bgfs;
}

extern "C" __declspec(dllexport)
void BGSubtract_BGFG_Close(BGSubtract_BGFG *bgfs)
{
    delete bgfs;
}

extern "C" __declspec(dllexport)
int *BGSubtract_BGFG_Run(BGSubtract_BGFG *bgfs, int *rgbPtr, int rows, int cols, int channels)
{
	bgfs->src = Mat(rows, cols, (channels == 3) ? CV_8UC3 : CV_8UC1, rgbPtr);
	bgfs->Run();
    return (int *) bgfs->fgMask.data; // return this C++ allocated data to managed code
}

