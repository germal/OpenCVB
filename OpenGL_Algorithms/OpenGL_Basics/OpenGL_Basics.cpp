// License: Apache 2.0. See LICENSE file in root directory.
// Copyright(c) 2015 Intel Corporation. All Rights Reserved.
#include "OpenGLcommon.h"

int main(int argc, char * argv[])
{ 
	windowTitle << "OpenCVB Point Cloud"; // this will create the window title.
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

		tex.upload(rgbBuffer, imageWidth, imageHeight);

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
		glScalef(scaleXYZ.x, scaleXYZ.y, scaleXYZ.z);

		glTranslatef(0, 0, zTrans);
		glRotated(app_state.pitch, 1, 0, 0);
		glRotated(app_state.yaw,   0, 1, 0);
		glRotated(app_state.roll,  0, 0, 1);
		glTranslatef(0, 0, -zTrans);

		glEnable(GL_DEPTH_TEST);
		glEnable(GL_TEXTURE_2D);
		glBindTexture(GL_TEXTURE_2D, tex.get_gl_handle());

		glColor3f(1, 1, 1);
		glBegin(GL_POINTS);

		float2 pt; int pcIndex = 0; float3* pcIntel = (float3*)pointCloudBuffer;
		for (int y = 0; y < imageHeight; ++y)
		{
			for (int x = 0; x < imageWidth; ++x)
			{
				if (pcIntel[pcIndex].z > 0)
				{
					glVertex3fv((float*)& pcIntel[pcIndex]);
					pt.x = (float)((x + 0.5f) / imageWidth);
					pt.y = (float)((y + 0.5f) / imageHeight);
					glTexCoord2fv((const GLfloat*)& pt);
				}
				pcIndex++;
			}
		}

		glEnd();
		glDisable(GL_TEXTURE_2D);

		drawAxes(10, 0, 0, 1);
		draw_floor(10);

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
