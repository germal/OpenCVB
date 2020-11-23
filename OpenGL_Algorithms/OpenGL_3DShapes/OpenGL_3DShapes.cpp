#include "../OpenGL_Basics/OpenGLcommon.h"
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
using namespace cv;

int main(int argc, char * argv[])
{ 
	windowTitle << "OpenCVB Data Cloud"; // this will create the window title.
	if (initializeNamedPipeAndMemMap(argc, argv) != 0) return -1;

	glfwInit();
	win = glfwCreateWindow(windowWidth, windowHeight, windowTitle.str().c_str(), 0, 0);

	app_state = { 0, 0, 0, 0, false, 0 };
	glfwSetWindowUserPointer(win, &app_state);
	glfwMakeContextCurrent(win);

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
		glRotated(app_state.yaw, 0, 1, 0);
		glRotated(app_state.roll, 0, 0, 1);
		glTranslatef(0, 0, -zTrans);

		glEnable(GL_DEPTH_TEST);
		glEnable(GL_TEXTURE_2D);
		glBindTexture(GL_TEXTURE_2D, rgb.get_gl_handle());

		glColor3f(1, 1, 1);
		glBegin(GL_POINTS);

		Mat gray(dataHeight, dataWidth, CV_8U, dataBuffer);

		int planes = 10; // an arbitrary number of planes
		if (planes > rgbBufferSize / 3) planes = rgbBufferSize / 3; // only 256 colors are supplied.
		float3 xyz;
		for (int y = 0; y < dataHeight; ++y)
		{
			for (int x = 0; x < dataWidth; ++x)
			{
				uchar next = gray.at<uchar>(y, x); 
				if (next != 255)
				{
					xyz.x = (float)(x) / dataWidth;
					xyz.y = (float)(-y) / dataHeight;
					for (int plane = 0; plane < planes; ++plane)
					{
						glColor3ub(rgbBuffer[plane % 255], rgbBuffer[plane % 255 + 1], rgbBuffer[plane % 255 + 2]);
						xyz.z = (float)plane;
						glVertex3fv((const GLfloat *)&xyz);
					}
				}
			}
		}

		glEnd();
		glDisable(GL_TEXTURE_2D);

		float zDistance = 10.0f;
		drawAxes(100.0f, 0, 0, zDistance);
		draw_floor(20, 1, 0);

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
