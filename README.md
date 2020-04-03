**Introduction**

There is no better documentation of an algorithm than a working example. Now
imagine hundreds of computer vision examples in a single app where each
algorithm is less than a page of code and is in a familiar language – C++, C\#,
Python, or VB.Net. That is what "OpenCVB" Version 1.0 provides. Here are the
requirements:

-   Windows 10

-   Visual Studio 2019 Community Edition (Free version of Visual Studio)

-   Any of the following RGBZ cameras:

    -   Microsoft Kinect for Azure

    -   Intel RealSense camera – D415, D435, D435i or T265

    -   StereoLabs ZED2

The Microsoft Kinect for Azure camera has an IMU (Inertial Measurement Unit) and
has better depth accuracy but requires more power and is not as portable as the
Intel D4xx cameras.

**The Objective**

This software is targeting cameras that produce depth as well as color. Depth
should prompt a complete review of existing computer vision algorithms, i.e. how
could depth improve the results? To enable revisiting many existing algorithms,
this software provides a single application that can run almost 600 algorithms.

There are many computer vision examples on the web but too often something is
missing. And when the build is finally working, a file or option needs to be
specified and is not provided. OpenCVB collects many of these algorithms into a
single application and guarantees that each will build and run. In addition, the
process of adding variants and experimenting with the example is simplified.

If this approach is to be useful, the languages for computer vision projects -
C++, C\#, Python, and VB.Net – need support. Secondly, it is important to get
access to multiple libraries - OpenCV, OpenCVSharp, OpenGL, Emgu, and OpenMP.
And lastly, it is important to use all the possible image representations - 3D,
bitmaps, plots, bar charts, spreadsheets, or text.

Making these languages and libraries available while using the same
infrastructure shaped a standardized class for computer vision examples.
Implementing hundreds of examples with the same reusable class structure has
confirmed the approach is useful. The result is a starting point to add depth
and explore further computer vision experiments.

There are other objectives. Convolutions combined with neural nets (CNN’s) are a
successful approach to computer vision. CNN’s detect differences within a set of
images and identify content surprisingly well. OpenCVB is a pathway to search
for more and better features than convolutions, features that are measured and
essential. Depth, infrared, gravity, and camera motion are the kind of objective
features that can enhance almost any color image algorithm.

And what if all cameras had depth and an IMU? Making this assumption explains
why only a few cameras from Intel, Microsoft, and others are currently
supported. More cameras with depth are expected to arrive and integration with
OpenCVB is likely to follow. OpenCVB is an opportunity to exploit the new
features of these cameras.

The algorithms are notably short, almost always less than a page of code,
labelled reasonably well, easily searched and grouped and combined, while often
providing links to online versions for other platforms. Many downloadable
algorithms are encumbered by environmental considerations that can obscure the
meaning or context of an algorithm. All the algorithms here contain just the
algorithm all will work with the supported cameras. Isolating the algorithm
functionality enables easy adaptation to other environments or platforms.

**Pre-Install Notes**

You will need to download and install the following before starting:

-   Microsoft Visual Studio 2019 Community Edition (Free)

    -   <https://visualstudio.microsoft.com/downloads/>

-   CMAKE 3.0 or later

    -   <https://cmake.org/download/>

-   The latest TortoiseGit and Git

    -   <https://tortoisegit.org/>

    -   <https://git-scm.com/downloads>

**Installation – Quick Reference**

-   <https://github.com/bobdavies2000/OpenCVB> - download OpenCVB from GitHub

-   <https://www.python.org/downloads/release/python-380/> - Install the latest
    Python (version 3.8)

    -   One of the algorithms – PythonPackages.py – checks that all the required
        Python packages are present.

-   In the OpenCVB directory, run the “PrepareTree.bat” script to download and
    run CMake for OpenCV, OpenCV_Contrib, librealsense, and Kinect4Azure.

    -   This will take a fair amount of time depending on the network speed.

    -   After “PrepareTree.bat” completes, Visual Studio projects will open. Run
        “Batch Build” in each Visual Studio project.

