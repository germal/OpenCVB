#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/plot.hpp>

using namespace std;
using namespace cv;

// https://github.com/opencv/opencv_contrib/blob/master/modules/plot/samples/plot_demo.cpp
extern "C" __declspec(dllexport)
void Plot_OpenCVBasics(int* plot1, int* plot2, int rows, int cols)
{
	Mat plotA = Mat(rows, cols, CV_8UC3, plot1);
	Mat plotB = Mat(rows, cols, CV_8UC3, plot2);
	Mat result;

	Mat data_x(1, 51, CV_64F);
	Mat data_y(1, 51, CV_64F);

	for (int i = 0; i < data_x.cols; i++)
	{
		double x = (i - (double) data_x.cols / 2);
		data_x.at<double>(0, i) = x;
		data_y.at<double>(0, i) = x * x * x;
	}

	Ptr<plot::Plot2d> plot = plot::Plot2d::create(data_x, data_y);
	plot->render(result);
	resize(result, plotA, plotA.size());

	plot->setShowText(false);
	plot->setShowGrid(false);
	plot->setPlotBackgroundColor(Scalar(255, 200, 200));
	plot->setPlotLineColor(Scalar(255, 0, 0));
	plot->setPlotLineWidth(2);
	plot->setInvertOrientation(true);
	plot->render(result);
	resize(result, plotB, plotB.size());
}