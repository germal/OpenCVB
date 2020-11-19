// Create a new project with this as the main module.  Simplest way: copy the OpenGL_Callbacks project to another directory.
// Rename the directory and project and add it to the OpenCVB solution.  Don't forget to update VB_Classes dependencies.
// When VB_Classes depends on your new project, your new OpenGL will recompile (if changed) with every restart of OpenCVB.
#include "example.hpp"          // Include short list of convenience functions for rendering
#define NOGLFW
#include "../OpenGL_Basics/OpenGLcommon.h"
#include <iostream>
#include <cmath>
#include <cfloat>

int main(int argc, char* argv[])
{
	GLuint gl_handle = 0;
	windowTitle << "OpenGL_Callbacks";
	initializeNamedPipeAndMemMap(argc, argv);

	window app(windowWidth, windowHeight, windowTitle.str().c_str());
	glfw_state MyState;
	register_glfw_callbacks(app, MyState);
	double pixels = imageWidth * imageHeight;

	while (app)
	{
		float tex_border_color[] = { 0.8f, 0.8f, 0.8f, 0.8f };
		readPipeAndMemMap();

		rgb.upload(rgbBuffer, imageWidth, imageHeight);
		tex.upload(textureBuffer, 256, 256);

		// OpenGL commands that prep screen for the pointcloud
		glLoadIdentity();
		glPushAttrib(GL_ALL_ATTRIB_BITS);

		// glClearColor(153.f / 255, 153.f / 255, 153.f / 255, 1);
		glClear(GL_DEPTH_BUFFER_BIT);

		glMatrixMode(GL_PROJECTION);
		glPushMatrix();
		gluPerspective(60, imageWidth / imageHeight, 0.01f, 10.0f);

		glMatrixMode(GL_MODELVIEW);
		glPushMatrix();
		gluLookAt(0, 0, 0, 0, 0, 1, 0, -1, 0);

		glTranslatef(0, 0, +1.5f + MyState.offset_y * 0.05f);
		glRotated(MyState.pitch, 1, 0, 0);
		glRotated(MyState.yaw, 0, 1, 0);
		glTranslatef(0, 0, -0.5f);

		glPointSize((float)pointSize);

		// draw and texture the floor --------------------------------------------------------------------------------------------------------
		glMatrixMode(GL_TEXTURE); 
		glEnable(GL_TEXTURE_2D);
		glBindTexture(GL_TEXTURE_2D, tex.get_gl_handle());
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT); 
		glBegin(GL_POLYGON);

		float* data = (float*)dataBuffer;
		// glColor3f(data[0], data[1], data[2]);
		float x = 10;
		float y = data[3];
		float z = 10;
		glTexCoord2f(0.0f, 100.0f); glVertex3f(-x, y, z);
		glTexCoord2f(100.0f, 0.0f); glVertex3f(-x, y, 0);
		glTexCoord2f(0.0f, 0.0f);  glVertex3f(x, y, 0);
		glTexCoord2f(100.0f, 100.0f); glVertex3f(x, y, z);

		glEnd();
		glDisable(GL_TEXTURE_2D);

		// draw the scene ---------------------------------------------------------------------------------------------------------------------
		glEnable(GL_DEPTH_TEST);
		glEnable(GL_TEXTURE_2D);
		glBindTexture(GL_TEXTURE_2D, rgb.get_gl_handle());
		glTexParameterfv(GL_TEXTURE_2D, GL_TEXTURE_BORDER_COLOR, tex_border_color);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, 0x812F); // GL_CLAMP_TO_EDGE
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, 0x812F); // GL_CLAMP_TO_EDGE

		glBegin(GL_POINTS);

		float2 pt; int pcIndex = 0; float3* pcIntel = (float3*)pointCloudBuffer;
		for (int y = 0; y < imageHeight; ++y)
		{
			for (int x = 0; x < imageWidth; ++x)
			{
				if (pcIntel[pcIndex].z > 0)
				{
					glVertex3fv((float*)&pcIntel[pcIndex]);
					pt.x = (float)((x + 0.5f) / imageWidth);
					pt.y = (float)((y + 0.5f) / imageHeight);
					glTexCoord2fv((const GLfloat*)&pt);
				}
				pcIndex++;
			}
		}

		glEnd();
		glDisable(GL_TEXTURE_2D);

		drawAxes(10, 0, 0, 1);

		glPopMatrix();
		glMatrixMode(GL_PROJECTION);
		glPopMatrix();
		glPopAttrib();

		glfwSetWindowTitle(app, imageLabel);
		if (ackBuffers()) break;
	}

	CloseHandle(hMapFile);
	CloseHandle(pipe);
	return EXIT_SUCCESS;
}
