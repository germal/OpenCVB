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

"C:\Program Files\CMake\bin\Cmake.exe" -DWITH_OPENGL=ON -DBUILD_EXAMPLES=ON -DBUILD_PERF_TESTS=0 -DBUILD_TESTS=0 -DBUILD_opencv_python_tests=0 -DOPENCV_EXTRA_MODULES_PATH=OpenCV/OpenCV_Contrib/Modules -DCMAKE_CONFIGURATION_TYPES=Debug;Release -S OpenCV -B OpenCV/Build
"C:\Program Files\CMake\bin\Cmake.exe" -DBUILD_CSHARP_BINDINGS=1 -DBUILD_CV_EXAMPLES=0 -DCMAKE_CONFIGURATION_TYPES=Debug;Release -S librealsense -B librealsense/Build
"C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -DOpenCV_DIR=OpenCV/Build -DCMAKE_BUILD_TYPE=Debug -S Azure-Kinect-Sensor-SDK -B Azure-Kinect-Sensor-SDK/Build
"C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -S eigen-git-mirror -B eigen-git-mirror/Build
"C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -S VTK -B VTK/Build


start Azure-Kinect-Sensor-SDK/Build/k4a.sln
start librealsense/Build/librealsense2.sln
start opencvsharp/OpenCvSharp.sln
start opencvsharp/samples/OpenCvSharpSamples.sln
start eigen-git-mirror/Build/eigen3.sln
start VTK/Build/vtk.sln
start OpenCV/Build/OpenCV.sln