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

class StereoLabsZed3
{
public:
	Camera zed;
	cv::Mat color;
	cv::Mat RGBDepth;
	cv::Mat disparity;
	cv::Mat depth16;
	sl::Mat leftViewRaw;
	sl::Mat rightViewRaw;
	float *vertices;
	CalibrationParameters intrinsicsLeft;
	CalibrationParameters intrinsicsRight;
	//rs2_extrinsics extrinsics;

private:
	InitParameters init_params;
	int width, height;
	bool IMUpresent = true;
	//rs2::frame leftImage;
	//rs2::frame rightImage;


public:
	~StereoLabsZed3()
	{
		zed.close();
	}
	StereoLabsZed3(int w, int h, int fps)
	{
		// Open the camera
		ERROR_CODE err = zed.open();
		if (err != ERROR_CODE::SUCCESS) {
			std::cout << "Error " << err << ", exit program.\n";
		}

		auto camera_infos = zed.getCameraInformation();
		printf("Serial number of StereoLabs Zed 3: %d\n", camera_infos.serial_number);
		
		sl::RESOLUTION res = sl::RESOLUTION::HD720;
		if (w != 720) {
			std::cout << "Error: only supporting 1280x720 right now.  exit program.\n";
		}

		sl::MODEL camera_model = sl::MODEL::LAST; // get the current camera model
		if (camera_model != sl::MODEL::ZED2) {
			std::cout << "Error: only Version 3  exit program.\n";
		}

		init_params.depth_mode = DEPTH_MODE::ULTRA;
		init_params.coordinate_system = COORDINATE_SYSTEM::RIGHT_HANDED_Y_UP; // OpenGL's coordinate system is right_handed
		
		init_params.camera_resolution = res;
		init_params.camera_fps = fps;
	}

	int *waitForFrame()
	{
		zed.grab();
		zed.retrieveImage(leftViewRaw, VIEW::LEFT);
		zed.retrieveImage(rightViewRaw, VIEW::LEFT);
		return (int*)leftViewRaw.getPtr<sl::uchar1>(sl::MEM::CPU);
	}
};

extern "C" __declspec(dllexport)
int* Zed3Open(int w, int h, int fps)
{
	StereoLabsZed3* Zed3 = new StereoLabsZed3(w, h, fps);
	return (int*)Zed3;
}

extern "C" __declspec(dllexport)
void Zed3Close(StereoLabsZed3 * Zed3)
{
	delete Zed3;
}

//extern "C" __declspec(dllexport)
//int* Zed3intrinsicsLeft(StereoLabsZed3 *Zed3)
//{
//	return (int*)&Zed3->intrinsicsLeft;
//}
//
//extern "C" __declspec(dllexport)
//int* Zed3Extrinsics(StereoLabsZed3*Zed3)
//{
//	return (int *) &Zed3->extrinsics;
//}

extern "C" __declspec(dllexport)
int* Zed3PointCloud(StereoLabsZed3 *Zed3)
{
	return (int*)&Zed3->vertices;
}

extern "C" __declspec(dllexport)
int* Zed3LeftRaw(StereoLabsZed3*Zed3)
{
	return (int*)Zed3->leftViewRaw.getPtr<sl::uchar1>(sl::MEM::CPU);
}

extern "C" __declspec(dllexport)
int* Zed3RightRaw(StereoLabsZed3 *Zed3)
{
	return (int*)Zed3->rightViewRaw.getPtr<sl::uchar1>(sl::MEM::CPU);
}

extern "C" __declspec(dllexport)
int* Zed3Depth16(StereoLabsZed3 *Zed3)
{
	return (int*)Zed3->depth16.data;
}

extern "C" __declspec(dllexport)
int* Zed3RGBDepth(StereoLabsZed3 *Zed3)
{
	return (int*)Zed3->RGBDepth.data;
}

extern "C" __declspec(dllexport)
int* Zed3WaitFrame(StereoLabsZed3* Zed3, void* color, void* depthRGB)
{
	return Zed3->waitForFrame();
}

