#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <iomanip>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/core/opengl.hpp"

#include "..\OpenGL_Algorithms\OpenGL_Basics\OpenGLcommon.h"
#include <windows.h>  

using namespace std;
using namespace cv;

struct DrawData
{
	ogl::Arrays arr;
	ogl::Texture2D tex;
	ogl::Buffer indices;
};

static const string winname = "OpenCVB OpenGL 3D";

static float tx = 0.f;
static float ty = 0.f;
static float thetaObs = -1.570f;
static float phiObs = 1.570f;
static float rObs = 10.f;
static float pi = static_cast<float>(CV_PI);
 
static int prevX = -1;
static int prevY = -1;
static int prevTheta = -1000;
static int prevPhi = -1000;
static DrawData *renderData;
static float yaw, roll, pitch;
static Mat_<int> indices;
static Mat_<Vec2f> texCoords;

static void draw(void* userdata)
{
	DrawData* renderData = static_cast<DrawData*>(userdata);
	glMatrixMode(GL_MODELVIEW);
	glLoadIdentity();
	gluLookAt(Eye.x , Eye.y, Eye.z, 0, 0, 10, 0, -1, 0);

	glTranslatef(0, 0, zTrans);
	glRotated(pitch, 1, 0, 0);
	glRotated(yaw,   0, 1, 0);
	glRotated(roll,  0, 0, 1);
	glTranslatef(0, 0, -zTrans);

	ogl::render(renderData->arr, renderData->indices, ogl::POINTS);
}

extern "C" __declspec(dllexport)
void OpenCVGL_Image_Open(int w, int h)
{
	namedWindow(winname, WINDOW_OPENGL | WINDOW_NORMAL);
	resizeWindow(winname, cv::Size(w, h));
	renderData = new DrawData;
	indices = Mat_<int>(1, (h - 1)*(6 * w));
	for (int c = w , nbPix = 0; c < w * h; c++)
	{
		indices.at<int>(0, nbPix++) = c;
		indices.at<int>(0, nbPix++) = c - 1;
		indices.at<int>(0, nbPix++) = c - w - 1;
		indices.at<int>(0, nbPix++) = c - w - 1;
		indices.at<int>(0, nbPix++) = c - w;
		indices.at<int>(0, nbPix++) = c;
	}
	renderData->indices.copyFrom(indices);
}

extern "C" __declspec(dllexport)
void OpenCVGL_Image_Close()
{
	destroyWindow(winname);
}

extern "C" __declspec(dllexport)
void OpenCVGL_Image_Control(float _ppx, float _ppy, float _fx, float _fy, float _FOV, float _zNear, float _zFar, float3 _eye, 
						    float _yaw, float _roll, float _pitch, int _pointSize, float _zTrans, int textureWidth, int textureHeight)
{
	ppx = _ppx;  ppy = _ppy; fx = _fx; fy = _fy; FOV = _FOV; zNear = _zNear; zFar = _zFar;
	Eye = (float3) _eye;
	yaw = _yaw; roll = _roll; pitch = _pitch; pointSize = _pointSize;
	zTrans = _zTrans;
	if (texCoords.total() != textureWidth * textureHeight) texCoords = Mat_<Vec2f>(1, textureWidth * textureHeight);
	float txWidth = float(textureWidth);
	float txHeight = float(textureHeight);
	for (int y = 0, nbPix = 0; y < textureHeight; y++)
	{
		for (int x = 0; x < textureWidth; x++, nbPix++)
		{
			texCoords.at< Vec2f>(0, nbPix) = Vec2f(float(x + 0.5) / txWidth, float(y + 0.5f) / txHeight);
		}
	}
}

extern "C" __declspec(dllexport)
void OpenCVGL_Image_Run(int *rgbPtr, int *pointCloud, int pcBufferSize, int rows, int cols)
{
	Mat rgb, depth;
	Mat_<Vec3f> vertex = Mat_<Vec3f>(1, rows * cols);
	rgb = Mat(rows, cols, CV_8UC3, rgbPtr);

	float* pc = (float *)pointCloud;
	for (int nbPix = 0; nbPix < rows * cols; nbPix++)
	{
		vertex.at<Vec3f>(0, nbPix) = Vec3f(pc[nbPix * 3], pc[nbPix * 3 + 1], pc[nbPix * 3 + 2]);
	}

	renderData->arr.setVertexArray(vertex);
	renderData->arr.setTexCoordArray(texCoords); 
	renderData->tex.copyFrom(rgb);

	glClearColor(1, 1, 1, 1);
	glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
	glPointSize((float)pointSize);

	drawAxes(10, 0, 0, 0);

	glMatrixMode(GL_PROJECTION);
	glLoadIdentity();
	gluPerspective(FOV, (double)cols / rows, zNear + 0.01, zFar);

	glMatrixMode(GL_MODELVIEW);
	glLoadIdentity();

	glEnable(GL_DEPTH_TEST);
	glEnable(GL_TEXTURE_2D);

	renderData->tex.bind();

	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
	glTexEnvi(GL_TEXTURE_ENV, GL_TEXTURE_ENV_MODE, GL_REPLACE);

	glDisable(GL_CULL_FACE);

	setOpenGlDrawCallback(winname, draw, renderData);

	updateWindow(winname);
}
