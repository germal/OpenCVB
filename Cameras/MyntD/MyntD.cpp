#include "../CameraDefines.hpp"
#ifdef MYNTD_1000
#ifdef DEBUG
#pragma comment(lib, "opencv_world343d.lib")
#else
#pragma comment(lib, "opencv_world343.lib")
#endif
#pragma comment(lib, "mynteye_depth.lib") 
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
#include "opencv2/imgproc.hpp"
#include "opencv2/imgcodecs.hpp"
#include "opencv2/highgui.hpp"

#include <mynteyed/camera.h>
#include <mynteyed/utils.h>

MYNTEYE_USE_NAMESPACE
using namespace std;

class CameraMyntD
{
public:
	Camera cam;
	DeviceInfo dev_info;

	//int serialNumber = 0;
	//CameraParameters intrinsicsLeft;
	//CameraParameters intrinsicsRight;
	//CalibrationParameters extrinsics;
	//float rotation[9];
	//float translation[3];
	//float acceleration[3];
	//SensorsData sensordata;
	//Orientation orientation;
	//float imuTemperature;
	//double imuTimeStamp;
	//Pose zed_pose;
private:
	//sl::Camera zed;
	//sl::InitParameters init_params;
	//int width, height;
	//float imuData;
	//long long pixelCount;
public:
	~CameraMyntD()
	{
		cam.Close();
	}
	CameraMyntD(int w, int h, int fps)
	{
		//width = w;
		//height = h;
		//pixelCount = (long long)width * height;

		//init_params.sensors_required = true;
		//init_params.depth_mode = DEPTH_MODE::ULTRA;
		//init_params.coordinate_system = COORDINATE_SYSTEM::RIGHT_HANDED_Y_UP; // OpenGL's coordinate system is right_handed

		//init_params.camera_resolution = sl::RESOLUTION::HD720;
		//init_params.camera_fps = fps;

		//zed.open(init_params);

		//auto camera_info = zed.getCameraInformation();
		//serialNumber = camera_info.serial_number;
		//extrinsics = camera_info.calibration_parameters_raw;
		//intrinsicsLeft = camera_info.calibration_parameters.left_cam;
		//intrinsicsRight = camera_info.calibration_parameters.right_cam;

		//PositionalTrackingParameters positional_tracking_param;
		//positional_tracking_param.enable_area_memory = true;
		//zed.enablePositionalTracking(positional_tracking_param);
	}

	int *waitForFrame(void* rgba, void* depthRGBA, void* _depth32f, void* left, void* right, void* pointCloud)
	{
		//// allocate and free the mat structures to try and avoid the flicker problem as GPU memory garbage collects.
		//sl::Mat color, RGBADepth, depth32F, leftView, rightView, pcMat;

		//zed.grab();
		//zed.retrieveImage(color, VIEW::LEFT, MEM::CPU);
		//memcpy(rgba, (void*)color.getPtr<sl::uchar1>(sl::MEM::CPU), pixelCount * 4);

		//zed.retrieveImage(RGBADepth, VIEW::DEPTH, MEM::CPU);
		//memcpy(depthRGBA, (void*)RGBADepth.getPtr<sl::uchar1>(sl::MEM::CPU), pixelCount * 4);

		//zed.retrieveMeasure(depth32F, MEASURE::DEPTH, MEM::CPU);
		//memcpy(_depth32f, (void*)depth32F.getPtr<sl::uchar1>(sl::MEM::CPU), pixelCount * 4);

		//zed.retrieveImage(leftView, VIEW::LEFT_GRAY, MEM::CPU);
		//memcpy(left, (void*)leftView.getPtr<sl::uchar1>(sl::MEM::CPU), pixelCount);

		//zed.retrieveImage(rightView, VIEW::RIGHT_GRAY, MEM::CPU);
		//memcpy(right, (void*)rightView.getPtr<sl::uchar1>(sl::MEM::CPU), pixelCount);

		//zed.retrieveMeasure(pcMat, MEASURE::XYZARGB, MEM::CPU);
		//float* pc = (float*)pcMat.getPtr<sl::uchar1>(sl::MEM::CPU);
		//float* pcXYZ = (float *)pointCloud;
		//for (int i = 0; i < pixelCount * 4; i+= 4)
		//{
		//	pcXYZ[0] = pc[i];
		//	pcXYZ[1] = pc[i + 1];
		//	pcXYZ[2] = pc[i + 2];
		//	pcXYZ += 3;
		//}

		//// explicitly free the mat structures - trying to fix the flicker problem with GPU memory.
		//color.free(); RGBADepth.free(); depth32F.free(); leftView.free(); rightView.free(); pcMat.free();
		//
		//zed.getPosition(zed_pose, REFERENCE_FRAME::WORLD);

		//memcpy((void*)&rotation, (void*)&zed_pose.getRotationMatrix(), sizeof(float) * 9);
		//memcpy((void*)&translation, (void*)&zed_pose.getTranslation(), sizeof(float) * 3);

		//zed.getSensorsData(sensordata, TIME_REFERENCE::CURRENT);
		//imuTimeStamp = static_cast<double>(zed_pose.timestamp.getMilliseconds());
		//return (int*)&zed_pose.pose_data;
		return 0;
	}
}; 

