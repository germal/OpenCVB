Imports System.ComponentModel
Imports System.Environment
Imports System.Globalization
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
Module opencv_module
    Public bufferLock As New Mutex(True, "bufferLock") ' this is a global lock on the camera buffers.
    Public delegateLock As New Mutex(True, "delegateLock")
    Public callTraceLock As New Mutex(True, "callTraceLock")
    Public algorithmThreadLock As New Mutex(True, "AlgorithmThreadLock")
    Public cameraThreadLock As New Mutex(True, "CameraThreadLock")
End Module
Public Class OpenCVB
#Region "Globals"
    Const displayFrames As Integer = 3
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
    Dim cameraKinect As Object
    Dim cameraMyntD As Object
    Dim cameraZed2 As Object
    Dim cameraTaskHandle As Thread
    Dim camPic(displayFrames - 1) As PictureBox
    Dim cameraRefresh As Boolean
    Dim algorithmRefresh As Boolean
    Dim CodeLineCount As Integer
    Dim DrawingRectangle As Boolean
    Dim drawRect As New cv.Rect(0, 0, 0, 0)
    Dim externalPythonInvocation As Boolean
    Dim fps As Integer = 30
    Dim imgResult As New cv.Mat
    Dim frameCount As Integer
    Dim GrabRectangleData As Boolean
    Dim HomeDir As DirectoryInfo

    Dim LastX As Integer
    Dim LastY As Integer
    Dim mouseClickFlag As Boolean
    Dim mouseClickPoint As New cv.Point
    Dim mousePicTag As Integer
    Dim mouseDownPoint As New cv.Point
    Dim mouseMovePoint As New cv.Point
    Dim mousePoint As New cv.Point
    Dim myBrush = New SolidBrush(System.Drawing.Color.White)
    Dim myPen As New System.Drawing.Pen(System.Drawing.Color.White)
    Dim openCVKeywords As New List(Of String)
    Dim OptionsBringToFront As Boolean
    Dim treeViewBringToFront As Boolean
    Dim optionsForm As OptionsDialog
    Dim TreeViewDialog As TreeviewForm
    Dim openFileForm As OpenFilename
    Dim picLabels() = {"RGB", "Depth", "", ""}
    Dim camWidth As Integer = 1280, camHeight As Integer = 720
    Dim resizeForDisplay = 2 ' indicates how much we have to resize to fit on the screen
    Public resolutionXY = New cv.Size(1280, 720)
    Public resolutionSetting As Integer = 1
    Dim stopCameraThread As Boolean
    Dim textDesc As String = ""
    Dim totalBytesOfMemoryUsed As Integer
    Dim TTtextData As List(Of VB_Classes.TTtext)

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
    Dim startAlgorithmTime As DateTime
    Const MAX_RECENT = 10
    Dim recentList As New List(Of String)
    Dim recentMenu(MAX_RECENT - 1) As ToolStripMenuItem
    Public intermediateReview As String
    Dim defaultWidth As Integer
    Dim defaultHeight As Integer
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
        setupRecentList()

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

        openFileForm = New OpenFilename

        optionsForm = New OptionsDialog
        optionsForm.OptionsDialog_Load(sender, e)

        optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.D435i) = USBenumeration("Intel(R) RealSense(TM) Depth Camera 435i Depth")
        optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.D455) = USBenumeration("Intel(R) RealSense(TM) Depth Camera 455  RGB")
        optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam) = USBenumeration("Azure Kinect 4K Camera")

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
            If optionsForm.cameraDeviceCount(optionsForm.cameraIndex) = 0 Then
                MsgBox("There are no supported cameras present.  Connect an Intel RealSense2 series camera (D455, D435i, Kinect 4 Azure, MyntEyeD 1000, or StereoLabs Zed2.")
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

        updateCamera()

        optionsForm.cameraRadioButton(optionsForm.cameraIndex).Checked = True ' make sure any switch is reflected in the UI.
        optionsForm.enableCameras()

        setupCamPics()
        loadAlgorithmComboBoxes()

        TestAllTimer.Interval = optionsForm.TestAllDuration.Text * 1000
        FindPython()
        If GetSetting("OpenCVB", "TreeButton", "TreeButton", False) Then TreeButton_Click(sender, e)
    End Sub
    Private Sub campic_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        Dim pic = DirectCast(sender, PictureBox)
        g.ScaleTransform(1, 1)
        g.DrawImage(pic.Image, 0, 0)
        If drawRect.Width > 0 And drawRect.Height > 0 Then
            g.DrawRectangle(myPen, drawRect.X, drawRect.Y, drawRect.Width, drawRect.Height)
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
                    Console.WriteLine("Error in OpenCVB/Paint updating dst output: " + ex.Message)
                End Try
            End SyncLock
        End If
        If cameraRefresh And (pic.Tag = 0 Or pic.Tag = 1) Then
            cameraRefresh = False
            SyncLock bufferLock ' avoid updating the image while copying into it in the algorithm and camera tasks
                If camera.color IsNot Nothing Then
                    If camera.color.width > 0 Then
                        Try
                            Dim RGBDepth = camera.RGBDepth.Resize(New cv.Size(camPic(1).Size.Width, camPic(1).Size.Height))
                            Dim color = camera.color.Resize(New cv.Size(camPic(0).Size.Width, camPic(0).Size.Height))
                            cvext.BitmapConverter.ToBitmap(Color, camPic(0).Image)
                            cvext.BitmapConverter.ToBitmap(RGBDepth, camPic(1).Image)
                        Catch ex As Exception
                            Console.WriteLine("OpenCVB: Error in campic_Paint: " + ex.Message)
                        End Try
                    End If
                End If
            End SyncLock
        End If
        ' draw any TrueType font data on the image 
        Dim maxline = 21
        SyncLock TTtextData
            Try
                Dim ratio = camPic(2).Width / imgResult.Width
                If pic.Tag = 2 Then
                    For i = 0 To TTtextData.Count - 1
                        Dim tt = TTtextData(i)
                        If tt IsNot Nothing Then
                            If TTtextData(i).picTag = 3 Then
                                g.DrawString(tt.text, optionsForm.fontInfo.Font, New SolidBrush(System.Drawing.Color.White),
                                             tt.x * ratio + camPic(0).Width, tt.y * ratio)
                            Else
                                g.DrawString(tt.text, optionsForm.fontInfo.Font, New SolidBrush(System.Drawing.Color.White),
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
                g.DrawString(picLabels(pic.Tag), optionsForm.fontInfo.Font, New SolidBrush(System.Drawing.Color.Black), 0, 0)
                If Len(picLabels(3)) Then
                    textRect = New Rectangle(camPic(0).Width, 0, camPic(0).Width / 2, If(resizeForDisplay = 4, 12, 20))
                    g.FillRectangle(myBrush, textRect)
                    g.DrawString(picLabels(3), optionsForm.fontInfo.Font, New SolidBrush(System.Drawing.Color.Black), camPic(0).Width, 0)
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
            ' the list of active algorithms for this group does not contain the algorithm requested so just add it!
            AvailableAlgorithms.Items.Add(item.Text)
        End If
        AvailableAlgorithms.SelectedItem = item.Name
    End Sub
    Private Sub RestartCamera()
        camera.closePipe()
        cameraTaskHandle = Nothing
        updateCamera()
    End Sub
    Public Sub updateCamera()
        ' order is same as in optionsdialog enum
        Try
            camera = Choose(optionsForm.cameraIndex + 1, cameraKinect, cameraZed2, cameraMyntD, cameraD435i, cameraD455)
        Catch ex As Exception
            camera = cameraKinect
        End Try
        If camera Is Nothing Then
            camera = cameraKinect
            optionsForm.cameraIndex = 0
        End If
        If camera.devicename = "" Then
            camera.width = camWidth
            camera.height = camHeight
            camera.initialize(fps)
        End If
        camera.pipelineclosed = False
        SaveSetting("OpenCVB", "CameraIndex", "CameraIndex", optionsForm.cameraIndex)
    End Sub
    Private Sub TreeButton_Click(sender As Object, e As EventArgs) Handles TreeButton.Click
        If TreeButton.CheckState = CheckState.Unchecked Then
            TreeButton.CheckState = CheckState.Checked
            TreeViewDialog = New TreeviewForm
            TreeViewDialog.updateTree()
            TreeViewDialog.Show()
            TreeViewDialog.TreeviewForm_Resize(sender, e)
        Else
            TreeViewDialog.Show()
            TreeViewDialog.BringToFront()
        End If
    End Sub
    Private Sub OpenCVB_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        If camPic Is Nothing Then Exit Sub ' when first opening, campic may not be built yet
        If camPic(2) Is Nothing Then Exit Sub ' individual pictureboxes need to be ready as well.
        LineUpCamPics()
    End Sub
    Private Sub LineUpCamPics()
        Dim width = CInt(resolutionXY.width)
        Dim height = CInt(resolutionXY.height)
        If Math.Abs(width - camWidth / 2) < 2 Then width = camWidth / 2
        If Math.Abs(height - camHeight / 2) < 2 Then height = camHeight / 2
        Dim padX = 12
        Dim padY = 60
        camPic(0).Size = New Size(width, height)
        camPic(1).Size = New Size(width, height)
        camPic(2).Size = New Size(resolutionXY.width * 2, height)

        camPic(0).Image = New Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb)
        camPic(1).Image = New Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb)
        camPic(2).Image = New Bitmap(width * 2, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb)
        camPic(0).Location = New Point(padX, padY)
        camPic(1).Location = New Point(camPic(0).Left + camPic(0).Width, padY)
        camPic(2).Location = New Point(padX, camPic(0).Top + camPic(0).Height)
        saveLayout()
    End Sub
    Public Function USBenumeration(searchName As String) As Integer
        Static firstCall = 0
        Dim deviceCount As Integer
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

        defaultwidth = camWidth * 2 / resizeForDisplay + border * 7
        defaultHeight = camHeight * 2 / resizeForDisplay + ToolStrip1.Height + border * 12
        Me.Width = GetSetting("OpenCVB", "OpenCVBWidth", "OpenCVBWidth", defaultWidth)
        Me.Height = GetSetting("OpenCVB", "OpenCVBHeight", "OpenCVBHeight", defaultHeight)
        If Me.Height < 50 Then
            Me.Width = defaultWidth
            Me.Height = defaultHeight
        End If

        TTtextData = New List(Of VB_Classes.TTtext)

        For i = 0 To camPic.Length - 1
            If camPic(i) Is Nothing Then camPic(i) = New PictureBox()
            camPic(i).Size = New Size(If(i < 2, camWidth / resizeForDisplay, camWidth * 2 / resizeForDisplay), camHeight / resizeForDisplay)
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
            If e.Button = Windows.Forms.MouseButtons.Left Then
                If DrawingRectangle Then
                    DrawingRectangle = False
                    GrabRectangleData = True
                End If
            ElseIf e.Button = Windows.Forms.MouseButtons.Right Then
                BothFirstAndLastReady = False ' we have to see some movement after mousedown.
                DrawingRectangle = False

                Dim pic = DirectCast(sender, PictureBox)
                Dim src = Choose(pic.Tag + 1, camera.Color, camera.RGBDepth, imgResult)
                drawRect = validateRect(drawRect, src.width, src.height)
                Dim offset = 0
                If drawRect.X > camPic(0).Width Then offset = camPic(0).Width
                Dim srcROI = New cv.Mat
                srcROI = src(drawRect).clone()
                Dim csvName As New FileInfo(System.IO.Path.GetTempFileName() + ".csv")
                Dim sw = New StreamWriter(csvName.FullName)
                sw.WriteLine("Color image in BGR format - 3 columns per pixel" + vbCrLf)
                sw.WriteLine(vbCrLf + "width = " + CStr(drawRect.Width) + vbCrLf + "height = " + CStr(drawRect.Height))
                sw.Write("Row," + vbTab)
                For i = 0 To drawRect.Width - 1
                    sw.Write("B" + Format(drawRect.X - offset + i, "000") + "," + "G" + Format(drawRect.X - offset + i, "000") + "," + "R" + Format(drawRect.X - offset + i, "000") + ",")
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
                drawRect.X = Math.Min(mouseDownPoint.X, mouseMovePoint.X)
                drawRect.Y = Math.Min(mouseDownPoint.Y, mouseMovePoint.Y)
                drawRect.Width = Math.Abs(mouseDownPoint.X - mouseMovePoint.X)
                drawRect.Height = Math.Abs(mouseDownPoint.Y - mouseMovePoint.Y)
                If drawRect.X + drawRect.Width > camWidth Then drawRect.Width = camWidth - drawRect.X
                If drawRect.Y + drawRect.Height > camHeight Then drawRect.Height = camHeight - drawRect.Y
                BothFirstAndLastReady = True
            End If
            mousePicTag = pic.Tag
            mousePoint.X = e.X
            mousePoint.Y = e.Y
            If mousePicTag = 2 And mousePoint.X > camPic(0).Width Then
                mousePoint.X -= camPic(0).Width
                mousePicTag = 3 ' pretend this is coming from the fictional campic(3) which was dst2
            End If
            Dim resizeFactor = camWidth / camPic(0).Width
            mousePoint *= resizeFactor * optionsForm.resolutionResizeFactor

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
            PausePlayButton.Image = Image.FromFile("../../OpenCVB/Data/PauseButton.png")
        Else
            PausePlayButton.Text = "Run"
            pauseAlgorithmThread = True
            saveTestAllState = TestAllTimer.Enabled
            If TestAllTimer.Enabled Then testAllButton_Click(sender, e)
            PausePlayButton.Image = Image.FromFile("../../OpenCVB/Data/PauseButtonRun.png")
        End If
    End Sub
    Private Sub testAllButton_Click(sender As Object, e As EventArgs) Handles TestAllButton.Click
        If TestAllButton.Text = "Test All" Then
            AlgorithmTestCount = 0
            TestAllButton.Text = "Stop Test"
            TestAllButton.Image = Image.FromFile("../../OpenCVB/Data/StopTest.png")
            If logActive Then logAlgorithms = New StreamWriter("C:\Temp\logAlgorithms.csv")
            TestAllTimer_Tick(sender, e)
            TestAllTimer.Enabled = True
            If TreeViewDialog IsNot Nothing Then TreeViewDialog.Timer1.Enabled = True
        Else
            TestAllTimer.Enabled = False
            TestAllButton.Text = "Test All"
            If logActive Then logAlgorithms.Close()
            TestAllButton.Image = Image.FromFile("../../OpenCVB/Data/testall.png")
        End If
    End Sub
    Private Sub OpenCVB_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        Dim diff = Now().Subtract(startAlgorithmTime)
        If diff.TotalSeconds > 5 Then OptionsBringToFront = True
    End Sub
    Private Sub OpenCVB_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd
        saveLayout()
    End Sub
    Public Sub raiseEventCamera()
        SyncLock delegateLock
            For i = 0 To displayFrames - 1
                camPic(i).Refresh()
            Next
        End SyncLock
    End Sub
    Private Sub fpsTimer_Tick(sender As Object, e As EventArgs) Handles fpsTimer.Tick
        If TreeViewDialog IsNot Nothing Then
            If TreeViewDialog.TreeView1.IsDisposed Then TreeButton.CheckState = CheckState.Unchecked
        End If

        Static lastFrame As Integer
        If lastFrame > frameCount Then lastFrame = 0
        Dim countFrames = frameCount - lastFrame
        lastFrame = frameCount
        Dim fps As Single = countFrames / (fpsTimer.Interval / 1000)

        Static lastCameraFrame As Integer
        If lastCameraFrame > camera.frameCount Then lastCameraFrame = 0
        Dim camFrames = camera.frameCount - lastCameraFrame
        lastCameraFrame = camera.frameCount
        Dim cameraFPS As Single = camFrames / (fpsTimer.Interval / 1000)

        Me.Text = "OpenCVB (" + Format(CodeLineCount, "###,##0") + " lines / " + CStr(AlgorithmCount) + " algorithms = " + CStr(CInt(CodeLineCount / AlgorithmCount)) +
                  " lines per) - " + optionsForm.cameraRadioButton(optionsForm.cameraIndex).Text + " - " + Format(cameraFPS, "#0.0") +
                  "/" + Format(fps, "#0.0") + " " + CStr(totalBytesOfMemoryUsed) + " Mb (working set)"
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
                resolutionDesc = "320x180"
            Case "Medium"
                resolutionDesc = "640x360"
            Case "High"
                resolutionDesc = "1280x720"
        End Select
        Dim details = " Display at " + CStr(camPic(0).Width) + "x" + CStr(camPic(0).Height) + ", Working Res. = " + resolutionDesc
        picLabels(0) = "RGB:" + details
        picLabels(1) = "Depth:" + details
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
        If AlgorithmTestCount Mod AvailableAlgorithms.Items.Count = 0 And AlgorithmTestCount > 0 Or specialSingleCount Then
            If optionsForm.LowResolution.Checked Then
                optionsForm.mediumResolution.Checked = True
            ElseIf optionsForm.mediumResolution.Checked Then
                optionsForm.HighResolution.Checked = True
            ElseIf optionsForm.HighResolution.Checked Then
                optionsForm.LowResolution.Checked = True
            End If
            saveLayout()
        End If

        If optionsForm.LowResolution.Checked Then ' only change cameras when in low resolution.
            ' after sweeping through low to high resolution, sweep through the cameras as well...
            If (AlgorithmTestCount Mod AvailableAlgorithms.Items.Count = 0 And AlgorithmTestCount > 0) Then
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
            If saveCurrentCamera <> optionsForm.cameraIndex Then RestartCamera()
            TestAllTimer.Interval = optionsForm.TestAllDuration.Value * 1000

            If optionsForm.SnapToGrid.Checked Then
                camPic(0).Size = New Size(camWidth / 2, camHeight / 2)
                camPic(1).Size = New Size(camWidth / 2, camHeight / 2)
                camPic(2).Size = New Size(camWidth, camHeight / 2)

                camPic(1).Left = camPic(0).Left + camPic(0).Width
                camPic(2).Top = camPic(0).Top + camPic(0).Height

                Me.Width = defaultWidth
                Me.Height = defaultHeight
            End If
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

        ' opengl algorithms are only to be run at full resolution.  All other algorithms respect the options setting...
        If AvailableAlgorithms.Text.Contains("OpenGL") Or AvailableAlgorithms.Text.Contains("OpenCVGL") Then OptionsDialog.HighResolution.Checked = True

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

        PausePlayButton.Image = Image.FromFile("../../OpenCVB/Data/PauseButton.png")

        Dim imgSize = New cv.Size(CInt(resolutionXY.width * 2), CInt(resolutionXY.Height))
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
        startAlgorithmTime = Now() ' black out optionsbringtofront
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
            Dim task = New VB_Classes.ActiveTask(parms, resolutionXY, algName, myLocation)
            textDesc = task.ocvb.desc
            openFileInitialDirectory = task.ocvb.openFileInitialDirectory
            openFileDialogRequested = task.ocvb.openFileDialogRequested
            openFileinitialStartSetting = task.ocvb.initialStartSetting
            task.ocvb.fileStarted = task.ocvb.initialStartSetting
            openFileStarted = task.ocvb.initialStartSetting
            openFileFilterIndex = task.ocvb.openFileFilterIndex
            openFileFilter = task.ocvb.openFileFilter
            openFileDialogName = task.ocvb.openFileDialogName
            openfileDialogTitle = task.ocvb.openFileDialogTitle
            intermediateReview = ""

            Console.WriteLine(vbCrLf + vbCrLf + vbTab + algName + " " + textDesc + vbCrLf + vbTab + CStr(AlgorithmTestCount) + vbTab + "Algorithms tested")
            Console.WriteLine(vbTab + Format(totalBytesOfMemoryUsed, "#,##0") + "Mb working set before running " + algName + vbCrLf + vbCrLf)

            If logActive And TestAllTimer.Enabled Then logAlgorithms.WriteLine(algName + "," + CStr(totalBytesOfMemoryUsed))

            ' if the constructor for the algorithm sets the drawrect, adjust it for the ratio of the actual size and algorithm sized image.
            If task.ocvb.drawRect <> New cv.Rect(0, 0, 0, 0) Then
                drawRect = task.ocvb.drawRect
                Dim ratio = task.ocvb.color.Width / camPic(0).Width  ' relative size of algorithm size image to displayed image
                drawRect = New cv.Rect(drawRect.X / ratio, drawRect.Y / ratio, drawRect.Width / ratio, drawRect.Height / ratio)
            End If

            TTtextData.Clear()

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

            ' remove all options forms.  They can only be made topmost (see OptionsBringToFront above) when created on the same thread.
            ' This deletes the options forms for the current thread so they can be created (if needed) with the next algorithm thread.
            Try
                Dim frmlist As New List(Of Form)
                For Each frm In Application.OpenForms
                    If frm.name.startswith("Option") Then frmlist.Add(frm)
                Next
                For Each frm In frmlist
                    frm.Close()
                Next
            Catch ex As Exception
                Console.WriteLine("Error removing an Options form: " + ex.Message)
            End Try

            task.Dispose()
            frameCount = 0
            If parms.testAllRunning Then Console.WriteLine(vbTab + "Ending " + algName)
        End SyncLock
    End Sub
    Private Sub Exit_Click(sender As Object, e As EventArgs) Handles ExitCall.Click
        stopCameraThread = True
        saveAlgorithmName = ""
        If TestAllTimer.Enabled Then testAllButton_Click(sender, e) ' close the log file if needed.
        Application.DoEvents()
        camera.closePipe()
        textDesc = ""
        saveLayout()
        SaveSetting("OpenCVB", "TreeButton", "TreeButton", TreeButton.Checked)
        End
    End Sub
    Private Sub AboutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem.Click
        MsgBox("The objective is to solve many small computer vision problems and do so in a way that enables " +
               "any of the examples to be reused. The result is a toolkit for solving ever bigger and more " +
               "difficult problems. The philosophy behind this approach is that human vision is built on many " +
               "seemingly trivial approaches working together. Is the combined effort of many small operations " +
               "the source of understanding?  It may take years to answer that question. " + vbCrLf + vbCrLf +
               "Fall 2020 Fremont CA")
    End Sub
    Private Sub Run(task As VB_Classes.ActiveTask, algName As String)
        While 1
            While 1
                If saveAlgorithmName <> algName Or saveAlgorithmName = "" Then Exit Sub ' pause will stop the current algorithm as well.
                Application.DoEvents() ' this will allow any options for the algorithm to be updated...
                If camera.newImagesAvailable And pauseAlgorithmThread = False Then Exit While
            End While

            ' bring the data into the algorithm task.
            SyncLock bufferLock
                If camera.color.width = 0 Or camera.RGBDepth.width = 0 Or camera.leftView.width = 0 Or camera.rightView.width = 0 Then Continue While
                camera.newImagesAvailable = False

                task.ocvb.color = camera.color.Resize(resolutionXY)
                task.ocvb.RGBDepth = camera.RGBDepth.Resize(resolutionXY)
                task.ocvb.leftView = camera.leftView.Resize(resolutionXY)
                task.ocvb.rightView = camera.rightView.Resize(resolutionXY)
                task.ocvb.pointCloud = camera.PointCloud.clone

                task.ocvb.depth16 = camera.depth16.clone
                task.ocvb.transformationMatrix = camera.transformationMatrix
                task.ocvb.IMU_TimeStamp = camera.IMU_TimeStamp
                task.ocvb.IMU_Barometer = camera.IMU_Barometer
                task.ocvb.IMU_Magnetometer = camera.IMU_Magnetometer
                task.ocvb.IMU_Temperature = camera.IMU_Temperature
                task.ocvb.IMU_Rotation = camera.IMU_Rotation
                task.ocvb.IMU_Translation = camera.IMU_Translation
                task.ocvb.IMU_Acceleration = camera.IMU_Acceleration
                task.ocvb.IMU_Velocity = camera.IMU_Velocity
                task.ocvb.IMU_AngularAcceleration = camera.IMU_AngularAcceleration
                task.ocvb.IMU_AngularVelocity = camera.IMU_AngularVelocity
                task.ocvb.IMU_FrameTime = camera.IMU_FrameTime
                task.ocvb.CPU_TimeStamp = camera.CPU_TimeStamp
                task.ocvb.CPU_FrameTime = camera.CPU_FrameTime
                task.ocvb.intermediateReview = intermediateReview
            End SyncLock

            Try
                Dim ratio = task.ocvb.color.Width / camPic(0).Width  ' relative size of displayed image and algorithm size image.
                If GrabRectangleData Then
                    GrabRectangleData = False
                    task.ocvb.drawRect = New cv.Rect(drawRect.X * ratio, drawRect.Y * ratio, drawRect.Width * ratio, drawRect.Height * ratio)
                    If task.ocvb.drawRect.Width <= 2 Then task.ocvb.drawRect.Width = 0 ' too small?
                    Dim w = task.ocvb.color.Width
                    If task.ocvb.drawRect.X > w Then task.ocvb.drawRect.X -= w
                    If task.ocvb.drawRect.X < w And task.ocvb.drawRect.X + task.ocvb.drawRect.Width > w Then
                        task.ocvb.drawRect.Width = w - task.ocvb.drawRect.X
                    End If
                    BothFirstAndLastReady = False
                End If

                task.ocvb.mousePoint = mousePoint
                task.ocvb.mousePicTag = mousePicTag
                task.ocvb.mouseClickFlag = mouseClickFlag
                If mouseClickFlag Then task.ocvb.mouseClickPoint = mousePoint
                mouseClickFlag = False

                task.ocvb.fileStarted = openFileStarted ' UI may have stopped play.

                task.RunAlgorithm()

                If task.ocvb.drawRectClear Then
                    drawRect = New cv.Rect
                    task.ocvb.drawRect = drawRect
                    task.ocvb.drawRectClear = False
                End If

                If openFileDialogName <> "" Then
                    If openFileDialogName <> task.ocvb.openFileDialogName Or openFileStarted <> task.ocvb.fileStarted Then
                        task.ocvb.fileStarted = openFileStarted
                        task.ocvb.openFileDialogName = openFileDialogName
                    End If
                    openfileSliderPercent = task.ocvb.openFileSliderPercent
                End If

                Static inputFile As String = "" ' task.ocvb.openFileDialogName
                If inputFile <> task.ocvb.openFileDialogName Then
                    inputFile = task.ocvb.openFileDialogName
                    openFileInitialDirectory = task.ocvb.openFileInitialDirectory
                    openFileDialogRequested = task.ocvb.openFileDialogRequested
                    openFileinitialStartSetting = True ' if the file playing changes while the algorithm is running, automatically start playing the new file.
                    openFileFilterIndex = task.ocvb.openFileFilterIndex
                    openFileFilter = task.ocvb.openFileFilter
                    openFileDialogName = task.ocvb.openFileDialogName
                    openfileDialogTitle = task.ocvb.openFileDialogTitle
                End If

                picLabels(2) = task.ocvb.label1
                picLabels(3) = task.ocvb.label2

                ' share the results of the algorithm task.
                SyncLock TTtextData
                    algorithmRefresh = True
                    imgResult = task.ocvb.result.Clone()
                    TTtextData.Clear()
                    If task.ocvb.TTtextData.Count Then
                        For i = 0 To task.ocvb.TTtextData.Count - 1
                            TTtextData.Add(task.ocvb.TTtextData(i)) ' pull over any truetype text data so paint can access it.
                        Next
                        task.ocvb.TTtextData.Clear()
                    End If
                End SyncLock
                If OptionsBringToFront Then
                    OptionsBringToFront = False
                    Try
                        For Each frm In Application.OpenForms
                            If frm.name.startswith("Option") Then frm.topmost = True
                        Next
                        For Each frm In Application.OpenForms
                            If frm.name.startswith("Option") Then frm.topmost = False
                        Next
                    Catch ex As Exception
                        Console.WriteLine("Error in OptionsBringToFront: " + ex.Message)
                    End Try
                    openFileFormLocated = False
                End If
                If Me.IsDisposed Then Exit While
            Catch ex As Exception
                Console.WriteLine("Error in AlgorithmTask: " + ex.Message)
                Exit While
            End Try

            If frameCount Mod 100 = 0 Then
                SyncLock callTraceLock
                    ' this allows for dynamic allocation of new algorithms.
                    callTrace.Clear()
                    For i = 0 To task.ocvb.callTrace.Count - 1
                        callTrace.Add(task.ocvb.callTrace(i))
                    Next
                End SyncLock
            End If
            frameCount += 1
        End While
    End Sub
End Class

