#include <iostream>
#include <iomanip>
#include <cstring>
#include <string>
#include <thread>
#include <librealsense2/rs.hpp>
#include <mutex>
#include <cstdlib>
#include <cstdio>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/calib3d.hpp>
#include "opencv2/imgproc.hpp"
#include "opencv2/imgcodecs.hpp"
#include "opencv2/highgui.hpp"
#include <map>

using namespace std;
using namespace cv;

class D400Camera
{
public:
	Mat color;
	Mat RGBDepth;
	Mat disparity;
	Mat depth16;
	Mat leftViewRaw;
	Mat rightViewRaw;
	float *vertices;
	rs2_intrinsics intrinsicsLeft;
	rs2_extrinsics extrinsics;
	double IMU_TimeStamp = 0;
	std::map<int, int> counters;
	rs2::pipeline_profile profiles;
	rs2::pipeline pipeline;
	std::mutex mutex;
	std::map<int, std::string> stream_names;

private:
	int width, height;
	std::mutex data_mutex;

	rs2::context ctx;

public:
	~D400Camera(){}

	D400Camera(int w, int h, bool IMUPresent)
	{
		rs2_error* e = 0;
		width = w;
		height = h;

		rs2::config cfg;
		cfg.enable_stream(RS2_STREAM_COLOR, width, height, RS2_FORMAT_BGR8, 30);
		cfg.enable_stream(RS2_STREAM_DEPTH, width, height, RS2_FORMAT_BGR8, 30);
		cfg.enable_stream(RS2_STREAM_INFRARED, 1, width, height, RS2_FORMAT_Y8, 30);
		cfg.enable_stream(RS2_STREAM_INFRARED, 2, width, height, RS2_FORMAT_Y8, 30);

		if (IMUPresent)
		{
			cfg.enable_stream(RS2_STREAM_GYRO);
			cfg.enable_stream(RS2_STREAM_ACCEL);
		}

		profiles = pipeline.start();
		
		auto stream = profiles.get_stream(RS2_STREAM_COLOR);
		intrinsicsLeft = stream.as<rs2::video_stream_profile>().get_intrinsics();
		auto fromStream = profiles.get_stream(RS2_STREAM_COLOR);
		extrinsics = fromStream.get_extrinsics_to(profiles.get_stream(RS2_STREAM_INFRARED));

		int vSize = int(w * h * 4 * 3);
		vertices = new float[vSize](); // 3 floats or 12 bytes per pixel.  
	}

	int *waitForFrame()
	{
		return (int *) color.data;
	}
};

extern "C" __declspec(dllexport)
int *D400Open(int w, int h, bool IMUPresent)
{
	D400Camera* tp = new D400Camera(w, h, IMUPresent);
	for (auto p : tp->profiles.get_streams())
		tp->stream_names[p.unique_id()] = p.stream_name();
	return (int *)tp;
}

extern "C" __declspec(dllexport)
int* D400intrinsicsLeft(D400Camera * tp)
{
	return (int*)&tp->intrinsicsLeft;
}

extern "C" __declspec(dllexport)
int* D400Extrinsics(D400Camera* tp)
{
	return (int *) &tp->extrinsics;
}

extern "C" __declspec(dllexport)
double D400IMUTimeStamp(D400Camera * tp)
{
	return tp->IMU_TimeStamp;
}

extern "C" __declspec(dllexport)
int* D400PointCloud(D400Camera * tp)
{
	return (int*)&tp->vertices;
}

extern "C" __declspec(dllexport)
int* D400LeftRaw(D400Camera* tp)
{
	return (int *) tp->leftViewRaw.data;
}

extern "C" __declspec(dllexport)
int* D400RightRaw(D400Camera * tp)
{
	return (int*)tp->rightViewRaw.data;
}

extern "C" __declspec(dllexport)
int* D400Depth16(D400Camera * tp)
{
	return (int*)tp->depth16.data;
}

extern "C" __declspec(dllexport)
int* D400RGBDepth(D400Camera * tp)
{
	return (int*)tp->RGBDepth.data;
}

extern "C" __declspec(dllexport)
int* D400WaitForFrame(D400Camera * tp)
{
	return (int*)tp->waitForFrame();
}
