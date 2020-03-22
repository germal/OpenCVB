rmdir vtk /s

"c:\Program Files\Git\bin\git.exe" clone "https://gitlab.kitware.com/vtk/vtk.git"

"C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -DCMAKE_BUILD_TYPE=Debug -S vtk -B vtk/Build

start PrepareOpenCV_WithVTK.bat

start vtk/Build/k4a.sln