-   Set Environmental variable OpenCV_Version to 430. – This depends on the
    version of OpenCV, currently 4.30.

-   <https://docs.microsoft.com/en-us/azure/Kinect-dk/sensor-sdk-download> –
    Select “Windows Installer” to get proprietary Kinect4Azure support.

-   Build and run OpenCVB.sln

**Full Installation Description with Discussion**

The first step is to download OpenCVB from GitHub:

-   <https://github.com/bobdavies2000/OpenCVB>

The second step is to download the desired version of Python for use with
OpenCVB. Only Python 3.x is supported. The latest Python is available here:

-   <https://www.python.org/downloads/release/python-380/> - Install the x86-64
    bit version.

The third step is where all the work is.

-   Run the “PrepareTree.bat” script in the OpenCVB directory.

The “PrepareTree.bat” script will download OpenCV, librealsense, and
Kinect4Azure from their respective GitHub locations and install them in the
OpenCVB tree. In addition, the script will run the CMake commands that setup
OpenCV, librealsense, and Kinect4Azure for OpenCVB’s use. The script will then
open Visual Studio for each solution file. Build the Debug and Release versions
of each with the “Build/Batch Build” Visual Studio menu entry. The download and
CMake steps take about 20 minutes depending on the speed of the network
connection. The Visual Studio builds should take about an hour depending on the
speed of the machine.

After all the packages have been built, then there is one environmental variable
that needs to be set and it will depend on which version of OpenCV was just
downloaded and built.

-   Environmental variable “OpenCV_Version” should be set to 430

The currently available OpenCV download is 4.30 so setting OpenCV_Version to 430
reflects that but note that OpenCV is updated several times a year and the
environmental variable may need to be updated.

The last step before building OpenCVB is to download the proprietary binaries
from Microsoft for their Kinect4Azure camera. The “PrepareTree.bat” script built
the open source portion of the camera support but this step will complete the
installation of the Kinect4Azure camera:

-   <https://docs.microsoft.com/en-us/azure/Kinect-dk/sensor-sdk-download>

    -   Click “Microsoft Installer” to download and install the proprietary
        Kinect code from Microsoft

The last step is to open the OpenCVB.sln file and build OpenCVB.

Post build steps (optional):

-   If you are using the StereoLabs ZED 2 camera (released Q1 2020), install the
    StereoLabs SDK from <https://www.stereolabs.com/>

-   Edit the “Cameras/CameraDefines.hpp” file to uncomment the necessary
    \#define for StereoLabs Zed 2 support to build.

**Trouble-Shooting New Install**

Some typical problems with new installations:

-   Link problems: check the “OpenCV_Version” environmental variable for OpenCV
    version. It may need to reflect a newer version of OpenCV. For OpenCV 4.3,
    the environmental variable is OpenCV_Version = 430. Make sure that OpenCV
    Debug and Release versions were built successfully.

-   Camera Failure: check the camera installation by testing camera with
    examples provided by the vendor. Did the Kinect4Azure support get upgraded
    recently? Post if some configuration problems prevent the camera from
    working in OpenCVB.

-   Python Scripts Fail: this is likely a missing package. Run the algorithm
    “PythonPackages.py” in OpenCVB to verify that all the necessary packages are
    installed. If still failing, check the Python setting in the Options (click
    the Settings icon.) Make sure it points to the Python version that was
    installed. Test Python scripts independently using \<OpenCVB Home
    Directory\>/VB_Classes/Python/PythonDebug.sln.

**Build New Experiments**

OpenCVB is a WinForms application and most of the algorithms are written using
Microsoft's managed code but C++ examples are provided as well (with appropriate
VB.Net wrappers.) Python examples don’t require a VB.Net wrapper unless you want
to pass RGB, depth, or point cloud images to your Python script. There are
several VB.Net examples that demonstrate how to move images to Python (see
AddWeighted_Trackbar_PS.py as an example that is only a few lines of code.)

For C++, C\#, and VB.Net writing a new experiment requires a new class be added
anywhere in the “VB_Classes” project. OpenCVB will automatically detect the new
class during the build and present it in the user interface. The code is
self-aware in this regard – the UI_Generator project is invoked in a pre-compile
step for the VB_Classes project.

