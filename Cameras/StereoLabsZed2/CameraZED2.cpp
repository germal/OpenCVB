#include "../CameraDefines.hpp"
#ifdef STEREOLAB_INSTALLED
#pragma comment(lib, "sl_zed64.lib")
#pragma comment(lib, "cuda.lib") 
#pragma comment(lib, "cudart.lib") 

#include <iostream>
#include <iomanip>
#include <cstring>
#include <string>
#include <thread>
#include <mutex>
#include <cstdlib>
#include <cstdio>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/calib3d.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/imgcodecs.hpp>
#include <opencv2/highgui.hpp>
#include <sl/Camera.hpp>
#include "../../CPP_Classes/DepthColorizer.hpp"

using namespace sl;
using namespace std;
using namespace cv;

class StereoLabsZed2
{
public:
	int serialNumber = 0;
	CameraParameters intrinsicsLeft;
	CameraParameters intrinsicsRight;
	CalibrationParameters extrinsics;
	float rotation[9] = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
	float translation[3] = { 0, 0, 0 };
	float acceleration[3] = { 0, 0, 0 };
	SensorsData sensordata;
	Orientation orientation;
	float imuTemperature = 0;
	double imuTimeStamp = 0;
	Pose zed_pose;
	sl::Camera zed;
	cv::Mat colorMat, leftViewMat, rightViewMat, pointCloudMat;
	Depth_Colorizer* cPtr;
private:
	sl::InitParameters init_params;
	int width, height;
	float imuData = 0;
	long long pixelCount;
public:
	~StereoLabsZed2()
	{
		zed.close();
	}
	StereoLabsZed2(int w, int h, int fps)
	{
		width = w;
		height = h;
		pixelCount = (long long)width * height;

		init_params.sensors_required = true;
		init_params.depth_mode = DEPTH_MODE::ULTRA;
		init_params.coordinate_system = COORDINATE_SYSTEM::RIGHT_HANDED_Y_UP; // OpenGL's coordinate system is right_handed

		init_params.camera_resolution = sl::RESOLUTION::HD720;
		init_params.camera_fps = fps;

		auto rc = zed.open(init_params);
		printf("rc = %d\n", rc);

		auto camera_info = zed.getCameraInformation();
		serialNumber = camera_info.serial_number;
		extrinsics = camera_info.calibration_parameters_raw;
		intrinsicsLeft = camera_info.calibration_parameters.left_cam;
		intrinsicsRight = camera_info.calibration_parameters.right_cam;

		PositionalTrackingParameters positional_tracking_param;
		positional_tracking_param.enable_area_memory = true;
		zed.enablePositionalTracking(positional_tracking_param);
		cPtr = new Depth_Colorizer();
	}

	void waitForFrame()
	{
		zed.grab();
	}

	void GetData()
	{
		sl::Mat color, depth32f, leftView, rightView, pcMat;

		zed.retrieveImage(color, VIEW::LEFT, MEM::CPU);
		cv::Mat tmp = cv::Mat(height, width, CV_8UC4, (void*)color.getPtr<sl::uchar1>(sl::MEM::CPU));
		cv::cvtColor(tmp, colorMat, cv::ColorConversionCodes::COLOR_BGRA2BGR);

		zed.retrieveMeasure(depth32f, MEASURE::DEPTH, MEM::CPU);
		cv::Mat depth = cv::Mat(height, width, CV_32FC1, (void*)depth32f.getPtr<sl::uchar1>(sl::MEM::CPU));
		cv::threshold(depth, depth, 20000, 20000, cv::ThresholdTypes::THRESH_TRUNC);

		depth.convertTo(cPtr->depth16, CV_16U);

		cPtr->dst = cv::Mat(height, width, CV_8UC3);
		cPtr->Run();

		zed.retrieveImage(leftView, VIEW::LEFT_GRAY, MEM::CPU);
		leftViewMat = cv::Mat(height, width, CV_8U, (void*)leftView.getPtr<sl::uchar1>(sl::MEM::CPU)).clone();

		zed.retrieveImage(rightView, VIEW::RIGHT_GRAY, MEM::CPU);
		rightViewMat = cv::Mat(height, width, CV_8U, (void*)rightView.getPtr<sl::uchar1>(sl::MEM::CPU)).clone();

		zed.retrieveMeasure(pcMat, MEASURE::XYZARGB, MEM::CPU);
		cv::Mat pc = cv::Mat(height, width, CV_32FC4, (void*)pcMat.getPtr<sl::uchar1>(sl::MEM::CPU));
		pc.convertTo(pointCloudMat, CV_32FC3, 0.001);

		zed.getPosition(zed_pose, REFERENCE_FRAME::WORLD);

		memcpy((void*)&rotation, (void*)&zed_pose.getRotationMatrix(), sizeof(float) * 9);
		memcpy((void*)&translation, (void*)&zed_pose.getTranslation(), sizeof(float) * 3);

		zed.getSensorsData(sensordata, TIME_REFERENCE::CURRENT);
		imuTimeStamp = static_cast<double>(zed_pose.timestamp.getMilliseconds());
	}
};

