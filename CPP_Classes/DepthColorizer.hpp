#pragma once
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
using namespace cv;
class Depth_Colorizer
{
private:
public:
	Mat depth, dst;
	Depth_Colorizer() {}
	void Run()
	{
		unsigned char nearColor[3] = { 0, 255, 255 };
		unsigned char farColor[3] = { 255, 0, 0 };
		// Produce a cumulative histogram of depth values
		int histogram[256 * 256] = { 1 };
		unsigned short* depthImage = (unsigned short*)depth.data;
		for (int i = 0; i < depth.cols * depth.rows; ++i)
		{
			if (auto d = depthImage[i]) ++histogram[d];
		}
		for (int i = 1; i < 256 * 256; i++)
		{
			histogram[i] += histogram[i - 1];
		}

		// Remap the cumulative histogram to the range 0..256
		for (int i = 1; i < 256 * 256; i++)
		{
			histogram[i] = (histogram[i] << 8) / histogram[256 * 256 - 1];
		}

		// Produce RGB image by using the histogram to interpolate between two colors
		auto rgb = (unsigned char*)dst.data;
		for (int i = 0; i < dst.cols * dst.rows; i++)
		{
			if (uint16_t d = depthImage[i]) // For valid depth values (depth > 0)
			{
				auto t = histogram[d]; // Use the histogram entry (in the range of 0..256) to interpolate between nearColor and farColor
				*rgb++ = ((256 - t) * nearColor[0] + t * farColor[0]) >> 8;
				*rgb++ = ((256 - t) * nearColor[1] + t * farColor[1]) >> 8;
				*rgb++ = ((256 - t) * nearColor[2] + t * farColor[2]) >> 8;
			}
			else // Use black pixels for invalid values (depth == 0)
			{
				*rgb++ = 0;
				*rgb++ = 0;
				*rgb++ = 0;
			}
		}
	}
};



class Depth_Colorizer2
{
private:
public:
	Mat depth, dst;
	Depth_Colorizer2() {} 
	void Run()
	{
		float nearColor[3] = { 0, 1.0f, 1.0f };
		float farColor[3] = { 1.0f, 0, 0 };
		// Produce a cumulative histogram of depth values
		int histSize = 256 * 256;
		float hRange[] = { 1, float(histSize) }; // ranges are exclusive in the top of the range, hence 256
		const float* range[] = { hRange };
		int hbins[] = { histSize };
		Mat hist;
		calcHist(&depth, 1, 0, Mat(), hist, 1, hbins, range, true, false); 

		float* histogram = (float*)hist.data;
		for (int i = 1; i < histSize; i++)
		{
			histogram[i] += histogram[i - 1];
		}

		hist *= 1.0f / histogram[histSize - 1];

		// Produce RGB image by using the histogram to interpolate between two colors
		auto rgb = (unsigned char*)dst.data;
		unsigned short* depthImage = (unsigned short*)depth.data;
		for (int i = 0; i < dst.cols * dst.rows; i++)
		{
			if (uint16_t d = depthImage[i]) // For valid depth values (depth > 0)
			{
				auto t = histogram[d]; // Use the histogram entry (in the range of 0..1) to interpolate between nearColor and farColor
				*rgb++ = uchar(((1 - t) * nearColor[0] + t * farColor[0]) * 255);
				*rgb++ = uchar(((1 - t) * nearColor[1] + t * farColor[1]) * 255);
				*rgb++ = uchar(((1 - t) * nearColor[2] + t * farColor[2]) * 255);
			}
			else // Use black pixels for invalid values (depth == 0)
			{
				*rgb++ = 0;
				*rgb++ = 0;
				*rgb++ = 0;
			}
		}
	}
};
