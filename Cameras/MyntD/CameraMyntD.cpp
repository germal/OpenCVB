#include "../CameraDefines.hpp"
#ifdef MYNTD_1000
#pragma comment(lib, "mynteye_depth.lib")
#pragma comment(lib, "opencv_world343.lib") 
#include <iostream>

#include <opencv2/highgui/highgui.hpp>

#define WITH_OPENCV
#include <mynteyed/camera.h>
#include <mynteyed/utils.h> 
#include "../../CPP_Classes/DepthColorizer.hpp"

MYNTEYE_USE_NAMESPACE
using namespace cv;
using namespace std;

class CameraMyntD
{
public:
	Camera cam;
	DeviceInfo dev_info;
	uchar *left_color, *right_color, * image_depth, * image_RGBdepth;
	int rows, cols;
	Depth_Colorizer16 * cPtr;
	StreamIntrinsics intrinsicsBoth;
	StreamExtrinsics extrinsics;
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
	//float imuData;
public:
	~CameraMyntD()
	{
		cam.Close();
	}
	CameraMyntD(int w, int h, int fps)
	{
		DeviceInfo dev_info;
		if (!util::select(cam, &dev_info))
			std::cerr << "Error: select failed" << std::endl;

		util::print_stream_infos(cam, dev_info.index);

		OpenParams params(dev_info.index);
		params.stream_mode = StreamMode::STREAM_2560x720;
		params.ir_intensity = 4;
		params.color_mode = ColorMode::COLOR_RECTIFIED;
		params.color_stream_format = StreamFormat::STREAM_YUYV;
		params.depth_mode = DepthMode::DEPTH_RAW;
		params.framerate = 30;

		cam.Open(params);

		std::cout << std::endl;
		if (!cam.IsOpened()) 
			std::cerr << "Error: Open camera failed" << std::endl;
		 
		cPtr = new Depth_Colorizer16();
		rows = h;
		cols = w;
		intrinsicsBoth = cam.GetStreamIntrinsics(params.stream_mode);
		bool ex_ok;
		extrinsics = cam.GetStreamExtrinsics(StreamMode::STREAM_2560x720, &ex_ok);
	}

	int *waitForFrame()
	{
		left_color = right_color = image_depth = 0;  // assume we don't get any images.
		auto left = cam.GetStreamData(ImageType::IMAGE_LEFT_COLOR);
		if (left.img) left_color = left.img->To(ImageFormat::COLOR_BGR)->ToMat().data;
		
		auto right = cam.GetStreamData(ImageType::IMAGE_RIGHT_COLOR);
		if (right.img) right_color = right.img->To(ImageFormat::COLOR_BGR)->ToMat().data;
		
		auto depth = cam.GetStreamData(ImageType::IMAGE_DEPTH);
		if (depth.img)
		{
			auto depth16 = depth.img->To(ImageFormat::DEPTH_RAW)->ToMat();
			cPtr->depth16 = depth16;
			cPtr->dst = cv::Mat(rows, cols, CV_8UC3);
			cPtr->Run();
			image_depth = depth.img->To(ImageFormat::DEPTH_RAW)->ToMat().data;
		}
		return (int *) left_color;
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
extern "C" __declspec(dllexport) int* MyntDLeftImage(CameraMyntD * MyntD)
{
	return (int*)MyntD->left_color;
}
extern "C" __declspec(dllexport) int* MyntDRightImage(CameraMyntD * MyntD)
{
	return (int*)MyntD->right_color;
}
extern "C" __declspec(dllexport) int* MyntDImageRGBdepth(CameraMyntD * MyntD)
{
	return (int*)MyntD->cPtr->dst.data;
}
extern "C" __declspec(dllexport) int* MyntDImageDepth(CameraMyntD * MyntD)
{
	return (int*)MyntD->image_depth;
}
extern "C" __declspec(dllexport) int* MyntDintrinsicsLeft(CameraMyntD * MyntD)
{
	return (int*)&MyntD->intrinsicsBoth.left.width;
}
extern "C" __declspec(dllexport) int* MyntDintrinsicsRight(CameraMyntD * MyntD)
{
	return (int*)&MyntD->intrinsicsBoth.right.width;
}

extern "C" __declspec(dllexport) int* MyntDProjectionMatrix(CameraMyntD * MyntD)
{
	return (int*)&MyntD->intrinsicsBoth.right.p[0];
}

extern "C" __declspec(dllexport) int* MyntDRotationMatrix(CameraMyntD * MyntD)
{
	return (int*)&MyntD->intrinsicsBoth.right.r[0];
}


extern "C" __declspec(dllexport) int* MyntDExtrinsics(CameraMyntD*MyntD)
{
	return (int *) &MyntD->extrinsics;
}
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
extern "C" __declspec(dllexport) int *MyntDWaitFrame(CameraMyntD* MyntD)
{
	return MyntD->waitForFrame();
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