extern "C" __declspec(dllexport) int* MyntDOpen(int w, int h, int fps)
{
	CameraMyntD* MyntD = new CameraMyntD(w, h, fps);
	return (int*)MyntD;
}
extern "C" __declspec(dllexport) void MyntDClose(CameraMyntD * MyntD)
{
	delete MyntD;
}
//extern "C" __declspec(dllexport) int* MyntDintrinsicsLeft(CameraMyntD* MyntD)
//{
//	return (int*)&MyntD->intrinsicsLeft;
//}
//extern "C" __declspec(dllexport) int* MyntDintrinsicsRight(CameraMyntD* MyntD)
//{
//	return (int*)&MyntD->intrinsicsRight;
//}
//extern "C" __declspec(dllexport) int* MyntDExtrinsics(CameraMyntD*MyntD)
//{
//	return (int *) &MyntD->extrinsics;
//}
//extern "C" __declspec(dllexport) int* MyntDAcceleration(CameraMyntD * MyntD)
//{
//	return (int*)&MyntD->sensordata.imu.linear_acceleration;
//}
//extern "C" __declspec(dllexport) int* MyntDTranslation(CameraMyntD * MyntD)
//{
//	return (int*)&MyntD->translation;
//}
//extern "C" __declspec(dllexport) int* MyntDRotationMatrix(CameraMyntD * MyntD)
//{
//	return (int*)&MyntD->rotation;
//}
//extern "C" __declspec(dllexport) int* MyntDAngularVelocity(CameraMyntD * MyntD)
//{
//	return (int*)&MyntD->sensordata.imu.angular_velocity;
//}
//extern "C" __declspec(dllexport) float MyntDIMU_Barometer(CameraMyntD * MyntD)
//{
//	return MyntD->sensordata.barometer.pressure;
//}
//extern "C" __declspec(dllexport) int* MyntDOrientation(CameraMyntD * MyntD)
//{
//	MyntD->orientation = MyntD->sensordata.imu.pose.getOrientation();
//	return (int*)&MyntD->orientation;
//}
//extern "C" __declspec(dllexport) int MyntDSerialNumber(CameraMyntD * MyntD)
//{
//	return MyntD->serialNumber;
//}
extern "C" __declspec(dllexport) int* MyntDWaitFrame(CameraMyntD* MyntD, void* rgba, void* depthRGBA, void* depth32f, void* left, void* right, void *pointCloud )
{
	return MyntD->waitForFrame(rgba, depthRGBA, depth32f, left, right, pointCloud);
}
//extern "C" __declspec(dllexport) int* MyntDIMU_Magnetometer(CameraMyntD * MyntD)
//{
//	return (int*)&MyntD->sensordata.magnetometer.magnetic_field_uncalibrated; // calibrated values look incorrect.
//}
//extern "C" __declspec(dllexport) double MyntDIMU_TimeStamp(CameraMyntD * MyntD)
//{
//	return MyntD->imuTimeStamp;
//}
//extern "C" __declspec(dllexport)float MyntDIMU_Temperature(CameraMyntD * MyntD)
//{
//	MyntD->sensordata.temperature.get(sl::SensorsData::TemperatureData::SENSOR_LOCATION::IMU, MyntD->imuTemperature);
//	return MyntD->imuTemperature;
//}
#endif