"c:\Program Files\Git\bin\git.exe" clone "https://github.com/IntelRealSense/librealsense"
"c:\Program Files\Git\bin\git.exe" clone "https://github.com/microsoft/Azure-Kinect-Sensor-SDK"
"c:\Program Files\Git\bin\git.exe" clone "https://github.com/eigenteam/eigen-git-mirror.git"
"c:\Program Files\Git\bin\git.exe" clone "https://github.com/Kitware/VTK.git"

"c:\Program Files\Git\bin\git.exe" clone "https://github.com/shimat/opencvsharp.git"
cd opencvsharp
git submodule update --init --recursive
cd ..\

"c:\Program Files\Git\bin\git.exe" clone "https://github.com/opencv/opencv"
cd OpenCV
"c:\Program Files\Git\bin\git.exe" clone "https://github.com/opencv/opencv_contrib"
cd ..\

"C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -DWITH_OPENGL=ON -DWITH_VTK=ON -DBUILD_EXAMPLES=OFF -DCPU_DISPATCH=SSE4_1;SSE4_2;AVX;FP16 -DVTK_DIR=VTK/Build/ -DBUILD_PERF_TESTS=OFF -DBUILD_TESTS=OFF -DBUILD_opencv_python_tests=OFF -DOPENCV_EXTRA_MODULES_PATH=OpenCV/OpenCV_Contrib/Modules -S OpenCV -B OpenCV/Build
"C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -DBUILD_CSHARP_BINDINGS=1 -DBUILD_CV_EXAMPLES=0 -S librealsense -B librealsense/Build
"C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -DOpenCV_DIR=OpenCV/Build -DCMAKE_BUILD_TYPE=Debug -S Azure-Kinect-Sensor-SDK -B Azure-Kinect-Sensor-SDK/Build
"C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -S eigen-git-mirror -B eigen-git-mirror/Build
"C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -DVTK_BUILD_EXAMPLES=1 -S VTK -B VTK/Build


start Azure-Kinect-Sensor-SDK/Build/k4a.sln
start librealsense/Build/librealsense2.sln
start opencvsharp/OpenCvSharp.sln
start opencvsharp/samples/OpenCvSharpSamples.sln
start eigen-git-mirror/Build/eigen3.sln
rem start VTK/Build/vtk.sln
rem start OpenCV/Build/OpenCV.sln