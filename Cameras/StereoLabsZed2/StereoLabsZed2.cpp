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
#include <sl/Camera.hpp>
using namespace sl;
using namespace std;

class StereoLabsZed2
{
public:
	int serialNumber = 0;
	cv::Mat disparity;
	CameraParameters intrinsicsLeft;
	CameraParameters intrinsicsRight;
	CalibrationParameters extrinsics;
	float *pointCloud;
	sl::Mat leftView;
	sl::Mat rightView;
	sl::Mat RGBADepth;
	sl::Mat depth32F;
	sl::Mat color;

private:
	sl::Camera zed;
	sl::InitParameters init_params;
	int width, height;
	bool IMUpresent = true;


public:
	~StereoLabsZed2()
	{
		zed.close();
	}
	StereoLabsZed2(int w, int h, int fps)
	{
		width = w;
		height = h;

		ERROR_CODE err = zed.open();
		if (err != ERROR_CODE::SUCCESS) {
			std::cout << "Error " << err << ", exit program.\n";
		}

		auto camera_info = zed.getCameraInformation();
		serialNumber = camera_info.serial_number;
		extrinsics = camera_info.calibration_parameters_raw;
		intrinsicsLeft = camera_info.calibration_parameters.left_cam;
		intrinsicsRight = camera_info.calibration_parameters.right_cam;

		init_params.depth_mode = DEPTH_MODE::ULTRA;
		init_params.coordinate_system = COORDINATE_SYSTEM::RIGHT_HANDED_Y_UP; // OpenGL's coordinate system is right_handed
		
		init_params.camera_resolution = sl::RESOLUTION::HD720;
		init_params.camera_fps = fps;
		pointCloud = new float[(long long)width * height * 3];
		// pointCloudXYZRGB = sl::Mat(camera_info.camera_resolution, MAT_TYPE::F32_C4, MEM::CPU);
	}

	int *waitForFrame()
	{
		zed.grab();
		zed.retrieveImage(color, VIEW::LEFT, MEM::CPU);
		zed.retrieveImage(leftView, VIEW::LEFT_GRAY, MEM::CPU);
		zed.retrieveImage(rightView, VIEW::RIGHT_GRAY, MEM::CPU);
		zed.retrieveImage(RGBADepth, VIEW::DEPTH, MEM::CPU);
		zed.retrieveMeasure(depth32F, MEASURE::DEPTH, MEM::CPU);

		sl::Mat pointCloudXYZRGB;
		zed.retrieveMeasure(pointCloudXYZRGB, MEASURE::XYZARGB, MEM::CPU);
		float* pc = (float*)pointCloudXYZRGB.getPtr<sl::uchar1>(sl::MEM::CPU);

		float* pcXYZ = pointCloud;
		for (int i = 0; i < (long long)width * height * 4; i+= 4)
		{
			pcXYZ[0] = pc[i];
			pcXYZ[1] = pc[i + 1];
			pcXYZ[2] = pc[i + 2];
			pcXYZ += 3;
		}

		return (int*)color.getPtr<sl::uchar1>(sl::MEM::CPU);
	}
};

extern "C" __declspec(dllexport)
int* Zed2Open(int w, int h, int fps)
{
	StereoLabsZed2* Zed2 = new StereoLabsZed2(w, h, fps);
	return (int*)Zed2;
}

extern "C" __declspec(dllexport)
void Zed2Close(StereoLabsZed2 * Zed2)
{
	delete Zed2;
}

extern "C" __declspec(dllexport)
int* Zed2intrinsicsLeft(StereoLabsZed2* Zed2)
{
	return (int*)&Zed2->intrinsicsLeft;
}

extern "C" __declspec(dllexport)
int* Zed2intrinsicsRight(StereoLabsZed2* Zed2)
{
	return (int*)&Zed2->intrinsicsRight;
}

extern "C" __declspec(dllexport)
int* Zed2Extrinsics(StereoLabsZed2*Zed2)
{
	return (int *) &Zed2->extrinsics;
}

extern "C" __declspec(dllexport)
int* Zed2PointCloud(StereoLabsZed2 * Zed2)
{
	return (int*)Zed2->pointCloud;
}

extern "C" __declspec(dllexport)
int Zed2SerialNumber(StereoLabsZed2 * Zed2)
{
	return Zed2->serialNumber;
}

extern "C" __declspec(dllexport)
int* Zed2LeftView(StereoLabsZed2*Zed2)
{
	return (int*)Zed2->leftView.getPtr<sl::uchar1>(sl::MEM::CPU);
}

extern "C" __declspec(dllexport)
int* Zed2RightView(StereoLabsZed2 *Zed2)
{
	return (int*)Zed2->rightView.getPtr<sl::uchar1>(sl::MEM::CPU);
}

extern "C" __declspec(dllexport)
int* Zed2Depth32f(StereoLabsZed2 *Zed2)
{
	return (int*)Zed2->depth32F.getPtr<sl::uchar1>(sl::MEM::CPU);
}

extern "C" __declspec(dllexport)
int* Zed2RGBADepth(StereoLabsZed2 * Zed2)
{
	return (int*)Zed2->RGBADepth.getPtr<sl::uchar1>(sl::MEM::CPU);
}

extern "C" __declspec(dllexport)
int* Zed2WaitFrame(StereoLabsZed2* Zed2)
{
	return Zed2->waitForFrame();
}

