#include <winsock2.h>
#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include <string>
#include "time.h"
#include <iomanip> 
#include <sstream>
#include <random>
#include <intrin.h>

#include <stdio.h>
#include <stdlib.h>
#include <k4a/k4a.h>
#include <k4a/k4a.hpp>
#include <k4apixel.h>
#include "../../CPP_Algorithms/DepthColorizer.hpp"

using namespace std;
using namespace cv;
using namespace k4aviewer;

class KinectCamera
{
public:
	char *serial_number = NULL;
	uint32_t deviceCount = 0;
	k4a_imu_sample_t imu_sample;
	k4a_device_t device = NULL;
	k4a_calibration_t calibration;
	k4a_transformation_t transformation;
	k4a_image_t depthInColor;
	k4a_image_t point_cloud_image;
	int pointCloudBuffSize = 0;
	k4a_image_t colorImage = NULL;
	Depth_Colorizer* dcptr = NULL;
private:
	k4a_capture_t capture = NULL;
	const int32_t TIMEOUT_IN_MS = 1000;
	size_t infraredSize = 0;
	k4a_image_t point_cloud = NULL;
	int colorRows = 0, colorCols = 0, colorBuffSize = 0;
	int depthRows = 0, depthCols = 0;
	k4a_device_configuration_t config = K4A_DEVICE_CONFIG_INIT_DISABLE_ALL;
public:
	~KinectCamera()
	{
	}
	KinectCamera()
	{
		deviceCount = k4a_device_get_installed_count();
		if (deviceCount > 0)
		{
			device = NULL;
			if (K4A_RESULT_SUCCEEDED != k4a_device_open(K4A_DEVICE_DEFAULT, &device)) { deviceCount = 0; return; }

			KinectSerialNumber();

			config.color_format = K4A_IMAGE_FORMAT_COLOR_BGRA32;
			config.color_resolution = K4A_COLOR_RESOLUTION_720P;
			config.depth_mode = K4A_DEPTH_MODE_WFOV_2X2BINNED;
			config.camera_fps = K4A_FRAMES_PER_SECOND_30;

			k4a_device_get_calibration(device, config.depth_mode, config.color_resolution, &calibration);

			colorRows = calibration.color_camera_calibration.resolution_height;
			colorCols = calibration.color_camera_calibration.resolution_width;
			colorBuffSize = colorRows * colorCols;
			pointCloudBuffSize = colorBuffSize * 3 * (int) sizeof(int16_t);

			depthRows = calibration.depth_camera_calibration.resolution_height;
			depthCols = calibration.depth_camera_calibration.resolution_width;

			k4a_image_create(K4A_IMAGE_FORMAT_CUSTOM, colorCols, colorRows, colorCols * (int)sizeof(int16_t), &depthInColor);
			k4a_image_create(K4A_IMAGE_FORMAT_CUSTOM, colorCols, colorRows, colorCols * 3 * (int)sizeof(int16_t), &point_cloud_image); // int16_t - not a mistake.
			k4a_image_create(K4A_IMAGE_FORMAT_COLOR_BGRA32, colorCols, colorRows, colorCols * 3 * (int)sizeof(int16_t), &colorImage);

			k4a_device_start_cameras(device, &config);

			transformation = k4a_transformation_create(&calibration);

			k4a_device_start_imu(device);
			dcptr = new Depth_Colorizer();
		}
	}

	void KinectSerialNumber()
	{
		size_t length = 0;
		if (K4A_BUFFER_RESULT_TOO_SMALL != k4a_device_get_serialnum(device, NULL, &length))
		{
			printf("%d: Failed to get serial number length\n", 0);
			k4a_device_close(device);
		}

		serial_number = (char *)malloc(length);
		k4a_device_get_serialnum(device, serial_number, &length);
	}

