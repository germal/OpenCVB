#define _USE_MATH_DEFINES
#include <math.h>
#include "..\OpenGL_Basics\OpenGLcommon.h"
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>

using namespace cv;

struct short3
{
	uint16_t x, y, z;
};
#include "d435.h"

void draw_axes()
{
	glLineWidth(2);
	glBegin(GL_LINES);
	// Draw x, y, z axes
	glColor3f(1, 0, 0); glVertex3f(0, 0, 0);  glVertex3f(-1, 0, 0);
	glColor3f(0, 1, 0); glVertex3f(0, 0, 0);  glVertex3f(0, -1, 0);
	glColor3f(0, 0, 1); glVertex3f(0, 0, 0);  glVertex3f(0, 0, 1);
	glEnd();

	glLineWidth(1);
}

void render_scene(state app_state)
{
	glClearColor(0.0, 0.0, 0.0, 1.0);
	glColor3f(1.0, 1.0, 1.0);

	glMatrixMode(GL_PROJECTION);
	glLoadIdentity();
	gluPerspective(60.0, 4.0 / 3.0, 1, 40);

	glClear(GL_COLOR_BUFFER_BIT);
	glMatrixMode(GL_MODELVIEW);

	glLoadIdentity();
	gluLookAt(1, 0, 5, 1, 0, 0, 0, -1, 0);

	glTranslatef(0, 0, +0.5f + app_state.offset_y*0.05f);
	glRotated(app_state.pitch, -1, 0, 0);
	glRotated(app_state.yaw,    0, 1, 0);
	glRotated(app_state.roll,   0, 0, 1);
	draw_floor(8, 1, 0);
}

class camera_renderer
{
	std::vector<float3> positions, normals;
	std::vector<short3> indexes;
public:
	// Initialize renderer with data needed to draw the camera
	camera_renderer()
	{
		uncompress_d435_obj(positions, normals, indexes);
	}

	// Takes the calculated angle as input and rotates the 3D camera model accordignly
	void render_camera(float3 theta)
	{

		glEnable(GL_BLEND);
		glBlendFunc(GL_ONE, GL_ONE);

		glPushMatrix();
		// Set the rotation, converting theta to degrees
		glRotatef((float) (theta.x * 180 / M_PI), 0, 0, -1);
		glRotatef((float) (theta.y * 180 / M_PI), 0, -1, 0);
		glRotatef((float) ((theta.z - M_PI / 2) * 180 / M_PI), -1, 0, 0);

		draw_axes();

		// Scale camera drawing
		glScalef(0.035f, 0.035f, 0.035f);

		glBegin(GL_TRIANGLES);
		// Draw the camera
		for (auto& i : indexes)
		{
			glVertex3fv(&positions[i.x].x);
			glVertex3fv(&positions[i.y].x);
			glVertex3fv(&positions[i.z].x);
			glColor4f(0.15f, 0.15f, 0.15f, 1.0f);
		}
		glEnd();

		glPopMatrix();

		glDisable(GL_BLEND);
		glFlush();
	}

};

class rotation_estimator
{
	// theta is the angle of camera rotation in x, y and z components
	float3 theta;
	std::mutex theta_mtx;
	bool first = true;
	// Keeps the arrival time of previous gyro frame
	double last_ts_gyro = 0;
public:
	// Function to calculate the change in angle of motion based on data from gyro
	void process_gyro(float3 gyro_data, double ts)
	{
		if (first) // On the first iteration, use only data from accelerometer to set the camera's initial position
		{
			last_ts_gyro = ts;
			return;
		}
		// Holds the change in angle, as calculated from gyro
		float3 gyro_angle;

		// Initialize gyro_angle with data from gyro
		gyro_angle.x = gyro_data.x; // Pitch
		gyro_angle.y = gyro_data.y; // Yaw
		gyro_angle.z = gyro_data.z; // Roll

		// Compute the difference between arrival times of previous and current gyro frames
		double dt_gyro = (ts - last_ts_gyro) / timeConversionUnits;  // units problem with Kinect?
		last_ts_gyro = ts;

		// Change in angle equals gyro measures * time passed since last measurement
		gyro_angle = gyro_angle * (float) dt_gyro;

		// Apply the calculated change of angle to the current angle (theta)
		std::lock_guard<std::mutex> lock(theta_mtx);
		theta.add(-gyro_angle.z, -gyro_angle.y, gyro_angle.x);
	}

