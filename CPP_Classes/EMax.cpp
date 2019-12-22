#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/ml.hpp>

using namespace std;
using namespace cv;
using namespace cv::ml;
// Why did we need a C++ version of the EM OpenCV API's?  Because the OpenCVSharp Predict2 interface is broken.
class EMax_Basics
{
private:
public:
    Mat samples, dst;
	Mat labels;
	Mat testInput;
	Ptr<EM> em_model;
	Scalar colors[256];
	RNG rng;
	int clusters;
	int covarianceMatrixType;
	int stepSize;
	EMax_Basics()
	{
		for (int i = 0; i <= 255; ++i)
		{
			colors[i] = Scalar(rng.uniform(0, 255), rng.uniform(0, 255), rng.uniform(0, 255));
		}
		em_model = EM::create();
	}
    void Run()
    {
		em_model->setClustersNumber(clusters);
		em_model->setCovarianceMatrixType(covarianceMatrixType);
		em_model->setTermCriteria(TermCriteria(TermCriteria::COUNT + TermCriteria::EPS, 300, 0.1));
		em_model->trainEM(samples, noArray(), labels, noArray());

		// classify every image pixel
		Mat sample(1, 2, CV_32FC1);
		for (int i = 0; i < dst.rows; i+=stepSize)
		{
//#pragma omp parallel for
			for (int j = 0; j < dst.cols; j+= stepSize)
			{
				sample.at<float>(0) = (float)j;
				sample.at<float>(1) = (float)i;
				int response = cvRound(em_model->predict2(sample, noArray())[1]);
				Scalar c = colors[response];

				circle(dst, Point(j, i), stepSize, c*0.75, FILLED);
			}
		}
    }
};

extern "C" __declspec(dllexport)
EMax_Basics *EMax_Basics_Open()
{
    EMax_Basics *EMax_BasicsPtr = new EMax_Basics();
    return EMax_BasicsPtr;
}

extern "C" __declspec(dllexport)
void EMax_Basics_Close(EMax_Basics *EMax_BasicsPtr)
{
    delete EMax_BasicsPtr;
}

extern "C" __declspec(dllexport)
int *EMax_Basics_Run(EMax_Basics *EMax_BasicsPtr, int *samplePtr, int *labelsPtr, int rows, int cols, int imgRows, int imgCols, int clusters,
					 int stepSize, int covarianceMatrixType)
{
	EMax_BasicsPtr->covarianceMatrixType = covarianceMatrixType;
	EMax_BasicsPtr->stepSize = stepSize;
	EMax_BasicsPtr->clusters = clusters;
	EMax_BasicsPtr->labels = Mat(rows, 1, CV_32S, labelsPtr);
	EMax_BasicsPtr->samples = Mat(rows, cols, CV_32FC1, samplePtr);
	EMax_BasicsPtr->dst = Mat(imgRows, imgCols, CV_8UC3);
	EMax_BasicsPtr->Run();
    return (int *) EMax_BasicsPtr->dst.data; // return this C++ allocated data to managed code where it will be used in the marshal.copy
}