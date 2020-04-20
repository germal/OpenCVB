@echo on
xcopy /Y cameras\cameraDefinesAll.hpp cameras\cameraDefines.hpp
copy /b cameras\cameraDefines.hpp +,,