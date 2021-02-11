Imports cv = OpenCvSharp
Imports System.IO
Imports System.Windows.Forms
Module Algorithm_Module
    Public ocvb As VBocvb
    Public task As ActiveTask
    Public aOptions As OptionsContainer
    Public Const RESULT1 = 2 ' 0=rgb 1=depth 2=result1 3=Result2
    Public Const RESULT2 = 3 ' 0=rgb 1=depth 2=result1 3=Result2
    Public PipeTaskIndex As Integer
    Public vtkTaskIndex As Integer
    Public term As New cv.TermCriteria(cv.CriteriaTypes.Eps + cv.CriteriaTypes.Count, 10, 1.0)
    Public recordedData As Replay_Play
    <System.Runtime.CompilerServices.Extension()>
    Public Sub SwapWith(Of T)(ByRef thisObj As T, ByRef withThisObj As T)
        Dim tempObj = thisObj
        thisObj = withThisObj
        withThisObj = tempObj
    End Sub
    Public Sub Swap(Of T)(ByRef a As T, ByRef b As T)
        Dim temp = b
        b = a
        a = temp
    End Sub
    Public Function findfrm(title As String) As Windows.Forms.Form
        For Each frm In Application.OpenForms
            If frm.text = title Then Return frm
        Next
        Return Nothing
    End Function
    Public Function findCheckBox(opt As String) As CheckBox
        While 1
            Try
                For Each frm In Application.OpenForms
                    If frm.text.endswith(" CheckBox Options") Then
                        For j = 0 To frm.Box.length - 1
                            If frm.Box(j).text.contains(opt) Then Return frm.Box(j)
                        Next
                    End If
                Next
            Catch ex As Exception
                Console.WriteLine("findCheckBox failed.  The application list of forms changed while iterating.  Not critical.")
            End Try
            Application.DoEvents()
            Static retryCount As Integer
            retryCount += 1
            If retryCount >= 5 Then
                MsgBox("A checkbox was not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")
                Exit While
            End If
        End While
        Return Nothing
    End Function
    Public Function findRadio(opt As String) As RadioButton
        While 1
            Try
                For Each frm In Application.OpenForms
                    If frm.text.endswith(" Radio Options") Then
                        For j = 0 To frm.check.length - 1
                            If frm.check(j).text.contains(opt) Then Return frm.check(j)
                        Next
                    End If
                Next
            Catch ex As Exception
                Console.WriteLine("findRadio failed.  The application list of forms changed while iterating.  Not critical.")
            End Try
            Application.DoEvents()
            Static retryCount As Integer
            retryCount += 1
            If retryCount >= 5 Then
                MsgBox("A findRadio was not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")
                Exit While
            End If
        End While
        Return Nothing
    End Function
    Public Sub hideForm(title As String)
        If aOptions.optionsTitle.Contains(title) Then
            For i = 0 To aOptions.optionsTitle.Count - 1
                If aOptions.optionsTitle(i) = title Then
                    aOptions.optionsTitle.RemoveAt(i)
                    Exit For
                End If
            Next
        End If
        aOptions.hiddenOptions.Add(title)
    End Sub
    Public Function findSlider(opt As String) As TrackBar
        Try
            For Each frm In Application.OpenForms
                If frm.text.endswith(" Slider Options") Then
                    For j = 0 To frm.trackbar.length - 1
                        If frm.sLabels(j).text.contains(opt) Then Return frm.trackbar(j)
                    Next
                End If
            Next
        Catch ex As Exception
            Console.WriteLine("findSlider failed.  The application list of forms changed while iterating.  Not critical.")
        End Try
        MsgBox("A slider was not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")

        Return Nothing
    End Function
End Module