	int *waitForFrame(void *color, void * depthRGB)
	{
		bool waiting = true;
		while (waiting)
		{
			switch (k4a_device_get_capture(device, &capture, TIMEOUT_IN_MS))
			{
			case K4A_WAIT_RESULT_SUCCEEDED:
				waiting = false;
				break;
			case K4A_WAIT_RESULT_TIMEOUT:
				return 0;
			case K4A_WAIT_RESULT_FAILED:
				return 0;
			}
		}

		k4a_image_t depthImage = k4a_capture_get_depth_image(capture);
		if (depthImage != NULL and depthImage->_rsvd != 0)
		{
			k4a_transformation_depth_image_to_color_camera(transformation, depthImage, depthInColor);
			uint16_t *depthBuffer = (uint16_t *) k4a_image_get_buffer(depthInColor); 
			dcptr->depth = Mat(colorRows, colorCols, CV_16U, depthBuffer);
			dcptr->dst = Mat(colorRows, colorCols, CV_8UC3, depthRGB);
			dcptr->Run();
		}

		if (colorImage) k4a_image_release(colorImage);  // we want to keep the colorimage around between calls.
		colorImage = k4a_capture_get_color_image(capture);
		if (colorImage)
		{
			uint8_t *tmpColor = k4a_image_get_buffer(colorImage);
			if (tmpColor == NULL) return 0; // just have to use the last buffers.  Nothing new...
			Mat bgr = Mat(colorCols, colorRows, CV_8UC3, color);
			Mat bgra = Mat(colorCols, colorRows, CV_8UC4, tmpColor);
			cvtColor(bgra, bgr, COLOR_BGRA2BGR);
		}

		k4a_transformation_depth_image_to_point_cloud(transformation, depthInColor, K4A_CALIBRATION_TYPE_COLOR, point_cloud_image);
		k4a_device_get_imu_sample(device, &imu_sample, 2000);

		if (depthImage) k4a_image_release(depthImage);

		k4a_capture_release(capture);
		return (int *)&imu_sample;
	}
};

extern "C" __declspec(dllexport)
int KinectDeviceCount(KinectCamera *kc)
{
	return kc->deviceCount;
}

extern "C" __declspec(dllexport)
int *KinectOpen()
{
	KinectCamera *kc = new KinectCamera();
	if (kc->deviceCount == 0) return 0;
	return (int *)kc;
}

extern "C" __declspec(dllexport)
int *KinectDeviceName(KinectCamera *kc)
{
	return (int *)kc->serial_number;
}

extern "C" __declspec(dllexport)
int *KinectExtrinsics(KinectCamera *kc)
{
	return (int *)&kc->calibration.extrinsics[K4A_CALIBRATION_TYPE_DEPTH][K4A_CALIBRATION_TYPE_COLOR];
}

extern "C" __declspec(dllexport)
int *KinectIntrinsics(KinectCamera *kc)
{
	return (int *)&kc->calibration.color_camera_calibration.intrinsics.parameters;
}

extern "C" __declspec(dllexport)
int* KinectPointCloud(KinectCamera* kc)
{
	return (int *) k4a_image_get_buffer(kc->point_cloud_image);
}
extern "C" __declspec(dllexport)
int* KinectDepthInColor(KinectCamera* kc)
{
	return (int*)k4a_image_get_buffer(kc->depthInColor);
}
extern "C" __declspec(dllexport)
int* KinectWaitFrame(KinectCamera* kc, void* color, void* depthRGB)
{
	return kc->waitForFrame(color, depthRGB);
}

extern "C" __declspec(dllexport)
void KinectClose(KinectCamera *kc)
{
	if (kc->point_cloud_image) k4a_image_release(kc->point_cloud_image);
	if (kc->depthInColor) k4a_image_release(kc->depthInColor);
	if (kc->colorImage) k4a_image_release(kc->colorImage);

	k4a_device_stop_imu(kc->device);
	k4a_device_stop_cameras(kc->device);
	k4a_transformation_destroy(kc->transformation);
	free(kc->serial_number);
	if (kc == 0) return;
	k4a_device_close(kc->device);
	delete kc;
}