extern "C" __declspec(dllexport) int* Zed2Open(int w, int h, int fps)
{
	StereoLabsZed2* Zed2 = new StereoLabsZed2(w, h, fps);
	return (int*)Zed2;
}
extern "C" __declspec(dllexport) void Zed2Close(StereoLabsZed2 * Zed2)
{
	delete Zed2;
}
extern "C" __declspec(dllexport) int* Zed2intrinsicsLeft(StereoLabsZed2* Zed2)
{
	return (int*)&Zed2->intrinsicsLeft;
}
extern "C" __declspec(dllexport) int* Zed2intrinsicsRight(StereoLabsZed2* Zed2)
{
	return (int*)&Zed2->intrinsicsRight;
}
extern "C" __declspec(dllexport) int* Zed2Extrinsics(StereoLabsZed2*Zed2)
{
	return (int *) &Zed2->extrinsics;
}
extern "C" __declspec(dllexport) int* Zed2Acceleration(StereoLabsZed2 * Zed2)
{
	return (int*)&Zed2->sensordata.imu.linear_acceleration;
}
extern "C" __declspec(dllexport) int* Zed2Translation(StereoLabsZed2 * Zed2)
{
	return (int*)&Zed2->translation;
}
extern "C" __declspec(dllexport) int* Zed2RotationMatrix(StereoLabsZed2 * Zed2)
{
	return (int*)&Zed2->rotation;
}
extern "C" __declspec(dllexport) int* Zed2AngularVelocity(StereoLabsZed2 * Zed2)
{
	return (int*)&Zed2->sensordata.imu.angular_velocity;
}
extern "C" __declspec(dllexport) float Zed2IMU_Barometer(StereoLabsZed2 * Zed2)
{
	return Zed2->sensordata.barometer.pressure;
}
extern "C" __declspec(dllexport) int* Zed2Orientation(StereoLabsZed2 * Zed2)
{
	Zed2->orientation = Zed2->sensordata.imu.pose.getOrientation();
	return (int*)&Zed2->orientation;
}
extern "C" __declspec(dllexport) int Zed2SerialNumber(StereoLabsZed2 * Zed2)
{
	return Zed2->serialNumber;
}
extern "C" __declspec(dllexport) void Zed2WaitFrame(StereoLabsZed2 * Zed2)
{
	Zed2->waitForFrame();
}
extern "C" __declspec(dllexport) int* Zed2IMU_Magnetometer(StereoLabsZed2 * Zed2)
{
	return (int*)&Zed2->sensordata.magnetometer.magnetic_field_uncalibrated; // calibrated values look incorrect.
}
extern "C" __declspec(dllexport) double Zed2IMU_TimeStamp(StereoLabsZed2 * Zed2)
{
	// printf("ts (uint64) =%ju (0x%jx)\n", Zed2->zed_pose.timestamp.getMilliseconds(), Zed2->zed_pose.timestamp.getMilliseconds());
	return Zed2->imuTimeStamp;
}
extern "C" __declspec(dllexport)float Zed2IMU_Temperature(StereoLabsZed2 * Zed2)
{
	Zed2->sensordata.temperature.get(sl::SensorsData::TemperatureData::SENSOR_LOCATION::IMU, Zed2->imuTemperature);
	return Zed2->imuTemperature;
}
extern "C" __declspec(dllexport) int* Zed2GetPoseData(StereoLabsZed2 * Zed2)
{
	return (int*)&Zed2->zed_pose.pose_data;
}




extern "C" __declspec(dllexport) void Zed2GetData(StereoLabsZed2 * Zed2)
{
	Zed2->GetData();
}

extern "C" __declspec(dllexport)
int* Zed2Color(StereoLabsZed2 * Zed2)
{
	return (int*)Zed2->colorMat.data;
}

extern "C" __declspec(dllexport)
int* Zed2RGBDepth(StereoLabsZed2 * Zed2)
{
	return (int*)Zed2->cPtr->dst.data;
}

extern "C" __declspec(dllexport)
int* Zed2Depth(StereoLabsZed2 * Zed2)
{
	return (int*)Zed2->cPtr->depth16.data;
}

extern "C" __declspec(dllexport)
int* Zed2PointCloud(StereoLabsZed2 * Zed2)
{
	return (int*)Zed2->pointCloudMat.data;
}

extern "C" __declspec(dllexport)
int* Zed2LeftView(StereoLabsZed2 * Zed2)
{
	return (int*)Zed2->leftViewMat.data;
}

extern "C" __declspec(dllexport)
int* Zed2RightView(StereoLabsZed2 * Zed2)
{
	return (int*)Zed2->rightViewMat.data;
}

#endif


