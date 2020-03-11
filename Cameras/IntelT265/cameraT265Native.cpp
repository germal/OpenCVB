#include <iostream>
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

using namespace std;
using namespace cv;




class t265sgm
{
private:
public:
	Mat disp16s;
	Ptr<StereoSGBM> stereo;
	t265sgm(int minDisp, int windowSize, int numDisp)
	{
		//stereo = cv::StereoSGBM::create(0, 112, 16, 8 * 3 * 25, 32 * 3 * 25, 1, 0, 10, 100, 32);
		stereo = cv::StereoSGBM::create(minDisp, numDisp, 16, 8 * 3 * windowSize * windowSize, 32 * 3 * windowSize * windowSize, 1, 0, 10, 100, 32);
	}
	Mat Run(Mat leftimg, Mat rightimg, int maxDisp)
	{
		stereo->compute(leftimg, rightimg, disp16s);
		return disp16s;
	}
};




class t265Camera
{
public:
	Mat leftViewMap1;
	Mat leftViewMap2;
	Mat rightViewMap1;
	Mat rightViewMap2;
	Mat color;
	Mat RGBDepth;
	Mat disparity;
	Mat depth16;
	Mat leftViewRaw;
	Mat rightViewRaw;
	float *vertices;
	int rawWidth;
	int rawHeight;
	rs2_intrinsics intrinsicsLeft;
	rs2_intrinsics intrinsicsLeftRight;
	rs2_extrinsics extrinsics;
	int stereo_width_px;
	int stereo_height_px;
	rs2_pose pose_data;
	double IMU_TimeStamp;

private:
	int width, height;
	rs2::config cfg;
	rs2::pipeline pipeline;
	std::mutex data_mutex;
	rs2::pipeline_profile pipeline_profile;
	int numDisp;
	int minDisp = 0;
	int maxDisp;
	int windowSize = 5;
	Ptr<StereoSGBM> stereoPtr;

	Mat kMatleft;
	Mat dMatleft;
	Mat rMatleft;
	Mat pMatleft;

	Mat lm1, lm2, rm1, rm2;

	Mat kMatRight;
	Mat dMatRight;
	Mat rMatRight;
	Mat pMatRight;

	double stereo_fov_rad;
	double stereo_focal_px;
	double stereo_cx;
	double stereo_cy;
	cv::Size stereo_size;
	rs2::frame leftImage;
	rs2::frame rightImage;

	t265sgm* sgm;

public:
	~t265Camera(){}
	rs2::device get_device(const std::string& serial_number = "") {
		rs2::context ctx;
		while (true) {
			for (auto&& dev : ctx.query_devices()) {
				if (((serial_number.empty() && std::strstr(dev.get_info(RS2_CAMERA_INFO_NAME), "T265")) ||
					std::strcmp(dev.get_info(RS2_CAMERA_INFO_SERIAL_NUMBER), serial_number.c_str()) == 0))
					return dev;
			}
			std::this_thread::sleep_for(std::chrono::milliseconds(10));
		}
	}

