#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/calib3d.hpp"
#include <string>
#include "time.h"
#include <iomanip> 
#include <sstream>
#include <random>
#include <intrin.h>

#include <stdio.h>
#include <stdlib.h>
#include "../../CPP_Classes/DepthColorizer.hpp"
#include <librealsense2/rs.hpp>

using namespace std;
using namespace cv;

class t265Camera
{
public:
	int frame_counter = 0;
private:
	rs2::pipeline pipe;
	rs2::config cfg;
	std::mutex data_mutex;
public:
	~t265Camera(){}
	t265Camera()
	{
		cfg.enable_stream(RS2_STREAM_POSE, RS2_FORMAT_6DOF);
		cfg.enable_stream(RS2_STREAM_FISHEYE, 1, RS2_FORMAT_Y8);
		cfg.enable_stream(RS2_STREAM_FISHEYE, 2, RS2_FORMAT_Y8);

		pipe.start(cfg);
	}

	int *waitForFrame()
	{
		auto frameset = pipe.wait_for_frames();
		// Get a frame from the pose stream
		auto f = frameset.first_or_default(RS2_STREAM_POSE);
		// Cast the frame to pose_frame and get its data
		auto pose_data = f.as<rs2::pose_frame>().get_pose_data();

		auto fs = frameset.as<rs2::frameset>();
		auto colorImage = fs.get_color_frame();
		frame_counter++;
		return (int *) colorImage.get_data();
	}
};

extern "C" __declspec(dllexport)
int *t265Open()
{
	t265Camera *kc = new t265Camera();
	return (int *)kc;
}

//extern "C" __declspec(dllexport)
//int *t265Extrinsics(t265Camera *kc)
//{
//	return (int *)&kc->calibration.extrinsics[K4A_CALIBRATION_TYPE_DEPTH][K4A_CALIBRATION_TYPE_COLOR];
//}

//extern "C" __declspec(dllexport)
//int *t265Intrinsics(t265Camera *kc)
//{
//	return (int *)&kc->calibration.color_camera_calibration.intrinsics.parameters;
//}

//extern "C" __declspec(dllexport)
//int* t265PointCloud(t265Camera* kc)
//{
//	return (int *) k4a_image_get_buffer(kc->point_cloud_image);
//}

extern "C" __declspec(dllexport)
int* t265WaitFrame(t265Camera* kc, void* color, void* depthRGB)
{
	return kc->waitForFrame();
}

extern "C" __declspec(dllexport)
void t265Close(t265Camera *kc)
{
	delete kc;
}