Code “snippets” are provided to accelerate development of new VB.Net, OpenGL,
and C++ algorithms. To use any snippets, you must first install them in Visual
Studio: use the menu “Tools/Code Snippets Manager” and add the “\<OpenCVB Home
Directory\>/OpenCVB.snippets” directory. After installing, access the code
snippets with a right-click in the VB.Net code, select “Snippet/Insert Snippet”
and select “OpenCVB.snippets”. NOTE: even C++ algorithms can use snippets, but
each C++ algorithm has a VB.Net entry that includes both the C++ and the VB.Net
code in the snippet. The C++ portion is to be cut and pasted anywhere in the
“CPP_Classes” Visual Studio project.

**Experimental Subsets**

The complete list of algorithms may be grouped into smaller subsets to study
some shared OpenCV API reference or to switch quickly between algorithms.
Algorithm subsets may be created and accessed through the Subset Combo Box in
the toolbar (indicated below.) The list of subsets is built from all the OpenCVB
algorithm names and all the OpenCV API’s referenced. For instance, selecting
“Threshold” in the Subset Combo Box, will update the Algorithm Combo Box with
all the algorithms that use the OpenCV “Threshold” API.

![](media/38bd5de162aff996693b6f96d7b1d58e.png)

*In the image above, the subset “\<All\>” can be selected and the complete list
of algorithms will appear in the combo box on the left. If the subset selected
was “Threshold”, only those algorithms using the OpenCV threshold API will
appear in the combo box on the left.*

The ability to create subsets from the hundreds of algorithms makes it easier to
study examples of an OpenCV API or OpenCVB algorithm usage. All the OpenCV API’s
that are used and all the OpenCVB algorithms are available in the list of
subsets. In addition, several higher-level groupings are available. For
instance, selecting “\<OpenGL\>” will select only the algorithms that use
OpenGL. The “\<All\>” entry in the Subset Combo Box will restore the complete
list of algorithms.

**Testing All Experiments**

Testing is integrated into OpenCVB. Clicking the icon below runs through a
checklist of all the algorithms at all the resolutions with all the supported
cameras available on the system. The duration of each test can be selected in
the Options dialog.

![](media/4551393d07ab7de64ae7f0422f672cb7.png)

When using a subset of the algorithms, the “Test All” button will test only the
algorithms in the subset. This can be useful when changing an algorithm that is
reused frequently by other algorithms. For instance, if the Edges_Sobel
algorithm is changed, first select the subset of all algorithms using
Edges_Sobel then select “Test All” to visually review each algorithm using the
updated Edges_Sobel.

One side benefit of the “Test All” feature is that is provides a way to visually
review all the algorithms. When used in combination with the subset feature, it
can make it easier to search for a desired effect.

**Why VB.Net?**

VB.Net is not a language associated with computer vision algorithms. But the
proliferation of examples in OpenCVB suggests this may be an oversight. Even the
seasoned developer should recognize what is obvious to the beginner: VB.Net has
the ability to provide clarity. VB.Net is a full-featured language just like C\#
with lambda functions and multi-threading and VB.Net includes user interface
tools that are flexible and complete (check boxes, radio buttons, sliders,
TrueType fonts, and much more) - options missing from OpenCV's popular HighGUI
library. (All existing HighGUI interfaces are still supported though.)

The main caution in using VB.Net is to treat it as a scripting language like
Python. Most of the algorithms avoid pixel-by-pixel details – VB.Net can be
detailed but it will be slower than optimized C++. Usually, OpenCV is doing most
of the real work in optimized C++ through the OpenCVSharp interface. Most
algorithms run reasonably fast even in Debug mode because the release version of
OpenCVSharp is active even when the solution is in Debug mode.

