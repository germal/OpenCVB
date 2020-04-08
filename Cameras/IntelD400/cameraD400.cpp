#include <iostream>
#include <list> 
#include <iterator> 
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
#include <map>
#include "example.hpp"

using namespace std;
using namespace cv;

#if 1
class D400Camera
{
public:
	rs2_intrinsics intrinsicsLeft;
	rs2_extrinsics extrinsics;
	rs2::pipeline_profile profiles;
	rs2::pipeline pipeline;
	rs2::frameset frames;
	rs2::colorizer colorizer;
	rs2::align align = rs2::align(RS2_STREAM_COLOR);
	rs2::pointcloud pc;

private:
	int width, height;

	rs2::context ctx;

public:
	~D400Camera(){}

	D400Camera(int w, int h, bool IMUPresent)
	{
		rs2_error* e = 0;
		width = w;
		height = h;

		rs2::config cfg;
		cfg.enable_stream(RS2_STREAM_COLOR, width, height, RS2_FORMAT_BGR8);
		cfg.enable_stream(RS2_STREAM_DEPTH, width, height, RS2_FORMAT_Z16);
		cfg.enable_stream(RS2_STREAM_INFRARED, 1, width, height, RS2_FORMAT_Y8);
		cfg.enable_stream(RS2_STREAM_INFRARED, 2, width, height, RS2_FORMAT_Y8);

		if (IMUPresent)
		{
			cfg.enable_stream(RS2_STREAM_GYRO);
			cfg.enable_stream(RS2_STREAM_ACCEL);
		}

		profiles = pipeline.start(cfg);

		auto stream = profiles.get_stream(RS2_STREAM_COLOR);
		intrinsicsLeft = stream.as<rs2::video_stream_profile>().get_intrinsics();
		auto fromStream = profiles.get_stream(RS2_STREAM_COLOR);
		auto toStream = profiles.get_stream(RS2_STREAM_INFRARED);

		extrinsics = fromStream.get_extrinsics_to(toStream);
	}

	void waitForFrame()
	{
		frames = pipeline.wait_for_frames(1000);
	}
};


extern "C" __declspec(dllexport)
int *D400Open(int w, int h, bool IMUPresent)
{
	D400Camera* tp = new D400Camera(w, h, IMUPresent);
	return (int *)tp;
}

extern "C" __declspec(dllexport)
int* D400intrinsicsLeft(D400Camera * tp)
{
	return (int*)&tp->intrinsicsLeft;
}

extern "C" __declspec(dllexport)
int* D400Extrinsics(D400Camera* tp)
{
	return (int *) &tp->extrinsics;
}

extern "C" __declspec(dllexport)
double D400IMUTimeStamp(D400Camera* tp)
{
	auto gyro = tp->frames.first_or_default(RS2_STREAM_GYRO, RS2_FORMAT_MOTION_XYZ32F);
	if (gyro == 0) return 0;
	return gyro.get_timestamp();
}

extern "C" __declspec(dllexport)
int* D400Gyro(D400Camera * tp)
{
	auto gyro = tp->frames.first_or_default(RS2_STREAM_GYRO, RS2_FORMAT_MOTION_XYZ32F);
	if (gyro == 0) return 0;
	return (int*)gyro.get_data();
}

extern "C" __declspec(dllexport)
int * D400Accel(D400Camera * tp)
{
	auto accel = tp->frames.first_or_default(RS2_STREAM_ACCEL, RS2_FORMAT_MOTION_XYZ32F);
	if (accel == 0) return 0;
	return (int *)accel.get_data();
}

extern "C" __declspec(dllexport)
int* D400PointCloud(D400Camera * tp)
{
	return (int*)tp->pc.process(tp->frames.get_depth_frame()).as<rs2::points>().get_data();
}

extern "C" __declspec(dllexport)
int* D400Color(D400Camera * tp)
{
	rs2::frameset procFrames = tp->colorizer.process(tp->frames);
    procFrames = tp->align.process(procFrames);
	return (int*)procFrames.get_color_frame().get_data();
}

extern "C" __declspec(dllexport)
int* D400LeftRaw(D400Camera* tp)
{
	return (int*)tp->frames.get_infrared_frame(1).get_data();
}

extern "C" __declspec(dllexport)
int* D400RightRaw(D400Camera * tp)
{
	return (int*)tp->frames.get_infrared_frame(2).get_data();
}

extern "C" __declspec(dllexport)
int* D400Depth16(D400Camera * tp)
{
	return (int*)tp->frames.get_depth_frame().get_data();
}

extern "C" __declspec(dllexport)
int* D400RGBDepth(D400Camera * tp)
{
	auto RGBDepth = tp->colorizer.process(tp->frames.get_depth_frame());
	return (int*)RGBDepth.get_data();
}

