Imports cv = OpenCvSharp
Imports System.IO
Imports System.Windows.Forms
Module Algorithm_Module
    Public ocvbx As VBocvb
    Public aOptions As aOptionsFrm
    Public Const RESULT1 = 2 ' 0=rgb 1=depth 2=result1 3=Result2
    Public Const RESULT2 = 3 ' 0=rgb 1=depth 2=result1 3=Result2
    Public PipeTaskIndex As Integer
    Public vtkTaskIndex As Integer
    Public term As New cv.TermCriteria(cv.CriteriaType.Eps + cv.CriteriaType.Count, 10, 1.0)
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
End Module
Public Class ActiveTask : Implements IDisposable
    Public ocvb As VBocvb
    Dim algoList As New algorithmList
    Dim algorithmObject As Object
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
        ' these are parameters needed early in the task initialization, either by the algorithm constructor or the VBparent initialization or
        ' one-time only constants needed by the algorithms.
        Public cameraName As camNames
        Enum camNames
            Kinect4AzureCam
            StereoLabsZED2
            MyntD1000
            D435i
            D455
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

        Public intrinsicsLeft As intrinsics_VB
        Public intrinsicsRight As intrinsics_VB
        Public extrinsics As Extrinsics_VB
    End Structure
    Private Sub buildColors(ocvb As VBocvb)
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
    Private Sub layoutOptions(mainLocation As cv.Rect)
        Dim sliderOffset As New cv.Point(mainLocation.Left, mainLocation.Top + mainLocation.Height)
        Dim otherOffset As New cv.Point(mainLocation.Left + mainLocation.Width / 2, mainLocation.Top + mainLocation.Height)
        Try
            Dim indexS As Integer = 0
            Dim indexO As Integer = 0
            For Each frm In Application.OpenForms
                If frm.name.startswith("OptionsSliders") Or frm.name.startswith("OptionsKeyboardInput") Or frm.name.startswith("OptionsAlphaBlend") Then
                    If frm.visible Then
                        Try
                            frm.SetDesktopLocation(sliderOffset.X + indexS * ocvb.optionsOffset, sliderOffset.Y + indexS * ocvb.optionsOffset)
                        Catch ex As Exception

                        End Try
                        indexS += 1
                    End If
                End If
                If frm.name.startswith("OptionsRadioButtons") Or frm.name.startswith("OptionsCheckbox") Or frm.name.startswith("OptionsCombo") Then
                    If frm.visible Then
                        frm.SetDesktopLocation(otherOffset.X + indexO * ocvb.optionsOffset, otherOffset.Y + indexO * ocvb.optionsOffset)
                        indexO += 1
                    End If
                End If
            Next
        Catch ex As Exception
            Console.WriteLine("Error in layoutOptions: " + ex.Message)
        End Try
    End Sub
    Public Sub New(parms As algParms, resolution As cv.Size, algName As String, location As cv.Rect, camWidth As Integer, camHeight As Integer)
        Randomize() ' just in case anyone uses VB.Net's Rnd
        ocvb = New VBocvb(resolution, parms, location, camWidth, camHeight)
        If LCase(algName).EndsWith(".py") Then ocvb.PythonFileName = algName
        ocvb.mainLocation = location
        ocvb.optionsOffset = 30
        ocvb.parms = parms
        buildColors(ocvb)
        algorithmObject = algoList.createAlgorithm(ocvb, algName)
        If algorithmObject Is Nothing Then
            MsgBox("The algorithm: " + algName + " was not found in the algorithmList.vb code." + vbCrLf +
                   "Problem likely originated with the UIindexer.")
        End If
        If parms.useRecordedData Then recordedData = New Replay_Play(ocvb)

        ' https://docs.microsoft.com/en-us/azure/kinect-dk/hardware-specification
        ' https://support.stereolabs.com/hc/en-us/articles/360007395634-What-is-the-camera-focal-length-and-field-of-view-
        ' https://www.mynteye.com/pages/mynt-eye-d
        ' https://www.intelrealsense.com/depth-camera-d435i/
        ' https://www.intelrealsense.com/depth-camera-d455/
        ' order of cameras is the same as the order above...
        ' Microsoft Kinect4Azure, StereoLabs Zed 2, Mynt EyeD 1000, RealSense D435i, RealSense D455
        Dim hFOVangles() As Single = {90, 104, 105, 69.4, 86} ' all values from the specification.
        Dim vFOVangles() As Single = {59, 72, 58, 42.5, 57} ' all values from the specification.

        ocvb.hFov = hFOVangles(parms.cameraName)
        ocvb.vFov = vFOVangles(parms.cameraName)

        ocvbx = ocvb
        layoutOptions(location)
    End Sub
    Public Sub RunAlgorithm()
        Try
            If ocvb.parms.useRecordedData Then
                Dim recordingFilename = New FileInfo(ocvb.openFileDialogName)
                If ocvb.parms.useRecordedData And recordingFilename.Exists = False Then
                    ocvb.trueText("Record the file: " + recordingFilename.FullName + " first before attempting to use it in the regression tests.", 10, 125)
                    Exit Sub
                End If
                recordedData.Run(ocvb)
            End If
            algorithmObject.NextFrame(ocvb)
        Catch ex As Exception
            Console.WriteLine("Active Algorithm exception occurred: " + ex.Message)
        End Try
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If recordedData IsNot Nothing Then recordedData.Dispose()
        If algorithmObject IsNot Nothing Then algorithmObject.Dispose()
    End Sub
End Class