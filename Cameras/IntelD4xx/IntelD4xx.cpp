// License: Apache 2.0. See LICENSE file in root directory.
// Copyright(c) 2015-2017 Intel Corporation. All Rights Reserved.

#include <librealsense2/rs.hpp> // Include RealSense Cross Platform API
#include "example.hpp"          // Include short list of convenience functions for rendering

#include <algorithm>            // std::min, std::max
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "cv-helpers.hpp"    // Helper functions for conversions between RealSense and OpenCV

using namespace std;
using namespace cv;

class d4xxCamera
{
public:
	// Declare pointcloud object, for calculating pointclouds and texture mappings
	rs2::pointcloud pc;
	// We want the points object to be persistent so we can display the last cloud when a frame drops
	rs2::points points;
	// Declare RealSense pipeline, encapsulating the actual device and sensors
	rs2::pipeline pipe;
	cv::Mat colorMat;
	cv::Mat depthMat;
	cv::Mat depthRGBMat;
	cv::Mat redLeftMat;
	cv::Mat redRightMat;
private:
public:
	~d4xxCamera() {}
	d4xxCamera() 
	{ 
		pipe.start(); 
	} // Start streaming with default recommended configuration 
	void waitForFrame()
	{
		// Wait for the next set of frames from the camera
		auto frames = pipe.wait_for_frames();
		auto color = frames.get_color_frame();
		colorMat = frame_to_mat(color);
		imshow("color", colorMat);

		pc.map_to(color); // Tell pointcloud object to map to this color frame

		auto depth = frames.get_depth_frame();
		depthMat = frame_to_mat(depth);

		auto redLeft = frames.get_infrared_frame(0);
		depthMat = frame_to_mat(redLeft);

		auto redRight = frames.get_infrared_frame(1);
		depthMat = frame_to_mat(redRight);

		// Generate the pointcloud and texture mappings
		points = pc.calculate(depth);
	}
};

extern "C" __declspec(dllexport)
int d4xxDeviceCount(d4xxCamera* kc)
{
	return 1;
}

extern "C" __declspec(dllexport)
int* d4xxOpen()
{
	d4xxCamera* rs = new d4xxCamera();
	return (int*)rs;
}

//extern "C" __declspec(dllexport)
//int* d4xxExtrinsics(d4xxCamera* rs)
//{
//	return (int*)& rs->calibration.extrinsics[K4A_CALIBRATION_TYPE_DEPTH][K4A_CALIBRATION_TYPE_COLOR];
//}

//extern "C" __declspec(dllexport)
//int* d4xxIntrinsics(d4xxCamera* rs)
//{
//	return (int*)& rs->calibration.color_camera_calibration.intrinsics.parameters;
//}

extern "C" __declspec(dllexport)
int* d4xxPointCloud(d4xxCamera* rs)
{
	return (int*)rs->points.get_vertices(); 
}

extern "C" __declspec(dllexport)
int* d4xxColor(d4xxCamera* rs)
{
	return (int*)rs->colorMat.data;
}

extern "C" __declspec(dllexport)
int* d4xxDepthRGB(d4xxCamera* rs)
{
	return (int*)rs->depthRGBMat.data;
}

extern "C" __declspec(dllexport)
int* d4xxDepth(d4xxCamera* rs)
{
	return (int*)rs->depthMat.data;
}

extern "C" __declspec(dllexport)
int* d4xxRedLeft(d4xxCamera* rs)
{
	return (int*)rs->redLeftMat.data;
}

extern "C" __declspec(dllexport)
int* d4xxRedRight(d4xxCamera* rs)
{
	return (int*)rs->redRightMat.data;
}

extern "C" __declspec(dllexport)
void d4xxWaitFrame(d4xxCamera* rs)
{
	rs->waitForFrame();
}

extern "C" __declspec(dllexport)
void d4xxClose(d4xxCamera* rs)
{
	delete rs;
}
