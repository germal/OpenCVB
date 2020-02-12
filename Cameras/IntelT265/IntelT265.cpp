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
#include <opencv2/calib3d.hpp>
using namespace std;
using namespace cv;

class t265Camera
{
public:
	int frame_counter = 0;
private:
	rs2::pipeline pipe;
	//rs2::config cfg;
	std::mutex data_mutex;
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

	t265Camera()
	{
		rs2::pipeline pipe;
		rs2::config cfg;
		//cfg.enable_stream(RS2_STREAM_POSE, RS2_FORMAT_6DOF);
		//cfg.enable_stream(RS2_STREAM_FISHEYE, 1, RS2_FORMAT_Y8);
		//cfg.enable_stream(RS2_STREAM_FISHEYE, 2, RS2_FORMAT_Y8);

		//pipe.start(cfg);


		cfg.enable_stream(RS2_STREAM_POSE, RS2_FORMAT_6DOF);
		// Enable both image streams
		// Note: It is not currently possible to enable only one
		cfg.enable_stream(RS2_STREAM_FISHEYE, 1, RS2_FORMAT_Y8);
		cfg.enable_stream(RS2_STREAM_FISHEYE, 2, RS2_FORMAT_Y8);

		// Define frame callback

		// The callback is executed on a sensor thread and can be called simultaneously from multiple sensors
		// Therefore any modification to common memory should be done under lock
		std::mutex data_mutex;
		uint64_t pose_counter = 0;
		uint64_t frame_counter = 0;
		bool first_data = true;
		auto callback = [&](const rs2::frame& frame)
		{
			std::lock_guard<std::mutex> lock(data_mutex);

			if (auto fp = frame.as<rs2::pose_frame>()) {
				pose_counter++;
			}
			if (auto fs = frame.as<rs2::frameset>()) {
				auto colorImage = fs.get_color_frame();
				frame_counter++;
			}
		};

		// Start streaming through the callback
		rs2::pipeline_profile profiles = pipe.start(cfg, callback);

		// Sleep this thread until we are done
		while (true) {
			std::this_thread::sleep_for(std::chrono::milliseconds(10));
		}
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
int *T265Open()
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
int* T265WaitFrame(t265Camera* kc, void* color, void* depthRGB)
{
	return kc->waitForFrame();
}

extern "C" __declspec(dllexport)
void T265Close(t265Camera *kc)
{
	delete kc;
}








class t265sgm
{
private:
public:
	Mat disparity;
	Ptr<StereoSGBM> stereo;
	t265sgm()
	{
		stereo = cv::StereoSGBM::create(0, 112, 16, 8 * 3 * 25, 32 * 3 * 25, 1, 0, 10, 100, 32);
	}
	void Run(Mat leftimg, Mat rightimg, int maxDisp)
	{
		Mat disp16s;
		stereo->compute(leftimg, rightimg, disp16s);
		Rect validRect = Rect(maxDisp, 0, disp16s.cols - maxDisp, disp16s.rows);
		disp16s = disp16s(validRect);
		disp16s.convertTo(disparity, CV_32F, 1.0f / 16.0f);
	}
};

extern "C" __declspec(dllexport)
t265sgm * t265sgm_Open()
{
	t265sgm* tPtr = new t265sgm();
	return tPtr;
}

extern "C" __declspec(dllexport)
void t265sgm_Close(t265sgm * tPtr)
{
	delete tPtr;
}
extern "C" __declspec(dllexport)
int* t265sgm_Run(t265sgm * tPtr, int* leftPtr, int* rightPtr, int rows, int cols, int maxDisp)
{
	Mat leftimg = Mat(rows, cols, CV_8UC1, leftPtr);
	Mat rightimg = Mat(rows, cols, CV_8UC1, rightPtr);
	tPtr->Run(leftimg, rightimg, maxDisp);
	return (int*)tPtr->disparity.data; // return this C++ allocated data to managed code where it will be used in the marshal.copy
}



