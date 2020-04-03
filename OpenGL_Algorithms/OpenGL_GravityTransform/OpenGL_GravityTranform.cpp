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
	windowTitle << "OpenGL_GravityTransform";
	initializeNamedPipeAndMemMap(argc, argv);

	window app(windowWidth, windowHeight, windowTitle.str().c_str());
	glfw_state MyState;
	register_glfw_callbacks(app, MyState);
	MyState.offset_y = 100.0f;
	double pixels = imageWidth * imageHeight;

	while (app)
	{
		readPipeAndMemMap();

		tex.upload(rgbBuffer, imageWidth, imageHeight);

		// OpenGL commands that prep screen for the pointcloud
		glLoadIdentity();
		glPushAttrib(GL_ALL_ATTRIB_BITS);

		glClearColor(153.f / 255, 153.f / 255, 153.f / 255, 1);
		glClear(GL_DEPTH_BUFFER_BIT);

		glMatrixMode(GL_PROJECTION);
		glPushMatrix();
		gluPerspective(60, imageWidth / imageHeight, 0.01f, 10.0f);

		glMatrixMode(GL_MODELVIEW);
		glPushMatrix();
		gluLookAt(0, 0, 0, 0, 0, 1, 0, -1, 0);

		glTranslatef(0, 0, +1.5f + MyState.offset_y * 0.05f);
		glRotated(imuAngleY * 57.2958, 1, 0, 0);
		glRotated(imuAngleX * 57.2958, 0, 1, 0);
		glRotated(imuAngleZ * 57.2958, 1, 0, 0);
		glTranslatef(0, 0, -0.1f);

		glPointSize((float)pointSize);
		glEnable(GL_DEPTH_TEST);
		glEnable(GL_TEXTURE_2D);
		glBindTexture(GL_TEXTURE_2D, tex.get_gl_handle());
		float tex_border_color[] = { 0.8f, 0.8f, 0.8f, 0.8f };
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
					glTexCoord2fv((const GLfloat*)& pt);
				}
				pcIndex++;
			}
		}

		glEnd();
		glDisable(GL_TEXTURE_2D);

		glPopMatrix();
		glMatrixMode(GL_PROJECTION);
		glPopMatrix();
		glPopAttrib();
		
		if (ackBuffers()) break;
	}

	CloseHandle(hMapFile);
	CloseHandle(pipe);
	return EXIT_SUCCESS;
}
