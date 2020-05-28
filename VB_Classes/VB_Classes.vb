Imports cv = OpenCvSharp
Imports System.Drawing

Module Algorithm_Module
    ' these are all global settings that are updated by individual algorithms.  
    Public appLocation As cv.Rect
    Public offsetIncr = 25
    Public slidersOffset As New cv.Point(10, 10)
    Public radioOffset As New cv.Point(10, 10)
    Public PipeTaskIndex As Int32
    Public vtkTaskIndex As Int32
    Public Const RESULT1 = 2 ' 0=rgb 1=depth 2=result1 3=Result2
    Public Const RESULT2 = 3 ' 0=rgb 1=depth 2=result1 3=Result2
    Public colorCols As Int32
    Public colorRows As Int32

    Public Const D400Cam As Int32 = 0 ' Must be defined in OptionsDialog.vb the same way!
    Public Const Kinect4AzureCam As Int32 = 1 ' Must be defined in OptionsDialog.vb the same way!
    Public Const T265Camera As Int32 = 2 ' Must be defined in OptionsDialog.vb the same way!
    Public Const StereoLabsZED2 As Int32 = 3 ' Must be defined in OptionsDialog.vb the same way!
    Public Const MyntD1000 As Int32 = 4 ' Must be defined in OptionsDialog.vb the same way!

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

Public Class ActiveClass : Implements IDisposable
    Public Class TrueType
        Public Const RESULT1 = 2
        Public Const RESULT2 = 3
        Public text As String

        ' Change the default TrueType font and font size here.  Any individual ocvbClass can override with a specific font if needed.
        ' To use the global TrueType font, specify _fontName = ocvb.fontName and _fontSize = ocvb.fontSize on the TrueType constructor.
        Const defaultFont = "Microsoft Sans Serif"
        Const defaultFontSize = 8

        Public fontName As String = defaultFont
        Public fontSize As Double = defaultFontSize
        Public picTag As Int32
        Public x As Int32
        Public y As Int32
        Public Sub New(_text As String, _x As Int32, _y As Int32, Optional _fontName As String = defaultFont,
                       Optional _fontSize As Double = defaultFontSize, Optional _picTag As Int32 = RESULT1)
            text = _text
            x = _x
            y = _y
            fontName = _fontName
            fontSize = _fontSize
            picTag = _picTag
        End Sub
        Public Sub New(_text As String, _x As Int32, _y As Int32, _picTag As Int32)
            text = _text
            x = _x
            y = _y
            picTag = _picTag
        End Sub

        Public Sub New(_text As String, _x As Int32, _y As Int32)
            text = _text
            x = _x
            y = _y
            picTag = RESULT1
        End Sub
    End Class

    Public ocvb As AlgorithmData
    Public Const RESULT1 = 2 ' 0=rgb 1=depth 2=result1 3=Result2
    Public Const RESULT2 = 3 ' 0=rgb 1=depth 2=result1 3=Result2

    Dim algoList As New algorithmList
    Dim ActiveAlgorithm As Object
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
    Public Structure algorithmParameters
        Dim activeAlgorithm As Object
        Dim AvoidDNNCrashes As Boolean
        Dim cameraIndex As Int32
        Dim cameraName As String
        Dim externalPythonInvocation As Boolean
        Dim extrinsics As Extrinsics_VB
        Dim HomeDir As String
        Dim VBTestInterface As Object
        Dim imageToTrueTypeLoc As Single
        Dim keyboardInput As String
        Dim keyInputAccepted As Boolean
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
        Dim lowResolution As Boolean
        Dim mainFormHeight As Int32
        Dim mainFormLoc As Point
        Dim minimizeMemoryFootprint As Boolean
        Dim OpenCV_Version_ID As String
        Dim OpenCVfullPath As String
        Dim PythonExe As String
        Dim ShowConsoleLog As Boolean
        Dim ShowOptions As Boolean
        Dim speedFactor As Int32
        Dim testAllRunning As Boolean
        Dim transformationMatrix() As Single
        Dim useRecordedData As Boolean
    End Structure
    Public Sub New(parms As algorithmParameters, _width As Integer, _height As Integer)
        Randomize() ' just in case anyone uses VB.Net's Rnd
        UpdateHostLocation(parms.mainFormLoc.X, parms.mainFormLoc.Y, parms.mainFormHeight)
        ocvb = New AlgorithmData(parms, _width, _height)
        If LCase(parms.activeAlgorithm).EndsWith(".py") Then ocvb.PythonFileName = parms.activeAlgorithm
        ocvb.PythonExe = parms.PythonExe
        ocvb.parms = parms
        colorRows = ocvb.color.Rows
        colorCols = ocvb.color.Cols
        ActiveAlgorithm = algoList.createAlgorithm(ocvb, parms.activeAlgorithm)
        If ActiveAlgorithm Is Nothing Then
            MsgBox("The algorithm: " + parms.activeAlgorithm + " was not found in the algorithmList.vb code." + vbCrLf +
                   "Review the code to determine why.")
        End If
        If ActiveAlgorithm Is Nothing And parms.activeAlgorithm.EndsWith(".py") Then
            parms.activeAlgorithm = parms.activeAlgorithm.Substring(0, Len(parms.activeAlgorithm) - 3)
            ActiveAlgorithm = algoList.createAlgorithm(ocvb, parms.activeAlgorithm)
        End If
        If parms.useRecordedData Then recordedData = New Replay_Play(ocvb, "VB_Classes.vb")
    End Sub
    Public Sub UpdateHostLocation(left As Int32, top As Int32, height As Int32)
        appLocation = New cv.Rect(left, top, 0, height)
    End Sub
    Public Sub RunAlgorithm()
        Try
            If ocvb.parms.useRecordedData Then recordedData.Run(ocvb)
            If ocvb.color IsNot Nothing And ocvb.RGBDepth IsNot Nothing Then ActiveAlgorithm.NextFrame(ocvb)
        Catch ex As Exception
            Console.WriteLine("Active Algorithm exception occurred: " + ex.Message)
        End Try
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If recordedData IsNot Nothing Then recordedData.Dispose()
        If ActiveAlgorithm IsNot Nothing Then ActiveAlgorithm.Dispose()
    End Sub
End Class
