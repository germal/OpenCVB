Imports cv = OpenCvSharp
Imports System.IO
Imports System.Windows.Forms
Module Algorithm_Module
    Public optionLocation As cv.Point
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
        Public cameraIndex As Integer
        Public PythonExe As String
        Public useRecordedData As Boolean
        Public externalPythonInvocation As Boolean ' OpenCVB was initialized remotely...
        Public ShowConsoleLog As Boolean
        Public NumPyEnabled As Boolean
        Public testAllRunning As Boolean
        Public IMU_RotationMatrix() As Single
        Public IMU_Present As Boolean
        Public IMU_RotationVector As cv.Point3f

        Public intrinsicsLeft As intrinsics_VB
        Public intrinsicsRight As intrinsics_VB
        Public extrinsics As Extrinsics_VB

        Public Const Kinect4AzureCam As Integer = 0
        Public Const T265Camera As Integer = 1
        Public Const StereoLabsZED2 As Integer = 2
        Public Const MyntD1000 As Integer = 3
        Public Const D435i As Integer = 4
        Public Const L515 As Integer = 5
        Public Const D455 As Integer = 6
    End Structure
    Private Sub buildColors(ocvb As VBocvb)
        Dim vec As cv.Scalar, r As Integer = 120, b As Integer = 255, g As Integer = 0
        Dim scalarList As New List(Of cv.Scalar)
        For i = 0 To ocvb.scalarColors.Length - 1
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
    End Sub
    Private Sub layoutOptions(mainLocation As cv.Rect)
        Dim sliderOffset As New cv.Point(mainLocation.Left, mainLocation.Top + mainLocation.Height)
        Dim otherOffset As New cv.Point(mainLocation.Left + mainLocation.Width / 2, mainLocation.Top + mainLocation.Height)
        Dim offset = 30
        Try
            Dim indexS As Integer = 0
            Dim indexO As Integer = 0
            For Each frm In Application.OpenForms
                If frm.name.startswith("OptionsSliders") Or frm.name.startswith("OptionsKeyboardInput") Or frm.name.startswith("OptionsAlphaBlend") Then
                    If frm.visible Then
                        frm.SetDesktopLocation(sliderOffset.X + indexS * offset, sliderOffset.Y + indexS * offset)
                        indexS += 1
                    End If
                End If
                If frm.name.startswith("OptionsRadioButtons") Or frm.name.startswith("OptionsCheckbox") Or frm.name.startswith("OptionsCombo") Then
                    If frm.visible Then
                        frm.SetDesktopLocation(otherOffset.X + indexO * offset, otherOffset.Y + indexO * offset)
                        indexO += 1
                    End If
                End If
            Next
        Catch ex As Exception
            Console.WriteLine("Error in layoutOptions: " + ex.Message)
        End Try
    End Sub
    Public Sub New(parms As algParms, resolution As cv.Size, algName As String, homeDir As String, location As cv.Rect)
        optionLocation = New cv.Point(location.X, location.Y + location.Height)
        Randomize() ' just in case anyone uses VB.Net's Rnd
        ocvb = New VBocvb(resolution, parms, location)
        ocvb.testAllRunning = parms.testAllRunning
        If LCase(algName).EndsWith(".py") Then ocvb.PythonFileName = algName
        ocvb.PythonExe = parms.PythonExe
        ocvb.HomeDir = homeDir
        ocvb.parms = parms
        buildColors(ocvb)
        algorithmObject = algoList.createAlgorithm(ocvb, algName)
        If algorithmObject Is Nothing Then
            MsgBox("The algorithm: " + algName + " was not found in the algorithmList.vb code." + vbCrLf +
                   "Problem likely originated with the UIindexer.")
        End If
        If parms.useRecordedData Then recordedData = New Replay_Play(ocvb)
        ocvb.description = algorithmObject.desc
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
