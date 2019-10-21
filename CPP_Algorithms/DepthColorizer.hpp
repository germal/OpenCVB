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
		unsigned short *depthImage = (unsigned short *)depth.data;
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
		auto rgb = (unsigned char *)dst.data;
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