extern "C" __declspec(dllexport)
void D400WaitForFrame(D400Camera * tp)
{
	tp->waitForFrame();
}
#else
class D400Camera
{
public:
    rs2::pipeline pipe;
    rs2::pipeline_profile profile;
    rs2_extrinsics extrinsics;
    rs2_intrinsics intrinsics;
    long long pixelCount = 0;

    rs2_stream find_stream_to_align(const std::vector<rs2::stream_profile>& streams)
    {
        //Given a vector of streams, we try to find a depth stream and another stream to align depth with.
        //We prioritize color streams to make the view look better.
        //If color is not available, we take another stream that (other than depth)
        rs2_stream align_to = RS2_STREAM_ANY;
        bool depth_stream_found = false;
        bool color_stream_found = false;
        for (rs2::stream_profile sp : streams)
        {
            rs2_stream profile_stream = sp.stream_type();
            if (profile_stream != RS2_STREAM_DEPTH)
            {
                if (!color_stream_found)         //Prefer color
                    align_to = profile_stream;

                if (profile_stream == RS2_STREAM_COLOR)
                {
                    color_stream_found = true;
                }
            }
            else
            {
                depth_stream_found = true;
            }
        }

        if (!depth_stream_found)
            throw std::runtime_error("No Depth stream available");

        if (align_to == RS2_STREAM_ANY)
            throw std::runtime_error("No stream found to align with Depth");

        return align_to;
    }

    float get_depth_scale(rs2::device dev)
    {
        // Go over the device's sensors
        for (rs2::sensor& sensor : dev.query_sensors())
        {
            // Check if the sensor if a depth sensor
            if (rs2::depth_sensor dpt = sensor.as<rs2::depth_sensor>())
            {
                return dpt.get_depth_scale();
            }
        }
        throw std::runtime_error("Device does not have a depth sensor");
    }

    bool profile_changed(const std::vector<rs2::stream_profile>& current, const std::vector<rs2::stream_profile>& prev)
    {
        for (auto&& sp : prev)
        {
            //If previous profile is in current (maybe just added another)
            auto itr = std::find_if(std::begin(current), std::end(current), [&sp](const rs2::stream_profile& current_sp) { return sp.unique_id() == current_sp.unique_id(); });
            if (itr == std::end(current)) //If it previous stream wasn't found in current
            {
                return true;
            }
        }
        return false;
    }

    void remove_background(rs2::video_frame& other_frame, const rs2::depth_frame& depth_frame, float depth_scale, float clipping_dist)
    {
        const uint16_t* p_depth_frame = reinterpret_cast<const uint16_t*>(depth_frame.get_data());
        uint8_t* p_other_frame = reinterpret_cast<uint8_t*>(const_cast<void*>(other_frame.get_data()));

        int width = other_frame.get_width();
        int height = other_frame.get_height();
        int other_bpp = other_frame.get_bytes_per_pixel();

#pragma omp parallel for schedule(dynamic) //Using OpenMP to try to parallelise the loop
        for (int y = 0; y < height; y++)
        {
            auto depth_pixel_index = y * width;
            for (int x = 0; x < width; x++, ++depth_pixel_index)
            {
                // Get the depth value of the current pixel
                auto pixels_distance = depth_scale * p_depth_frame[depth_pixel_index];

                // Check if the depth value is invalid (<=0) or greater than the threashold
                if (pixels_distance <= 0.f || pixels_distance > clipping_dist)
                {
                    // Calculate the offset in other frame's buffer to current pixel
                    auto offset = depth_pixel_index * other_bpp;

                    // Set pixel to "background" color (0x999999)
                    std::memset(&p_other_frame[offset], 0x99, other_bpp);
                }
            }
        }
    }
private:
    int width, height; 

public:
    ~D400Camera() {}
    
    D400Camera(int width, int height, bool IMUPresent)
    {
        rs2::config cfg;
        cfg.enable_stream(RS2_STREAM_COLOR, width, height, RS2_FORMAT_BGR8);
        cfg.enable_stream(RS2_STREAM_DEPTH, width, height, RS2_FORMAT_Z16);
        cfg.enable_stream(RS2_STREAM_INFRARED, 1, width, height, RS2_FORMAT_Y8);
        cfg.enable_stream(RS2_STREAM_INFRARED, 2, width, height, RS2_FORMAT_Y8);

        if (IMUPresent)
        {
            cfg.enable_stream(RS2_STREAM_GYRO);
            cfg.enable_stream(RS2_STREAM_ACCEL);
        }

        pixelCount = (long long) width * height;
        profile = pipe.start(cfg);
    }
};

D400Camera* cPtr;

extern "C"  __declspec(dllexport)
void D400Open(int width, int height, bool IMUPresent)
{
    cPtr = new D400Camera(width, height, IMUPresent);
}

