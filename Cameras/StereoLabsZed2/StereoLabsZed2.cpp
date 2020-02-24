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
	CameraParameters intrinsicsLeft;
	CameraParameters intrinsicsRight;
	CalibrationParameters extrinsics;

private:
	sl::Camera zed;
	sl::InitParameters init_params;
	int width, height;
	float imuData;
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
	}

	int *waitForFrame(void* rgba, void* depthRGBA, void* _depth32f, void* left, void* right, void* pointCloud)
	{
		// allocate and free the mat structures to try and avoid the flicker problem as GPU memory garbage collects.
		sl::Mat color, RGBADepth, depth32F, leftView, rightView, pcMat;

		zed.grab();
		zed.retrieveImage(color, VIEW::LEFT, MEM::CPU);
		memcpy(rgba, (void*)color.getPtr<sl::uchar1>(sl::MEM::CPU), pixelCount * 4);

		zed.retrieveImage(RGBADepth, VIEW::DEPTH, MEM::CPU);
		memcpy(depthRGBA, (void*)RGBADepth.getPtr<sl::uchar1>(sl::MEM::CPU), pixelCount * 4);

		zed.retrieveMeasure(depth32F, MEASURE::DEPTH, MEM::CPU);
		memcpy(_depth32f, (void*)depth32F.getPtr<sl::uchar1>(sl::MEM::CPU), pixelCount * 4);

		zed.retrieveImage(leftView, VIEW::LEFT_GRAY, MEM::CPU);
		memcpy(left, (void*)leftView.getPtr<sl::uchar1>(sl::MEM::CPU), pixelCount);

		zed.retrieveImage(rightView, VIEW::RIGHT_GRAY, MEM::CPU);
		memcpy(right, (void*)rightView.getPtr<sl::uchar1>(sl::MEM::CPU), pixelCount);

		zed.retrieveMeasure(pcMat, MEASURE::XYZARGB, MEM::CPU);
		float* pc = (float*)pcMat.getPtr<sl::uchar1>(sl::MEM::CPU);
		float* pcXYZ = (float *)pointCloud;
		for (int i = 0; i < pixelCount * 4; i+= 4)
		{
			pcXYZ[0] = pc[i];
			pcXYZ[1] = pc[i + 1];
			pcXYZ[2] = pc[i + 2];
			pcXYZ += 3;
		}

		// explicitly free the mat structures - trying to fix the flicker problem with GPU memory.
		color.free(); RGBADepth.free(); depth32F.free(); leftView.free(); rightView.free(); pcMat.free();
		return (int*)&imuData;
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
int Zed2SerialNumber(StereoLabsZed2 * Zed2)
{
	return Zed2->serialNumber;
}

extern "C" __declspec(dllexport)
int* Zed2WaitFrame(StereoLabsZed2* Zed2, void* rgba, void* depthRGBA, void* depth32f, void* left, void* right, void *pointCloud )
{
	return Zed2->waitForFrame(rgba, depthRGBA, depth32f, left, right, pointCloud);
}

