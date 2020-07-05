Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
Imports System.IO
Imports System.ComponentModel
Imports System.Threading
Imports System.Globalization
Imports System.Text.RegularExpressions
Imports System.Environment
Imports Numpy
Imports py = Python.Runtime
Module opencv_module
    Public bufferLock As New PictureBox ' this is a global lock on the camera buffers.
End Module
Public Class OpenCVB
#Region "Globals"
    Const displayFrames As Int32 = 4
    Dim AlgorithmCount As Int32
    Dim AlgorithmTestCount As Int32
    Dim algorithmTaskHandle As Thread
    Dim saveAlgorithmName As String
    Dim border As Int32 = 6
    Dim BothFirstAndLastReady As Boolean
    Dim camera As Object
    Dim cameraRS2Generic As Object ' used only to initialize D435i and L515
    Dim cameraD435i As Object
    Dim cameraL515 As Object
    Dim cameraKinect As Object
    Dim cameraMyntD As Object
    Dim cameraZed2 As Object
    Dim cameraT265 As Object
    Dim cameraTaskHandle As Thread
    Public camPic(displayFrames - 1) As PictureBox
    Dim cameraRefresh As Boolean
    Dim algorithmRefresh As Boolean
    Dim CodeLineCount As Int32
    Dim DrawingRectangle As Boolean
    Dim drawRect As New cv.Rect(0, 0, 0, 0)
    Dim externalPythonInvocation As Boolean
    Dim fps As Int32 = 30
    Dim formResult1 As New cv.Mat, formResult2 As New cv.Mat
    Dim frameCount As Int32
    Dim GrabRectangleData As Boolean
    Dim HomeDir As DirectoryInfo
    Dim keyboardInput As String
    Dim keyboardLastInput As String

    Dim LastX As Int32
    Dim LastY As Int32
    Dim lowResolution As Boolean
    Dim mouseClickFlag As Boolean
    Dim mouseClickPoint As New cv.Point
    Dim mouseDownPoint As New cv.Point
    Dim mouseMovePoint As New cv.Point
    Dim mousePicTag As Int32
    Dim mousePoint As New cv.Point
    Dim myBrush = New SolidBrush(System.Drawing.Color.White)
    Dim myPen As New System.Drawing.Pen(System.Drawing.Color.White)
    Dim OpenCVfullPath As String
    Dim openCVKeywords As New List(Of String)
    Dim OptionsBringToFront As Boolean
    Dim optionsForm As OptionsDialog
    Dim picLabels() = {"RGB", "Depth", "Result1", "Result2"}
    Dim RefreshAvailable As Boolean = True ' This variable allows us to dodge a refresh from the system after a move.  There is no synclock around that system refresh.
    Dim regWidth As Int32 = 1280, regHeight As Int32 = 720
    Dim resizeForDisplay = 2 ' indicates how much we have to resize to fit on the screen
    Dim fastSize As cv.Size
    Dim stopAlgorithmThread As Boolean
    Dim stopCameraThread As Boolean
    Dim textDesc As String = ""
    Dim totalBytesOfMemoryUsed As Integer
    Dim TTtextData(displayFrames - 1) As List(Of VB_Classes.oTrueType)
