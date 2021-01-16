//#define WITH_VTK
#ifdef WITH_VTK

#ifndef NO_EXPAND_VTK
#define NOMINMAX

#include <stdio.h>
#include <chrono>
#include <vector>
#include <sstream>
#include <iostream>
#include <algorithm>
#include <winsock2.h>
#include <chrono>
#include <thread>
#include <tchar.h>
#include <string>

#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>

static HANDLE pipe;
static int MemMapBufferSize;
static double *sharedMem; 
static int pointSize;
static int dataBufferSize;
static float *dataBuffer;
static int rgbBufferSize;
static unsigned char *rgbBuffer;
static HANDLE hMapFile; 
static int windowWidth;
static int windowHeight;
static int dataWidth;
static int dataHeight;
static std::ostringstream windowTitle;
static int lastFrame = 0;
static int rgbWidth;
static int rgbHeight;
cv::Mat src;
cv::Mat data32f;
#define USER_DATA_LENGTH (10)
static double UserData[USER_DATA_LENGTH];

std::wstring s2ws(const std::string& s)
{
	int len;
	int slength = (int)s.length() + 1;
	len = MultiByteToWideChar(CP_ACP, 0, s.c_str(), slength, 0, 0);
	wchar_t* buf = new wchar_t[len];
	MultiByteToWideChar(CP_ACP, 0, s.c_str(), slength, buf, len);
	std::wstring r(buf);
	delete[] buf;
	return r;
}

int initializeNamedPipeAndMemMap(int argc, char * argv[])
{
	if (argc != 5)
	{
		MessageBox(0, L"Incorrect number of parameters.  There should be 5: <program> <width> <height> <MemMapBufferSize> <pipeName>", L"OpenCVB", MB_OK);
		MessageBox(0, L"Use OpenCVB as the startup project in Visual Studio", L"OpenCVB", MB_OK);
		return -1;
	}

	windowWidth = std::stoi(argv[1]);
	windowHeight = std::stoi(argv[2]);
	MemMapBufferSize = std::stoi(argv[3]);
	printf("MemMapBufferSize = %d\n", MemMapBufferSize);
	std::string pipeName(argv[4]);

	// setup named pipe interface
	std::string pipePrefix("\\\\.\\pipe\\");
	pipeName = pipePrefix + pipeName;
	std::wstring fullPipeName = s2ws(pipeName);
	printf("pipeName = %s\n", pipeName.c_str());
	pipe = CreateFile(fullPipeName.c_str(), GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
	TCHAR szName[] = TEXT("OpenCVBControl");
	hMapFile = CreateFileMapping(INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, 0, MemMapBufferSize, szName);

	if (hMapFile != 0)
	{
		sharedMem = (double*)MapViewOfFile(hMapFile, FILE_MAP_ALL_ACCESS, 0, 0, MemMapBufferSize);
		if (sharedMem) if (sharedMem[3] != 0) dataBufferSize = (int)sharedMem[3];
		dataBuffer = (float*)malloc(dataBufferSize);
	}
	return 0;
}

void readPipeAndMemMap()
{
	int skipCount = 0;
	while (1)
	{
		if ((int)sharedMem[0] != lastFrame) break;
		if (++skipCount > 100) break; // process the current image again to enable getting a closeWindow request (if one comes.)
		std::this_thread::sleep_for(std::chrono::milliseconds(1));
	}
	lastFrame = (int)sharedMem[0];
	rgbWidth = (int)sharedMem[1];
	rgbHeight = (int)sharedMem[2];

	if ((int)sharedMem[3] != dataBufferSize)
	{
		dataBufferSize = (int)sharedMem[3];
		free(dataBuffer);
		dataBuffer = (float *)malloc(dataBufferSize);
	}

	dataWidth = (int)sharedMem[4];
	dataHeight = (int)sharedMem[5];

	printf("data width = %d  data height = %d rgb width = %d rgb height = %d\n", dataWidth, dataHeight, rgbWidth, rgbHeight);
	if ((int)sharedMem[6] != rgbBufferSize)
	{
		free(rgbBuffer);
		rgbBufferSize = (int)sharedMem[6];
		rgbBuffer = (unsigned char *)malloc(rgbBufferSize);
	}

	for (int i = 0; i < USER_DATA_LENGTH; ++i)
	{
		UserData[i] = sharedMem[i + 7];
	}

	DWORD dwRead;
	if (rgbBufferSize > 0)
	{
		BOOL rc = ReadFile(pipe, rgbBuffer, rgbBufferSize, &dwRead, NULL);
		if (!rc) MessageBox(0, L"RGB buffer could not be read - see ReadFile in VTK.H", L"OpenCVB", MB_OK);
		src = cv::Mat(rgbHeight, rgbWidth, CV_8UC3, rgbBuffer);
	}
	if (dataBufferSize > 0)
	{
		BOOL rc = ReadFile(pipe, dataBuffer, dataBufferSize, &dwRead, NULL);
		if (!rc) MessageBox(0, L"Data buffer could not be read - see ReadFile in VTK.H", L"OpenCVB", MB_OK);
		data32f = cv::Mat(dataHeight, dataWidth, CV_32F, dataBuffer);
	}
}

int ackBuffers()
{
	DWORD dwWrite = 0;
	WriteFile(pipe, &lastFrame, 4, &dwWrite, NULL);
	if (dwWrite != 4)
	{
		printf("WriteFile failed\n");
		return -1;
	}
	return 0;
}
#endif 
#endif