	t265Camera(int w, int h)
	{
		width = w;
		height = h;

		cfg.enable_stream(RS2_STREAM_POSE, RS2_FORMAT_6DOF);
		cfg.enable_stream(RS2_STREAM_GYRO);
		cfg.enable_stream(RS2_STREAM_ACCEL);
		cfg.enable_stream(RS2_STREAM_FISHEYE, 1, RS2_FORMAT_Y8);
		cfg.enable_stream(RS2_STREAM_FISHEYE, 2, RS2_FORMAT_Y8);

		pipeline_profile = pipeline.start(cfg);

		numDisp = 112 - minDisp;
		maxDisp = minDisp + numDisp;

		//stereoPtr = cv::StereoSGBM::create(minDisp, numDisp, 16, 8 * 3 * windowSize * windowSize, 32 * 3 * windowSize * windowSize, 1, 0, 10, 100);
		sgm = new t265sgm(minDisp, windowSize, numDisp);

		auto leftStream = pipeline_profile.get_stream(RS2_STREAM_FISHEYE, 1).as<rs2::video_stream_profile>();
		auto rightStream = pipeline_profile.get_stream(RS2_STREAM_FISHEYE, 2).as<rs2::video_stream_profile>();

		extrinsics = leftStream.get_extrinsics_to(rightStream);

		intrinsicsLeft = leftStream.as<rs2::video_stream_profile>().get_intrinsics();
		intrinsicsLeftRight = rightStream.as<rs2::video_stream_profile>().get_intrinsics();

		rawWidth = intrinsicsLeft.width;
		rawHeight = intrinsicsLeft.height;

		double kLeft[9] = { intrinsicsLeft.fx, 0, intrinsicsLeft.ppx, 0, intrinsicsLeft.fy, intrinsicsLeft.ppy, 0, 0, 1 };
		double dLeft[4] = { intrinsicsLeft.coeffs[0], intrinsicsLeft.coeffs[1], intrinsicsLeft.coeffs[2], intrinsicsLeft.coeffs[3] };

		double kRight[9] = { intrinsicsLeftRight.fx, 0, intrinsicsLeftRight.ppx, 0, intrinsicsLeftRight.fy, intrinsicsLeftRight.ppy, 0, 0, 1 };
		double dRight[4] = { intrinsicsLeftRight.coeffs[0], intrinsicsLeftRight.coeffs[1], intrinsicsLeftRight.coeffs[2], intrinsicsLeftRight.coeffs[3] };

		// We need To determine what focal length our undistorted images should have
		// In order To Set up the camera matrices For initUndistortRectifyMap.  We
		// could use stereoRectify, but here we show how To derive these projection
		// matrices from the calibration And a desired height And field Of view      
		// We calculate the undistorted focal length :
		//
		//         h
		// -----------------
		//  \      |      /
		//    \    | f  /
		//     \   |   /
		//      \ fov /
		//        \|/
		stereo_fov_rad = 90.0 * CV_PI / 180.0;  // 90 degree desired fov
		stereo_height_px = 300; // 300x300 pixel stereo output
		stereo_focal_px = stereo_height_px / 2 / tan(stereo_fov_rad / 2);

		// We Set the left rotation To identity And the right rotation
		// the rotation between the cameras
		double rLeft[9] = { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
		float* r = (float *) &extrinsics.rotation;
		double rRight[9] = { r[0],r[1],r[2],r[3],r[4],r[5],r[6],r[7],r[8] };
		// The stereo algorithm needs max_disp extra pixels In order To produce valid
		// disparity On the desired output region. This changes the width, but the
		// center Of projection should be On the center Of the cropped image
		stereo_width_px = stereo_height_px + maxDisp;
		stereo_size = cv::Size(stereo_width_px, stereo_height_px);
		stereo_cx = double((stereo_height_px - 1.0) / 2.0 + maxDisp);
		stereo_cy = (stereo_height_px - 1.0) / 2.0;

		// Construct the left And right projection matrices, the only difference Is
		// that the right projection matrix should have a shift along the x axis Of
		// baseline*focal_length
		double pLeft[12] = { stereo_focal_px, 0, stereo_cx, 0, 0, stereo_focal_px, stereo_cy, 0, 0, 0, 1, 0 };
		double pRight[12] = { stereo_focal_px, 0, stereo_cx, 0, 0, stereo_focal_px, stereo_cy, 0, 0, 0, 1, 0 };
		pRight[3] = extrinsics.translation[0] * stereo_focal_px;

		kMatleft = Mat(3, 3, CV_64F, kLeft);
		dMatleft = Mat(1, 4, CV_64F, dLeft);
		rMatleft = Mat(3, 3, CV_64F, rLeft);
		pMatleft = Mat(3, 4, CV_64F, pLeft);
		cv::fisheye::initUndistortRectifyMap(kMatleft, dMatleft, rMatleft, pMatleft, stereo_size, CV_32FC1, lm1, lm2);
		cv::fisheye::initUndistortRectifyMap(kMatleft, dMatleft, rMatleft, pMatleft, Size(rawWidth, rawHeight), CV_32FC1, leftViewMap1, leftViewMap2);

		kMatRight = Mat(3, 3, CV_64F, kRight);
		dMatRight = Mat(1, 4, CV_64F, dRight);
		rMatRight = Mat(3, 3, CV_64F, rRight);
		pMatRight = Mat(3, 4, CV_64F, pRight);

		cv::fisheye::initUndistortRectifyMap(kMatRight, dMatRight, rMatRight, pMatRight, stereo_size, CV_32FC1, rm1, rm2);
		cv::fisheye::initUndistortRectifyMap(kMatRight, dMatRight, rMatRight, pMatRight, Size(rawWidth, rawHeight), CV_32FC1, rightViewMap1, rightViewMap2);

		int vSize = int(w * h * 4 * 3);
		vertices = new float[vSize](); // 3 floats or 12 bytes per pixel.  
	}

	int *waitForFrame()
	{
		auto frameset = pipeline.wait_for_frames(1000);
		auto f = frameset.first_or_default(RS2_STREAM_POSE);
		IMU_TimeStamp = f.get_timestamp();

		auto fs = frameset.as<rs2::frameset>();
		leftImage = fs.get_fisheye_frame(1);
		rightImage = fs.get_fisheye_frame(2);

		leftViewRaw = Mat(rawHeight, rawWidth, CV_8U, (void *)leftImage.get_data());
		rightViewRaw = Mat(rawHeight, rawWidth, CV_8U, (void*)rightImage.get_data());

		remap(leftViewRaw, color, leftViewMap1, leftViewMap2, INTER_LINEAR);
		resize(color, color, Size(width, height));
		cvtColor(color, color, COLOR_GRAY2BGR);
		RGBDepth = color.clone();

		Mat remapLeft, remapRight;
		cv::remap(leftViewRaw, remapLeft, lm1, lm2, INTER_LINEAR);
		cv::remap(rightViewRaw, remapRight, rm1, rm2, INTER_LINEAR);

		Mat disp16s;
		//stereoPtr->compute(remapLeft, remapRight, disp16s);
		disp16s = sgm->Run(remapLeft, remapRight, maxDisp);

		Rect validRect = Rect(maxDisp, 0, disp16s.cols - maxDisp, disp16s.rows);
		disp16s = disp16s(validRect);
		disp16s.convertTo(disparity, CV_32F, 1.0f / 16.0f);

		Mat disp_vis = disparity.clone();

		Mat mask;
		threshold(disp_vis, mask, 1, 255, THRESH_BINARY);
		mask.convertTo(mask, CV_8U);
		disp_vis *= 255.0 / numDisp;

		// convert disparity To 0 - 255 And color it
		Mat tmpRGBDepth;
		cv::convertScaleAbs(disp_vis, disp_vis, 1);
		cv::applyColorMap(disp_vis, tmpRGBDepth, cv::COLORMAP_JET);
		cv::Rect depthRect = Rect(int(stereo_cx), 0, tmpRGBDepth.cols, tmpRGBDepth.rows);
		tmpRGBDepth.copyTo(RGBDepth(depthRect), mask);
		depth16 = disparity.clone();

		// Cast the frame to pose_frame and get its data
		pose_data = f.as<rs2::pose_frame>().get_pose_data();
		return (int *) color.data;
	}
};

extern "C" __declspec(dllexport)
int *T265Open(int w, int h)
{
	t265Camera *kc = new t265Camera(w, h);
	return (int *)kc;
}

extern "C" __declspec(dllexport)
int* T265intrinsicsLeft(t265Camera * tp)
{
	return (int*)&tp->intrinsicsLeft;
}

extern "C" __declspec(dllexport)
int T265RawWidth(t265Camera * tp)
{
	return tp->rawWidth;
}

extern "C" __declspec(dllexport)
int T265RawHeight(t265Camera * tp)
{
	return tp->rawHeight;
}

extern "C" __declspec(dllexport)
int T265Depth16Width(t265Camera * tp)
{
	return tp->stereo_height_px; // it is a square 300x300
}

extern "C" __declspec(dllexport)
int T265Depth16Height(t265Camera * tp)
{
	return tp->stereo_height_px;
}

extern "C" __declspec(dllexport)
int* T265Extrinsics(t265Camera* tp)
{
	return (int *) &tp->extrinsics;
}

extern "C" __declspec(dllexport)
int* T265PointCloud(t265Camera * tp)
{
	return (int*)&tp->vertices;
}

extern "C" __declspec(dllexport)
int* T265LeftRaw(t265Camera* tp)
{
	return (int *) tp->leftViewRaw.data;
}

extern "C" __declspec(dllexport)
int* T265RightRaw(t265Camera * tp)
{
	return (int*)tp->rightViewRaw.data;
}

extern "C" __declspec(dllexport)
int* T265Depth16(t265Camera * tp)
{
	return (int*)tp->depth16.data;
}

extern "C" __declspec(dllexport)
int* T265RGBDepth(t265Camera * tp)
{
	return (int*)tp->RGBDepth.data;
}

extern "C" __declspec(dllexport)
int* T265PoseData(t265Camera * tp)
{
	return (int*)&tp->pose_data;
}

extern "C" __declspec(dllexport)
double T265IMUTimeStamp(t265Camera * tp)
{
	return tp->IMU_TimeStamp;
}

extern "C" __declspec(dllexport)
int* T265WaitFrame(t265Camera * kc, void* color, void* depthRGB)
{
	return kc->waitForFrame();
}

extern "C" __declspec(dllexport)
float T265timeStampLatency(double timeStampMS)
{
	auto now = std::chrono::system_clock::now().time_since_epoch();
	double now_ms = static_cast<double>(std::chrono::duration_cast<std::chrono::milliseconds>(now).count());

	return static_cast<float>(std::max(0., (now_ms - timeStampMS)));
}