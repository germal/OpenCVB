﻿Imports System.ComponentModel
Imports System.Environment
Imports System.Globalization
Imports System.Drawing
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
Imports System.Runtime.InteropServices
Imports System.Management
Module opencv_module
    Public bufferLock As New Mutex(True, "bufferLock") ' this is a global lock on the camera buffers.
    Public delegateLock As New Mutex(True, "delegateLock")
    Public callTraceLock As New Mutex(True, "callTraceLock")
    Public algorithmThreadLock As New Mutex(True, "AlgorithmThreadLock")
    Public cameraThreadLock As New Mutex(True, "CameraThreadLock")
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function VTKPresentTest() As Integer
    End Function
End Module
Public Class OpenCVB
#Region "Globals"
    Dim AlgorithmCount As Integer
    Dim AlgorithmTestCount As Integer
    Dim algorithmTaskHandle As Thread
    Dim saveAlgorithmName As String
    Dim border As Integer = 6
    Dim BothFirstAndLastReady As Boolean
    Dim camera As Object
    Dim cameraRS2Generic As Object ' used only to initialize D435i
    Dim cameraD435i As Object
    Dim cameraD455 As Object
    Dim cameraOakD As Object
    Dim cameraKinect As Object
    Dim cameraMyntD As Object
    Dim cameraZed2 As Object
    Dim cameraTaskHandle As Thread
    Dim camPic(3 - 1) As PictureBox
    Dim cameraRefresh As Boolean
    Dim algorithmRefresh As Boolean
    Dim CodeLineCount As Integer
    Dim DrawingRectangle As Boolean
    Dim drawRect As New cv.Rect
    Dim drawRectPic As Integer
    Dim externalPythonInvocation As Boolean
    Dim fps As Integer = 30
    Dim imgResult As New cv.Mat
    Dim frameCount As Integer
    Dim GrabRectangleData As Boolean
    Public HomeDir As DirectoryInfo

    Dim LastX As Integer
    Dim LastY As Integer
    Dim mouseClickFlag As Boolean
    Dim mouseClickPoint As New cv.Point
    Dim mousePicTag As Integer
    Dim mouseDownPoint As New cv.Point
    Dim mouseMovePoint As New cv.Point
    Dim mousePoint As New cv.Point
    Dim myBrush = New SolidBrush(Color.White)
    Dim myPen As New Pen(Color.White)
    Dim openCVKeywords As New List(Of String)
    Public optionsForm As OptionsDialog
    Dim TreeViewDialog As TreeviewForm
    Dim openFileForm As OpenFilename
    Dim picLabels() = {"RGB", "Depth", "", ""}
    Dim resizeForDisplay = 2 ' indicates how much we have to resize to fit on the screen
    Public resolutionXY As cv.Size
    Dim stopCameraThread As Boolean
    Dim textDesc As String = ""
    Dim totalBytesOfMemoryUsed As Integer
    Dim ttTextData As List(Of VB_Classes.TTtext)

    Dim openFileDialogRequested As Boolean
    Dim openFileinitialStartSetting As Boolean
    Dim openFileInitialDirectory As String
    Dim openFileFilter As String
    Dim openFileFilterIndex As Integer
    Dim openFileDialogName As String
    Dim openFileStarted As Boolean
    Dim openfileDialogTitle As String
    Dim openfileSliderPercent As Single
    Dim openFileFormLocated As Boolean
    Dim pauseAlgorithmThread As Boolean
    Private Delegate Sub delegateEvent()
    Dim logAlgorithms As StreamWriter
    Dim logActive As Boolean = False ' turn this on/off to collect data on algorithms and memory use.
    Public callTrace As New List(Of String)
    Const MAX_RECENT = 25
    Dim recentList As New List(Of String)
    Dim recentMenu(MAX_RECENT - 1) As ToolStripMenuItem
    Public intermediateReview As String
    Dim VTK_Present As Boolean
    Dim meActivateNeeded As Boolean
    Dim pixelViewerOn As Boolean
    Dim pixelViewerRect As cv.Rect
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
            HomeDir = New DirectoryInfo(CurDir() + "\..\")
        Else
            HomeDir = New DirectoryInfo(CurDir() + "\..\..\")
        End If

        setupRecentList()

        ' Camera DLL's and OpenGL apps are built in Release mode even when configured for Debug (performance is much better).  
        ' OpenGL apps cannot be debugged from OpenCVB and the camera interfaces are not likely to need debugging.
        ' To debug a camera interface: change the Build Configuration and enable "Native Code Debugging" in the OpenCVB project.
        updatePath(HomeDir.FullName + "bin\Release\", "Release Version of camera DLL's.")

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

        Dim DebugDir = HomeDir.FullName + "bin\Debug\"
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

        openFileForm = New OpenFilename

        optionsForm = New OptionsDialog
        optionsForm.OptionsDialog_Load(sender, e)

        optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.D435i) = USBenumeration("Intel(R) RealSense(TM) Depth Camera 435i Depth")
        optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.D455) = USBenumeration("Intel(R) RealSense(TM) Depth Camera 455  RGB")
        optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam) = USBenumeration("Azure Kinect 4K Camera")
        optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.OakDCamera) = USBenumeration("Movidius MyriadX")

        ' Some devices may be present but their opencvb camera interface needs to be present as well.
        optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.MyntD1000) = USBenumeration("MYNT-EYE-D1000")
        If optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.MyntD1000) > 0 And myntSDKready = False Then
            optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.MyntD1000) = 0 ' hardware is there but dll is not installed yet.
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
        optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2) = USBenumeration("ZED 2")
        If optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2) > 0 And zed2SDKready = False Then
            optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2) = 0 ' hardware is present but dll is not installed yet.
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
            If optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam) Then
                optionsForm.cameraIndex = VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam
            End If
            If optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2) Then optionsForm.cameraIndex = VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2
            If optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.MyntD1000) Then optionsForm.cameraIndex = VB_Classes.ActiveTask.algParms.camNames.MyntD1000
            If optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.D435i) Then optionsForm.cameraIndex = VB_Classes.ActiveTask.algParms.camNames.D435i
            If optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.D455) Then optionsForm.cameraIndex = VB_Classes.ActiveTask.algParms.camNames.D455
            If optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.OakDCamera) Then optionsForm.cameraIndex = VB_Classes.ActiveTask.algParms.camNames.OakDCamera
            If optionsForm.cameraDeviceCount(optionsForm.cameraIndex) = 0 Then
                MsgBox("There are no supported cameras present!" + vbCrLf + vbCrLf +
                       "Connect any of these cameras: " + vbCrLf + vbCrLf + "Intel RealSense2 D455" + vbCrLf + "Intel RealSense2 D435i" + vbCrLf +
                       "OpenCV Oak-D camera" + vbCrLf + "Microsoft Kinect 4 Azure" + vbCrLf + "MyntEyeD 1000" + vbCrLf + "StereoLabs Zed2")
            End If
        End If


        ' OpenCV needs to be in the path and the librealsense and kinect open source code needs to be in the path.
        updatePath(HomeDir.FullName + "librealsense\build\Release\", "Realsense camera support.")
        updatePath(HomeDir.FullName + "Azure-Kinect-Sensor-SDK\build\bin\Release\", "Kinect camera support.")
        updatePath(HomeDir.FullName + "OpenCV\Build\bin\Debug\", "OpenCV and OpenCV Contrib are needed for C++ classes.")
        updatePath(HomeDir.FullName + "OpenCV\Build\bin\Release\", "OpenCV and OpenCV Contrib are needed for C++ classes.")

        Dim vizDir = New DirectoryInfo(HomeDir.FullName + "OpenCV\Build\bin\Debug\")
        Dim vizFiles = vizDir.GetFiles("opencv_viz*")
        If vizFiles.Count > 0 Then VTK_Present = True
        If VTK_Present Then updatePath("c:/Program Files/VTK/bin/", "VTK directory needed for VTK examples")
        ' Check that the VTK apps are built with "WITH_VTK" as well.
        If VTKPresentTest() = 0 Then VTK_Present = False ' "WITH_VTK" has not been set in VTK.h

        ' the Kinect depthEngine DLL is not included in the SDK.  It is distributed separately because it is NOT open source.
        ' The depthEngine DLL is supposed to be installed in C:\Program Files\Azure Kinect SDK v1.1.0\sdk\windows-desktop\amd64\$(Configuration)
        ' Post an issue if this Is Not a valid assumption
        Dim kinectDLL As New FileInfo("C:\Program Files\Azure Kinect SDK v1.3.0\sdk\windows-desktop\amd64\release\bin\depthengine_2_0.dll")
        If kinectDLL.Exists = False Then ' try a later version.
            kinectDLL = New FileInfo("C:\Program Files\Azure Kinect SDK v1.4.0\sdk\windows-desktop\amd64\release\bin\depthengine_2_0.dll")
        End If
        If kinectDLL.Exists = False Then
            MsgBox("The Microsoft installer for the Kinect camera proprietary portion" + vbCrLf +
                   "was not installed in:" + vbCrLf + vbCrLf + kinectDLL.FullName + vbCrLf + vbCrLf +
                   "Did a new Version get installed?" + vbCrLf +
                   "Support for the Kinect camera may not work up you update the code near this message.")
            optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam) = 0 ' we can't use this device
        Else
            updatePath(kinectDLL.Directory.FullName, "Kinect depth engine dll.")
        End If

        For i = 0 To VB_Classes.ActiveTask.algParms.camNames.D455
            If optionsForm.cameraDeviceCount(i) > 0 Then optionsForm.cameraTotalCount += 1
        Next

        cameraRS2Generic = New CameraRS2
        Dim RS2count = cameraRS2Generic.queryDeviceCount()
        For i = 0 To RS2count - 1
            Dim deviceName = cameraRS2Generic.queryDevice(i)
            Select Case deviceName
                Case "Intel RealSense D455"
                    cameraD455 = New CameraRS2
                    cameraD455.deviceIndex = i
                    cameraD455.serialNumber = cameraRS2Generic.querySerialNumber(i)
                    cameraD455.cameraName = deviceName
                Case "Intel RealSense D435I"
                    cameraD435i = New CameraRS2
                    cameraD435i.deviceIndex = i
                    cameraD435i.serialNumber = cameraRS2Generic.querySerialNumber(i)
                    cameraD435i.cameraName = deviceName
            End Select
        Next
        cameraKinect = New CameraKinect
        cameraZed2 = New CameraZED2
        cameraMyntD = New CameraMyntD
        cameraOakD = New CameraOakD

        updateCamera()

        optionsForm.cameraRadioButton(optionsForm.cameraIndex).Checked = True ' make sure any switch is reflected in the UI.
        optionsForm.enableCameras()

        setupCamPics()
        loadAlgorithmComboBoxes()

        TestAllTimer.Interval = optionsForm.TestAllDuration.Text * 1000
        FindPython()
        If GetSetting("OpenCVB", "TreeButton", "TreeButton", False) Then TreeButton_Click(sender, e)
        If GetSetting("OpenCVB", "PixelViewerActive", "PixelViewerActive", False) Then PixelViewerButton_Click(sender, e)
    End Sub
    Private Sub campic_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        Dim pic = DirectCast(sender, PictureBox)
        Dim ratio = camPic(2).Width / imgResult.Width
        g.ScaleTransform(1, 1)
        g.DrawImage(pic.Image, 0, 0)

        If pixelViewerOn Then
            Dim picTagOk As Boolean
            If pic.Tag = 2 Then
                If mousePicTag = 3 Then
                    pixelViewerRect.X += camPic(0).Width / ratio
                    picTagOk = True
                End If
            End If
            If mousePicTag = pic.Tag Or picTagOk Then
                g.DrawRectangle(myPen, CInt(pixelViewerRect.X * ratio), CInt(pixelViewerRect.Y * ratio),
                                       CInt(pixelViewerRect.Width * ratio), CInt(pixelViewerRect.Height * ratio))
            End If
        End If
        If drawRect.Width > 0 And drawRect.Height > 0 Then
            g.DrawRectangle(myPen, drawRect.X, drawRect.Y, drawRect.Width, drawRect.Height)
            If pic.Tag = 2 Then
                g.DrawRectangle(myPen, drawRect.X + camPic(0).Width, drawRect.Y, drawRect.Width, drawRect.Height)
            End If
        End If
            If algorithmRefresh And (pic.Tag = 2) Then
            algorithmRefresh = False
            SyncLock imgResult
                Try
                    If imgResult.Width <> camPic(2).Width Or imgResult.Height <> camPic(2).Height Then
                        Dim result = imgResult.Resize(New cv.Size(camPic(2).Size.Width, camPic(2).Size.Height))
                        cvext.BitmapConverter.ToBitmap(result, camPic(2).Image)
                    Else
                        cvext.BitmapConverter.ToBitmap(imgResult, camPic(2).Image)
                    End If
                Catch ex As Exception
                    Console.WriteLine("OpenCVB: Error in OpenCVB/Paint updating dst output: " + ex.Message)
                End Try
            End SyncLock
        End If
        If cameraRefresh And (pic.Tag = 0 Or pic.Tag = 1) Then
            cameraRefresh = False
            If camera.color IsNot Nothing Then
                If camera.color.width > 0 Then
                    Dim RGBDepth = camera.RGBDepth.Resize(New cv.Size(camPic(1).Size.Width, camPic(1).Size.Height))
                    Dim color = camera.color.Resize(New cv.Size(camPic(0).Size.Width, camPic(0).Size.Height))
                    Try
                        cvext.BitmapConverter.ToBitmap(color, camPic(0).Image)
                        cvext.BitmapConverter.ToBitmap(RGBDepth, camPic(1).Image)
                    Catch ex As Exception
                        Console.WriteLine("OpenCVB: Error in campic_Paint: " + ex.Message)
                    End Try
                End If
            End If
        End If
        ' draw any TrueType font data on the image 
        Dim maxline = 21
        SyncLock ttTextData
            Try
                If pic.Tag = 2 Or pic.Tag = 3 Then
                    Dim ttText = New List(Of VB_Classes.TTtext)(ttTextData)
                    For i = 0 To ttText.Count - 1
                        Dim tt = ttText(i)
                        If tt IsNot Nothing Then
                            If ttText(i).picTag = 3 Then
                                g.DrawString(tt.text, optionsForm.fontInfo.Font, New SolidBrush(Color.White),
                                             tt.x * ratio + camPic(0).Width, tt.y * ratio)
                            Else
                                g.DrawString(tt.text, optionsForm.fontInfo.Font, New SolidBrush(Color.White),
                                             tt.x * ratio, tt.y * ratio)
                            End If
                            maxline -= 1
                            If maxline <= 0 Then Exit For
                        End If
                    Next
                End If
            Catch ex As Exception
                Console.WriteLine("Error in ttextData update: " + ex.Message)
            End Try

            If optionsForm.ShowLabels.Checked Then
                Dim textRect As New Rectangle(0, 0, camPic(0).Width / 2, If(resizeForDisplay = 4, 12, 20))
                If Len(picLabels(pic.Tag)) Then g.FillRectangle(myBrush, textRect)
                g.DrawString(picLabels(pic.Tag), optionsForm.fontInfo.Font, New SolidBrush(Color.Black), 0, 0)
                If Len(picLabels(3)) Then
                    textRect = New Rectangle(camPic(0).Width, 0, camPic(0).Width / 2, If(resizeForDisplay = 4, 12, 20))
                    g.FillRectangle(myBrush, textRect)
                    g.DrawString(picLabels(3), optionsForm.fontInfo.Font, New SolidBrush(Color.Black), camPic(0).Width, 0)
                End If
            End If
        End SyncLock

        ' only the main task can have an openfiledialog box!  Move results to the algorithm task from specified locations in this form.
        If openFileInitialDirectory <> "" Then
            If openFileDialogRequested Then
                openFileDialogRequested = False
                openFileForm.OpenFileDialog1.InitialDirectory = openFileInitialDirectory
                openFileForm.OpenFileDialog1.FileName = "*.*"
                openFileForm.OpenFileDialog1.CheckFileExists = False
                openFileForm.OpenFileDialog1.Filter = openFileFilter
                openFileForm.OpenFileDialog1.FilterIndex = openFileFilterIndex
                openFileForm.filename.Text = openFileDialogName
                openFileForm.Text = openfileDialogTitle
                openFileForm.Label1.Text = "Select a file for use with the " + AvailableAlgorithms.Text + " algorithm."
                openFileForm.Show()
                openFileStarted = openFileinitialStartSetting
                If openFileinitialStartSetting And openFileForm.PlayButton.Text = "Start" Then
                    openFileForm.PlayButton.PerformClick()
                Else
                    If openFileinitialStartSetting = False Then
                        openFileForm.fileStarted = False
                        openFileForm.PlayButton.Text = "Start"
                    End If
                End If
            Else
                If (openFileForm.Location.X <> Me.Left Or openFileForm.Location.Y <> Me.Top + Me.Height) And openFileFormLocated = False Then
                    openFileFormLocated = True
                    openFileForm.Location = New Point(Me.Left, Me.Top + Me.Height)
                End If
                If openFileDialogName <> openFileForm.filename.Text Then openFileDialogName = openFileForm.filename.Text
                If openfileSliderPercent >= 0 And openfileSliderPercent <= 1 Then openFileForm.TrackBar1.Value = openfileSliderPercent * 10000
                openFileForm.PlayButton.Visible = openfileSliderPercent >= 0 ' negative indicates it should not be shown.
                openFileForm.TrackBar1.Visible = openFileForm.PlayButton.Visible
            End If
            openFileStarted = openFileForm.fileStarted
        End If
        AlgorithmDesc.Text = textDesc
    End Sub
    Private Sub setupRecentList()
        For i = 0 To MAX_RECENT - 1
            Dim nextA = GetSetting("OpenCVB", "RecentList" + CStr(i), "RecentList" + CStr(i), "recent algorithm " + CStr(i))
            If nextA = "" Then Exit For
            If recentList.Contains(nextA) = False Then
                recentList.Add(nextA)
                recentMenu(i) = New ToolStripMenuItem() With {.Text = nextA, .Name = nextA}
                AddHandler recentMenu(i).Click, AddressOf recentList_Clicked
                MainMenu.DropDownItems.Add(recentMenu(i))
            End If
        Next
    End Sub
    Private Sub updateRecentList()
        If TestAllTimer.Enabled Then Exit Sub
        Dim copyList As List(Of String)
        If recentList.Contains(AvailableAlgorithms.Text) Then
            ' make it the most recent
            copyList = New List(Of String)(recentList)
            recentList.Clear()
            recentList.Add(AvailableAlgorithms.Text)
            For i = 0 To copyList.Count - 1
                If recentList.Contains(copyList(i)) = False Then recentList.Add(copyList(i))
            Next
        Else
            recentList.RemoveAt(recentList.Count - 1)
            copyList = New List(Of String)(recentList)
            recentList.Clear()
            recentList.Add(AvailableAlgorithms.Text)
            For i = 0 To copyList.Count - 1
                If recentList.Contains(copyList(i)) = False Then recentList.Add(copyList(i))
            Next
        End If
        For i = 0 To recentList.Count - 1
            If recentList(i) <> "" Then
                recentMenu(i).Text = recentList(i)
                recentMenu(i).Name = recentList(i)
                SaveSetting("OpenCVB", "RecentList" + CStr(i), "RecentList" + CStr(i), recentList(i))
            End If
        Next
    End Sub
    Private Sub recentList_Clicked(sender As Object, e As EventArgs)
        Dim item = TryCast(sender, ToolStripMenuItem)
        If AvailableAlgorithms.Items.Contains(item.Text) = False Then
            AvailableAlgorithms.SelectedIndex = 0
        Else
            AvailableAlgorithms.SelectedItem = item.Name
        End If
    End Sub
    Private Sub RestartCamera()
        cameraTaskHandle = Nothing
        updateCamera()
    End Sub
    Public Sub updateCamera()
        If camera IsNot Nothing Then camera.stopCamera()

        ' order is same as in optionsdialog enum
        Try
            camera = Choose(optionsForm.cameraIndex + 1, cameraKinect, cameraZed2, cameraMyntD, cameraD435i, cameraD455, cameraOakD)
        Catch ex As Exception
            camera = cameraKinect
        End Try
        If camera Is Nothing Then
            camera = cameraKinect
            optionsForm.cameraIndex = 0
        End If
        camera.initialize(resolutionXY.Width, resolutionXY.Height, fps)

        camera.pipelineclosed = False
        SaveSetting("OpenCVB", "CameraIndex", "CameraIndex", optionsForm.cameraIndex)
    End Sub
    Private Sub TreeButton_Click(sender As Object, e As EventArgs) Handles TreeButton.Click
        TreeButton.Checked = Not TreeButton.Checked
        If TreeButton.Checked Then
            TreeViewDialog = New TreeviewForm
            TreeViewDialog.updateTree()
            TreeViewDialog.TreeviewForm_Resize(sender, e)
            TreeViewDialog.Show()
            TreeViewDialog.BringToFront()
        Else
            TreeViewDialog.Close()
        End If
    End Sub
    Private Sub PixelViewerButton_Click(sender As Object, e As EventArgs) Handles PixelViewerButton.Click
        PixelViewerButton.Checked = Not PixelViewerButton.Checked
        pixelViewerOn = PixelViewerButton.Checked
    End Sub
    Public Function USBenumeration(searchName As String) As Integer
        Static firstCall = 0
        Dim deviceCount As Integer
        ' See if the desired device shows up in the device manager.'
        Dim info As ManagementObject
        Dim search As ManagementObjectSearcher
        Dim Name As String
        search = New ManagementObjectSearcher("SELECT * From Win32_PnPEntity")
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

        Dim defaultWidth = resolutionXY.Width * 2 + border * 7
        Dim defaultHeight = resolutionXY.Height * 2 + ToolStrip1.Height + border * 12
        Me.Width = GetSetting("OpenCVB", "OpenCVBWidth", "OpenCVBWidth", defaultWidth)
        Me.Height = GetSetting("OpenCVB", "OpenCVBHeight", "OpenCVBHeight", defaultHeight)
        If Me.Height < 50 Then
            Me.Width = defaultWidth
            Me.Height = defaultHeight
        End If

        ttTextData = New List(Of VB_Classes.TTtext)

        For i = 0 To camPic.Length - 1
            If camPic(i) Is Nothing Then camPic(i) = New PictureBox()
            camPic(i).Size = New Size(If(i < 2, resolutionXY.Width, resolutionXY.Width * 2), resolutionXY.Height)
            AddHandler camPic(i).DoubleClick, AddressOf campic_DoubleClick
            AddHandler camPic(i).Click, AddressOf campic_Click
            AddHandler camPic(i).Paint, AddressOf campic_Paint
            AddHandler camPic(i).MouseDown, AddressOf camPic_MouseDown
            AddHandler camPic(i).MouseUp, AddressOf camPic_MouseUp
            AddHandler camPic(i).MouseMove, AddressOf camPic_MouseMove
            camPic(i).Tag = i
            Me.Controls.Add(camPic(i))
        Next
        LineUpCamPics(resizing:=False)
    End Sub
    Private Sub OpenCVB_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        If camPic Is Nothing Then Exit Sub ' when first opening, campic may not be built yet
        If camPic(2) Is Nothing Then Exit Sub ' individual pictureboxes need to be ready as well.
        LineUpCamPics(resizing:=True)
    End Sub
    Private Sub LineUpCamPics(resizing As Boolean)
        If resizing = False Then
            If optionsForm.SnapToGrid.Checked Then
                Select Case resolutionXY.Height
                    Case 240
                        Me.Width = 683
                        Me.Height = 592
                    Case 480
                        Me.Width = 1321
                        Me.Height = 1071
                    Case 720
                        Me.Width = 1321
                        Me.Height = 835
                End Select
            End If
        End If

        Dim width = CInt((Me.Width - 42) / 2)
        Dim height = CInt(width * resolutionXY.Height / resolutionXY.Width)
        If Math.Abs(width - resolutionXY.Width / 2) < 2 Then width = resolutionXY.Width / 2
        If Math.Abs(height - resolutionXY.Height / 2) < 2 Then height = resolutionXY.Height / 2
        Dim padX = 12
        Dim padY = 60
        camPic(0).Size = New Size(width, height)
        camPic(1).Size = New Size(width, height)
        camPic(2).Size = New Size(width * 2, height)

        camPic(0).Image = New Bitmap(width, height, Imaging.PixelFormat.Format24bppRgb)
        camPic(1).Image = New Bitmap(width, height, Imaging.PixelFormat.Format24bppRgb)
        camPic(2).Image = New Bitmap(width * 2, height, Imaging.PixelFormat.Format24bppRgb)
        camPic(0).Location = New Point(padX, padY)
        camPic(1).Location = New Point(camPic(0).Left + camPic(0).Width, padY)
        camPic(2).Location = New Point(padX, camPic(0).Top + camPic(0).Height)
        saveLayout()
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
        Dim AlgorithmListFileInfo = New FileInfo(HomeDir.FullName + "Data/AlgorithmList.txt")
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

        Dim AlgorithmMapFileInfo = New FileInfo(HomeDir.FullName + "Data/AlgorithmMapToOpenCV.txt")
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
        If OpenCVkeyword.Text = "<All>" Or OpenCVkeyword.Text = "<All using recorded data>" Then
            Dim AlgorithmListFileInfo = New FileInfo(HomeDir.FullName + "Data/AlgorithmList.txt")
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
    Private Sub AvailableAlgorithms_SelectedIndexChanged(sender As Object, e As EventArgs) Handles AvailableAlgorithms.SelectedIndexChanged
        If AvailableAlgorithms.Enabled Then
            If PausePlayButton.Text = "Run" Then PausePlayButton_Click(sender, e) ' if paused, then restart.
            SaveSetting("OpenCVB", OpenCVkeyword.Text, OpenCVkeyword.Text, AvailableAlgorithms.Text)
            StartAlgorithmTask()
            updateRecentList()
        End If
    End Sub
    Private Sub updatePath(neededDirectory As String, notFoundMessage As String)
        Dim systemPath = Environment.GetEnvironmentVariable("Path")
        Dim foundDirectory As Boolean
        If Directory.Exists(neededDirectory) Then
            foundDirectory = True
            systemPath = neededDirectory + ";" + systemPath
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
    Public Function validateRect(r As cv.Rect, width As Integer, height As Integer) As cv.Rect
        Dim ratio = imgResult.Width / camPic(2).Width
        r = New cv.Rect(r.X * ratio, r.Y * ratio, r.Width * ratio, r.Height * ratio)
        If r.Width < 0 Then r.Width = 1
        If r.Height < 0 Then r.Height = 1
        If r.X < 0 Then r.X = 0
        If r.Y < 0 Then r.Y = 0
        If r.X > width Then r.X = width
        If r.Y > height Then r.Y = height
        If r.X + r.Width > width Then r.Width = width - r.X
        If r.Y + r.Height > height Then r.Height = height - r.Y

        Return r
    End Function
    Private Sub campic_Click(sender As Object, e As EventArgs)
        mouseClickFlag = True
    End Sub
    Private Sub camPic_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
        Try
            If DrawingRectangle Then
                DrawingRectangle = False
                GrabRectangleData = True
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
    Private Sub AvailableAlgorithms_MouseClick(sender As Object, e As MouseEventArgs) Handles AvailableAlgorithms.MouseClick
        ' If they Then had been Using the treeview feature To click On a tree entry, the timer was disable.  
        ' Clicking on availablealgorithms indicates they are done with using the treeview.
        If TreeViewDialog IsNot Nothing Then TreeViewDialog.Timer1.Enabled = True
    End Sub
    Private Sub camPic_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
        Try
            Dim pic = DirectCast(sender, PictureBox)
            If DrawingRectangle Then
                mouseMovePoint.X = e.X
                mouseMovePoint.Y = e.Y
                If mouseMovePoint.X < 0 Then mouseMovePoint.X = 0
                If mouseMovePoint.Y < 0 Then mouseMovePoint.Y = 0
                drawRectPic = pic.Tag
                If e.X < camPic(0).Width Then
                    drawRect.X = Math.Min(mouseDownPoint.X, mouseMovePoint.X)
                Else
                    drawRect.X = Math.Min(mouseDownPoint.X - camPic(0).Width, mouseMovePoint.X - camPic(0).Width)
                    drawRectPic = 3 ' When wider than campic(0), it can only be dst2 which has no pic.tag (because campic(2) is double-wide for timing reasons.
                End If
                drawRect.Y = Math.Min(mouseDownPoint.Y, mouseMovePoint.Y)
                drawRect.Width = Math.Abs(mouseDownPoint.X - mouseMovePoint.X)
                drawRect.Height = Math.Abs(mouseDownPoint.Y - mouseMovePoint.Y)
                If drawRect.X + drawRect.Width > resolutionXY.Width Then drawRect.Width = resolutionXY.Width - drawRect.X
                If drawRect.Y + drawRect.Height > resolutionXY.Height Then drawRect.Height = resolutionXY.Height - drawRect.Y
                BothFirstAndLastReady = True
            End If
            mousePicTag = pic.Tag
            mousePoint.X = e.X
            mousePoint.Y = e.Y
            If mousePicTag = 2 And mousePoint.X > camPic(0).Width Then
                mousePoint.X -= camPic(0).Width
                mousePicTag = 3 ' pretend this is coming from the fictional campic(3) which was dst2
            End If
            mousePoint *= resolutionXY.Width / camPic(0).Width

        Catch ex As Exception
            Console.WriteLine("Error in camPic_MouseMove: " + ex.Message)
        End Try
    End Sub
    Private Sub campic_DoubleClick(sender As Object, e As EventArgs)
        DrawingRectangle = False
    End Sub
    Private Sub PausePlayButton_Click(sender As Object, e As EventArgs) Handles PausePlayButton.Click
        Static saveTestAllState As Boolean
        Static algorithmRunning = True
        If PausePlayButton.Text = "Run" Then
            PausePlayButton.Text = "Pause"
            pauseAlgorithmThread = False
            If saveTestAllState Then testAllButton_Click(sender, e)
            PausePlayButton.Image = Image.FromFile(HomeDir.FullName + "OpenCVB/Data/PauseButton.png")
        Else
            PausePlayButton.Text = "Run"
            pauseAlgorithmThread = True
            saveTestAllState = TestAllTimer.Enabled
            If TestAllTimer.Enabled Then testAllButton_Click(sender, e)
            PausePlayButton.Image = Image.FromFile(HomeDir.FullName + "OpenCVB/Data/PauseButtonRun.png")
        End If
    End Sub
    Private Sub testAllButton_Click(sender As Object, e As EventArgs) Handles TestAllButton.Click
        If TestAllButton.Text = "Test All" Then
            AlgorithmTestCount = 0
            TestAllButton.Text = "Stop Test"
            TestAllButton.Image = Image.FromFile(HomeDir.FullName + "OpenCVB/Data/StopTest.png")
            If logActive Then logAlgorithms = New StreamWriter("C:\Temp\logAlgorithms.csv")
            TestAllTimer_Tick(sender, e)
            TestAllTimer.Enabled = True
            If TreeViewDialog IsNot Nothing Then TreeViewDialog.Timer1.Enabled = True
        Else
            TestAllTimer.Enabled = False
            TestAllButton.Text = "Test All"
            If logActive Then logAlgorithms.Close()
            TestAllButton.Image = Image.FromFile(HomeDir.FullName + "OpenCVB/Data/testall.png")
        End If
    End Sub
    Private Sub OpenCVB_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd
        saveLayout()
    End Sub
    Public Sub raiseEventCamera()
        SyncLock delegateLock
            For i = 0 To camPic.Length - 1
                camPic(i).Refresh()
            Next
        End SyncLock
    End Sub
    Private Sub fpsTimer_Tick(sender As Object, e As EventArgs) Handles fpsTimer.Tick
        Static lastAlgorithmFrame As Integer
        Static lastCameraFrame As Integer
        If lastAlgorithmFrame > frameCount Then lastAlgorithmFrame = 0
        If lastCameraFrame > camera.frameCount Then lastCameraFrame = 0
        If AvailableAlgorithms.Text.Contains(".py") Then meActivateNeeded = False
        If AvailableAlgorithms.Text.StartsWith("OpenGL") Then meActivateNeeded = False
        If AvailableAlgorithms.Text.StartsWith("VTK") Then meActivateNeeded = False
        If meActivateNeeded Then
            Me.Activate()
            meActivateNeeded = False
        End If
        If TreeViewDialog IsNot Nothing Then
            If TreeViewDialog.TreeView1.IsDisposed Then TreeButton.CheckState = CheckState.Unchecked
        End If

        Dim countFrames = frameCount - lastAlgorithmFrame
        lastAlgorithmFrame = frameCount
        Dim algorithmFPS As Single = countFrames / (fpsTimer.Interval / 1000)

        Dim camFrames = camera.frameCount - lastCameraFrame
        lastCameraFrame = camera.frameCount
        Dim cameraFPS As Single = camFrames / (fpsTimer.Interval / 1000)

        Me.Text = "OpenCVB (" + Format(CodeLineCount, "###,##0") + " lines / " + CStr(AlgorithmCount) + " algorithms = " + CStr(CInt(CodeLineCount / AlgorithmCount)) +
                  " lines per) - " + optionsForm.cameraRadioButton(optionsForm.cameraIndex).Text + " - " + Format(cameraFPS, "#0.0") +
                  "/" + Format(algorithmFPS, "#0.0") + " " + CStr(totalBytesOfMemoryUsed) + " Mb (working set)"
    End Sub
    Private Sub saveLayout()
        optionsForm.saveResolution()
        SaveSetting("OpenCVB", "OpenCVBLeft", "OpenCVBLeft", Me.Left)
        SaveSetting("OpenCVB", "OpenCVBTop", "OpenCVBTop", Me.Top)
        SaveSetting("OpenCVB", "OpenCVBWidth", "OpenCVBWidth", Me.Width)
        SaveSetting("OpenCVB", "OpenCVBHeight", "OpenCVBHeight", Me.Height)

        Dim resolutionDesc As String = ""
        Select Case optionsForm.resolutionName
            Case "Low"
                resolutionDesc = "320x240"
            Case "Medium"
                resolutionDesc = "640x480"
            Case "High"
                resolutionDesc = "1280x720"
        End Select
        Dim details = " Display at " + CStr(camPic(0).Width) + "x" + CStr(camPic(0).Height) + ", Working Res. = " + resolutionDesc
        picLabels(0) = "RGB:" + details
        picLabels(1) = "Depth:" + details
    End Sub
    Private Sub Exit_Click(sender As Object, e As EventArgs) Handles ExitCall.Click
        SaveSetting("OpenCVB", "TreeButton", "TreeButton", TreeButton.Checked)
        SaveSetting("OpenCVB", "PixelViewerActive", "PixelViewerActive", PixelViewerButton.Checked)
        stopCameraThread = True
        saveAlgorithmName = ""
        If TestAllTimer.Enabled Then testAllButton_Click(sender, e) ' close the log file if needed.
        textDesc = ""
        saveLayout()
        Application.DoEvents()
    End Sub
    Private Sub AboutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem.Click
        MsgBox("The objective is to solve many small computer vision problems" + vbCrLf +
               "and do so in a way that enables any of the solutions " + vbCrLf +
               "to be reused. The result is a toolkit for solving " + vbCrLf +
               "ever bigger and more difficult problems. The " + vbCrLf +
               "philosophy behind this approach is that human vision " + vbCrLf +
               "is not computationally intensive but is built " + vbCrLf +
               "on many almost trivial algorithms working together.  " + vbCrLf + vbCrLf +
               "Fall 2020 Fremont CA")
    End Sub
    Private Sub MainFrm_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        Exit_Click(sender, e)
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
                            resultMat = imgResult(New cv.Rect(0, 0, imgResult.Width / 2, imgResult.Height)).Clone()
                        Case 4 ' result2
                            resultMat = imgResult(New cv.Rect(imgResult.Width / 2, 0, imgResult.Width / 2, imgResult.Height)).Clone()
                    End Select
                    Exit For
                End SyncLock
            End If
        Next
        img = cv.Extensions.BitmapConverter.ToBitmap(resultMat)
        Clipboard.SetImage(img)
    End Sub
    Private Sub CameraTask()
        stopCameraThread = True ' stop the current camera task
        SyncLock cameraThreadLock
            stopCameraThread = False
            While stopCameraThread = False
                SyncLock bufferLock
                    camera.GetNextFrame()
                End SyncLock
                cameraRefresh = True

                Static delegateX As New delegateEvent(AddressOf raiseEventCamera)
                Me.Invoke(delegateX)

                Dim currentProcess = System.Diagnostics.Process.GetCurrentProcess()
                totalBytesOfMemoryUsed = currentProcess.WorkingSet64 / (1024 * 1024)
                GC.Collect() ' minimize memory footprint - the frames have just been sent so this task isn't busy.
            End While
            camera.frameCount = 0
        End SyncLock
    End Sub

    Private Sub TestAllTimer_Tick(sender As Object, e As EventArgs) Handles TestAllTimer.Tick
        ' run at all the different resolutions...
        Dim specialSingleCount = AvailableAlgorithms.Items.Count = 1
        If specialSingleCount Then saveAlgorithmName = "" ' stop the current algorith which we will restart below (only 1 algorithm in the list.)
        Dim only1Resolution As Boolean
        If AlgorithmTestCount Mod AvailableAlgorithms.Items.Count = 0 And AlgorithmTestCount > 0 Or specialSingleCount Then
            If OptionsDialog.cameraIndex = VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam Or
               OptionsDialog.cameraIndex = VB_Classes.ActiveTask.algParms.camNames.MyntD1000 Or
               OptionsDialog.cameraIndex = VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2 Then

                only1Resolution = True
            Else
                If optionsForm.resolution640.Checked Then
                    optionsForm.resolution1280.Checked = True
                ElseIf optionsForm.resolution1280.Checked Then
                    optionsForm.resolution640.Checked = True
                End If
            End If
            saveLayout()
        End If

        If optionsForm.resolution640.Checked Or only1Resolution Then ' only change cameras when in medium resolution or when only 1 resolution
            ' after sweeping through resolutions, sweep through the cameras as well...
            If (AlgorithmTestCount Mod AvailableAlgorithms.Items.Count = 0 And AlgorithmTestCount > 0) Or specialSingleCount Then
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
        saveAlgorithmName = ""

        Dim saveCurrentCamera = optionsForm.cameraIndex

        Dim OKcancel = optionsForm.ShowDialog()

        If OKcancel = DialogResult.OK Then
            optionsForm.saveResolution()
            optionsForm.TestEnableNumPy()
            If saveCurrentCamera <> optionsForm.cameraIndex Or camera.width <> resolutionXY.Width Then RestartCamera()
            TestAllTimer.Interval = optionsForm.TestAllDuration.Value * 1000

            LineUpCamPics(resizing:=False)
            saveLayout()
        End If
        StartAlgorithmTask()
    End Sub
    Private Sub StartAlgorithmTask()
        openFileForm.Hide()
        openFileForm.PlayButton.Text = "Start"
        openFileDialogName = ""
        openFileInitialDirectory = ""
        openFileForm.fileStarted = False
        openFileFormLocated = False

        Dim parms As New VB_Classes.ActiveTask.algParms
        ReDim parms.IMU_RotationMatrix(9 - 1)
        parms.IMU_RotationMatrix = camera.IMU_RotationMatrix
        parms.IMU_RotationVector = camera.IMU_RotationVector

        parms.cameraName = GetSetting("OpenCVB", "CameraIndex", "CameraIndex", VB_Classes.ActiveTask.algParms.camNames.D435i)
        parms.PythonExe = optionsForm.PythonExeName.Text

        parms.useRecordedData = OpenCVkeyword.Text = "<All using recorded data>"
        parms.testAllRunning = TestAllButton.Text = "Stop Test"
        parms.externalPythonInvocation = externalPythonInvocation
        parms.ShowConsoleLog = optionsForm.ShowConsoleLog.Checked
        parms.NumPyEnabled = optionsForm.EnableNumPy.Checked

        parms.intrinsicsLeft = camera.intrinsicsLeft_VB
        parms.intrinsicsRight = camera.intrinsicsRight_VB
        parms.extrinsics = camera.Extrinsics_VB
        parms.homeDir = HomeDir.FullName
        parms.VTK_Present = VTK_Present

        PausePlayButton.Image = Image.FromFile(HomeDir.FullName + "OpenCVB/Data/PauseButton.png")

        Dim imgSize = New cv.Size(CInt(resolutionXY.Width * 2), CInt(resolutionXY.Height))
        imgResult = New cv.Mat(imgSize, cv.MatType.CV_8UC3, 0)

        Thread.CurrentThread.Priority = ThreadPriority.Lowest

        If cameraTaskHandle Is Nothing Then
            cameraTaskHandle = New Thread(AddressOf CameraTask)
            cameraTaskHandle.Name = "CameraTask"
            cameraTaskHandle.Priority = ThreadPriority.Highest
            cameraTaskHandle.Start()
        End If

        algorithmTaskHandle = New Thread(AddressOf AlgorithmTask)
        saveAlgorithmName = AvailableAlgorithms.Text
        algorithmTaskHandle.Name = AvailableAlgorithms.Text
        algorithmTaskHandle.Start(parms)
        camera.frameCount = 0
        fpsTimer.Enabled = True
    End Sub
    Private Sub AlgorithmTask(ByVal parms As VB_Classes.ActiveTask.algParms)
        SyncLock algorithmThreadLock ' the duration of any algorithm varies a lot so wait here if previous algorithm is not finished.
            If saveAlgorithmName = "" Then Exit Sub ' shutting down the app...
            AlgorithmTestCount += 1
            drawRect = New cv.Rect
            Dim algName = algorithmTaskHandle.Name
            If algName = "" Then Exit Sub

            Dim myLocation = New cv.Rect(Me.Left, Me.Top, Me.Width, Me.Height)
            Dim task = New VB_Classes.ActiveTask(parms, resolutionXY, algName, resolutionXY.Width, resolutionXY.Height, myLocation)
            textDesc = task.desc
            openFileInitialDirectory = task.openFileInitialDirectory
            openFileDialogRequested = task.openFileDialogRequested
            openFileinitialStartSetting = task.initialStartSetting
            task.fileStarted = task.initialStartSetting
            openFileStarted = task.initialStartSetting
            openFileFilterIndex = task.openFileFilterIndex
            openFileFilter = task.openFileFilter
            openFileDialogName = task.openFileDialogName
            openfileDialogTitle = task.openFileDialogTitle
            intermediateReview = ""

            Console.WriteLine(vbCrLf + vbCrLf + vbTab + algName + " " + textDesc + vbCrLf + vbTab + CStr(AlgorithmTestCount) + vbTab + "Algorithms tested")
            Console.WriteLine(vbTab + Format(totalBytesOfMemoryUsed, "#,##0") + "Mb working set before running " + algName + vbCrLf + vbCrLf)

            If logActive And TestAllTimer.Enabled Then logAlgorithms.WriteLine(algName + "," + CStr(totalBytesOfMemoryUsed))

            ' if the constructor for the algorithm sets the drawrect, adjust it for the ratio of the actual size and algorithm sized image.
            If task.drawRect <> New cv.Rect Then
                drawRect = task.drawRect
                Dim ratio = task.color.Width / camPic(0).Width  ' relative size of algorithm size image to displayed image
                drawRect = New cv.Rect(drawRect.X / ratio, drawRect.Y / ratio, drawRect.Width / ratio, drawRect.Height / ratio)
            End If

            ttTextData.Clear()

            BothFirstAndLastReady = False
            frameCount = 0 ' restart the count...
#If USE_NUMPY Then
            If task.ocvb.parms.NumPyEnabled Then
                Using py.Py.GIL() ' for explanation see http://pythonnet.github.io/ and https://github.com/SciSharp/Numpy.NET (see multi-threading (Must read!))
                    Run(task, algName)
                End Using
            Else
                Run(task, algName)
            End If
#Else
            Run(task, algName)
#End If
            task.Dispose()
            frameCount = 0
            If parms.testAllRunning Then Console.WriteLine(vbTab + "Ending " + algName)
        End SyncLock
    End Sub
    Private Sub Run(task As VB_Classes.ActiveTask, algName As String)
        While 1
            Dim ratioImageToCampic = task.color.Width / camPic(0).Width  ' relative size of displayed image and algorithm size image.
            While 1
                If saveAlgorithmName <> algName Or saveAlgorithmName = "" Then Exit Sub ' pause will stop the current algorithm as well.
                Application.DoEvents() ' this will allow any options for the algorithm to be updated...
                SyncLock bufferLock
                    If camera.newImagesAvailable And pauseAlgorithmThread = False And camera.color.width > 0 Then
                        ' bring the data into the algorithm task.
                        task.color = camera.color.Resize(resolutionXY)
                        task.RGBDepth = camera.RGBDepth.Resize(resolutionXY)
                        task.leftView = camera.leftView.Resize(resolutionXY)
                        task.rightView = camera.rightView.Resize(resolutionXY)
                        task.pointCloud = camera.PointCloud.clone.resize(resolutionXY)
                        task.depth16 = camera.depth16.clone

                        task.transformationMatrix = camera.transformationMatrix
                        task.IMU_TimeStamp = camera.IMU_TimeStamp
                        task.IMU_Barometer = camera.IMU_Barometer
                        task.IMU_Magnetometer = camera.IMU_Magnetometer
                        task.IMU_Temperature = camera.IMU_Temperature
                        task.IMU_Rotation = camera.IMU_Rotation
                        task.IMU_Translation = camera.IMU_Translation
                        task.IMU_Acceleration = camera.IMU_Acceleration
                        task.IMU_Velocity = camera.IMU_Velocity
                        task.IMU_AngularAcceleration = camera.IMU_AngularAcceleration
                        task.IMU_AngularVelocity = camera.IMU_AngularVelocity
                        task.IMU_FrameTime = camera.IMU_FrameTime
                        task.CPU_TimeStamp = camera.CPU_TimeStamp
                        task.CPU_FrameTime = camera.CPU_FrameTime
                        task.intermediateReview = intermediateReview
                        task.ratioImageToCampic = ratioImageToCampic
                        task.pixelViewerOn = pixelViewerOn
                        camera.newImagesAvailable = False

                        If GrabRectangleData Then
                            GrabRectangleData = False
                            Dim ratio = ratioImageToCampic
                            task.drawRect = New cv.Rect(drawRect.X * ratio, drawRect.Y * ratio, drawRect.Width * ratio, drawRect.Height * ratio)
                            If task.drawRect.Width <= 2 Then task.drawRect.Width = 0 ' too small?
                            Dim w = task.color.Width
                            If task.drawRect.X > w Then task.drawRect.X -= w
                            If task.drawRect.X < w And task.drawRect.X + task.drawRect.Width > w Then
                                task.drawRect.Width = w - task.drawRect.X
                            End If
                            BothFirstAndLastReady = False
                        End If

                        task.mousePoint = mousePoint
                        task.mousePicTag = mousePicTag
                        task.mouseClickFlag = mouseClickFlag
                        If mouseClickFlag Then task.mouseClickPoint = mousePoint
                        mouseClickFlag = False

                        task.fileStarted = openFileStarted ' UI may have stopped play.
                        Exit While
                    End If
                End SyncLock
            End While

            task.RunAlgorithm()

            If task.mousePointUpdated Then mousePoint = task.mousePoint ' in case the algorithm has changed the mouse location...
            If task.drawRectUpdated Then drawRect = task.drawRect
            If task.drawRectClear Then
                drawRect = New cv.Rect
                task.drawRect = drawRect
                task.drawRectClear = False
            End If

            pixelViewerRect = task.pixelViewerRect

            If openFileDialogName <> "" Then
                If openFileDialogName <> task.openFileDialogName Or openFileStarted <> task.fileStarted Then
                    task.fileStarted = openFileStarted
                    task.openFileDialogName = openFileDialogName
                End If
                openfileSliderPercent = task.openFileSliderPercent
            End If

            Static inputFile As String = "" ' task.openFileDialogName
            If inputFile <> task.openFileDialogName Then
                inputFile = task.openFileDialogName
                openFileInitialDirectory = task.openFileInitialDirectory
                openFileDialogRequested = task.openFileDialogRequested
                openFileinitialStartSetting = True ' if the file playing changes while the algorithm is running, automatically start playing the new file.
                openFileFilterIndex = task.openFileFilterIndex
                openFileFilter = task.openFileFilter
                openFileDialogName = task.openFileDialogName
                openfileDialogTitle = task.openFileDialogTitle
            End If

            If frameCount = 0 Then meActivateNeeded = True

            picLabels(2) = task.label1
            picLabels(3) = task.label2

            ' share the results of the algorithm task.
            SyncLock ttTextData
                If task.ttTextData.Count Then
                    ttTextData = New List(Of VB_Classes.TTtext)(task.ttTextData)
                    task.ttTextData.Clear()
                End If
            End SyncLock

            SyncLock imgResult
                imgResult = task.result.Clone()
                algorithmRefresh = True
            End SyncLock

            If Me.IsDisposed Then Exit While

            If frameCount Mod 100 = 0 Then
                SyncLock callTraceLock
                    ' this allows for dynamic allocation of new algorithms.
                    callTrace.Clear()
                    For i = 0 To task.callTrace.Count - 1
                        callTrace.Add(task.callTrace(i))
                    Next
                End SyncLock
            End If
            frameCount += 1
        End While
    End Sub
End Class

