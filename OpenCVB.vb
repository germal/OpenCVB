Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
Imports System.Runtime.InteropServices
Imports System.IO
Imports System.ComponentModel
Imports System.Threading
Imports System.Globalization
Imports System.Text.RegularExpressions
Imports System.Environment
Public Class OpenCVB
#Region "Globals"
    Const displayFrames As Int32 = 4
    Dim activeAlgorithm As String
    Dim AlgorithmCount As Int32
    Dim AlgorithmTestCount As Int32
    Dim algorithmTaskHandle As Thread
    Dim border As Int32 = 6
    Dim BothFirstAndLastReady As Boolean
    Dim cameraFrameCount As Int32
    Dim camera As Object
    Dim cameraDataUpdated As Boolean
    Dim cameraD400Series As Object
    Dim cameraKinect As Object
    Dim cameraT265 As Object
    Public cameraName As String
    Dim cameraTaskHandle As Thread
    Dim camPic(displayFrames - 1) As PictureBox
    Dim CodeLineCount As Int32
    Dim DrawingRectangle As Boolean
    Dim drawRect As New cv.Rect(0, 0, 0, 0)
    Dim externalInvocation As Boolean
    Dim formColor As New cv.Mat, formDepthRGB As New cv.Mat, formResult1 As New cv.Mat, formResult2 As New cv.Mat
    Dim formDepth As New cv.Mat, formPointCloud As New cv.Mat, formDisparity As New cv.Mat, formleftView As New cv.Mat, formrightView As New cv.Mat
    Dim formResultsUpdated As Boolean
    Dim frameCount As Int32
    Dim GrabRectangleData As Boolean
    Dim HomeDir As DirectoryInfo
    Dim imuGyro As cv.Point3f
    Dim imuAccel As cv.Point3f
    Dim imuTimeStamp As Double
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
    Dim TTtextData(displayFrames - 1) As List(Of VB_Classes.ActiveClass.TrueType)
    Dim vtkDirectory As String = ""

    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Public Shared Function GetCursorPos(ByRef point As System.Drawing.Point) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetForegroundWindow() As IntPtr
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetWindowRect(hWnd As IntPtr, ByRef rect As Rect) As IntPtr
    End Function
    <StructLayout(LayoutKind.Sequential)>
    Private Structure Rect
        Public Left As Integer
        Public Top As Integer
        Public Right As Integer
        Public Bottom As Integer
    End Structure