Public Class ActiveTask : Implements IDisposable
    Dim algoList As New algorithmList
    Public algorithmObject As Object

    Public color As cv.Mat
    Public RGBDepth As cv.Mat
    Public result As New cv.Mat
    Public pointCloud As cv.Mat
    Public depth16 As cv.Mat
    Public leftView As cv.Mat
    Public rightView As cv.Mat
    Public viewOptions As Object
    Public PixelViewer As Object

    ' add any global option algorithms here
    Public inrange As Object
    Public depth32f As New cv.Mat
    Public depthOptionsChanged As Boolean
    Public minRangeSlider As Windows.Forms.TrackBar
    Public maxRangeSlider As Windows.Forms.TrackBar
    Public thresholdSlider As Windows.Forms.TrackBar
    Public xRotateSlider As Windows.Forms.TrackBar
    Public yRotateSlider As Windows.Forms.TrackBar
    Public zRotateSlider As Windows.Forms.TrackBar
    Public fuseSlider As Windows.Forms.TrackBar

    Public mouseClickFlag As Boolean
    Public mouseClickPoint As cv.Point
    Public mousePicTag As Integer ' which image was the mouse in?
    Public mousePoint As cv.Point ' trace any mouse movements using this.
    Public mousePointUpdated As Boolean

    Public IMU_Barometer As Single
    Public IMU_Magnetometer As cv.Point3f
    Public IMU_Temperature As Single
    Public IMU_TimeStamp As Double
    Public IMU_Rotation As System.Numerics.Quaternion
    Public IMU_Translation As cv.Point3f
    Public IMU_Acceleration As cv.Point3f
    Public IMU_Velocity As cv.Point3f
    Public IMU_AngularAcceleration As cv.Point3f
    Public IMU_AngularVelocity As cv.Point3f
    Public IMU_FrameTime As Double
    Public CPU_TimeStamp As Double
    Public CPU_FrameTime As Double

    Public openFileDialogRequested As Boolean
    Public openFileInitialDirectory As String
    Public openFileFilter As String
    Public openFileFilterIndex As Integer
    Public openFileDialogName As String
    Public openFileDialogTitle As String
    Public openFileSliderPercent As Single
    Public fileStarted As Boolean
    Public initialStartSetting As Boolean

    Public drawRect As cv.Rect ' filled in if the user draws on any of the images.
    Public drawRectClear As Boolean ' used to remove the drawing rectangle when it has been used to initialize a camshift or mean shift.
    Public drawRectUpdated As Boolean

    Public pixelViewerRect As cv.Rect

    Public label1 As String
    Public label2 As String
    Public desc As String
    Public intermediateReview As String
    Public ratioImageToCampic As Single
    Public pixelViewerOn As Boolean

    Public transformationMatrix() As Single

    Public ttTextData As New List(Of TTtext)
    Public callTrace As New List(Of String)

    Public Structure Extrinsics_VB
        Public rotation As Single()
        Public translation As Single()
    End Structure
    Public Structure intrinsics_VB
        Public ppx As Single
        Public ppy As Single
        Public fx As Single
        Public fy As Single
        Public coeffs As Single()
        Public FOV As Single()
    End Structure
    Public Structure algParms
        Public cameraName As camNames
        Enum camNames
            Kinect4AzureCam
            StereoLabsZED2
            MyntD1000
            D435i
            D455
            OakDCamera
        End Enum

        Public PythonExe As String
        Public homeDir As String
        Public useRecordedData As Boolean
        Public externalPythonInvocation As Boolean ' OpenCVB was initialized remotely...
        Public ShowConsoleLog As Boolean
        Public NumPyEnabled As Boolean
        Public testAllRunning As Boolean
        Public IMU_RotationMatrix() As Single
        Public IMU_RotationVector As cv.Point3f
        Public VTK_Present As Boolean
        Public pixelViewerOn As Boolean

        Public intrinsicsLeft As intrinsics_VB
        Public intrinsicsRight As intrinsics_VB
        Public extrinsics As Extrinsics_VB
    End Structure
    Private Sub buildColors()
        Dim vec As cv.Scalar, r As Integer = 120, b As Integer = 255, g As Integer = 0
        Dim scalarList As New List(Of cv.Scalar)
        For i = 0 To ocvb.fixedColors.Length - 1
            Select Case i Mod 3
                Case 0
                    vec = New cv.Scalar(b, g, r)
                    r = (r + 50) Mod 255
                Case 1
                    vec = New cv.Scalar(b, g, r)
                    g = (g + 75) Mod 255
                Case 2
                    vec = New cv.Scalar(b, g, r)
                    b = (b + 150) Mod 255
            End Select
            If scalarList.Contains(New cv.Scalar(b, g, r)) Then b = (b + 100) Mod 255 ' try not to have duplicates.
            If r + g + b < 180 Then r = 120 ' need bright colors.

            ocvb.scalarColors(i) = New cv.Scalar(b, g, r)
            scalarList.Add(ocvb.scalarColors(i))
        Next
        Dim msrng As New System.Random
        For i = 0 To ocvb.vecColors.Length - 1
            ocvb.vecColors(i) = New cv.Vec3b(msrng.Next(100, 255), msrng.Next(100, 255), msrng.Next(100, 255)) ' note: cannot generate black!
            ocvb.scalarColors(i) = New cv.Scalar(ocvb.vecColors(i).Item0, ocvb.vecColors(i).Item1, ocvb.vecColors(i).Item2)
        Next
    End Sub
    Public Sub New(parms As algParms, resolution As cv.Size, algName As String, camWidth As Integer, camHeight As Integer, _defaultRect As cv.Rect)
        Randomize() ' just in case anyone uses VB.Net's Rnd
        color = New cv.Mat(resolution.Height, resolution.Width, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        RGBDepth = New cv.Mat(color.Size(), cv.MatType.CV_8UC3, cv.Scalar.All(0))
        pointCloud = New cv.Mat(camHeight, camWidth, cv.MatType.CV_32FC3, cv.Scalar.All(0))
        result = New cv.Mat(color.Height, color.Width * 2, cv.MatType.CV_8UC3, cv.Scalar.All(0))

        ocvb = New VBocvb(Me)
        task = Me
        ocvb.parms = parms
        ocvb.defaultRect = _defaultRect

        buildColors()
        ocvb.algName = algName
        ocvb.pythonTaskName = ocvb.parms.homeDir + "VB_Classes\" + algName

        aOptions = New OptionsContainer
        If algName.EndsWith(".py") = False Then aOptions.Show()
        inrange = algoList.createAlgorithm("OptionsCommon_Depth")
        viewOptions = algoList.createAlgorithm("OptionsCommon_Histogram")
        PixelViewer = algoList.createAlgorithm("Pixel_Viewer")

        algorithmObject = algoList.createAlgorithm(algName)

        If algorithmObject Is Nothing Then
            MsgBox("The algorithm: " + algName + " was not found in the algorithmList.vb code." + vbCrLf +
                   "Problem likely originated with the UIindexer.")
        End If
        If parms.useRecordedData Then recordedData = New Replay_Play()

        ' https://docs.microsoft.com/en-us/azure/kinect-dk/hardware-specification
        ' https://support.stereolabs.com/hc/en-us/articles/360007395634-What-is-the-camera-focal-length-and-field-of-view-
        ' https://www.mynteye.com/pages/mynt-eye-d
        ' https://www.intelrealsense.com/depth-camera-d435i/
        ' https://www.intelrealsense.com/depth-camera-d455/
        ' order of cameras is the same as the order above...
        ' Microsoft Kinect4Azure, StereoLabs Zed 2, Mynt EyeD 1000, RealSense D435i, RealSense D455
        Dim hFOVangles() As Single = {90, 104, 105, 69.4, 86, 72} ' all values from the specification.
        Dim vFOVangles() As Single = {59, 72, 58, 42.5, 57, 81} ' all values from the specification.
        ocvb.hFov = hFOVangles(parms.cameraName)
        ocvb.vFov = vFOVangles(parms.cameraName)

        If aOptions IsNot Nothing Then aOptions.layoutOptions()

        Application.DoEvents()
    End Sub
    Public Sub RunAlgorithm()
        Try
            If ocvb.parms.useRecordedData Then
                Dim recordingFilename = New FileInfo(task.openFileDialogName)
                If ocvb.parms.useRecordedData And recordingFilename.Exists = False Then
                    ocvb.trueText("Record the file: " + recordingFilename.FullName + " first before attempting to use it in the regression tests.", 10, 125)
                    Exit Sub
                End If
                recordedData.Run()
            End If

            ' run any global options algorithms here.
            If inrange IsNot Nothing Then inrange.Run()

            algorithmObject.NextFrame()

            If ocvb.parms.VTK_Present = False And ocvb.algName.StartsWith("VTK") Then
                ocvb.trueText("VTK support is disabled. " + vbCrLf + "Instructions to enable VTK are in the Readme.md for OpenCVB")
            End If

            label1 = ocvb.label1
            label2 = ocvb.label2
            intermediateReview = task.intermediateReview
        Catch ex As Exception
            Console.WriteLine("Active Algorithm exception occurred: " + ex.Message)
        End Try
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If recordedData IsNot Nothing Then recordedData.Dispose()
        If algorithmObject IsNot Nothing Then algorithmObject.Dispose()
    End Sub
End Class