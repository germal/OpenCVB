Imports cv = OpenCvSharp
Imports System.IO
Module Algorithm_Module
    Public appLocation As cv.Rect
    Public radioOffset As cv.Point
    Public slidersOffset As cv.Point
    ' these are all global settings that are updated by individual algorithms.  
    Public Const offsetIncr = 25
    Public Const offsetMax = 150
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
        Public cameraIndex As Integer
        Public PythonExe As String
        Public useRecordedData As Boolean
        Public externalPythonInvocation As Boolean
        Public ShowConsoleLog As Boolean
        Public NumPyEnabled As Boolean
        Public intrinsicsLeft As intrinsics_VB
        Public intrinsicsRight As intrinsics_VB
        Public extrinsics As Extrinsics_VB
        Public testAllRunning As Boolean
        Public IMU_RotationMatrix() As Single
        Public IMU_Present As Boolean
        Public IMU_RotationVector As cv.Point3f

        Public Const Kinect4AzureCam As Int32 = 0
        Public Const T265Camera As Int32 = 1
        Public Const StereoLabsZED2 As Int32 = 2
        Public Const MyntD1000 As Int32 = 3
        Public Const D435i As Int32 = 4
        Public Const L515 As Int32 = 5
        Public Const D455 As Int32 = 6
    End Structure
    Public Sub New(parms As algParms, resolution As cv.Size, algName As String, homeDir As String, location As cv.Rect)
        Randomize() ' just in case anyone uses VB.Net's Rnd
        ocvb = New VBocvb(resolution, parms, location)
        ocvb.testAllRunning = parms.testAllRunning
        UpdateHostLocation(location)
        If LCase(algName).EndsWith(".py") Then ocvb.PythonFileName = algName
        ocvb.PythonExe = parms.PythonExe
        ocvb.HomeDir = homeDir
        ocvb.parms = parms
        algorithmObject = algoList.createAlgorithm(ocvb, algName)
        If algorithmObject Is Nothing Then
            MsgBox("The algorithm: " + algName + " was not found in the algorithmList.vb code." + vbCrLf +
                   "Problem likely originated with the UIindexer.")
        End If
        If parms.useRecordedData Then recordedData = New Replay_Play(ocvb)
        ocvb.description = algorithmObject.desc
    End Sub
    Public Sub UpdateHostLocation(location As cv.Rect)
        appLocation = location
        radioOffset = New cv.Point
        slidersOffset = New cv.Point
    End Sub
    Public Sub RunAlgorithm()
        Try
            If ocvb.parms.useRecordedData Then
                Dim recordingFilename = New FileInfo(ocvb.openFileDialogName)
                If ocvb.parms.useRecordedData And recordingFilename.Exists = False Then
                    ocvb.trueText(New TTtext("Record the file: " + recordingFilename.FullName + " first before attempting to use it in the regression tests.", 10, 125))
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