Critics will point out that a Windows 10 app using VB.Net is not easily portable
but the entire OpenCVB application does not need to be ported to other
platforms. Only individual algorithms will need to be ported after they are
debugged and polished and most algorithms consist almost entirely of OpenCV
API’s which are already available everywhere. OpenCVB’s value lies in the
ability to freely experiment and finish an OpenCV algorithm before even starting
a port to a different platform. Confining development to OpenCVB’s C++ interface
should provide the most portable version of any algorithm but even VB.Net code
is also easily ported to a variety of non-Windows platforms.

**Camera Interface**

All the camera code is isolated in the “camera” class – see cameraD400Series.vb,
cameraKinect.vb, cameraMynt.vb (unfinished), cameraZed2.vb or cameraT265.vb.
There are no references to camera interfaces anywhere else in the code.
Isolating the camera support from the algorithms strips the code to just the
essential OpenCV API’s needed.

For example, the Kinect for Azure camera support is isolated to the
cameraKinect.vb class and a supporting Kinect4Azure DLL that provides all the
interface code to the Kinect for Azure libraries. Since there is likely to be
little interest in debugging the Kinect4Azure DLL, the Release version is used
even in the Debug configuration. If it is necessary to debug the camera
interface, set any Build Configuration components to the Debug version.
Optimizations enable a higher framerate than when running the Debug
configuration of the Kinect camera DLL. As a result, the VB.Net code in Debug
mode often runs as fast as the Release configuration.

**OpenGL Interface**

There have been several attempts to provide OpenGL interfaces into managed code,
but none is used here. OpenGL is simply run in a separate process. To
accommodate running separately, a named-pipe moves the image data to the
separate process and a memory-mapped file provides a control interface. The
result is both robust and economical leaving the OpenGL C++ code independent of
hardware specifics. The VB.Net code for the OpenGL interface is less than a page
and does not require much memory or CPU usage. The OpenGL C++ code provided with
OpenCVB is customized for specific applications in a format that should be
familiar to OpenGL developers. There are several examples – displaying RGB and
Depth, 3D histograms, 3D drawing, and IMU usage. A code snippet (See ‘Build New
Experiments’ above) provides everything needed to add a new OpenGL algorithm
that will consume RGB and a point cloud.

NOTE: it is easy to forget to include any new OpenGL project in the Project
Dependencies. This can be confusing because the new project will not build
automatically when restarting. The OpenCVB Project Dependencies need to be
updated whenever a new OpenGL application is added to the OpenCVB solution. To
update dependencies, select “Project/Dependencies” from the Visual Studio menu
and make sure that the “OpenCVB” project depends on any new OpenGL projects.
This ensures that the new project will always be rebuilt when OpenCVB is
restarted.

**OpenCV’s OpenGL Interface**

The “PrepareTree.bat” script will have configured OpenCV with OpenGL support.
OpenCVB can use this OpenGL interface – see the OpenCVGL algorithm. The
interface is sluggish and looks different from most OpenGL applications so the
alternative interface to OpenGL (discussed above) is preferred. Both interfaces
use the same sliders and options to control the OpenGL interface.

NOTE: why use sliders instead of OpenGL mouse or keyboard callbacks? The answer
in a single word is precision. It is often desirable to set appropriate defaults
with each of the numerous ways to change the display. Also, sliders were
valuable in learning which OpenGL API was controlling which feature of the 3D
effect. Preconfiguring the sliders allows the user to program a specific setup
for viewing 3D data.

For those that prefer using the mouse to adjust the OpenGL display, the OpenGL
code snippet provides sample code. To test a working example of OpenGL using
just the mouse interface, see the OpenGL_Callbacks algorithm.

**Python Interface**

OpenCV has numerous examples of Python scripts and Python is often used for
computer vision experiments. To add a new Python script for use with OpenCVB,
add a script in the Python directory of the VB_Classes project. It is convenient
for edits to add any script to the VB_Classes project but, more importantly, any
changes to a Python script will automatically show the new or renamed Python
files in the user interface. Python scripts don’t require a VB.Net wrapper –
just add them to the VB_Classes Project in the Python directory – and the script
will be present in the user interface.