#End Region
    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture
        Dim args() = Environment.GetCommandLineArgs()
        ' currently the only commandline arg is the name of the algorithm to run.  Save it and continue...
        If args.Length > 1 Then
            Dim algorithm As String = ""
            SaveSetting("OpenCVB", "OpenCVkeyword", "OpenCVkeyword", "<All>") ' this will guarantee the algorithm is available (if spelled correctly!)
            If args.Length > 2 Then ' arguments from python os.spawnv are passed as one character at a time.  
                For i = 0 To args.Length - 1
                    algorithm += args(i)
                Next
            Else
                algorithm = args(1)
            End If
            SaveSetting("OpenCVB", "<All>", "<All>", algorithm)
            externalPythonInvocation = True ' we don't need to start python because it started OpenCVB.
        End If

        HomeDir = New DirectoryInfo(CurDir() + "\..\..\")

        ' Camera DLL's are built in Release mode even when configured for Debug (performance while debugging an algorithm is much better).  
        ' It is not likely camera interfaces will need debugging but to do so change the Build Configuration and enable "Native Code Debugging" in the OpenCVB project.
        Dim releaseDir = HomeDir.FullName + "\bin\Release\"
        updatePath(releaseDir, "Release Version of camera DLL's.")

        ' check to make sure there are no camera dll's in the Debug directory by mistake!
        For i = 0 To 5
            Dim dllName = Choose(i + 1, "Cam_Kinect4.dll", "Cam_MyntD.dll", "Cam_T265.dll", "Cam_Zed2.dll", "Cam_RS2.dll", "CPP_Classes.dll")
            Dim dllFile = New FileInfo(HomeDir.FullName + "\bin\Debug\" + dllName)
            If dllFile.Exists Then
                ' if the debug dll exists, then remove the Release version because Release is ahead of Debug in the path for this app.
                Dim releaseDLL = New FileInfo(HomeDir.FullName + "\bin\Release\" + dllName)
                If releaseDLL.Exists Then
                    If DateTime.Compare(dllFile.LastWriteTime, releaseDLL.LastWriteTime) > 0 Then releaseDLL.Delete() Else dllFile.Delete()
                End If
            End If
        Next

        Dim DebugDir = HomeDir.FullName + "\bin\Debug\"
        updatePath(DebugDir, "Debug Version of any camera DLL's.")

        Dim IntelPERC_Lib_Dir = HomeDir.FullName + "librealsense\build\Debug\"
        updatePath(IntelPERC_Lib_Dir, "Realsense camera support.")
        Dim Kinect_Dir = HomeDir.FullName + "Azure-Kinect-Sensor-SDK\build\bin\Debug\"
        updatePath(Kinect_Dir, "Kinect camera support.")

        Dim myntSDKready As Boolean
        Dim zed2SDKready As Boolean
        Dim defines = New FileInfo(HomeDir.FullName + "Cameras\CameraDefines.hpp")
        Dim sr = New StreamReader(defines.FullName)
        While sr.EndOfStream = False
            Dim infoLine = Trim(sr.ReadLine)
            If infoLine.StartsWith("//") = False Then
                Dim Split = Regex.Split(infoLine, "\W+")
                If Split(2) = "MYNTD_1000" Then myntSDKready = True
                If Split(2) = "STEREOLAB_INSTALLED" Then zed2SDKready = True
            End If
        End While
        sr.Close()

        optionsForm = New OptionsDialog
        optionsForm.OptionsDialog_Load(sender, e)

        optionsForm.cameraDeviceCount(OptionsDialog.D435i) = USBenumeration("Intel(R) RealSense(TM) Depth Camera 435i Depth")
        optionsForm.cameraDeviceCount(OptionsDialog.L515) = USBenumeration("Intel(R) RealSense(TM) 515 RGB")
        optionsForm.cameraDeviceCount(OptionsDialog.Kinect4AzureCam) = USBenumeration("Azure Kinect 4K Camera")
        optionsForm.cameraDeviceCount(OptionsDialog.T265Camera) = USBenumeration("T265")
        If optionsForm.cameraDeviceCount(OptionsDialog.T265Camera) = 0 Then optionsForm.cameraDeviceCount(OptionsDialog.T265Camera) = USBenumeration("Movidius MA2X5X")

        ' Some devices may be present but their opencvb camera interface needs to be present as well.
        optionsForm.cameraDeviceCount(OptionsDialog.MyntD1000) = USBenumeration("MYNT-EYE-D1000")
        If optionsForm.cameraDeviceCount(OptionsDialog.MyntD1000) > 0 And myntSDKready = False Then
            optionsForm.cameraDeviceCount(OptionsDialog.MyntD1000) = 0 ' hardware is there but dll is not installed yet.
            If GetSetting("OpenCVB", "myntSDKready", "myntSDKready", True) Then
                MsgBox("A MYNT D 1000 camera is present but OpenCVB's" + vbCrLf +
                   "Cam_MyntD.dll has not been built with the SDK." + vbCrLf + vbCrLf +
                   "Edit " + HomeDir.FullName + "CameraDefines.hpp to add support" + vbCrLf +
                   "and rebuild OpenCVB with the MYNT SDK." + vbCrLf + vbCrLf +
                   "Also, add environmental variable " + vbCrLf +
                   "MYNTEYE_DEPTHLIB_OUTPUT" + vbCrLf +
                   "to point to '<MYNT_SDK_DIR>/_output'.")
                SaveSetting("OpenCVB", "myntSDKready", "myntSDKready", False)
            End If
        End If
        optionsForm.cameraDeviceCount(OptionsDialog.StereoLabsZED2) = USBenumeration("ZED 2")
        If optionsForm.cameraDeviceCount(OptionsDialog.StereoLabsZED2) > 0 And zed2SDKready = False Then
            optionsForm.cameraDeviceCount(OptionsDialog.StereoLabsZED2) = 0 ' hardware is present but dll is not installed yet.
            If GetSetting("OpenCVB", "zed2SDKready", "zed2SDKready", True) Then
                MsgBox("A StereoLabls ZED 2 camera is present but OpenCVB's" + vbCrLf +
                       "Cam_Zed2.dll has not been built with the SDK." + vbCrLf + vbCrLf +
                       "Edit " + HomeDir.FullName + "CameraDefines.hpp to add support" + vbCrLf +
                       "and rebuild OpenCVB with the StereoLabs SDK.")
                SaveSetting("OpenCVB", "zed2SDKready", "zed2SDKready", False) ' just show this message one time...
            End If
        End If

        ' if the default camera is not present, try to find another.
        If optionsForm.cameraDeviceCount(optionsForm.cameraIndex) = 0 Then
            If optionsForm.cameraDeviceCount(OptionsDialog.Kinect4AzureCam) Then optionsForm.cameraIndex = OptionsDialog.Kinect4AzureCam
            If optionsForm.cameraDeviceCount(OptionsDialog.T265Camera) Then optionsForm.cameraIndex = OptionsDialog.T265Camera
            If optionsForm.cameraDeviceCount(OptionsDialog.StereoLabsZED2) Then optionsForm.cameraIndex = OptionsDialog.StereoLabsZED2
            If optionsForm.cameraDeviceCount(OptionsDialog.MyntD1000) Then optionsForm.cameraIndex = OptionsDialog.MyntD1000
            If optionsForm.cameraDeviceCount(OptionsDialog.D435i) Then optionsForm.cameraIndex = OptionsDialog.D435i
            If optionsForm.cameraDeviceCount(OptionsDialog.L515) Then optionsForm.cameraIndex = OptionsDialog.L515
            If optionsForm.cameraDeviceCount(optionsForm.cameraIndex) = 0 Then
                MsgBox("There are no supported cameras present.  Connect a RS2series, Kinect 4 Azure, T265, MyntEyeD 1000, or StereoLabs Zed2.")
                End
            End If
        End If

        ' OpenCV needs to be in the path and the librealsense and kinect open source code needs to be in the path.
        updatePath(HomeDir.FullName + "librealsense\build\Release\", "Realsense camera support.")
        updatePath(HomeDir.FullName + "Azure-Kinect-Sensor-SDK\build\bin\Release\", "Kinect camera support.")
        updatePath(HomeDir.FullName + "OpenCV\Build\bin\Debug\", "OpenCV and OpenCV Contrib are needed for C++ classes.")
        updatePath(HomeDir.FullName + "OpenCV\Build\bin\Release\", "OpenCV and OpenCV Contrib are needed for C++ classes.")
        ' the Kinect depthEngine DLL is not included in the SDK.  It is distributed separately because it is NOT open source.
        ' The depthEngine DLL is supposed to be installed in C:\Program Files\Azure Kinect SDK v1.1.0\sdk\windows-desktop\amd64\$(Configuration)
        ' Post an issue if this Is Not a valid assumption
        Dim kinectDLL As New FileInfo("C:\Program Files\Azure Kinect SDK v1.3.0\sdk\windows-desktop\amd64\release\bin\depthengine_2_0.dll")
        If kinectDLL.Exists = False Then
            MsgBox("The Microsoft installer for the Kinect camera proprietary portion" + vbCrLf +
                   "was not installed in the expected place. (Has it changed?)" + vbCrLf +
                   "It was expected to be in " + kinectDLL.FullName + vbCrLf +
                   "Update the code near this message and restart.")
            optionsForm.cameraDeviceCount(OptionsDialog.Kinect4AzureCam) = 0 ' we can't use this device
        Else
            updatePath(kinectDLL.Directory.FullName, "Kinect depth engine dll.")
        End If
        OpenCVfullPath = HomeDir.FullName + "OpenCV\Build\bin\Release\"

        For i = 0 To OptionsDialog.MyntD1000
            If optionsForm.cameraDeviceCount(i) > 0 Then optionsForm.cameraTotalCount += 1
        Next

        cameraRS2Generic = New CameraRS2
        Dim RS2count = cameraRS2Generic.queryDeviceCount()
        For i = 0 To RS2count - 1
            Dim deviceName = cameraRS2Generic.queryDevice(i)
            Select Case deviceName
                Case "Intel RealSense D435I"
                    cameraD435i = New CameraRS2
                    cameraD435i.IMU_Present = True
                    cameraD435i.deviceName = deviceName
                Case "Intel RealSense L515"
                    cameraL515 = New CameraRS2
                    cameraL515.deviceName = deviceName
                Case "Intel RealSense T265"
                    cameraT265 = New CameraT265
                    cameraT265.deviceName = deviceName
            End Select
        Next
        cameraKinect = New CameraKinect
        cameraZed2 = New CameraZED2
        cameraMyntD = New CameraMyntD

        updateCamera()

        optionsForm.cameraRadioButton(optionsForm.cameraIndex).Checked = True ' make sure any switch is reflected in the UI.
        optionsForm.enableCameras()

        setupCamPics()
        loadAlgorithmComboBoxes()

        TestAllTimer.Interval = optionsForm.TestAllDuration.Text * 1000
        FindPython()
    End Sub
    Private Sub campic_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        Dim pic = DirectCast(sender, PictureBox)
        g.ScaleTransform(1, 1)
        g.DrawImage(pic.Image, 0, 0)
        If drawRect.Width > 0 And drawRect.Height > 0 Then
            g.DrawRectangle(myPen, drawRect.X, drawRect.Y, drawRect.Width, drawRect.Height)
        End If
        If algorithmRefresh And (pic.Tag = 2 Or pic.Tag = 3) Then
            algorithmRefresh = False
            SyncLock formResult1
                Try
                    If formResult1.Width <> camPic(2).Width Or formResult1.Height <> camPic(2).Height Then
                        Dim result1 = formResult1
                        Dim result2 = formResult2
                        result1 = result1.Resize(New cv.Size(camPic(2).Size.Width, camPic(2).Size.Height))
                        result2 = result2.Resize(New cv.Size(camPic(3).Size.Width, camPic(3).Size.Height))
                        cvext.BitmapConverter.ToBitmap(result1, camPic(2).Image)
                        cvext.BitmapConverter.ToBitmap(result2, camPic(3).Image)
                    Else
                        cvext.BitmapConverter.ToBitmap(formResult1, camPic(2).Image)
                        cvext.BitmapConverter.ToBitmap(formResult2, camPic(3).Image)
                    End If
                Catch ex As Exception
                    Console.WriteLine("Error in form result update: " + ex.Message)
                End Try
            End SyncLock
        End If
        If cameraRefresh And (pic.Tag = 0 Or pic.Tag = 1) Then
            cameraRefresh = False
            Dim RGBDepth As New cv.Mat
            Dim color As New cv.Mat
            SyncLock bufferLock ' avoid updating the image while copying into it in the algorithm and camera tasks
                If camera.color IsNot Nothing Then
                    If camera.color.width > 0 Then
                        Try
                            If RGBDepth.Width = 0 Then RGBDepth = New cv.Mat
                            RGBDepth = camera.RGBDepth.Resize(New cv.Size(camPic(1).Size.Width, camPic(1).Size.Height))
                            color = camera.color.Resize(New cv.Size(camPic(0).Size.Width, camPic(0).Size.Height))
                            cvext.BitmapConverter.ToBitmap(color, camPic(0).Image)
                            cvext.BitmapConverter.ToBitmap(RGBDepth, camPic(1).Image)
                        Catch ex As Exception
                            Console.WriteLine("Error in camera update: " + ex.Message)
                        End Try
                    End If
                End If
            End SyncLock
        End If
        ' draw any TrueType font data on the image 
        Dim maxline = 21
        SyncLock TTtextData
            Try
                For i = 0 To TTtextData(pic.Tag).Count - 1
                    Dim tt = TTtextData(pic.Tag)(i)
                    If tt IsNot Nothing Then
                        g.DrawString(tt.text, optionsForm.fontInfo.Font, New SolidBrush(System.Drawing.Color.White), tt.x, tt.y)
                        If tt.x >= camPic(pic.Tag).Width Or tt.y >= camPic(pic.Tag).Height Then
                            Console.WriteLine("TrueType text off image!  " + tt.text + " is being written to " + CStr(tt.x) + " and " + CStr(tt.y))
                        End If
                        maxline -= 1
                        If maxline <= 0 Then Exit For
                    End If
                Next
            Catch ex As Exception
                Console.WriteLine("Error in ttextData update: " + ex.Message)
            End Try

            If optionsForm.ShowLabels.Checked Then
                ' with the low resolution display, we need to use the entire width of the image to display the RGB and Depth text area.
                Dim textRect As New Rectangle(0, 0, pic.Width / 2, If(resizeForDisplay = 4, 12, 20))
                If Len(picLabels(pic.Tag)) Then g.FillRectangle(myBrush, textRect)
                g.DrawString(picLabels(pic.Tag), optionsForm.fontInfo.Font, New SolidBrush(System.Drawing.Color.Black), 0, 0)
            End If
        End SyncLock
        AlgorithmDesc.Text = textDesc
    End Sub
    Private Sub RestartCamera()
        camera.closePipe()
        stopCameraThread = True
        If threadStop(camera.frameCount) = False Then cameraTaskHandle.Abort()
        If threadStop(camera.frameCount) = False Then cameraTaskHandle.Abort()
        If cameraTaskHandle IsNot Nothing Then cameraTaskHandle.Abort()
        If cameraTaskHandle IsNot Nothing Then cameraTaskHandle.Abort()
        cameraTaskHandle = Nothing
        updateCamera()
    End Sub
    Public Sub updateCamera()
        camera = Choose(optionsForm.cameraIndex + 1, cameraKinect, cameraT265, cameraZed2, cameraMyntD, cameraD435i, cameraL515) ' order is same as in optionsdialog enum
        If camera.devicename = "" Or camera.devicename.startswith("Intel RealSense") Then
            camera.width = regWidth
            camera.height = regHeight
            camera.initialize(fps)
        End If
        camera.pipelineclosed = False
        SaveSetting("OpenCVB", "CameraIndex", "CameraIndex", optionsForm.cameraIndex)
    End Sub
    Private Sub OpenCVB_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        If camPic Is Nothing Then Exit Sub ' when first opening, campic may not be built yet
        If camPic(3) Is Nothing Then Exit Sub ' individual pictureboxes need to be ready as well.
        LineUpCamPics()
    End Sub
    Private Sub LineUpCamPics()
        Dim width = CInt((Me.Width - 38) / 2)
        Dim height = CInt(width * regHeight / regWidth)
        If Math.Abs(width - regWidth / 2) < 2 Then width = regWidth / 2
        If Math.Abs(height - regHeight / 2) < 2 Then height = regHeight / 2
        Dim padX = 12
        Dim padY = 40
        For i = 0 To camPic.Length - 1
            camPic(i).Size = New Size(width, height)
            camPic(i).Image = New Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb)
            Dim picleft = Choose(i + 1, padX, camPic(0).Left + camPic(i).Width, padX, camPic(0).Left + camPic(i).Width)
            Dim picTop = Choose(i + 1, padY, padY, camPic(0).Top + camPic(i).Height, camPic(0).Top + camPic(i).Height)
            camPic(i).Location = New Point(picleft, picTop)
        Next
        saveLayout()
    End Sub
    Public Function USBenumeration(searchName As String) As Int32
        Static firstCall = 0
        Dim deviceCount As Int32
        ' See if the desired device shows up in the device manager.'
        Dim info As Management.ManagementObject
        Dim search As System.Management.ManagementObjectSearcher
        Dim Name As String
        search = New System.Management.ManagementObjectSearcher("SELECT * From Win32_PnPEntity")
        For Each info In search.Get()
            Name = CType(info("Caption"), String) ' Get the name of the device.'
            ' toss the uninteresting names so we can find the cameras.
            If Name Is Nothing Then Continue For

            If firstCall = 0 Then
                ' why do this?  So enumeration can tell us about the cameras present in a short list.
                If InStr(Name, "Xeon") Or InStr(Name, "Chipset") Or InStr(Name, "Generic") Or InStr(Name, "Bluetooth") Or
                    InStr(Name, "Monitor") Or InStr(Name, "Mouse") Or InStr(Name, "NVIDIA") Or InStr(Name, "HID-compliant") Or
                    InStr(Name, " CPU ") Or InStr(Name, "PCI Express") Or Name.StartsWith("USB ") Or
                    Name.StartsWith("Microsoft") Or Name.StartsWith("Motherboard") Or InStr(Name, "SATA") Or
                    InStr(Name, "Volume") Or Name.StartsWith("WAN") Or InStr(Name, "ACPI") Or
                    Name.StartsWith("HID") Or InStr(Name, "OneNote") Or Name.StartsWith("Samsung") Or
                    Name.StartsWith("System ") Or Name.StartsWith("HP") Or InStr(Name, "Wireless") Or
                    Name.StartsWith("SanDisk") Or InStr(Name, "Wi-Fi") Or Name.StartsWith("Media ") Or
                    Name.StartsWith("High precision") Or Name.StartsWith("High Definition ") Or
                    InStr(Name, "Remote") Or InStr(Name, "Numeric") Or InStr(Name, "UMBus ") Or
                    Name.StartsWith("Plug or Play") Or InStr(Name, "Print") Or Name.StartsWith("Direct memory") Or
                    InStr(Name, "interrupt controller") Or Name.StartsWith("NVVHCI") Or Name.StartsWith("Plug and Play") Or
                    Name.StartsWith("ASMedia") Or Name = "Fax" Or Name.StartsWith("Speakers") Or
                    InStr(Name, "Host Controller") Or InStr(Name, "Management Engine") Or InStr(Name, "Legacy") Or
                    Name.StartsWith("NDIS") Or Name.StartsWith("Logitech USB Input Device") Or
                    Name.StartsWith("Simple Device") Or InStr(Name, "Ethernet") Or Name.StartsWith("WD ") Or
                    InStr(Name, "Composite Bus Enumerator") Or InStr(Name, "Turbo Boost") Or Name.StartsWith("Realtek") Or
                    Name.StartsWith("PCI-to-PCI") Or Name.StartsWith("Network Controller") Or Name.StartsWith("ATAPI ") Then
                Else
                    Console.WriteLine(Name) ' looking for new cameras 
                End If
            End If
            If InStr(Name, searchName, CompareMethod.Text) > 0 Then
                If firstCall = 0 Then Console.WriteLine(Name)
                deviceCount += 1
            End If
        Next
        firstCall += 1
        Return deviceCount
    End Function
    Private Sub setupCamPics()
        Me.Left = GetSetting("OpenCVB", "OpenCVBLeft", "OpenCVBLeft", Me.Left)
        Me.Top = GetSetting("OpenCVB", "OpenCVBTop", "OpenCVBTop", Me.Top)

        Dim goodPoint = Screen.GetWorkingArea(New Point(Me.Left, Me.Top)) ' when they change the main screen, old coordinates can go way off the screen.
        If goodPoint.X > Me.Left Then Me.Left = goodPoint.X
        If goodPoint.Y > Me.Top Then Me.Top = goodPoint.Y

        Dim defaultWidth = regWidth * 2 / resizeForDisplay + border * 7
        Dim defaultHeight = regHeight * 2 / resizeForDisplay + ToolStrip1.Height + border * 12
        Me.Width = GetSetting("OpenCVB", "OpenCVBWidth", "OpenCVBWidth", defaultWidth)
        Me.Height = GetSetting("OpenCVB", "OpenCVBHeight", "OpenCVBHeight", defaultHeight)
        If Me.Height < 50 Then
            Me.Width = defaultWidth
            Me.Height = defaultHeight
        End If

        For i = 0 To TTtextData.Count - 1
            TTtextData(i) = New List(Of VB_Classes.oTrueType)
        Next

        For i = 0 To camPic.Length - 1
            If camPic(i) Is Nothing Then camPic(i) = New PictureBox()
            camPic(i).Size = New Size(regWidth / resizeForDisplay, regHeight / resizeForDisplay)
            AddHandler camPic(i).DoubleClick, AddressOf campic_DoubleClick
            AddHandler camPic(i).Click, AddressOf campic_Click
            AddHandler camPic(i).Paint, AddressOf campic_Paint
            AddHandler camPic(i).MouseDown, AddressOf camPic_MouseDown
            AddHandler camPic(i).MouseUp, AddressOf camPic_MouseUp
            AddHandler camPic(i).MouseMove, AddressOf camPic_MouseMove
            camPic(i).Tag = i
            Me.Controls.Add(camPic(i))
        Next
        LineUpCamPics()
    End Sub
    Private Sub FindPython()
        Dim pythonStr = GetSetting("OpenCVB", "PythonExe", "PythonExe", "Python.exe")
        If pythonStr = "" Then pythonStr = "Python.exe" ' Legacy issue... New users won't hit this...
        Dim currentName = New FileInfo(pythonStr)
        If currentName.Exists = False Then
            Dim appData = GetFolderPath(SpecialFolder.ApplicationData)
            Dim directoryInfo As New DirectoryInfo(appData + "\..\Local\Programs\Python\")
            If directoryInfo.Exists = False Then
                MsgBox("OpenCVB cannot find an active Python.  Use Options/Python to specify Python.exe.")
                Exit Sub
            End If
            For Each Dir As String In System.IO.Directory.GetDirectories(directoryInfo.FullName)
                Dim dirInfo As New System.IO.DirectoryInfo(Dir)
                Dim pythonFileInfo = New FileInfo(dirInfo.FullName + "\Python.exe")
                If pythonFileInfo.Exists Then
                    SaveSetting("OpenCVB", "PythonExe", "PythonExe", pythonFileInfo.FullName)
                    optionsForm.PythonExeName.Text = pythonFileInfo.FullName
                    Exit For
                End If
            Next
        End If
    End Sub
    Private Sub loadAlgorithmComboBoxes()
        ' we always need the number of lines from the algorithmList.txt file (and it is not always read when working with a subset of algorithms.)
        Dim AlgorithmListFileInfo = New FileInfo("../../Data/AlgorithmList.txt")
        If AlgorithmListFileInfo.Exists = False Then
            MsgBox("The AlgorithmList.txt file is missing.  It should be in " + AlgorithmListFileInfo.FullName + "  Look at UI_Generator project.")
            End
        End If
        Dim sr = New StreamReader(AlgorithmListFileInfo.FullName)
        Dim infoLine = sr.ReadLine
        Dim Split = Regex.Split(infoLine, "\W+")
        CodeLineCount = Split(1)
        While sr.EndOfStream = False
            infoLine = sr.ReadLine
            AlgorithmCount += 1
        End While
        sr.Close()

        Dim AlgorithmMapFileInfo = New FileInfo("../../Data/AlgorithmMapToOpenCV.txt")
        If AlgorithmMapFileInfo.Exists = False Then
            MsgBox("The AlgorithmMapToOpenCV.txt file is missing.  Look at Index Project that creates the mapping of algorithms to OpenCV keywords.")
            End
        End If
        sr = New StreamReader(AlgorithmMapFileInfo.FullName)
        OpenCVkeyword.Items.Clear()
        While sr.EndOfStream = False
            infoLine = sr.ReadLine
            openCVKeywords.Add(infoLine)
            Split = Regex.Split(infoLine, ",")
            OpenCVkeyword.Items.Add(Split(0))
        End While
        sr.Close()

        OpenCVkeyword.Text = GetSetting("OpenCVB", "OpenCVkeyword", "OpenCVkeyword", "<All>")
        If OpenCVkeyword.Text = "" Then OpenCVkeyword.Text = "<All>"
        SaveSetting("OpenCVB", "OpenCVkeyword", "OpenCVkeyword", OpenCVkeyword.Text)
    End Sub
    Private Sub OpenCVkeyword_SelectedIndexChanged(sender As Object, e As EventArgs) Handles OpenCVkeyword.SelectedIndexChanged
        stopAlgorithmThread = True
        If OpenCVkeyword.Text = "<All>" Or OpenCVkeyword.Text = "<All using recorded data>" Then
            Dim AlgorithmListFileInfo = New FileInfo("../../Data/AlgorithmList.txt")
            Dim sr = New StreamReader(AlgorithmListFileInfo.FullName)

            Dim infoLine = sr.ReadLine
            Dim Split = Regex.Split(infoLine, "\W+")
            CodeLineCount = Split(1)
            AvailableAlgorithms.Items.Clear()
            While sr.EndOfStream = False
                infoLine = sr.ReadLine
                infoLine = UCase(Mid(infoLine, 1, 1)) + Mid(infoLine, 2)
                AvailableAlgorithms.Items.Add(infoLine)
            End While
            sr.Close()
        Else
            AvailableAlgorithms.Enabled = False
            Dim keyIndex = OpenCVkeyword.Items.IndexOf(OpenCVkeyword.Text)
            Dim openCVkeys = openCVKeywords(keyIndex)
            Dim split = Regex.Split(openCVkeys, ",")
            AvailableAlgorithms.Items.Clear()
            For i = 1 To split.Length - 1
                AvailableAlgorithms.Items.Add(split(i))
            Next
            AvailableAlgorithms.Enabled = True
        End If
        AvailableAlgorithms.Text = GetSetting("OpenCVB", OpenCVkeyword.Text, OpenCVkeyword.Text, AvailableAlgorithms.Items(0))
        Dim index = AvailableAlgorithms.Items.IndexOf(AvailableAlgorithms.Text)
        If index < 0 Then AvailableAlgorithms.SelectedIndex = 0 Else AvailableAlgorithms.SelectedIndex = index
        SaveSetting("OpenCVB", "OpenCVkeyword", "OpenCVkeyword", OpenCVkeyword.Text)
    End Sub
    Private Sub updatePath(neededDirectory As String, notFoundMessage As String)
        Dim systemPath = Environment.GetEnvironmentVariable("Path")
        Dim curDirectory = CurDir()
        If InStr(systemPath, curDirectory) = False Then systemPath = curDirectory + ";" + systemPath
        Dim foundDirectory As Boolean
        If Directory.Exists(neededDirectory) Then
            foundDirectory = True
            systemPath = neededDirectory + ";" + systemPath
        End If

        ' maybe they didn't build the release version yet.
        If foundDirectory = False And InStr(neededDirectory, "Release") Then
            neededDirectory.Replace("Release", "Debug")
            If Directory.Exists(neededDirectory) Then
                foundDirectory = True
                systemPath = neededDirectory + ";" + systemPath
            End If
        End If

        If foundDirectory = False And notFoundMessage.Length > 0 Then
            MsgBox(neededDirectory + " was not found.  " + notFoundMessage)
        End If
        Environment.SetEnvironmentVariable("Path", systemPath)
    End Sub

    Public Sub DisplayOfficeFile(ByVal WorkingDir As String, ByVal FileName As String)
        Try
            Dim MyProcess As New Process
            Dim BaseName As String = Dir$(FileName)
            MyProcess.StartInfo.FileName = FileName
            MyProcess.StartInfo.WorkingDirectory = WorkingDir
            MyProcess.StartInfo.Verb = "OPEN"
            MyProcess.StartInfo.CreateNoWindow = False
            MyProcess.Start()
        Catch ex As Exception
            MsgBox("DisplayOfficeFile failed with error = " + ex.Message)
        End Try
    End Sub
    Public Function validateRect(src As cv.Mat, r As cv.Rect) As cv.Rect
        If r.Width < 0 Then r.Width = 1
        If r.Height < 0 Then r.Height = 1
        If r.X < 0 Then r.X = 0
        If r.Y < 0 Then r.Y = 0
        If r.X > src.Width Then r.X = src.Width
        If r.Y > src.Height Then r.Y = src.Height
        If r.X + r.Width > src.Width Then r.Width = src.Width - r.X
        If r.Y + r.Height > src.Height Then r.Height = src.Height - r.Y
        Return r
    End Function
    Private Sub camPic_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
        Try
            If e.Button = Windows.Forms.MouseButtons.Left Then
                If DrawingRectangle Then
                    DrawingRectangle = False
                    GrabRectangleData = True
                End If
            ElseIf e.Button = Windows.Forms.MouseButtons.Right Then
                BothFirstAndLastReady = False ' we have to see some movement after mousedown.
                DrawingRectangle = False

                Dim pic = DirectCast(sender, PictureBox)
                Dim src = Choose(pic.Tag + 1, camera.Color, camera.RGBDepth, formResult1, formResult2)
                drawRect = validateRect(src, drawRect)
                Dim srcROI = New cv.Mat
                srcROI = src(drawRect).clone()
                Dim csvName As New FileInfo(System.IO.Path.GetTempFileName() + ".csv")
                Dim sw = New StreamWriter(csvName.FullName)
                sw.WriteLine("Color image in BGR format - 3 columns per pixel" + vbCrLf)
                sw.WriteLine(vbCrLf + "width = " + CStr(drawRect.Width) + vbCrLf + "height = " + CStr(drawRect.Height))
                sw.Write("Row," + vbTab)
                For i = 0 To drawRect.Width - 1
                    sw.Write("B" + Format(drawRect.X + i, "000") + "," + "G" + Format(drawRect.X + i, "000") + "," + "R" + Format(drawRect.X + i, "000") + ",")
                Next
                sw.WriteLine()
                For y = 0 To drawRect.Height - 1
                    sw.Write("Row " + Format(drawRect.Y + y, "000") + "," + vbTab)
                    For x = 0 To drawRect.Width - 1
                        Dim pt = srcROI.Get(Of cv.Vec3b)(y, x)
                        sw.Write(CStr(pt.Item0) + "," + CStr(pt.Item1) + "," + CStr(pt.Item2) + ",")
                    Next
                    sw.WriteLine("")
                Next
                ' write the min and max values
                sw.WriteLine(vbCrLf + vbCrLf)
                Dim split() = srcROI.Split()
                Dim min As Single, max As Single
                split(0).MinMaxLoc(min, max)
                sw.WriteLine("blu_min" + "," + CStr(min))
                sw.WriteLine("blu_max" + "," + CStr(max))

                split(1).MinMaxLoc(min, max)
                sw.WriteLine("grn_min" + "," + CStr(min))
                sw.WriteLine("grn_max" + "," + CStr(max))

                split(2).MinMaxLoc(min, max)
                sw.WriteLine("red_min" + "," + CStr(min))
                sw.WriteLine("red_max" + "," + CStr(max))

                sw.Close()
                DisplayOfficeFile(csvName.DirectoryName, csvName.FullName)
            End If
        Catch ex As Exception
            Console.WriteLine("Error in camPic_MouseUp: " + ex.Message)
        End Try
    End Sub

    Private Sub camPic_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
        Try
            Dim pic = DirectCast(sender, PictureBox)
            If e.Button = Windows.Forms.MouseButtons.Left Or e.Button = Windows.Forms.MouseButtons.Right Then
                DrawingRectangle = True
                BothFirstAndLastReady = False ' we have to see some movement after mousedown.
                drawRect.Width = 0
                drawRect.Height = 0
                mouseDownPoint.X = e.X
                mouseDownPoint.Y = e.Y
            End If
        Catch ex As Exception
            Console.WriteLine("Error in camPic_MouseDown: " + ex.Message)
        End Try
    End Sub
    Private Sub camPic_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
        Try
            Dim pic = DirectCast(sender, PictureBox)
            If DrawingRectangle Then
                mouseMovePoint.X = e.X
                mouseMovePoint.Y = e.Y
                If mouseMovePoint.X < 0 Then mouseMovePoint.X = 0
                If mouseMovePoint.Y < 0 Then mouseMovePoint.Y = 0
                drawRect.X = Math.Min(mouseDownPoint.X, mouseMovePoint.X)
                drawRect.Y = Math.Min(mouseDownPoint.Y, mouseMovePoint.Y)
                drawRect.Width = Math.Abs(mouseDownPoint.X - mouseMovePoint.X)
                drawRect.Height = Math.Abs(mouseDownPoint.Y - mouseMovePoint.Y)
                If drawRect.X + drawRect.Width > camera.Color.Width Then drawRect.Width = camera.Color.Width - drawRect.X
                If drawRect.Y + drawRect.Height > camera.Color.Height Then drawRect.Height = camera.Color.Height - drawRect.Y
                BothFirstAndLastReady = True
            End If
            mousePicTag = pic.Tag
            mousePoint.X = e.X
            mousePoint.Y = e.Y
        Catch ex As Exception
            Console.WriteLine("Error in camPic_MouseMove: " + ex.Message)
        End Try
    End Sub
    Private Sub AvailableAlgorithms_KeyDown(sender As Object, e As KeyEventArgs) Handles AvailableAlgorithms.KeyDown
        e.SuppressKeyPress = True
    End Sub
    Private Sub OpenCVkeyword_KeyDown(sender As Object, e As KeyEventArgs) Handles OpenCVkeyword.KeyDown
        e.SuppressKeyPress = True
    End Sub
    Private Sub keyholdTimer_Tick(sender As Object, e As EventArgs) Handles keyholdTimer.Tick
        keyboardInput += keyboardLastInput ' press and hold means send this key again...
    End Sub
    Private Sub OpenCVB_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        Dim repeated = keyholdTimer.Enabled
        keyholdTimer.Enabled = False
        If e.KeyCode = Keys.Escape Then
            keyboardInput = ""
            Exit Sub
        End If

        SyncLock bufferLock
            If repeated Then
                keyboardInput = (e.KeyData.ToString()).ToLower ' just the last key if we were repeating characters.
            Else
                keyboardInput += (e.KeyData.ToString()).ToLower
            End If
        End SyncLock
    End Sub
    Private Sub OpenCVB_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        keyholdTimer.Enabled = True
        keyboardLastInput = (e.KeyData.ToString()).ToLower
    End Sub
    Private Sub campic_DoubleClick(sender As Object, e As EventArgs)
        DrawingRectangle = False
    End Sub
    Private Sub campic_Click(sender As Object, e As EventArgs)
        mouseClickFlag = True
        mouseClickPoint = mousePoint
    End Sub
    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles PausePlayButton.Click
        Static saveTestAllState As Boolean
        If stopAlgorithmThread Then
            stopAlgorithmThread = False
            If saveTestAllState Then testAllButton_Click(sender, e) Else StartAlgorithmTask()
            PausePlayButton.Image = Image.FromFile("../../OpenCVB/Data/PauseButton.png")
        Else
            saveTestAllState = TestAllTimer.Enabled
            If TestAllTimer.Enabled Then testAllButton_Click(sender, e)
            stopAlgorithmThread = True
            PausePlayButton.Image = Image.FromFile("../../OpenCVB/Data/PauseButtonRun.png")
        End If
    End Sub
    Private Sub testAllButton_Click(sender As Object, e As EventArgs) Handles TestAllButton.Click
        If TestAllButton.Text = "Test All" Then
            TestAllButton.Text = "Stop Test"
            TestAllButton.Image = Image.FromFile("../../OpenCVB/Data/StopTest.png")
            TestAllTimer_Tick(sender, e)
            TestAllTimer.Enabled = True
        Else
            TestAllTimer.Enabled = False
            TestAllButton.Text = "Test All"
            TestAllButton.Image = Image.FromFile("../../OpenCVB/Data/testall.png")
        End If
    End Sub

    Private Sub OpenCVB_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        OptionsBringToFront = True
    End Sub
    Private Sub OpenCVB_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd
        saveLayout()
    End Sub
    Private Sub AvailableAlgorithms_SelectedIndexChanged(sender As Object, e As EventArgs) Handles AvailableAlgorithms.SelectedIndexChanged
        If AvailableAlgorithms.Enabled Then
            SaveSetting("OpenCVB", OpenCVkeyword.Text, OpenCVkeyword.Text, AvailableAlgorithms.Text)
            StartAlgorithmTask()
        End If
    End Sub
    Private Sub ActivateTimer_Tick(sender As Object, e As EventArgs) Handles ActivateTimer.Tick
        ActivateTimer.Enabled = False
        If TestAllButton.Text <> "Stop Test" Then Me.Activate()
        RefreshAvailable = True
    End Sub
    Private Sub RefreshTimer_Tick(sender As Object, e As EventArgs) Handles RefreshTimer.Tick
        If camera IsNot Nothing Then If camera.color.width Then Me.Refresh()
    End Sub
    Private Sub fpsTimer_Tick(sender As Object, e As EventArgs) Handles fpsTimer.Tick
        Static lastFrame As Int32
        If lastFrame > frameCount Then lastFrame = 0
        Dim countFrames = frameCount - lastFrame
        lastFrame = frameCount
        Dim fps As Single = countFrames / (fpsTimer.Interval / 1000)

        Static lastCameraFrame As Int32
        If lastCameraFrame > camera.frameCount Then lastCameraFrame = 0
        Dim camFrames = camera.frameCount - lastCameraFrame
        lastCameraFrame = camera.frameCount
        Dim cameraFPS As Single = camFrames / (fpsTimer.Interval / 1000)

        Me.Text = "OpenCVB (" + Format(CodeLineCount, "###,##0") + " lines / " + CStr(AlgorithmCount) + " algorithms = " + CStr(CInt(CodeLineCount / AlgorithmCount)) +
                  " lines per) - " + optionsForm.cameraRadioButton(optionsForm.cameraIndex).Text + " - " + Format(cameraFPS, "#0.0") +
                  "/" + Format(fps, "#0.0") + " " + CStr(totalBytesOfMemoryUsed) + " Mb (working set)"
    End Sub
    Private Sub saveLayout()
        SaveSetting("OpenCVB", "OpenCVBLeft", "OpenCVBLeft", Me.Left)
        SaveSetting("OpenCVB", "OpenCVBTop", "OpenCVBTop", Me.Top)
        SaveSetting("OpenCVB", "OpenCVBWidth", "OpenCVBWidth", Me.Width)
        SaveSetting("OpenCVB", "OpenCVBHeight", "OpenCVBHeight", Me.Height)

        Dim details = CStr(regWidth) + "x" + CStr(regHeight) + " display " + CStr(camPic(0).Width) + "x" + CStr(camPic(0).Height) + " Resolution="
        If optionsForm.lowResolution.Checked Then details += "Medium" Else details += "High"
        picLabels(0) = "Input " + details
        picLabels(1) = "Depth " + details
    End Sub
    Private Sub MainFrm_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        stopAlgorithmThread = True
        stopCameraThread = True
        Application.DoEvents()
        camera.closePipe()
        If threadStop(frameCount) = False Then algorithmTaskHandle.Abort()
        If algorithmTaskHandle IsNot Nothing Then algorithmTaskHandle.Abort()
        If threadStop(camera.frameCount) = False Then cameraTaskHandle.Abort()
        If cameraTaskHandle IsNot Nothing Then cameraTaskHandle.Abort()
        textDesc = ""
        saveLayout()
    End Sub
    Private Sub SnapShotButton_Click(sender As Object, e As EventArgs) Handles SnapShotButton.Click
        Dim img As New Bitmap(Me.Width, Me.Height)
        Me.DrawToBitmap(img, New Rectangle(0, 0, Me.Width, Me.Height))

        Dim snapForm = New SnapshotRequest
        snapForm.SnapshotRequest_Load(sender, e)
        snapForm.PictureBox1.Image = img
        snapForm.AllImages.Checked = True
        If snapForm.ShowDialog() <> DialogResult.OK Then Exit Sub

        Dim resultMat As New cv.Mat
        For i = 0 To 4
            Dim radioButton = Choose(i + 1, snapForm.AllImages, snapForm.ColorImage, snapForm.RGBDepth, snapForm.Result1, snapForm.Result2)
            If radioButton.checked Then
                SyncLock bufferLock
                    Select Case i
                        Case 0 ' all images
                            resultMat = cv.Extensions.BitmapConverter.ToMat(img)
                        Case 1 ' color image
                            resultMat = camera.Color.Clone()
                        Case 2 ' depth RGB
                            resultMat = camera.RGBDepth.Clone()
                        Case 3 ' result1
                            resultMat = formResult1.Clone()
                        Case 4 ' result2
                            resultMat = formResult2.Clone()
                    End Select
                    Exit For
                End SyncLock
            End If
        Next
        img = cv.Extensions.BitmapConverter.ToBitmap(resultMat)
        Clipboard.SetImage(img)
    End Sub
    Private Sub CameraTask()
        While stopCameraThread = False
            camera.GetNextFrame()

            cameraRefresh = True
            Dim currentProcess = System.Diagnostics.Process.GetCurrentProcess()
            totalBytesOfMemoryUsed = currentProcess.WorkingSet64 / (1024 * 1024)
            'Static warningIssued As Boolean = False
            'If totalBytesOfMemoryUsed > 4000 And warningIssued = False Then
            '    MsgBox("OpenCVB appears to have a memory leak in the " + saveAlgorithmName + " algorithm" + vbCrLf +
            '           "The memory footprint has grown above 4Gb which is more than expected.")
            '    warningIssued = True
            'End If
            GC.Collect() ' minimize memory footprint - the frames have just been sent so this task isn't busy.
        End While
        camera.frameCount = 0
    End Sub

    Private Sub TestAllTimer_Tick(sender As Object, e As EventArgs) Handles TestAllTimer.Tick
        If stopAlgorithmThread = True Then Exit Sub ' they have paused.

        ' if lowresolution is active and all the algorithms are covered, then switch to high res or vice versa...
        If AlgorithmTestCount Mod AvailableAlgorithms.Items.Count = 0 And AlgorithmTestCount > 0 Then
            optionsForm.lowResolution.Checked = Not optionsForm.lowResolution.Checked
            saveLayout()
        End If

        Static changeCameras As Integer
        If AvailableAlgorithms.Items.Count = 1 Then changeCameras += 1
        ' after sweeping through low and high resolution, sweep through the cameras as well...
        If (AlgorithmTestCount Mod (AvailableAlgorithms.Items.Count * 2) = 0 And AlgorithmTestCount > 0) Or changeCameras >= 2 Then
            changeCameras = 0
            Dim cameraIndex = optionsForm.cameraIndex
            Dim saveCameraIndex = optionsForm.cameraIndex
            cameraIndex += 1
            If cameraIndex >= optionsForm.cameraRadioButton.Count Then cameraIndex = 0
            For i = 0 To optionsForm.cameraRadioButton.Count - 1
                If optionsForm.cameraRadioButton(cameraIndex).Enabled Then
                    optionsForm.cameraRadioButton(cameraIndex).Checked = True
                    Exit For
                Else
                    cameraIndex += 1
                    If cameraIndex >= optionsForm.cameraRadioButton.Count Then cameraIndex = 0
                End If
            Next
            If saveCameraIndex <> cameraIndex Then
                optionsForm.cameraIndex = cameraIndex
                RestartCamera()
            End If
        End If

        If AvailableAlgorithms.SelectedIndex < AvailableAlgorithms.Items.Count - 1 Then
            AvailableAlgorithms.SelectedIndex += 1
        Else
            If AvailableAlgorithms.Items.Count = 1 Then ' selection index won't change if there is only one algorithm in the list.
                StartAlgorithmTask()
            Else
                AvailableAlgorithms.SelectedIndex = 0
            End If
        End If
    End Sub

    Private Sub Options_Click(sender As Object, e As EventArgs) Handles OptionsButton.Click
        If TestAllTimer.Enabled Then testAllButton_Click(sender, e)
        TestAllTimer.Enabled = False
        stopAlgorithmThread = True
        Dim saveCurrentCamera = optionsForm.cameraIndex

        Dim OKcancel = optionsForm.ShowDialog()

        If OKcancel = DialogResult.OK Then
            optionsForm.TestEnableNumPy()
            If saveCurrentCamera <> optionsForm.cameraIndex Then RestartCamera()
            TestAllTimer.Interval = optionsForm.TestAllDuration.Value * 1000
            RefreshTimer.Interval = CInt(1000 / optionsForm.RefreshRate.Value)

            If optionsForm.SnapToGrid.Checked Then
                For i = 0 To 3
                    camPic(i).Size = New Size(regWidth / 2, regHeight / 2)
                Next
                camPic(1).Left = camPic(0).Left + camPic(0).Width
                camPic(2).Top = camPic(0).Top + camPic(0).Height
                camPic(3).Left = camPic(2).Left + camPic(2).Width
                camPic(3).Top = camPic(0).Top + camPic(0).Height

                Me.Width = camPic(0).Width * 2 + 40
                Me.Height = camPic(0).Height * 2 + 90
            End If
            saveLayout()
        End If
        StartAlgorithmTask()
    End Sub
    Private Function threadStop(ByRef frame As Int32) As Boolean
        Dim sleepCount As Int32
        ' some algorithms can take a long time to finish a single iteration.  
        ' Each algorithm must run dispose() - to kill options forms and external Python or OpenGL taskes.  Wait until exit...
        While frame
            Thread.Sleep(10)  ' to allow the algorithm task to gracefully end and dispose OpenCVB.
            sleepCount += 1
            If sleepCount > 10 Then Return False
        End While
        Return True
    End Function
    Private Sub StartAlgorithmTask()
        stopAlgorithmThread = True
        ' there may be a long-running algorithmtask that doesn't see that the algorithm has been stopped.
        If threadStop(frameCount) = False Then algorithmTaskHandle.Abort()
        If threadStop(frameCount) = False Then algorithmTaskHandle.Abort()
        If algorithmTaskHandle IsNot Nothing Then algorithmTaskHandle.Abort()
        If algorithmTaskHandle IsNot Nothing Then algorithmTaskHandle.Abort()

        Dim parms As New VB_Classes.ActiveClass.algorithmParameters
        ReDim parms.IMU_RotationMatrix(9 - 1)
        lowResolution = optionsForm.lowResolution.Checked

        saveAlgorithmName = AvailableAlgorithms.Text ' to share with the camera task...
        parms.activeAlgorithm = AvailableAlgorithms.Text
        ' opengl algorithms are only to be run at full resolution.  All other algorithms respect the options setting...
        If parms.activeAlgorithm.Contains("OpenGL") Or parms.activeAlgorithm.Contains("OpenCVGL") Then lowResolution = False
        fastSize = If(lowResolution, New cv.Size(regWidth / 2, regHeight / 2), New cv.Size(regWidth, regHeight))

        parms.resolution = If(lowResolution, OptionsDialog.resMed, OptionsDialog.resHigh)
        parms.cameraIndex = optionsForm.cameraIndex ' index of active camera
        parms.cameraName = camera.deviceName

        parms.PythonExe = optionsForm.PythonExeName.Text
        parms.HomeDir = HomeDir.FullName

        parms.OpenCVfullPath = OpenCVfullPath
        parms.transformationMatrix = camera.transformationmatrix
        parms.OpenCV_Version_ID = Environment.GetEnvironmentVariable("OpenCV_Version")
        parms.imageToTrueTypeLoc = 1 / resizeForDisplay
        parms.useRecordedData = OpenCVkeyword.Text = "<All using recorded data>"
        parms.testAllRunning = TestAllButton.Text = "Stop Test"
        parms.keyboardInput = keyboardInput
        parms.externalPythonInvocation = externalPythonInvocation
        If parms.testAllRunning Then parms.ShowOptions = optionsForm.ShowOptions.Checked Else parms.ShowOptions = True ' always show options when not running 'test all'
        parms.ShowConsoleLog = optionsForm.ShowConsoleLog.Checked
        parms.AvoidDNNCrashes = optionsForm.AvoidDNNCrashes.Checked
        parms.NumPyEnabled = optionsForm.EnableNumPy.Checked

        If parms.resolution = OptionsDialog.resMed Then parms.speedFactor = 2 Else parms.speedFactor = 1
        If parms.resolution = OptionsDialog.resMed Then parms.imageToTrueTypeLoc *= parms.speedFactor

        PausePlayButton.Image = Image.FromFile("../../OpenCVB/Data/PauseButton.png")

        stopAlgorithmThread = False

        formResult1 = New cv.Mat(fastSize, cv.MatType.CV_8UC3, 0)
        formResult2 = New cv.Mat(fastSize, cv.MatType.CV_8UC3, 0)

        Thread.CurrentThread.Priority = ThreadPriority.Lowest

        If cameraTaskHandle Is Nothing Then
            stopCameraThread = False
            cameraTaskHandle = New Thread(AddressOf CameraTask)
            cameraTaskHandle.Name = "CameraTask"
            cameraTaskHandle.Priority = ThreadPriority.Highest
            cameraTaskHandle.Start()
        End If

        parms.IMU_Present = camera.IMU_Present
        parms.intrinsicsLeft = camera.intrinsicsLeft_VB
        parms.intrinsicsRight = camera.intrinsicsRight_VB
        parms.extrinsics = camera.Extrinsics_VB

        algorithmTaskHandle = New Thread(AddressOf AlgorithmTask)
        algorithmTaskHandle.Name = "AlgorithmTask"
        algorithmTaskHandle.Priority = ThreadPriority.Lowest
        algorithmTaskHandle.Start(parms)

        ActivateTimer.Enabled = True
        fpsTimer.Enabled = True
    End Sub
    Private Sub AlgorithmTask(ByVal parms As VB_Classes.ActiveClass.algorithmParameters)
        If parms.testAllRunning Then AlgorithmTestCount += 1
        drawRect = New cv.Rect
        Dim saveLowResSetting As Boolean = parms.resolution
        Dim OpenCVB = New VB_Classes.ActiveClass(parms, regWidth / parms.speedFactor, regHeight / parms.speedFactor, New cv.Rect(Me.Left, Me.Top, Me.Width, Me.Height))
        textDesc = OpenCVB.ocvb.desc

        Console.WriteLine(vbTab + parms.activeAlgorithm + " " + textDesc + vbCrLf + vbTab + "Algorithms tested: " + CStr(AlgorithmTestCount))

        ' Here we check to see if the algorithm constructor changed lowResolution.
        If OpenCVB.ocvb.parms.resolution <> saveLowResSetting Then
            If OpenCVB.ocvb.parms.resolution = OptionsDialog.resMed Then OpenCVB.ocvb.parms.speedFactor = 2 Else OpenCVB.ocvb.parms.speedFactor = 1
            OpenCVB.ocvb.parms.imageToTrueTypeLoc = 1 / resizeForDisplay
            If OpenCVB.ocvb.parms.resolution = OptionsDialog.resMed Then OpenCVB.ocvb.parms.imageToTrueTypeLoc *= OpenCVB.ocvb.parms.speedFactor
        End If

        ' if the constructor for the algorithm sets the drawrect, adjust it for the ratio of the actual size and algorithm sized image.
        If OpenCVB.ocvb.drawRect <> New cv.Rect(0, 0, 0, 0) Then ' the constructor defined drawrect.  Adjust it because lowResolution selected
            drawRect = OpenCVB.ocvb.drawRect
            Dim ratio = OpenCVB.ocvb.color.Width / camPic(0).Width  ' relative size of algorithm size image to displayed image
            drawRect = New cv.Rect(drawRect.X / ratio, drawRect.Y / ratio, drawRect.Width / ratio, drawRect.Height / ratio)
        End If

        For i = VB_Classes.ActiveClass.RESULT1 To VB_Classes.ActiveClass.RESULT2
            TTtextData(i).Clear()
        Next
        BothFirstAndLastReady = False
        frameCount = 0 ' restart the count...
        If OpenCVB.ocvb.parms.NumPyEnabled Then
            Using py.Py.GIL() ' for explanation see http://pythonnet.github.io/ 
                Run(OpenCVB)
            End Using
        Else
            Run(OpenCVB)
        End If

        ' remove all options forms.  They can only be made topmost (see OptionsBringToFront above) when created on the same thread.
        ' This deletes the options forms for the current thread so they will be created again with the next thread.
        Try
            Dim frmlist As New List(Of Form)
            For Each frm In Application.OpenForms
                If frm.name.startswith("Option") Then frmlist.Add(frm)
            Next
            For Each frm In frmlist
                frm.Close()
            Next
        Catch ex As Exception
            Console.WriteLine("Error in ttextData update: " + ex.Message)
        End Try

        OpenCVB.Dispose()
        frameCount = 0
        If parms.testAllRunning Then
            Console.WriteLine(vbTab + "Ending " + parms.activeAlgorithm)
        End If
    End Sub
    Private Sub Run(OpenCVB As VB_Classes.ActiveClass)
        Dim saveAlgorithmTestCount = AlgorithmTestCount ' use this to confirm that this task is to terminate.
        While 1
            ' wait until we have the latest camera data.
            While 1
                If stopAlgorithmThread Then Exit While ' really exit the while loop below...
                Application.DoEvents() ' this will allow any options for the algorithm to be updated...
                If camera.newImagesAvailable Then Exit While
            End While
            If stopAlgorithmThread Then Exit While
            If saveAlgorithmTestCount <> AlgorithmTestCount Then Exit While ' a failsafe provision.  This task needs to exit.

            If camera.color.width = 0 Or camera.RGBDepth.width = 0 Or camera.leftView.width = 0 Or camera.rightView.width = 0 Then Continue While

            ' bring the data into the algorithm task.
            SyncLock bufferLock
                camera.newImagesAvailable = False

                If lowResolution Then
                    OpenCVB.ocvb.color = camera.color.Resize(fastSize)
                    OpenCVB.ocvb.RGBDepth = camera.RGBDepth.Resize(fastSize)
                    OpenCVB.ocvb.leftView = camera.leftView.Resize(fastSize)
                    OpenCVB.ocvb.rightView = camera.rightView.Resize(fastSize)
                Else
                    OpenCVB.ocvb.color = camera.color
                    OpenCVB.ocvb.RGBDepth = camera.RGBDepth
                    OpenCVB.ocvb.leftView = camera.leftView
                    OpenCVB.ocvb.rightView = camera.rightView
                End If
                OpenCVB.ocvb.pointCloud = camera.PointCloud
                OpenCVB.ocvb.depth16 = camera.depth16
                OpenCVB.ocvb.parms.IMU_Acceleration = camera.IMU_Acceleration
                OpenCVB.ocvb.parms.transformationMatrix = camera.transformationMatrix
                OpenCVB.ocvb.parms.IMU_TimeStamp = camera.IMU_TimeStamp
                OpenCVB.ocvb.parms.IMU_Barometer = camera.IMU_Barometer
                OpenCVB.ocvb.parms.IMU_Magnetometer = camera.IMU_Magnetometer
                OpenCVB.ocvb.parms.IMU_Temperature = camera.IMU_Temperature
                OpenCVB.ocvb.parms.IMU_Rotation = camera.IMU_Rotation
                OpenCVB.ocvb.parms.IMU_RotationMatrix = camera.IMU_RotationMatrix
                OpenCVB.ocvb.parms.IMU_RotationVector = camera.IMU_RotationVector
                OpenCVB.ocvb.parms.IMU_Translation = camera.IMU_Translation
                OpenCVB.ocvb.parms.IMU_Acceleration = camera.IMU_Acceleration
                OpenCVB.ocvb.parms.IMU_Velocity = camera.IMU_Velocity
                OpenCVB.ocvb.parms.IMU_AngularAcceleration = camera.IMU_AngularAcceleration
                OpenCVB.ocvb.parms.IMU_AngularVelocity = camera.IMU_AngularVelocity
                OpenCVB.ocvb.parms.IMU_FrameTime = camera.IMU_FrameTime
                OpenCVB.ocvb.parms.CPU_TimeStamp = camera.CPU_TimeStamp
                OpenCVB.ocvb.parms.CPU_FrameTime = camera.CPU_FrameTime
                OpenCVB.ocvb.parms.keyboardInput = keyboardInput
            End SyncLock

            OpenCVB.UpdateHostLocation(New cv.Rect(Me.Left, Me.Top, Me.Width, Me.Height))

            Try
                If GrabRectangleData Then
                    GrabRectangleData = False
                    Dim ratio = camPic(0).Width / OpenCVB.ocvb.color.Width ' relative size of displayed image and algorithm size image.
                    OpenCVB.ocvb.drawRect = New cv.Rect(drawRect.X / ratio, drawRect.Y / ratio, drawRect.Width / ratio, drawRect.Height / ratio)
                    If OpenCVB.ocvb.drawRect.Width <= 2 Then OpenCVB.ocvb.drawRect.Width = 0 ' too small?
                    BothFirstAndLastReady = False
                End If

                OpenCVB.ocvb.mousePoint = mousePoint
                OpenCVB.ocvb.mousePicTag = mousePicTag
                OpenCVB.ocvb.mouseClickFlag = mouseClickFlag
                OpenCVB.ocvb.mouseClickPoint = mouseClickPoint
                mouseClickFlag = False

                OpenCVB.RunAlgorithm()

                If OpenCVB.ocvb.drawRectClear Then
                    drawRect = New cv.Rect
                    OpenCVB.ocvb.drawRect = drawRect
                    OpenCVB.ocvb.drawRectClear = False
                End If

                picLabels(2) = OpenCVB.ocvb.label1
                picLabels(3) = OpenCVB.ocvb.label2
                If RefreshAvailable Then
                    ' share the results of the algorithm task.
                    SyncLock TTtextData
                        If OpenCVB.ocvb.parms.keyInputAccepted Then keyboardInput = ""
                        algorithmRefresh = True
                        formResult1 = OpenCVB.ocvb.result1.Clone()
                        formResult2 = OpenCVB.ocvb.result2.Clone()
                        For i = VB_Classes.ActiveClass.RESULT1 To VB_Classes.ActiveClass.RESULT2
                            If OpenCVB.ocvb.TTtextData(i).Count Then
                                TTtextData(i).Clear()
                                For j = 0 To OpenCVB.ocvb.TTtextData(i).Count - 1
                                    TTtextData(i).Add(OpenCVB.ocvb.TTtextData(i)(j)) ' pull over any truetype text data so paint can access it.
                                Next
                                OpenCVB.ocvb.TTtextData(i).Clear()
                            End If
                        Next
                    End SyncLock
                End If
                If OptionsBringToFront And TestAllTimer.Enabled = False Then
                    OptionsBringToFront = False
                    Try
                        For Each frm In Application.OpenForms
                            If frm.name.startswith("Option") Then frm.topmost = True
                        Next
                        For Each frm In Application.OpenForms
                            If frm.name.startswith("Option") Then frm.topmost = False
                        Next
                    Catch ex As Exception ' ignoring exceptions here.  It is a transition to another class and form was activated...
                        Console.WriteLine("Error in OptionsBringToFront: " + ex.Message)
                    End Try
                End If
                If Me.IsDisposed Then Exit While
            Catch ex As Exception
                Console.WriteLine("Error in AlgorithmTask: " + ex.Message)
                Exit While
            End Try
            frameCount += 1
        End While
    End Sub
End Class
