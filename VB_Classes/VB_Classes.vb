Imports cv = OpenCvSharp
Imports System.Drawing
Imports System.IO
Module Algorithm_Module
    Public ocvbX As AlgorithmData
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
    Public ocvb As AlgorithmData
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
    Public Structure initParms
        Public description As String
    End Structure
    Public Structure algParms
        Dim algName As Object
        Public cameraIndex As Integer
        Dim externalPythonInvocation As Boolean
        Dim extrinsics As Extrinsics_VB
        Dim HomeDir As String
        Dim VBTestInterface As Object
        Dim IMU_Barometer As Single
        Dim IMU_Magnetometer As cv.Point3f
        Dim IMU_Present As Boolean
        Dim IMU_Temperature As Single
        Dim IMU_TimeStamp As Double
        Dim IMU_Rotation As System.Numerics.Quaternion
        Dim IMU_RotationMatrix() As Single
        Dim IMU_RotationVector As cv.Point3f
        Dim IMU_Translation As cv.Point3f
        Dim IMU_Acceleration As cv.Point3f
        Dim IMU_Velocity As cv.Point3f
        Dim IMU_AngularAcceleration As cv.Point3f
        Dim IMU_AngularVelocity As cv.Point3f
        Dim IMU_FrameTime As Double
        Dim CPU_TimeStamp As Double
        Dim CPU_FrameTime As Double
        Dim intrinsicsLeft As intrinsics_VB
        Dim intrinsicsRight As intrinsics_VB
        Dim resolution As cv.Size
        Dim minimizeMemoryFootprint As Boolean
        Dim OpenCV_Version_ID As String
        Dim OpenCVfullPath As String
        Dim PythonExe As String
        Dim ShowConsoleLog As Boolean
        Dim testAllRunning As Boolean
        Dim transformationMatrix() As Single
        Dim useRecordedData As Boolean
        Dim NumPyEnabled As Boolean

        Dim openFileDialogRequested As Boolean
        Dim openFileInitialDirectory As String
        Dim openFileFilter As String
        Dim openFileFilterIndex As Integer
        Dim openFileDialogName As String
        Dim openFileDialogTitle As String
        Dim openFileSliderPercent As Single
        Dim fileStarted As Boolean
        Dim initialStartSetting As Boolean

        Public Const Kinect4AzureCam As Int32 = 0 ' Must be defined in OptionDialog.vb the same way!
        Public Const T265Camera As Int32 = 1 ' Must be defined in OptionDialog.vb the same way!
        Public Const StereoLabsZED2 As Int32 = 2 ' Must be defined in OptionDialog.vb the same way!
        Public Const MyntD1000 As Int32 = 3 ' Must be defined in OptionDialog.vb the same way!
        Public Const D435i As Int32 = 4 ' Must be defined in OptionDialog.vb the same way!
        Public Const L515 As Int32 = 5 ' Must be defined in OptionDialog.vb the same way!
        Public Const D455 As Int32 = 6 ' Must be defined in OptionDialog.vb the same way!
    End Structure
    Public Sub New(parms As algParms, location As cv.Rect)
        Randomize() ' just in case anyone uses VB.Net's Rnd
        ocvb = New AlgorithmData(parms, location)
        ocvbX = ocvb
        UpdateHostLocation(location)
        If LCase(parms.algName).EndsWith(".py") Then ocvb.PythonFileName = parms.algName
        ocvb.PythonExe = parms.PythonExe
        ocvb.parms = parms
        algorithmObject = algoList.createAlgorithm(ocvb, parms.algName)
        If algorithmObject Is Nothing Then
            MsgBox("The algorithm: " + parms.algName + " was not found in the algorithmList.vb code." + vbCrLf +
                   "Problem likely originated with the UIindexer.")
        End If
        If algorithmObject Is Nothing And parms.algName.EndsWith(".py") Then
            parms.algName = parms.algName.Substring(0, Len(parms.algName) - 3)
            algorithmObject = algoList.createAlgorithm(ocvb, parms.algName)
        End If
        If parms.useRecordedData Then recordedData = New Replay_Play(ocvb)
        ocvb.initParms.description = algorithmObject.desc
    End Sub
    Public Sub UpdateHostLocation(location As cv.Rect)
        ocvbX.appLocation = location
    End Sub
    Public Sub RunAlgorithm()
        Try
            If ocvb.parms.useRecordedData Then
                Dim recordingFilename = New FileInfo(ocvb.parms.openFileDialogName)
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
