
start PrepareOpenCV.bat
start PrepareLibrealSense.bat
start PrepareAzure4Kinect.bat
















rem "c:\Program Files\Git\bin\git.exe" clone "https://github.com/IntelRealSense/librealsense"

rem "c:\Program Files\Git\bin\git.exe" clone "https://github.com/eigenteam/eigen-git-mirror.git"
rem "c:\Program Files\Git\bin\git.exe" clone "https://github.com/Kitware/VTK.git"

rem "c:\Program Files\Git\bin\git.exe" clone "https://github.com/shimat/opencvsharp.git"
rem cd opencvsharp
rem git submodule update --init --recursive
rem cd ..\

rem "c:\Program Files\Git\bin\git.exe" clone "https://github.com/opencv/opencv"
rem cd OpenCV
rem "c:\Program Files\Git\bin\git.exe" clone "https://github.com/opencv/opencv_contrib"
rem cd ..\

rem "C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -DBUILD_CSHARP_BINDINGS=1 -DBUILD_CV_EXAMPLES=0 -S librealsense -B librealsense/Build
rem "C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -S eigen-git-mirror -B eigen-git-mirror/Build
rem "C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -DVTK_BUILD_EXAMPLES=1 -S VTK -B VTK/Build



rem start librealsense/Build/librealsense2.sln
rem start opencvsharp/OpenCvSharp.sln
rem start opencvsharp/samples/OpenCvSharpSamples.sln
rem start eigen-git-mirror/Build/eigen3.sln
rem start VTK/Build/vtk.sln
