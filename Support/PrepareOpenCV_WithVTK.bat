cd ..\
rmdir OpenCV /s
"c:\Program Files\Git\bin\git.exe" clone "https://github.com/opencv/opencv"
cd OpenCV
"c:\Program Files\Git\bin\git.exe" clone "https://github.com/opencv/opencv_contrib"
cd ..\		

"C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -DWITH_VTK=ON -DVTK_DIR=c:/Program Files/VTK -DWITH_OPENGL=ON -DBUILD_EXAMPLES=OFF -DCPU_DISPATCH=SSE4_1;SSE4_2;AVX;FP16 -DBUILD_PERF_TESTS=OFF -DBUILD_TESTS=OFF -DBUILD_opencv_python_tests=OFF -DOPENCV_EXTRA_MODULES_PATH=OpenCV/OpenCV_Contrib/Modules -S OpenCV -B OpenCV/Build

start OpenCV/Build/OpenCV.sln