	void process_accel(float3 accel_data)
	{
		// Holds the angle as calculated from accelerometer data
		float3 accel_angle;
		accel_angle.x = accel_angle.y = accel_angle.z = 0;

		// Calculate rotation angle from accelerometer data
		accel_angle.z = atan2(accel_data.y, accel_data.z);
		accel_angle.x = atan2(accel_data.x, sqrt(accel_data.y * accel_data.y + accel_data.z * accel_data.z));

		// If it is the first iteration, set initial pose of camera according to accelerometer data (note the different handling for Y axis)
		std::lock_guard<std::mutex> lock(theta_mtx);
		if (first)
		{
			first = false;
			theta = accel_angle;
			// Since we can't infer the angle around Y axis using accelerometer data, we'll use PI as a convetion for the initial pose
			theta.y = (float) M_PI;
		}
		else
		{
			/*
			Apply Complementary Filter:
				- high-pass filter = theta * imuAlphaFactor:  allows short-duration signals to pass through while filtering out signals
				  that are steady over time, is used to cancel out drift.
				- low-pass filter = accel * (1- imuAlphaFactor): lets through long term changes, filtering out short term fluctuations
			*/
			theta.x = theta.x * imuAlphaFactor + accel_angle.x * (1 - imuAlphaFactor);
			theta.z = theta.z * imuAlphaFactor + accel_angle.z * (1 - imuAlphaFactor);
		}
	}

	// Returns the current rotation angle
	float3 get_theta()
	{
		std::lock_guard<std::mutex> lock(theta_mtx);
		return theta;
	}
};

int main(int argc, char * argv[])
{
	windowTitle << "OpenCVB IMU Demo"; // this will create the window title.
	if (initializeNamedPipeAndMemMap(argc, argv) != 0) return -1;

	glfwInit();
	win = glfwCreateWindow(windowWidth, windowHeight, windowTitle.str().c_str(), 0, 0);

	app_state = { 0, 0, 0, 0, false, 0 };
	glfwSetWindowUserPointer(win, &app_state);

	glfwMakeContextCurrent(win);
	camera_renderer camera;
	rotation_estimator algo; 
	
	while (!glfwWindowShouldClose(win))
	{
		glfwPollEvents();

		readPipeAndMemMap();

		glPointSize((float)pointSize);
		glPushAttrib(GL_ALL_ATTRIB_BITS);

		glfwGetFramebufferSize(win, &windowWidth, &windowHeight);
		glViewport(0, 0, windowWidth, windowHeight);
		glClearColor(1, 1, 1, 1);
		glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

		glMatrixMode(GL_PROJECTION);
		glPushMatrix();
		gluPerspective(FOV, (float)windowWidth / windowHeight, zNear + 0.01, zFar);

		glMatrixMode(GL_MODELVIEW);
		glPushMatrix();
		gluLookAt(Eye.x, Eye.y, Eye.z, 0, 0, 10, 0, -1, 0);

		float zTrans = 0.5;
		glTranslatef(0, 0, zTrans);
		glRotated(app_state.pitch, 1, 0, 0);
		glRotated(app_state.yaw,   0, 1, 0);
		glRotated(app_state.roll,  0, 0, 1);
		glTranslatef(0, 0, -zTrans);

		glEnable(GL_DEPTH_TEST);
		glEnable(GL_TEXTURE_2D);
		glBindTexture(GL_TEXTURE_2D, tex.get_gl_handle());

		glColor3f(1, 1, 1);

		// this is the real workhorse
		algo.process_gyro(gyro_data, timestamp);
		algo.process_accel(accel_data);
		render_scene(app_state);
		camera.render_camera(algo.get_theta());
		// end of real workhorse

		glDisable(GL_TEXTURE_2D);

		glPopMatrix();
		glMatrixMode(GL_PROJECTION);
		glPopMatrix();
		glPopAttrib();

		glfwGetWindowSize(win, &windowWidth, &windowHeight);
		glPushAttrib(GL_ALL_ATTRIB_BITS);
		glPushMatrix();
		glOrtho(0, windowWidth, windowHeight, 0, zNear, zFar);

		glPopMatrix();

		glfwSwapBuffers(win);

		if (ackBuffers()) break;
	}

	CloseHandle(hMapFile);
	CloseHandle(pipe);
	glfwDestroyWindow(win);
	glfwTerminate();
	return EXIT_SUCCESS;
}