However, to send the RGB, depth, or point cloud image data from the camera to
the Python script, a VB.Net wrapper is required. Examples are provided – see
Python_SurfaceBlit or Python_RGBDepth. There is a simple naming convention for
Python scripts with a VB.Net wrapper: use the same name for both, i.e. the
algorithm Python_RGBDepth is the companion for the Depth_RGB_PS.py script (the
\_PS suffix is an OpenCVB convention that indicates it is a Python Streaming
script sending the RGB and Depth images to the Python script. There is an
OpenCVB subset for all the Python Streaming scripts called “\<PyStream\>”.)

Python scripts show up in the list of algorithms in the OpenCVB user interface
and each Python script will be run when performing a regression tests. To change
which version of Python is used, open the “Options” dialog and in the “Python”
section, there is a browse button to select any Python.exe available on the
system.

**Python Packages**

The Python libraries “opencv-python” or “NumPy” are required for many of the
OpenCVB Python scripts but are not installed by default. To update missing
packages in Visual Studio, use the “Tools/Python/Python Environments” in the
Visual Studio menu:

-   “Tools/Python/Python Environments” – select “Packages” in the combo box then
    enter for “opencv-python” or “numpy” and select the package from the list.

To check that all the necessary packages are installed, run the
‘PythonPackages.py’ algorithm from OpenCVB’s user interface.

**Python Debugging**

Python scripts are run in a separate address space when invoked by OpenCVB so
Visual Studio’s Python debug environment is not available. When a Python script
fails in OpenCVB, it may be debugged in the PythonDebug project. Here are the
steps to debug Python:

-   Open VB_Classes\\Python\\PythonDebug.pyproj in Visual Studio.

-   Copy the failing Python script into the PythonDebug.py file, and run it.

The Python script will be running in the same environment as if it were invoked
from OpenCVB except the Python debugger will be active.

**Visual Studio Debugging**

The Visual Studio projects can be configured to simultaneously debug both
managed and unmanaged code seamlessly. The property “Enable Native Code
Debugging” for the managed projects controls whether C\# or VB.Net code will
step into C++ code while debugging. However, leaving that property enabled all
the time means that the OpenCVB will take longer to start – approximately 5
seconds vs. 3 seconds on a higher-end system. The default is to leave the
“Enable Native Code Debugging” property off so OpenCVB will load faster. Of
course, if there is a problem in the C++ code that is best handled with a debug
session, turn on the “Enable Native Code Debugging” property in the OpenCVB
VB.Net application.

**Record and Playback**

The ability to record and playback is provided with OpenCVB – see Replay_Record
and Replay_Play algorithms. RGB, Depth, point cloud, and IMU data are written to
the recording file. Any algorithm that normally runs with the live camera input
and IMU data can be run with recorded data. Use the “Subset Combo Box” to select
the option: “\<All using recorded data\>”. Running the regression tests with
that setting will run all the algorithms with recorded data instead of a live
camera feed.

**StereoLabs Zed 2 Support**

The StereoLabs Zed 2 camera is supported but the support is turned off by
default. To enable this support:

-   Download the Windows 10 SDK from
    <https://www.stereolabs.com/developers/release/>

-   Edit the CameraDefines.hpp file to turn on the interface to the Zed 2
    camera.

The Zed 2 camera support is always installed in the C:\\Program Files (x86)\\ZED
SDK (regardless of the version) so no additional changes are required to the
supporting C++ project.

**VTK Support**

VTK (the Visualization ToolKit) is an excellent tool and OpenCVB supports its
use with OpenCV but it takes a non-trivial amount of time and effort to install
and build it. The support is present in this version but is turned off. All the
algorithms using VTK will work with or without VTK installed but if it is not
present, the VTK algorithms will simply display a message explaining how to
enable VTK with the following steps:

-   Run “PrepareVTK.bat” in \<OpenCVB_Home\>

-   Build VTK for both Debug and Release

-   Build OpenCV for both Debug and Release

-   Edit mainVTK.cpp (project VTKDataExample) and modify the first line

**Sample Results**

The following images are a preview of some algorithms’ output.

The top left image is the RGB and top right is depth. Algorithm results are in
the bottom left and right or additional windows.

