#include <iostream>
#include <list> 
#include <iterator> 
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
#include "example.hpp"

using namespace std;
using namespace cv;

class D400Camera
{
public:
	rs2_intrinsics intrinsicsLeft;
	rs2_extrinsics extrinsics;
	rs2::pipeline_profile profiles;
	rs2::pipeline pipeline;
	rs2::frameset frames;
	rs2::frameset processedFrames;
	rs2::colorizer colorizer;
	rs2::align align = rs2::align(RS2_STREAM_COLOR);
	rs2::pointcloud pc;
	rs2::frame RGBDepth;
	int width, height;
	rs2::frame accel, gyro;

private:

	rs2::context ctx;

public:
	~D400Camera(){}

	D400Camera(int w, int h, bool IMUPresent)
	{
		rs2_error* e = 0;
		width = w;
		height = h;

		rs2::config cfg;
		cfg.enable_stream(RS2_STREAM_COLOR, width, height, RS2_FORMAT_BGR8);
		cfg.enable_stream(RS2_STREAM_DEPTH, width, height, RS2_FORMAT_Z16);
		cfg.enable_stream(RS2_STREAM_INFRARED, 1, width, height, RS2_FORMAT_Y8);
		cfg.enable_stream(RS2_STREAM_INFRARED, 2, width, height, RS2_FORMAT_Y8);

		if (IMUPresent)
		{
			cfg.enable_stream(RS2_STREAM_GYRO);
			cfg.enable_stream(RS2_STREAM_ACCEL);
		}

		profiles = pipeline.start(cfg);

		auto stream = profiles.get_stream(RS2_STREAM_COLOR);
		intrinsicsLeft = stream.as<rs2::video_stream_profile>().get_intrinsics();
		auto fromStream = profiles.get_stream(RS2_STREAM_COLOR);
		auto toStream = profiles.get_stream(RS2_STREAM_INFRARED);

		extrinsics = fromStream.get_extrinsics_to(toStream);
	}

	void waitForFrame()
	{
		frames = pipeline.wait_for_frames(5000);
		gyro = frames.first_or_default(RS2_STREAM_GYRO, RS2_FORMAT_MOTION_XYZ32F);
		accel = frames.first_or_default(RS2_STREAM_ACCEL, RS2_FORMAT_MOTION_XYZ32F);
	}
};


extern "C" __declspec(dllexport)
int *D400Open(int w, int h, bool IMUPresent)
{
	D400Camera* tp = new D400Camera(w, h, IMUPresent);
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
double D400IMUTimeStamp(D400Camera* tp)
{
	if (tp->gyro == 0) return 0;
	return tp->gyro.get_timestamp();
}

extern "C" __declspec(dllexport)
int* D400Gyro(D400Camera * tp)
{
	if (tp->gyro == 0) return 0;
	return (int*)tp->gyro.get_data();
}

extern "C" __declspec(dllexport)
int * D400Accel(D400Camera * tp)
{
	if (tp->accel == 0) return 0;
	return (int *)tp->accel.get_data();
}

extern "C" __declspec(dllexport)
int* D400PointCloud(D400Camera * tp)
{
	return (int*)tp->pc.process(tp->processedFrames.get_depth_frame()).as<rs2::points>().get_data();
}

extern "C" __declspec(dllexport)
int* D400Color(D400Camera * tp)
{
	tp->processedFrames = tp->colorizer.process(tp->frames);
	tp->processedFrames = tp->align.process(tp->processedFrames);
	return (int*)tp->processedFrames.get_color_frame().get_data();
}

extern "C" __declspec(dllexport)
int* D400LeftRaw(D400Camera* tp)
{
	return (int*)tp->frames.get_infrared_frame(1).get_data();
}

extern "C" __declspec(dllexport)
int* D400RightRaw(D400Camera * tp)
{
	return (int*)tp->frames.get_infrared_frame(2).get_data();
}

extern "C" __declspec(dllexport)
int* D400RGBDepth(D400Camera * tp)
{
	//tp->RGBDepth = tp->colorizer.process(tp->processedFrames.get_depth_frame()); 
	return (int*)tp->colorizer.process(tp->processedFrames.get_depth_frame()).get_data();
}

extern "C" __declspec(dllexport)
int* D400RawDepth(D400Camera * tp)
{
	return (int*)tp->frames.get_depth_frame().get_data();
}

extern "C" __declspec(dllexport)
void D400WaitForFrame(D400Camera * tp)
{
	tp->waitForFrame();
}