extern "C"  __declspec(dllexport)
double D400WaitForFrame(int *rgbImage, int *rgbDepth, int *depth16, int *left, int *right, int *pointCloud, int *gyroPtr, int *accelPtr)
{
    static rs2_stream align_to = cPtr->find_stream_to_align(cPtr->profile.get_streams());
    static rs2::align align(align_to);
    auto frameset = cPtr->pipe.wait_for_frames();

    //// rs2::pipeline::wait_for_frames() can replace the device it uses in case of device error or disconnection.
    //// Since rs2::align is aligning depth to some other stream, we need to make sure that the stream was not changed
    ////  after the call to wait_for_frames();
    //if (cPtr->profile_changed(cPtr->pipe.get_active_profile().get_streams(), cPtr->profile.get_streams()))
    //{
    //    //If the profile was changed, update the align object, and also get the new device's depth scale
    //    cPtr->profile = cPtr->pipe.get_active_profile();
    //    align_to = cPtr->find_stream_to_align(cPtr->profile.get_streams());
    //    align = rs2::align(align_to);
    //    //depth_scale = get_depth_scale(profile.get_device());
    //}

    auto processed = align.process(frameset);

    // Trying to get both other and aligned depth frames
    rs2::video_frame colorFrame = processed.first(align_to);
    memcpy(rgbImage, colorFrame.get_data(), 1280 * 720 * 3);

    static rs2::colorizer c;
    rs2::depth_frame aligned_depth_frame = processed.get_depth_frame();
    memcpy(rgbDepth, c.process(aligned_depth_frame).get_data(), cPtr->pixelCount * 3);

    memcpy(depth16, aligned_depth_frame.get_data(), cPtr->pixelCount * 2);

    memcpy(left, frameset.get_infrared_frame(1).get_data(), cPtr->pixelCount);

    memcpy(right, frameset.get_infrared_frame(2).get_data(), cPtr->pixelCount);

    rs2::pointcloud pc;
    //memcpy(pointCloud, pc.process(frameset.get_depth_frame()).get_data(), cPtr->pixelCount * 12);

    auto gyro = frameset.first_or_default(RS2_STREAM_GYRO, RS2_FORMAT_MOTION_XYZ32F);
    memcpy(gyroPtr, gyro.get_data(), 12);
    auto accel = frameset.first_or_default(RS2_STREAM_ACCEL, RS2_FORMAT_MOTION_XYZ32F);
    memcpy(accelPtr, accel.get_data(), 12); 
    return gyro.get_timestamp();
}

//extern "C"  __declspec(dllexport)
//int* D400RGBDepth()
//{
//    static rs2::colorizer c;
//    rs2::depth_frame aligned_depth_frame = cPtr->processed.get_depth_frame();
//    return (int*)c.process(aligned_depth_frame).get_data();
//}
//
//extern "C"  __declspec(dllexport)
//int* D400PointCloud()
//{
//    return (int*)cPtr->pc.process(cPtr->frameset.get_depth_frame()).get_data();
//}
//
//extern "C" __declspec(dllexport)
//int* D400LeftRaw()
//{
//    return (int*)cPtr->frameset.get_infrared_frame(1).get_data();
//}
//
//extern "C" __declspec(dllexport)
//int* D400RightRaw()
//{
//    return (int*)cPtr->frameset.get_infrared_frame(2).get_data();
//}
//
extern "C" __declspec(dllexport)
int* D400Intrinsics()
{
    auto stream = cPtr->profile.get_stream(RS2_STREAM_COLOR);
    cPtr->intrinsics = stream.as<rs2::video_stream_profile>().get_intrinsics();
    return (int*)&cPtr->intrinsics;
}

extern "C" __declspec(dllexport)
int* D400Extrinsics()
{
    auto fromStream = cPtr->profile.get_stream(RS2_STREAM_COLOR);
    auto toStream = cPtr->profile.get_stream(RS2_STREAM_INFRARED);
    cPtr->extrinsics = fromStream.get_extrinsics_to(toStream);
    return (int*)&cPtr->extrinsics;
}
//
//extern "C" __declspec(dllexport)
//double D400IMUTimeStamp()
//{
//    auto gyro = cPtr->frameset.first_or_default(RS2_STREAM_GYRO, RS2_FORMAT_MOTION_XYZ32F);
//    if (gyro == 0) return 0;
//    return gyro.get_timestamp();
//}
//
//extern "C" __declspec(dllexport)
//int* D400Gyro()
//{
//    auto gyro = cPtr->frameset.first_or_default(RS2_STREAM_GYRO, RS2_FORMAT_MOTION_XYZ32F);
//    if (gyro == 0) return 0;
//    return (int*)gyro.get_data();
//}
//
//extern "C" __declspec(dllexport)
//int* D400Accel()
//{
//    auto accel = cPtr->frameset.first_or_default(RS2_STREAM_ACCEL, RS2_FORMAT_MOTION_XYZ32F);
//    if (accel == 0) return 0;
//    return (int*)accel.get_data();
//}
#endif