![](media/cd7e699a6192e4daf1d540a15e35005a.png)

*Histogram Valleys are used to create clusters in depth data. The bottom left is
the histogram showing the different clusters. The bottom right is the
back-projection of the different clusters into the depth image using the same
colors as the histogram.*

![](media/f3e144361b0ffeb85ff30f51af3eac3e.png)

*Using the histogram to create clusters (see previous example ‘Histogram
Valleys’) allows an algorithm to segment an entire image (see lower right
image), creating blobs that can be measured and tracked. The black segment has
no depth. The number of blobs can be controlled with a lower limit on the size
of the blob.*

![](media/1df99676bf4ca2ef5c9351c3259c35ac.png)

*The IMU timestamp provides clues to the relationship between the IMU capture
and the image capture (the devices are on different clocks.) The image capture
triggers an interrupt but how long ago did the IMU capture the pose data or
Gyro/Acceleration? The plot in the lower right shows the actual frame durations
for the IMU and host interrupts following an image capture. The IMU and host
interrupt frame times are used to estimate the delay from the IMU capture to the
image capture. The blue dots are the actual IMU capture frame times, the green
is the host image capture frame time, and the red is the estimated delay from
the IMU capture to the image capture. The white row shows the IMU frame time
with the highest occurrence and is used as a boundary between lost IMU captures
(below the white line) and correctly sequenced IMU captures (at or above the
white line).*

![](media/3df8502dfd9e7ac7e50e5645cf8fe4ad.png)

*Here the MeanShift algorithm is used to track the 4 largest blobs in the image.
The lower left image shows the objects tracked by the algorithm while the lower
right shows the different histograms used to identify the object to track. The
histogram is for the hue portion of the HSV format of the image.*

![](media/69c6578e73d94f9941e3e19ad50ae4f4.jpg)

*KAZE feature detection algorithm matching points on the left and right infrared
images from the Intel RealSense 3D camera.*

![](media/59211cf16c0fcf1573366606022a3a84.jpg)

*The features detected by the FAST (Features from Accelerated Segment Test)
algorithm are shown with 2 different FAST options.*

![](media/a5fdb31abf7b00f7795a0cc3d9a70866.png)

*This is a combination of 4 variations of binarization and their corresponding
histograms. It demonstrates the use of the Mat_4to1 class to get 4 images in
each of a result image.*

![](media/3367c93aeb671c6cd3ed978040e9b3aa.png)

*A Kalman filter is used here to follow the mouse. The actual mouse movement is
in red while the output of the Kalman filter is in white.*

![](media/278991d0c7a1b70a4d912707974e49ca.jpg)

*Compare the original color and depth images with the image right below it to
see the impact of an edge-preserving filter applied to both.*

![](media/e1cf24b8d952e0a0ddf6af7075e82e04.png)

*The bottom images are the output of a multi-threaded Hough lines algorithm that
identifies featureless RGB surfaces (shown as white in the lower right image).
The blue color in the lower right image is meant to highlight depth shadow where
no depth data is available.*

![](media/d1b5776e1e2062cd4dd50ca206b2528e.png)

*The MeanShift algorithm output (lower left) is used here to find what is likely
to be the face – the highest point of the nearest depth fields (shown in a box
in the lower right.)*

![](media/1c93b65678b1664d6f8c6b95eb1dfde6.png)

*Algorithms may optionally work on only a selected region. Here the oil paint
effect is applied only to the region selected by drawing a rectangle on the
output image (lower left.) The lower right image is an intermediate stage with
only edges. All algorithms may draw a rectangle with a right-mouse to open a
spreadsheet with the data selected in the rectangle.*

![](media/ec6993a1021d7e22891e6c1575edd88c.png)

*The OpenGL window is controlled from the VB.Net user interface but is run in a
separate address space. The OpenGL axes are represented in this image as well.
In the lower left background are some of the sliders used to control the OpenGL
interface – Point Size, FOV, yaw, pitch, roll.*

![](media/2429d8d0dc72575c4321f17af66029b9.png)

