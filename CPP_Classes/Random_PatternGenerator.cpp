#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/ccalib/randpattern.hpp"

using namespace std;
using namespace cv;
class Random_PatternGenerator
{
private:
public:
	Mat pattern;
    Random_PatternGenerator(){}
    void Run() {}
};

extern "C" __declspec(dllexport)
Random_PatternGenerator *Random_PatternGenerator_Open() {
    Random_PatternGenerator *Random_PatternGeneratorPtr = new Random_PatternGenerator();
    return Random_PatternGeneratorPtr;
}

extern "C" __declspec(dllexport)
void Random_PatternGenerator_Close(Random_PatternGenerator * rPtr)
{
    delete rPtr;
}

extern "C" __declspec(dllexport)
int *Random_PatternGenerator_Run(Random_PatternGenerator *rPtr, int rows, int cols, int channels)
{
	randpattern::RandomPatternGenerator generator(cols, rows);
	generator.generatePattern();
	rPtr->pattern = generator.getPattern();
	return (int *)rPtr->pattern.data; // return this C++ allocated data to managed code
}