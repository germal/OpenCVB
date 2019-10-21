Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
Imports System.IO
Imports System.ComponentModel
Imports System.Threading
Imports System.Globalization
Imports System.Text.RegularExpressions
Public Class OpenCVB
#Region "Globals"
    Const displayFrames As Int32 = 4
    Dim camPic(displayFrames - 1) As PictureBox
    Dim resizeForDisplay = 2 ' indicates how much we have to resize to fit on the screen

    Dim mouseDownPoint As New cv.Point
    Dim mouseMovePoint As New cv.Point
    Dim DrawingRectangle As Boolean
    Dim BothFirstAndLastReady As Boolean
    Dim LastX As Int32
    Dim LastY As Int32
    Dim GrabRectangleData As Boolean
    Dim drawRect As New cv.Rect(0, 0, 0, 0)

    Dim frameCount As Int32
    Dim algorithmThread As Thread = Nothing
    Dim stopAlgorithmThread As Boolean
    Dim pauseUpdates As Boolean

    Dim RefreshAvailable As Boolean = True ' This variable allows us to dodge a refresh from the system after a move.  There is no synclock around that system refresh.

    Dim picLabels() = {"RGB", "Depth", "Result1", "Result2"}
    Dim myPen As New System.Drawing.Pen(System.Drawing.Color.White)
    Dim myBrush = New SolidBrush(System.Drawing.Color.White)
    Dim mousePoint As New cv.Point
    Dim mouseClickPoint As New cv.Point
    Dim mouseClickFlag As Boolean
    Dim mousePicTag As Int32
    Dim kinect As Kinect
    Dim border As Int32 = 6
    Dim TTtextData(displayFrames - 1) As List(Of VB_Classes.ActiveClass.TrueType)
    Dim formColor As cv.Mat, formDepRGB As cv.Mat, formResult1 As cv.Mat, formResult2 As cv.Mat
    Dim CodeLineCount As Int32
    Dim AlgorithmCount As Int32
    Dim OpenCVfullPath As String
    Dim vtkDirectory As String = ""
    Dim openCVKeywords As New List(Of String)
    Dim OptionsBringToFront As Boolean
    Dim regWidth As Int32 = 1280, regHeight As Int32 = 720
    Dim intelCamera As Object = Nothing
    Dim kinectCamera As Object = Nothing
    Dim optionsForm As OptionsDialog
    Dim textDesc As String = ""
    Dim externalInvocation As Boolean
    Dim HomeDir As DirectoryInfo
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
        OpenCVfullPath = HomeDir.FullName + "OpenCV\Build\bin\Release\"
        updatePath(OpenCVfullPath, "OpenCV and OpenCV Contrib are needed for C++ classes.")

        OpenCVfullPath = HomeDir.FullName + "OpenCV\Build\bin\debug\"
        updatePath(OpenCVfullPath, "OpenCV and OpenCV Contrib are needed for C++ classes.")

        Dim IntelPERC_Lib_Dir = HomeDir.FullName + "librealsense\build\Release\"
        updatePath(IntelPERC_Lib_Dir, "Realsense camera support.")

        IntelPERC_Lib_Dir = HomeDir.FullName + "librealsense\build\Debug\"
        updatePath(IntelPERC_Lib_Dir, "Realsense camera support.")

        Dim Kinect_Dir = HomeDir.FullName + "Azure-Kinect-Sensor-SDK\build\bin\Release\"
        updatePath(Kinect_Dir, "Kinect camera support.")

        Kinect_Dir = HomeDir.FullName + "Azure-Kinect-Sensor-SDK\build\bin\Debug\"
        updatePath(Kinect_Dir, "Kinect camera support.")

        ' the depthEngine DLL is not included in the SDK.  It is distributed separately because it is NOT open source.
        ' The depthEngine DLL is supposed to be installed in C:\Program Files\Azure Kinect SDK v1.1.0\sdk\windows-desktop\amd64\$(Configuration)
        ' Post an issue if this Is Not a valid assumption
        Dim kinectDLL As New FileInfo("C:/Program Files/Azure Kinect SDK v1.3.0/tools/depthengine_2_0.dll")
        If kinectDLL.Exists = False Then
            MsgBox("The Microsoft installer for the Kinect camera proprietary portion was not installed in the right place (or it has changed.)" + vbCrLf +
                "It was expected to be in " + kinectDLL.FullName + vbCrLf + "Update the code and restart.")
        End If
        updatePath(kinectDLL.Directory.FullName, "Kinect depth engine dll.")
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture

        optionsForm = New OptionsDialog
        optionsForm.Dialog1_Load(sender, e)

        setupCamPicsAndCameras()
        loadAlgorithmComboBoxes()

        TestAllTimer.Interval = optionsForm.TestAllDuration.Text * 1000
        FindPython()
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
    Private Function SetupIntelCamera(fps As Int32, recordingFile As String, playbackFile As String) As Object
        Dim fileInfo As FileInfo = Nothing
        If recordingFile <> "" Then
            fileInfo = New FileInfo(recordingFile)
            recordingFile = fileInfo.FullName ' better looking name for debugging
        End If
        If playbackFile <> "" Then
            fileInfo = New FileInfo(playbackFile)
            playbackFile = fileInfo.FullName ' better looking name for debugging
        End If

        Dim camera = New IntelD4xx(regWidth, regHeight, 30, "", "")

        If camera.deviceCount = 0 Then Return camera
        camera.DecimationFilter = optionsForm.DecimationFilter.Checked
        camera.ThresholdFilter = optionsForm.ThresholdFilter.Checked
        camera.DepthToDisparity = optionsForm.DepthToDisparity.Checked
        camera.SpatialFilter = optionsForm.SpatialFilter.Checked
        camera.TemporalFilter = optionsForm.TemporalFilter.Checked
        camera.HoleFillingFilter = optionsForm.HoleFillingFilter.Checked
        camera.DisparityToDepth = optionsForm.DisparityToDepth.Checked

        Dim OptionsHeight = 300
        If Screen.PrimaryScreen.Bounds.Height < regHeight * 2 + OptionsHeight Or Screen.PrimaryScreen.Bounds.Width < regWidth * 2 Then
            If regWidth <> 1280 Then resizeForDisplay = 4
        End If
        Return camera
    End Function
    Private Sub setupCamPicsAndCameras()
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

        If intelCamera Is Nothing Then intelCamera = SetupIntelCamera(30, "", "")
        Dim usingIntelCamera As Boolean = optionsForm.IntelCamera.Checked
        If intelCamera.deviceCount = 0 Then usingIntelCamera = False ' well, it has to be a Kinect system then.
        If kinectCamera Is Nothing Then kinectCamera = New Kinect()
        If usingIntelCamera = False And kinectCamera.devicecount = 0 Then usingIntelCamera = True
        If kinectCamera.deviceCount = 0 And intelCamera.deviceCount = 0 Then
            MsgBox("OpenCVB supports either a Kinect for Azure 3D camera or an Intel D4xx 3D camera.  Neither is present.")
            End
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
        ' Anaconda is distributed with Visual Studio.  That is the default Python.exe.  Override with the Options dialog.
        Dim pythonFileInfo As New FileInfo("C:\Program Files (x86)\Microsoft Visual Studio\Shared\Anaconda3_64\Python.exe")
        If pythonFileInfo.Exists Then
            If optionsForm.PythonExeName.Text = "" Then SaveSetting("OpenCVB", "PythonExe", "PythonExe", pythonFileInfo.FullName)
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
            stopAlgorithmThread = True
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
        Debug.WriteLine("system path = " + systemPath)
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
                Dim src = Choose(pic.Tag + 1, formColor, formDepRGB, formResult1, formResult2)

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
    Private Sub drawRegionOfInterest(g As Graphics, r As cv.Rect)
    End Sub
    Private Sub campic_DoubleClick(sender As Object, e As EventArgs)
        DrawingRectangle = False
    End Sub
    Private Sub campic_Click(sender As Object, e As EventArgs)
        mouseClickFlag = True
        mouseClickPoint = mousePoint
    End Sub
    Private Sub campic_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        Try
            SyncLock camPic ' avoid updating the image while copying into it in the algorithm task
                If formColor IsNot Nothing Then
                    cvext.BitmapConverter.ToBitmap(formColor, camPic(0).Image)
                    cvext.BitmapConverter.ToBitmap(formDepRGB, camPic(1).Image)
                    cvext.BitmapConverter.ToBitmap(formResult1, camPic(2).Image)
                    cvext.BitmapConverter.ToBitmap(formResult2, camPic(3).Image)
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

    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
        Dim res() As String = GetType(OpenCVB).Assembly.GetManifestResourceNames()
        If pauseUpdates Then
            pauseUpdates = False
            ToolStripButton1.Image = New System.Drawing.Bitmap(GetType(OpenCVB).Assembly.GetManifestResourceStream(res(3)))
        Else
            pauseUpdates = True
            ToolStripButton1.Image = New System.Drawing.Bitmap(GetType(OpenCVB).Assembly.GetManifestResourceStream(res(4)))
        End If
    End Sub
    Private Sub testAllButton_Click(sender As Object, e As EventArgs) Handles testAllButton.Click
        If testAllButton.Text = "Test All" Then
            testAllButton.Text = "Stop Test"
            TestAllTimer_Tick(sender, e)
            TestAllTimer.Enabled = True
        Else
            TestAllTimer.Enabled = False
            testAllButton.Text = "Test All"
        End If
    End Sub
    Private Sub opencvkeyword_dropdown(sender As Object, e As EventArgs) Handles OpenCVkeyword.DropDown
        pauseUpdates = True
    End Sub
    Private Sub opencvkeyword_dropdownclosed(sender As Object, e As EventArgs) Handles OpenCVkeyword.DropDownClosed
        pauseUpdates = False
    End Sub
    Private Sub algorithms_dropdown(sender As Object, e As EventArgs) Handles AvailableAlgorithms.DropDown
        pauseUpdates = True
    End Sub
    Private Sub algorithms_dropdownclosed(sender As Object, e As EventArgs) Handles AvailableAlgorithms.DropDownClosed
        pauseUpdates = False
    End Sub
    Private Sub TestAllTimer_Tick(sender As Object, e As EventArgs) Handles TestAllTimer.Tick
        stopAlgorithmThread = True
        If AvailableAlgorithms.SelectedIndex < AvailableAlgorithms.Items.Count - 1 Then AvailableAlgorithms.SelectedIndex += 1 Else AvailableAlgorithms.SelectedIndex = 0
    End Sub
    Private Sub MainFrm_Move(sender As Object, e As EventArgs) Handles Me.Move
        RefreshAvailable = False
        ActivateTimer.Enabled = True
    End Sub
    Private Sub OpenCVB_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        OptionsBringToFront = True
    End Sub
    Dim savePauseState As Boolean
    Private Sub OpenCVB_ResizeBegin(sender As Object, e As EventArgs) Handles Me.ResizeBegin
        savePauseState = pauseUpdates
        pauseUpdates = True
    End Sub
    Private Sub OpenCVB_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd
        saveLayout()
        pauseUpdates = savePauseState
    End Sub
    Private Sub Algorithms_SelectedIndexChanged(sender As Object, e As EventArgs) Handles AvailableAlgorithms.SelectedIndexChanged
        If AvailableAlgorithms.Enabled Then
            stopAlgorithmThread = True
            SaveSetting("OpenCVB", OpenCVkeyword.Text, OpenCVkeyword.Text, AvailableAlgorithms.Text)
            Application.DoEvents() ' update the visible name.
            RunAlgorithmTask()
        End If
    End Sub
    Private Sub ActivateTimer_Tick(sender As Object, e As EventArgs) Handles ActivateTimer.Tick
        ActivateTimer.Enabled = False
        If testAllButton.Text <> "Stop Test" Then Me.Activate()
        RefreshAvailable = True
    End Sub
    Private Sub fpsTimer_Tick(sender As Object, e As EventArgs) Handles fpsTimer.Tick
        Static lastFrame
        Dim countFrames = frameCount - lastFrame
        If countFrames < 0 Then countFrames = frameCount ' started a new algorithm!
        Dim fps As Single = countFrames / (fpsTimer.Interval / 1000)

        Dim activeCameraName As String
        If optionsForm.IntelCamera.Checked Then activeCameraName = intelCamera.deviceName Else activeCameraName = kinectCamera.deviceName
        Me.Text = "OpenCVB (" + CStr(AlgorithmCount) + " algorithms " + Format(CodeLineCount, "###,##0") + " lines) - fps = " +
                  Format(fps, "#0.0") + " " + activeCameraName
        lastFrame = frameCount
        If AlgorithmDesc.Text = "" Then AlgorithmDesc.Text = textDesc
    End Sub
    Private Sub Options_Click(sender As Object, e As EventArgs) Handles Options.Click
        pauseUpdates = True
        optionsForm.IntelCamera.Enabled = intelCamera.deviceCount > 0
        optionsForm.Kinect4Azure.Enabled = kinectCamera.deviceCount > 0

        Dim cancel = optionsForm.ShowDialog()
        Me.BringToFront()
        pauseUpdates = False
        If cancel = DialogResult.OK Then stopAlgorithmThread = True

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
        RunAlgorithmTask()
    End Sub
    Private Sub saveLayout()
        SaveSetting("OpenCVB", "OpenCVBLeft", "OpenCVBLeft", Me.Left)
        SaveSetting("OpenCVB", "OpenCVBTop", "OpenCVBTop", Me.Top)
        SaveSetting("OpenCVB", "OpenCVBWidth", "OpenCVBWidth", Me.Width)
        SaveSetting("OpenCVB", "OpenCVBHeight", "OpenCVBHeight", Me.Height)

        Dim details = CStr(regWidth) + "x" + CStr(regHeight) + " display " + CStr(camPic(0).Width) + "x" + CStr(camPic(0).Height) + " FastProcessing="
        If optionsForm.FastProcessing.Checked Then details += "On" Else details += "Off"
        picLabels(0) = "Input " + details
        picLabels(1) = "Depth " + details
    End Sub
    Private Sub MainFrm_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        stopAlgorithmThread = True
        textDesc = ""
        saveLayout()
        Dim sleepCount As Int32
        ' allow time for the runAlgorithmTask to exit
        While frameCount
            Application.DoEvents()
            sleepCount += 1
            If sleepCount > 50 Then Exit While
        End While
    End Sub
    Private Sub RunAlgorithmTask()
        ActivateTimer.Enabled = True
        fpsTimer.Enabled = True

        Dim parms As New VB_Classes.ActiveClass.algorithmParameters
        parms.minimizeMemoryFootprint = optionsForm.MinimizeMemoryFootprint.Checked
        parms.fastProcessing = optionsForm.FastProcessing.Checked
        parms.activeAlgorithm = AvailableAlgorithms.Text

        ' opengl algorithms are only to be run at full resolution.  All other algorithms respect the options setting...
        If parms.activeAlgorithm.Contains("OpenGL") Or parms.activeAlgorithm.Contains("OpenCVGL") Then
            If parms.fastProcessing Then parms.fastProcessing = False
        End If

        parms.PythonExe = optionsForm.PythonExeName.Text
        parms.vtkDirectory = vtkDirectory
        parms.HomeDir = HomeDir.FullName
        parms.OpenCVfullPath = OpenCVfullPath
        parms.UsingIntelCamera = optionsForm.IntelCamera.Checked
        parms.mainFormLoc = Me.Location
        parms.mainFormHeight = Me.Height
        parms.dataPath = CurDir() + "/../../Data/"
        parms.OpenCV_Version_ID = Environment.GetEnvironmentVariable("OpenCV_Version")
        parms.useRecordedData = OpenCVkeyword.Text = "<All using recorded data>"
        Dim camera As Object = Nothing
        If parms.UsingIntelCamera Then camera = intelCamera Else camera = kinectCamera
        parms.IMUpresent = camera.IMUpresent
        parms.pcBufferSize = camera.pcBufferSize
        parms.intrinsics = camera.Intrinsics_VB
        parms.extrinsics = camera.Extrinsics_VB
        parms.imageToTrueTypeLoc = 1 / resizeForDisplay
        parms.imuGyro = camera.imuGyro
        parms.imuAccel = camera.imuAccel
        parms.imuTimeStamp = camera.imuTimeStamp
        parms.testAllRunning = testAllButton.Text = "Stop Test"
        parms.externalInvocation = externalInvocation
        If parms.testAllRunning Then parms.ShowOptions = optionsForm.ShowOptions.Checked Else parms.ShowOptions = True ' always show options when not running 'test all'
        parms.ShowConsoleLog = optionsForm.ShowConsoleLog.Checked

        If parms.fastProcessing Then parms.speedFactor = 2 Else parms.speedFactor = 1
        parms.width = regWidth / parms.speedFactor
        parms.height = regHeight / parms.speedFactor
        If parms.fastProcessing Then parms.imageToTrueTypeLoc *= parms.speedFactor

        AlgorithmDesc.Text = ""

        While frameCount <> 0 ' previous thread must exit...
            Application.DoEvents()
        End While
        algorithmThread = New Thread(AddressOf AlgorithmTask)
        algorithmThread.Start(parms)
    End Sub

    Private Sub AlgorithmTask(ByVal parms As VB_Classes.ActiveClass.algorithmParameters)
        drawRect = New cv.Rect(0, 0, 0, 0)
        Dim saveFastProc As Boolean = parms.fastProcessing
        Dim OpenCVB = New VB_Classes.ActiveClass(parms)
        textDesc = OpenCVB.ocvb.desc
        ' some algorithms need to turn off the fastprocessing (OpenGL apps usually run at full resolution.)  Check to see if it changed here and setup related settings.
        If OpenCVB.ocvb.parms.fastProcessing <> saveFastProc Then
            If OpenCVB.ocvb.parms.fastProcessing Then OpenCVB.ocvb.parms.speedFactor = 2 Else OpenCVB.ocvb.parms.speedFactor = 1
            OpenCVB.ocvb.parms.width = regWidth / OpenCVB.ocvb.parms.speedFactor
            OpenCVB.ocvb.parms.height = regHeight / OpenCVB.ocvb.parms.speedFactor
            OpenCVB.ocvb.parms.imageToTrueTypeLoc = 1 / resizeForDisplay
            If OpenCVB.ocvb.parms.fastProcessing Then OpenCVB.ocvb.parms.imageToTrueTypeLoc *= OpenCVB.ocvb.parms.speedFactor
        End If

        Dim camera As Object = Nothing
        If OpenCVB.ocvb.parms.UsingIntelCamera Then camera = intelCamera Else camera = kinectCamera

        ' if the constructor for the algorithm sets the drawrect, adjust it for the ratio of the actual size and algorithm sized image.
        If OpenCVB.ocvb.drawRect <> New cv.Rect(0, 0, 0, 0) Then ' the constructor defined drawrect.  Adjust it because fastProcessing selected
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
        stopAlgorithmThread = False
        While 1
            Application.DoEvents() ' this will permit any options forms in the algorithm thread to get updated and the pull-downs in the main thread to react quickly.
            If stopAlgorithmThread Then Exit While
            If pauseUpdates Then Continue While
            If Me.Visible Then
                Me.Invoke(Sub()
                              Me.Refresh()
                          End Sub)
            End If

            camera.GetNextFrame()
            If camera.color Is Nothing Then Continue While ' at startup it may not be ready...
            OpenCVB.ocvb.pointCloud = camera.pointCloud ' the point cloud is never resized.
            If OpenCVB.ocvb.parms.fastProcessing Then
                OpenCVB.ocvb.color = camera.color.Resize(New cv.Size(regWidth / 2, regHeight / 2))
                OpenCVB.ocvb.depthRGB = camera.depthRGB.Resize(New cv.Size(regWidth / 2, regHeight / 2))
                OpenCVB.ocvb.depth = camera.depth.Resize(New cv.Size(regWidth / 2, regHeight / 2))
                OpenCVB.ocvb.disparity = camera.disparity.Resize(New cv.Size(regWidth / 2, regHeight / 2))
                OpenCVB.ocvb.redLeft = camera.redLeft.Resize(New cv.Size(regWidth / 2, regHeight / 2))
                OpenCVB.ocvb.redRight = camera.redRight.Resize(New cv.Size(regWidth / 2, regHeight / 2))
            Else
                OpenCVB.ocvb.color = camera.color
                OpenCVB.ocvb.depthRGB = camera.depthRGB
                OpenCVB.ocvb.depth = camera.depth
                OpenCVB.ocvb.disparity = camera.disparity
                OpenCVB.ocvb.redLeft = camera.redLeft
                OpenCVB.ocvb.redRight = camera.redRight
            End If

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

                OpenCVB.RunAlgorithmTask()

                picLabels(2) = OpenCVB.ocvb.label1
                picLabels(3) = OpenCVB.ocvb.label2
                If RefreshAvailable Then
                    SyncLock camPic
                        formColor = OpenCVB.ocvb.color.Resize(New cv.Size(camPic(0).Size.Width, camPic(0).Size.Height))
                        formDepRGB = OpenCVB.ocvb.depthRGB.Resize(New cv.Size(camPic(0).Size.Width, camPic(0).Size.Height))
                        formResult1 = OpenCVB.ocvb.result1.Resize(New cv.Size(camPic(2).Size.Width, camPic(2).Size.Height))
                        formResult2 = OpenCVB.ocvb.result2.Resize(New cv.Size(camPic(2).Size.Width, camPic(2).Size.Height))
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
                    If Me.IsDisposed Then Exit While
                    If OptionsBringToFront And TestAllTimer.Enabled = False Then
                        Try
                            For Each frm In Application.OpenForms
                                If frm.name.startswith("Option") Then frm.topmost = True
                            Next
                            Application.DoEvents()
                            For Each frm In Application.OpenForms
                                If frm.name.startswith("Option") Then frm.topmost = False
                            Next
                        Catch ex As Exception ' ignoring exceptions here.  It is a transition to another class and form was activated...
                            Console.WriteLine("Error in OptionsBringToFront: " + ex.Message)
                        End Try
                        OptionsBringToFront = False
                    End If
                End If
            Catch ex As Exception
                ' MsgBox("hit error = '" + ex.Message + "' in algorithmThread.  Select algorithm again to restart.")
                Exit While
            End Try
            If OpenCVB.ocvb.parms.minimizeMemoryFootprint Then GC.Collect() ' minimize memory footprint - helps to slow runaway memory usage.  

            frameCount += 1
        End While
        OpenCVB.Dispose()
        frameCount = 0
    End Sub
End Class