*This dnn Caffe example in VB.Net is a reinterpretation of the C++ sample
program distributed with the Intel librealsense library. The application is
meant to detect and highlight different objects shown in the yellow box (lower
right). The algorithm requires square input (shown centered in the lower left
image.)*

![](media/8497c5db9233508b0cecc82bf0cc13c6.png)

*This example of a compound class shows an image that has been motion-blurred
(lower left) and then de-blurred in the lower right. The options for the motion
blur and de-blur are also shown.*

![](media/a3f9a39a4f120ef7829003267332a9b3.jpg)

*It is an option to use VTK (Visualization Tool Kit) but it is turned off by
default in the distribution. The test pattern in the lower right image behind
the VTK output is sent to VTK where the 3D histogram is computed and displayed.*

![](media/2f8061c17229863e8216ee67ee5cfb4f.png)

*The Expectation Maximization algorithm learns the different clusters and then
predicts the boundaries of those clusters. The lower left image defines the
different clusters (each with a different color) and the lower right fills in
the maximized regions for each cluster. The RGB and Depth images are not used in
this example.*

![](media/676099b58f6e449a8fae146232512495.png)

*In this example of multi-threaded simulated annealing, the lower right image
shows 4 annealing solutions - the 2 best solutions with 2 worst solutions. The
log in the bottom left shows each thread getting different energy levels,
confirming that each is independently searching the solution space. The example
was taken directly from the OpenCV examples but was multi-threaded on an Intel
Core i9 18-core (36 thread) processor here*.

![](media/fc40ee0a0f63d30e70b12177a3b3a0c4.png)

*First, two estimated triangles are created to surround two sets of random
points. Then an affine transform is computed that converts one triangle to the
other. The matrix of the computed affine transform is shown of the lower right
image. The RGB and depth images are not used for this algorithm.*

![](media/11f9fa473841d322771407e024e5cc99.png)

*First, a randomly oriented rectangle is created. Then a transformation matrix
is computed to change the shape and orientation of the color image to the new
perspective. The matrix of the computed affine transformation is shown in the
lower right image.*

![](media/c1e4ce70ae8054b2fc0b5bbcf0d50f0a.png)

*The histograms displayed in OpenCVB have an option to use Kalman filtering to
smooth the presentation of the bar graph (lower right.) A check box (below the
bar graph) allows alternating between a Kalman-smoothed and real-time bar graph.
The bar colors show a progression of dark to light to link the color of the
underlying grayscale pixel into the bar graph. There is also an option to use
different transition matrices in the Kalman filter.*

![](media/ce8590b89b130234594bd9ecae5f0c64.png)

*This example shows the Plot_Basics class which will plot XY values. The bottom
left image shows the mean values for red, green, and blue in the color image
smoothed using Kalman prediction while the bottom right shows the real-time
values.*

![](media/2a29ff58c53f1225d7b2b99fcf1d79c4.png)

*In the OpenCV distribution there is a bio-inspired model of the human retina.
The objective is to enhance the range of colors present in an image. In this
example the “useLogSampling” is enabled and emphasizes colors (bottom left) and
motion (bottom right).*

![](media/6602327efd37ee81572ae5072fc75f78.jpg)

*Applying the bio-inspired enhancements, the high-dynamic range image is shown
in the bottom left while the motion-enhanced image is shown in the bottom right.
The monitor on the desk that appears black in the image in the top left has much
more detail in the bottom left image which shows the reflection that would
likely be visible to the human retina.*

![](media/b81abe738e142a7aa80f53ea6ca5b97b.png)

*In this Python example taken from the OpenCV distribution, the various
positions of an etalon (not shown) are visualized in a 3D matplotlib control.
See the “Camera_calibration_show_extrinsics.py” algorithm.*

![](media/7410ae7a79c21c3f20aef18ee58750aa.png)

*In this multi-threaded version of the Gabor filter, the bottom left image is
the Gabor result while the bottom right figure shows the 32 Gabor kernels used
to produce the result on the bottom left.*

![](media/cbabb4e31400daa77e482733a5b535f9.png)