#End Region
    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        HomeDir = New DirectoryInfo(CurDir() + "\..\..\") ' running in OpenCVB/bin/Debug mostly...
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
            externalInvocation = True ' we don't need to start python because it started OpenCVB.
        End If

        OpenCVfullPath = HomeDir.FullName + "OpenCV\Build\bin\Debug\"
        updatePath(OpenCVfullPath, "OpenCV and OpenCV Contrib are needed for C++ classes.")

        OpenCVfullPath = HomeDir.FullName + "OpenCV\Build\bin\Release\"
        updatePath(OpenCVfullPath, "OpenCV and OpenCV Contrib are needed for C++ classes.")

        Dim IntelPERC_Lib_Dir = HomeDir.FullName + "librealsense\build\Debug\"
        updatePath(IntelPERC_Lib_Dir, "Realsense camera support.")

        IntelPERC_Lib_Dir = HomeDir.FullName + "librealsense\build\Release\"
        updatePath(IntelPERC_Lib_Dir, "Realsense camera support.")

        Dim Kinect_Dir = HomeDir.FullName + "Azure-Kinect-Sensor-SDK\build\bin\Debug\"
        updatePath(Kinect_Dir, "Kinect camera support.")

        Kinect_Dir = HomeDir.FullName + "Azure-Kinect-Sensor-SDK\build\bin\Release\"
        updatePath(Kinect_Dir, "Kinect camera support.")

        ' the depthEngine DLL is not included in the SDK.  It is distributed separately because it is NOT open source.
        ' The depthEngine DLL is supposed to be installed in C:\Program Files\Azure Kinect SDK v1.1.0\sdk\windows-desktop\amd64\$(Configuration)
        ' Post an issue if this Is Not a valid assumption
        Dim kinectDLL As New FileInfo("C:\Program Files\Azure Kinect SDK v1.3.0\sdk\windows-desktop\amd64\release\bin\depthengine_2_0.dll")
        If kinectDLL.Exists = False Then
            MsgBox("The Microsoft installer for the Kinect camera proprietary portion was not installed in the right place (or it has changed.)" + vbCrLf +
                "It was expected to be in " + kinectDLL.FullName + vbCrLf + "Update the code and restart.")
        End If
        updatePath(kinectDLL.Directory.FullName, "Kinect depth engine dll.")
        Debug.WriteLine("system path = " + Environment.GetEnvironmentVariable("Path"))
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture

        optionsForm = New OptionsDialog
        optionsForm.OptionsDialog_Load(sender, e)

        cameraD400Series = New IntelD400Series()
        cameraD400Series.deviceCount = USBenumeration("Depth Camera 435")
        cameraD400Series.deviceCount += USBenumeration("Depth Camera 415")
        If cameraD400Series.deviceCount > 0 Then cameraD400Series.initialize(30, regWidth, regHeight)

        cameraKinect = New Kinect(30, regWidth, regHeight)

        cameraT265 = New IntelT265()
        cameraT265.deviceCount = USBenumeration("T265")
        If cameraT265.deviceCount > 0 Then cameraT265.initialize(30, regWidth, regHeight)

        optionsForm.cameraDeviceCount(OptionsDialog.D400Cam) = cameraD400Series.devicecount
        optionsForm.cameraDeviceCount(OptionsDialog.Kinect4AzureCam) = cameraKinect.devicecount
        optionsForm.cameraDeviceCount(OptionsDialog.T265Camera) = cameraT265.devicecount

        updateCamera()

        ' if a camera is missing, try to find another.
        If camera.deviceCount = 0 And cameraD400Series.deviceCount > 0 Then
            optionsForm.cameraIndex = OptionsDialog.D400Cam
            updateCamera()
        End If
        If camera.deviceCount = 0 And cameraKinect.deviceCount > 0 Then
            optionsForm.cameraIndex = OptionsDialog.Kinect4AzureCam
            updateCamera()
        End If
        If camera.deviceCount = 0 And cameraT265.deviceCount > 0 Then
            optionsForm.cameraIndex = OptionsDialog.T265Camera
            updateCamera()
        End If

        optionsForm.cameraRadioButton(optionsForm.cameraIndex).Checked = True ' make sure any switch is reflected in the UI.
        SaveSetting("OpenCVB", "CameraIndex", "CameraIndex", optionsForm.cameraIndex)

        If camera.deviceCount = 0 Then
            MsgBox("OpenCVB supports Kinect for Azure 3D camera, Intel D400Series 3D camera, or Intel T265.  Nothing found!")
            End
        End If

        setupCamPics()
        loadAlgorithmComboBoxes()

        TestAllTimer.Interval = optionsForm.TestAllDuration.Text * 1000
        FindPython()
    End Sub
    Private Sub campic_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        Try
            SyncLock camPic ' avoid updating the image while copying into it in the algorithm and camera tasks
                If formColor IsNot Nothing And formColor.Width > 0 And formColor.Height > 0 Then
                    cameraDataUpdated = False
                    If formColor.Width <> camPic(0).Width Or formColor.Height <> camPic(0).Height Or
                       formDepthRGB.Width <> camPic(1).Width Or formDepthRGB.Height <> camPic(1).Height Then

                        Dim color = formColor
                        Dim depthRGB = formDepthRGB
                        color = color.Resize(New cv.Size(camPic(0).Size.Width, camPic(0).Size.Height))
                        depthRGB = depthRGB.Resize(New cv.Size(camPic(1).Size.Width, camPic(1).Size.Height))
                        cvext.BitmapConverter.ToBitmap(color, camPic(0).Image)
                        cvext.BitmapConverter.ToBitmap(depthRGB, camPic(1).Image)
                    Else
                        cvext.BitmapConverter.ToBitmap(formColor, camPic(0).Image)
                        cvext.BitmapConverter.ToBitmap(formDepthRGB, camPic(1).Image)
                    End If

                    If formResultsUpdated Then
                        If formResult1.Width <> camPic(2).Width Or formResult1.Height <> camPic(2).Height Or
                            formResult2.Width <> camPic(3).Width Or formResult2.Height <> camPic(3).Height Then

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
                        formResultsUpdated = False
                    End If
                End If
                Dim pic = DirectCast(sender, PictureBox)
                g.ScaleTransform(1, 1)
                g.DrawImage(pic.Image, 0, 0)
                g.DrawRectangle(myPen, drawRect.X, drawRect.Y, drawRect.Width, drawRect.Height)

                If optionsForm.ShowLabels.Checked Then
                    ' with the low resolution display, we need to use the entire width of the image to display the RGB and Depth text area.
                    Dim textRect As New Rectangle(0, 0, pic.Width / 2, If(resizeForDisplay = 4, 12, 20))
                    If Len(picLabels(pic.Tag)) Then g.FillRectangle(myBrush, textRect)
                    Dim black = System.Drawing.Color.Black
                    Dim fontSize = If(resizeForDisplay = 4, 6, 10)
                    g.DrawString(picLabels(pic.Tag), New Font("Microsoft Sans Serif", fontSize), New SolidBrush(black), 0, 0)
                End If

                ' draw any TrueType font data on the image 
                Dim white = System.Drawing.Color.White
                Dim maxline = 21
                For Each tt In TTtextData(pic.Tag)
                    g.DrawString(tt.text, New Font(tt.fontName, tt.fontSize), New SolidBrush(white), tt.x, tt.y)
                    If tt.x >= camPic(pic.Tag).Width Or tt.y >= camPic(pic.Tag).Height Then
                        Console.WriteLine("TrueType text off image!  " + tt.text + " is being written to " + CStr(tt.x) + " and " + CStr(tt.y))
                    End If
                    maxline -= 1
                    If maxline <= 0 Then Exit For
                Next
            End SyncLock
        Catch ex As Exception
            Console.WriteLine("Paint exception occurred: " + ex.Message)
        End Try
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
        Dim deviceCount As Int32
        ' See if the desired device shows up in the device manager.'
        Dim info As Management.ManagementObject
        Dim search As System.Management.ManagementObjectSearcher
        Dim Name As String
        search = New System.Management.ManagementObjectSearcher("SELECT * From Win32_PnPEntity")
        For Each info In search.Get()
            Name = CType(info("Caption"), String) ' Get the name of the device.'
            If InStr(Name, searchName, CompareMethod.Text) > 0 Then deviceCount += 1
        Next
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
            TTtextData(i) = New List(Of VB_Classes.ActiveClass.TrueType)
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
                Dim src = Choose(pic.Tag + 1, formColor, formDepthRGB, formResult1, formResult2)

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
                    sw.Write("Row " + Format(y, "000") + "," + vbTab)
                    For x = 0 To drawRect.Width - 1
                        Dim pt = srcROI.At(Of cv.Vec3b)(y, x)
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
                If drawRect.X + drawRect.Width > formColor.Width Then drawRect.Width = formColor.Width - drawRect.X
                If drawRect.Y + drawRect.Height > formColor.Height Then drawRect.Height = formColor.Height - drawRect.Y
                BothFirstAndLastReady = True
            End If
            mousePicTag = pic.Tag
            mousePoint.X = e.X
            mousePoint.Y = e.Y
        Catch ex As Exception
            Console.WriteLine("Error in camPic_MouseMove: " + ex.Message)
        End Try
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
            PausePlayButton.Image = Image.FromFile("../../Data/PauseButton.png")
        Else
            saveTestAllState = TestAllTimer.Enabled
            If TestAllTimer.Enabled Then testAllButton_Click(sender, e)
            stopAlgorithmThread = True
            PausePlayButton.Image = Image.FromFile("../../Data/PauseButtonRun.png")
        End If
    End Sub
    Private Sub testAllButton_Click(sender As Object, e As EventArgs) Handles TestAllButton.Click
        If TestAllButton.Text = "Test All" Then
            TestAllButton.Text = "Stop Test"
            TestAllButton.Image = Image.FromFile("../../Data/StopTest.png")
            TestAllTimer_Tick(sender, e)
            TestAllTimer.Enabled = True
        Else
            TestAllTimer.Enabled = False
            TestAllButton.Text = "Test All"
            TestAllButton.Image = Image.FromFile("../../Data/testall.png")
        End If
    End Sub
    Private Sub TestAllTimer_Tick(sender As Object, e As EventArgs) Handles TestAllTimer.Tick
        If stopAlgorithmThread = True Then Exit Sub ' they have paused.
        If AvailableAlgorithms.SelectedIndex < AvailableAlgorithms.Items.Count - 1 Then
            AvailableAlgorithms.SelectedIndex += 1
        Else
            AvailableAlgorithms.SelectedIndex = 0
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
    Private Sub fpsTimer_Tick(sender As Object, e As EventArgs) Handles fpsTimer.Tick
        Static lastFrame As Int32
        If lastFrame > frameCount Then lastFrame = 0
        Dim countFrames = frameCount - lastFrame
        lastFrame = frameCount
        Dim fps As Single = countFrames / (fpsTimer.Interval / 1000)

        Static lastCameraFrame As Int32
        If lastCameraFrame > cameraFrameCount Then lastCameraFrame = 0
        Dim camFrames = cameraFrameCount - lastCameraFrame
        lastCameraFrame = cameraFrameCount
        Dim cameraFPS As Single = camFrames / (fpsTimer.Interval / 1000)

        Me.Text = "OpenCVB (" + CStr(AlgorithmCount) + " algorithms " + Format(CodeLineCount, "###,##0") + " lines) - " + cameraName +
                  " FPS = " + Format(cameraFPS, "#0.0") + ", Algorithm FPS = " + Format(fps, "#0.0")
        If AlgorithmDesc.Text = "" Then AlgorithmDesc.Text = textDesc
    End Sub
    Private Sub saveLayout()
        SaveSetting("OpenCVB", "OpenCVBLeft", "OpenCVBLeft", Me.Left)
        SaveSetting("OpenCVB", "OpenCVBTop", "OpenCVBTop", Me.Top)
        SaveSetting("OpenCVB", "OpenCVBWidth", "OpenCVBWidth", Me.Width)
        SaveSetting("OpenCVB", "OpenCVBHeight", "OpenCVBHeight", Me.Height)

        Dim details = CStr(regWidth) + "x" + CStr(regHeight) + " display " + CStr(camPic(0).Width) + "x" + CStr(camPic(0).Height) + " lowResolution="
        If optionsForm.lowResolution.Checked Then details += "On" Else details += "Off"
        picLabels(0) = "Input " + details
        picLabels(1) = "Depth " + details
    End Sub
    Public Sub updateCamera()
        Select Case optionsForm.cameraIndex
            Case OptionsDialog.D400Cam
                camera = cameraD400Series
            Case OptionsDialog.Kinect4AzureCam
                camera = cameraKinect
            Case OptionsDialog.T265Camera
                camera = cameraT265
        End Select
        cameraName = camera.devicename
    End Sub
    Private Sub MainFrm_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        stopAlgorithmThread = True
        stopCameraThread = True
        Application.DoEvents()
        If threadStop(frameCount) = False Then algorithmTaskHandle.Abort()
        If threadStop(cameraFrameCount) = False Then cameraTaskHandle.Abort()
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
            Dim radioButton = Choose(i + 1, snapForm.AllImages, snapForm.ColorImage, snapForm.DepthRGB, snapForm.Result1, snapForm.Result2)
            If radioButton.checked Then
                SyncLock camPic
                    Select Case i
                        Case 0 ' all images
                            resultMat = cv.Extensions.BitmapConverter.ToMat(img)
                        Case 1 ' color image
                            resultMat = formColor.Clone()
                        Case 2 ' depth RGB
                            resultMat = formDepthRGB.Clone()
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
    Private Sub Options_Click(sender As Object, e As EventArgs) Handles OptionsButton.Click
        If TestAllTimer.Enabled Then testAllButton_Click(sender, e)
        TestAllTimer.Enabled = False
        stopAlgorithmThread = True
        Dim saveCurrentCamera = optionsForm.cameraIndex

        Dim OKcancel = optionsForm.ShowDialog()

        If OKcancel = DialogResult.OK Then
            If saveCurrentCamera <> optionsForm.cameraIndex Then
                stopCameraThread = True
                If threadStop(cameraFrameCount) = False Then cameraTaskHandle.Abort()
                cameraTaskHandle = Nothing
            End If
            TestAllTimer.Interval = optionsForm.TestAllDuration.Value * 1000

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
        If frame <> 0 Then
            Dim sleepCount As Int32
            ' some algorithms can take a long time to finish a single iteration.  
            ' Each algorithm must run dispose() - to kill options forms and external Python or OpenGL taskes.  Wait until exit...
            While frame
                Application.DoEvents() ' to allow the algorithm task to gracefully end and dispose OpenCVB.
                Thread.Sleep(100)
                sleepCount += 1
                If sleepCount > 100 Then Return False
            End While
        End If
        Return True
    End Function
    Private Sub StartAlgorithmTask()
        stopAlgorithmThread = True
        If threadStop(frameCount) = False Then algorithmTaskHandle.Abort()

        cameraD400Series.DecimationFilter = GetSetting("OpenCVB", "DecimationFilter", "DecimationFilter", False)
        cameraD400Series.ThresholdFilter = GetSetting("OpenCVB", "ThresholdFilter", "ThresholdFilter", False)
        cameraD400Series.DepthToDisparity = GetSetting("OpenCVB", "DepthToDisparity", "DepthToDisparity", True)
        cameraD400Series.SpatialFilter = GetSetting("OpenCVB", "SpatialFilter", "SpatialFilter", True)
        cameraD400Series.TemporalFilter = GetSetting("OpenCVB", "TemporalFilter", "TemporalFilter", False)
        cameraD400Series.HoleFillingFilter = GetSetting("OpenCVB", "HoleFillingFilter", "HoleFillingFilter", True)
        cameraD400Series.DisparityToDepth = GetSetting("OpenCVB", "DisparityToDepth", "DisparityToDepth", True)

        Dim parms As New VB_Classes.ActiveClass.algorithmParameters
        lowResolution = optionsForm.lowResolution.Checked

        parms.activeAlgorithm = AvailableAlgorithms.Text
        ' opengl algorithms are only to be run at full resolution.  All other algorithms respect the options setting...
        If parms.activeAlgorithm.Contains("OpenGL") Or parms.activeAlgorithm.Contains("OpenCVGL") Then lowResolution = False
        fastSize = If(lowResolution, New cv.Size(regWidth / 2, regHeight / 2), New cv.Size(regWidth, regHeight))

        parms.lowResolution = lowResolution
        parms.cameraIndex = optionsForm.cameraIndex ' index of active camera

        parms.PythonExe = optionsForm.PythonExeName.Text
        parms.vtkDirectory = vtkDirectory
        parms.HomeDir = HomeDir.FullName
        parms.OpenCVfullPath = OpenCVfullPath
        parms.mainFormLoc = Me.Location
        parms.mainFormHeight = Me.Height
        parms.OpenCV_Version_ID = Environment.GetEnvironmentVariable("OpenCV_Version")
        parms.imageToTrueTypeLoc = 1 / resizeForDisplay
        parms.useRecordedData = OpenCVkeyword.Text = "<All using recorded data>"
        parms.testAllRunning = TestAllButton.Text = "Stop Test"
        parms.externalInvocation = externalInvocation
        If parms.testAllRunning Then parms.ShowOptions = optionsForm.ShowOptions.Checked Else parms.ShowOptions = True ' always show options when not running 'test all'
        parms.ShowConsoleLog = optionsForm.ShowConsoleLog.Checked
        parms.AvoidDNNCrashes = optionsForm.AvoidDNNCrashes.Checked

        If parms.lowResolution Then parms.speedFactor = 2 Else parms.speedFactor = 1
        parms.width = regWidth / parms.speedFactor
        parms.height = regHeight / parms.speedFactor
        If parms.lowResolution Then parms.imageToTrueTypeLoc *= parms.speedFactor

        AlgorithmDesc.Text = ""
        PausePlayButton.Image = Image.FromFile("../../Data/PauseButton.png")

        stopAlgorithmThread = False

        formResult1 = New cv.Mat(fastSize, cv.MatType.CV_8UC3, 0)
        formResult2 = New cv.Mat(fastSize, cv.MatType.CV_8UC3, 0)
        formResultsUpdated = True ' one time update to zero out the results when starting a new camera or algorithm.

        Thread.CurrentThread.Priority = ThreadPriority.Lowest

        If cameraTaskHandle Is Nothing Then
            stopCameraThread = False
            updateCamera()

            If camera.deviceCount = 0 Then SaveSetting("OpenCVB", "CameraIndex", "CameraIndex", OptionsDialog.cameraIndex)

            cameraFrameCount = 0
            cameraTaskHandle = New Thread(AddressOf CameraTask)
            cameraTaskHandle.Name = "CameraTask"
            cameraTaskHandle.Priority = ThreadPriority.Highest
            cameraTaskHandle.Start(camera.pcMultiplier)
            ' wait for the first frame to appear.
            While cameraFrameCount = 0
                Application.DoEvents()
            End While
        End If

        parms.IMUpresent = camera.IMUpresent
        parms.intrinsics = camera.Intrinsics_VB
        parms.extrinsics = camera.Extrinsics_VB

        algorithmTaskHandle = New Thread(AddressOf AlgorithmTask)
        algorithmTaskHandle.Name = "AlgorithmTask"
        algorithmTaskHandle.Priority = ThreadPriority.Lowest
        algorithmTaskHandle.Start(parms)

        ActivateTimer.Enabled = True
        fpsTimer.Enabled = True
    End Sub
    Private Sub AlgorithmTask(ByVal parms As VB_Classes.ActiveClass.algorithmParameters)
        If parms.testAllRunning Then
            AlgorithmTestCount += 1
            Console.WriteLine(vbTab + "Starting " + parms.activeAlgorithm + " " + CStr(AlgorithmTestCount) + " algorithms tested.")
        End If
        drawRect = New cv.Rect
        Dim saveLowResSetting As Boolean = parms.lowResolution
        Dim OpenCVB = New VB_Classes.ActiveClass(parms)
        textDesc = OpenCVB.ocvb.desc
        ' some algorithms need to turn off the lowResolution (OpenGL apps run at full resolution.)  
        ' Here we check to see if the algorithm constructor changed lowResolution.
        If OpenCVB.ocvb.parms.lowResolution <> saveLowResSetting Then
            If OpenCVB.ocvb.parms.lowResolution Then OpenCVB.ocvb.parms.speedFactor = 2 Else OpenCVB.ocvb.parms.speedFactor = 1
            OpenCVB.ocvb.parms.width = regWidth / OpenCVB.ocvb.parms.speedFactor
            OpenCVB.ocvb.parms.height = regHeight / OpenCVB.ocvb.parms.speedFactor
            OpenCVB.ocvb.parms.imageToTrueTypeLoc = 1 / resizeForDisplay
            If OpenCVB.ocvb.parms.lowResolution Then OpenCVB.ocvb.parms.imageToTrueTypeLoc *= OpenCVB.ocvb.parms.speedFactor
        End If

        ' if the constructor for the algorithm sets the drawrect, adjust it for the ratio of the actual size and algorithm sized image.
        If OpenCVB.ocvb.drawRect <> New cv.Rect(0, 0, 0, 0) Then ' the constructor defined drawrect.  Adjust it because lowResolution selected
            drawRect = OpenCVB.ocvb.drawRect
            Dim ratio = OpenCVB.ocvb.color.Width / camPic(0).Width  ' relative size of algorithm size image to displayed image
            drawRect = New cv.Rect(drawRect.X / ratio, drawRect.Y / ratio, drawRect.Width / ratio, drawRect.Height / ratio)
        End If

        For i = VB_Classes.ActiveClass._RESULT1 To VB_Classes.ActiveClass._RESULT2
            TTtextData(i).Clear()
        Next
        BothFirstAndLastReady = False
        OpenCVB.ocvb.fontSize = GetSetting("OpenCVB", "FontSize", "FontSize", 12)
        OpenCVB.ocvb.fontName = GetSetting("OpenCVB", "FontName", "FontName", "Tahoma")

        frameCount = 0 ' restart the count... 
        While 1
            ' wait until we have the latest camera data.
            While 1
                If stopAlgorithmThread Then Exit While ' really exit the while loop below...
                Application.DoEvents() ' this will allow any options for the algorithm to be updated...
                If cameraDataUpdated Then Exit While
            End While
            If stopAlgorithmThread Then Exit While

            ' bring the data into the algorithm task.
            SyncLock camPic
                cameraDataUpdated = False
                OpenCVB.ocvb.pointCloud = formPointCloud
                OpenCVB.ocvb.color = formColor
                OpenCVB.ocvb.depthRGB = formDepthRGB
                OpenCVB.ocvb.depth = formDepth
                OpenCVB.ocvb.disparity = formDisparity
                OpenCVB.ocvb.leftView = formleftView
                OpenCVB.ocvb.rightView = formrightView
                OpenCVB.ocvb.parms.imuGyro = imuGyro
                OpenCVB.ocvb.parms.imuAccel = imuAccel
                OpenCVB.ocvb.parms.imuTimeStamp = imuTimeStamp
            End SyncLock
            OpenCVB.UpdateHostLocation(Me.Left, Me.Top, Me.Height)

            Try
                If GrabRectangleData Then
                    GrabRectangleData = False
                    Dim ratio = camPic(0).Width / OpenCVB.ocvb.color.Width ' relative size of displayed image and algorithm size image.
                    OpenCVB.ocvb.drawRect = New cv.Rect(drawRect.X / ratio, drawRect.Y / ratio, drawRect.Width / ratio, drawRect.Height / ratio)
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
                    SyncLock camPic
                        formResult1 = OpenCVB.ocvb.result1.Clone()
                        formResult2 = OpenCVB.ocvb.result2.Clone()
                        formResultsUpdated = True
                        For i = VB_Classes.ActiveClass._RESULT1 To VB_Classes.ActiveClass._RESULT2
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

        ' remove all options forms.  They can only be made topmost (see OptionsBringToFront above) when created on the same thread.
        ' This deletes the options forms for the current thread so they will be created again with the next thread.
        Dim frmlist As New List(Of Form)
        For Each frm In Application.OpenForms
            If frm.name.startswith("Option") Then frmlist.Add(frm)
        Next
        Try
            For Each frm In frmlist
                frm.Close()
            Next
        Catch ex As Exception
        End Try

        OpenCVB.Dispose()
        frameCount = 0
        If parms.testAllRunning Then
            Console.WriteLine(vbTab + "Ending " + parms.activeAlgorithm)
        End If
    End Sub
    Private Sub CameraTask(PCmultiplier As Single)
        While stopCameraThread = False
            Application.DoEvents()
            If Me.Visible And Me.IsDisposed = False Then
                Me.Invoke(Sub()
                              Me.Refresh()
                          End Sub
                )
            End If
            camera.GetNextFrame()

            If camera.color Is Nothing Then Continue While
            SyncLock camPic
                imuGyro = camera.imuGyro ' The data may not be present but just copy it...
                imuAccel = camera.imuaccel
                imuTimeStamp = camera.imutimestamp
                formPointCloud = camera.pointCloud ' the point cloud is never resized - OpenGL apps.
                If PCmultiplier <> 1 Then formPointCloud *= 0.001 ' units are millimeters for Kinect
                If lowResolution Then
                    formColor = camera.color.Resize(fastSize)
                    formDepthRGB = camera.depthRGB.Resize(fastSize)
                    formDepth = camera.depth.Resize(fastSize)
                    formDisparity = camera.disparity.Resize(fastSize)
                    formleftView = camera.leftView.Resize(fastSize)
                    formrightView = camera.rightView.Resize(fastSize)
                Else
                    formColor = camera.color
                    formDepthRGB = camera.depthRGB
                    formDepth = camera.depth
                    formDisparity = camera.disparity
                    formleftView = camera.leftView
                    formrightView = camera.rightView
                End If
                cameraDataUpdated = True
            End SyncLock

            ' If Me.IsDisposed Then Exit While
            cameraFrameCount += 1
            GC.Collect() ' minimize memory footprint - the frames have just been sent so this task isn't busy.
        End While
        cameraFrameCount = 0
        camera.dispose()
    End Sub
End Class