*Emgu applications are supported as well. Here the planar subdivision of the
image is shown for a random set of points with Voronoi facets and Delauney
triangles. Emgu interfaces, while based on OpenCV, are significantly different
from OpenCVSharp. For instance, the Emgu Mat is not the same as the OpenCVSharp
Mat. Nonetheless, there is considerable ongoing investment in the Emgu APIs’ and
they are fully supported.*

![](media/b675880a11fc9b9a94d6873b1bf40fa8.png)

*The Aruco markers are found in an image allowing the image to be transformed
using OpenCV’s WarpPerspective transform. This example is from the OpenCVSharp
Sample programs.*

![](media/08085bb3df8c48de755fc8e9e10074e7.png)

*This synthetic problem demonstrates how to use the Python SciKit package to
perform a gradient descent problem.*

![](media/2bb85cf64b4b39f190ba31e5059be427.png)

*A voxel-style view of the depth data in OpenGL is available – see top window.
The bottom left shows the thread grid used to compute the median depth in each
voxel while the bottom right shows the voxels colored with their median depth
(using the typical colors for depth – yellow for close, blue for distant.) The
options used in each of the contributing algorithms are present below the main
OpenCVB window.*

![](media/83446a818084895c88dd13d1b30b9e2a.png)

*Support for Intel’s T265 is rudimentary – it is not meant to capture depth and
the camera is grayscale. The upper left image is the undistorted and remapped
Left View. The upper right color section shows depth (computed on the host)
while the rest of the image is the Left View. The bottom views are the left and
right raw camera views (stretched to accommodate the frame size.) While none of
the algorithms fail when running the T265, not all of them produce anything
useful as depth is not provided by the camera.*

![](media/64c52c2be85cafd12a707e52aa3a8cbc.png)

*OpenCV’s InitUndistortRectifyMat and Remap API’s can be used to manipulate the
image arbitrarily. Here one of the sliders in the interface has introduced
barrel distortion in the lower left image. This is a useful exercise to get an
more intuitive understanding of the parameters needed to rectify and undistort
an image.*

![](media/282608bc4f343ce30c844cadc10763b5.png)

*The 100 year-old photos of Prokudin-Gorskii were made with red, green, and blue
filters. Combining the 3 images is a useful application of the OpenCV
findTransformECC API that can align the images. One of the transformation
matrices is displayed as well. The option to use gradients for alignment
(instead of RGB values) is available. There are 4 possible OpenCV parameters to
define the extent of the search for alignment in the user interface.*

**Future Work**

The plan is to continue adding more algorithms. There are numerous published
algorithms on the web but there is also the task to combine the different
algorithms in OpenCVB. The current edition of the code contains examples of
compound algorithms and more will arrive in future releases. The class-oriented
structure of the code almost enforces reuse because any algorithm with sliders
or check boxes suggests reusing an existing class rather than provide another
with similar set of sliders and check boxes. The options forms for combined
algorithms are automatically cascaded for easy selection.

**Acknowledgements**

The list of people who have made OpenCVB possible is long but starts with the
OpenCV contributors – particularly, Gary Bradski, Victor Erukhimov, and Vadim
Pisarevsky - and Intel’s decision to contribute the code to the open source
community. Also, this code would not exist without OpenCVSharp managed code
interface to OpenCV provided by user “shimat”. There is a further Intel
contribution to this software in the form of RealSense cameras – low-cost 3D
cameras for the maker community as well as robotics developers and others.
RealSense developers Sterling Orsten and Leo Keselman were helpful in educating
this author. While others may disagree, there is no better platform than the one
provided by Microsoft Visual Studio and VB.Net. And Microsoft’s Kinect for Azure
camera is a valuable addition to the 3D camera effort. And lastly, it should be
obvious that Google’s contribution to this effort was invaluable. Thanks to all
the computer vision developers who posted algorithms where Google could find
them. All these varied organizations deserve most of the credit for this
software.

**MIT License**

<https://opensource.org/licenses/mit-license.php> - explicit license statement

Fremont, California

Winter 2020
