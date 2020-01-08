Imports cv = OpenCvSharp
Imports System.Drawing

Module VB_Classes
    Public appLocation As cv.Rect
    Public offsetIncr = 25
    Public slidersOffset As cv.Point
    Public radioOffset As cv.Point
    Public PipeTaskIndex As Int32
    Public vtkTaskIndex As Int32
    Public Const FRAME_RGB = 0
    Public Const FRAME_DEPTH = 1
    Public Const RESULT1 = 2
    Public Const RESULT2 = 3
    Public term As New cv.TermCriteria(cv.CriteriaType.Eps + cv.CriteriaType.Count, 10, 1.0)
    Public recordedData As Replay_Play
    Public Sub MakeSureImage8uC3(ByRef src As cv.Mat)
        If src.Type = cv.MatType.CV_32F Then
            ' it must be a 1 channel 32f image so convert it to 8-bit and let it get converted to RGB below
            src = src.Normalize(0, 255, cv.NormTypes.MinMax)
            src.ConvertTo(src, cv.MatType.CV_8UC1)
        End If
        If src.Channels = 1 And src.Type = cv.MatType.CV_8UC1 Then
            src = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        End If
    End Sub

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
    Public ocvb As AlgorithmData
    Public Const _RESULT1 = RESULT1
    Public Const _RESULT2 = RESULT2
    Public Class TrueType
        Public text As String

        ' Change the default TrueType font and font size here.  Any individual VB_Class can override with a specific font if needed.
        ' To use the global TrueType font, specify _fontName = ocvb.fontName and _fontSize = ocvb.fontSize on the TrueType constructor.
        Const defaultFont = "Microsoft Sans Serif"
        Const defaultFontSize = 8

        Public fontName As String = defaultFont
        Public fontSize As Double = defaultFontSize
        Public picTag As Int32
        Public x As Int32
        Public y As Int32
        Public Sub New(_text As String, _x As Int32, _y As Int32, Optional _fontName As String = defaultFont, Optional _fontSize As Double = defaultFontSize, Optional _picTag As Int32 = _RESULT1)
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
            picTag = _RESULT1
        End Sub
    End Class
    Dim algoList As New algorithmList
    Dim ActiveAlgorithm As Object
    Public Structure Extrinsics_VB
        Public rotation As Single()
        Public translation As Single()
    End Structure
    Public Structure Intrinsics_VB
        Public width As Integer
        Public height As Integer
        Public ppx As Single
        Public ppy As Single
        Public fx As Single
        Public fy As Single
        Public coeffs As Single()
        Public Property FOV As Single()
    End Structure
    Public Structure algorithmParameters
        Dim lowResolution As Boolean
        Dim minimizeMemoryFootprint As Boolean
        Dim useRecordedData As Boolean
        Dim UsingIntelCamera As Boolean
        Dim IMUpresent As Boolean
        Dim testAllRunning As Boolean
        Dim ShowOptions As Boolean
        Dim ShowConsoleLog As Boolean
        Dim AvoidDNNCrashes As Boolean
        Dim externalInvocation As Boolean
        Dim PythonExe As String
        Dim activeAlgorithm As String
        Dim vtkDirectory As String
        Dim HomeDir As String
        Dim OpenCVfullPath As String
        Dim OpenCV_Version_ID As String
        Dim speedFactor As Int32
        Dim width As Int32
        Dim height As Int32
        Dim mainFormHeight As Int32
        Dim mainFormLoc As Point
        Dim imageToTrueTypeLoc As Single
        Dim intrinsics As Intrinsics_VB
        Dim extrinsics As Extrinsics_VB
        Dim imuGyro As cv.Point3f
        Dim imuAccel As cv.Point3f
        Dim imuTimeStamp As Double
    End Structure
    Public Sub New(parms As algorithmParameters)
        UpdateHostLocation(parms.mainFormLoc.X, parms.mainFormLoc.Y, parms.mainFormHeight)
        ocvb = New AlgorithmData(parms)
        If LCase(parms.activeAlgorithm).EndsWith(".py") Then ocvb.PythonFileName = parms.activeAlgorithm
        ocvb.PythonExe = parms.PythonExe
        ocvb.parms = parms
        ActiveAlgorithm = algoList.createAlgorithm(parms.activeAlgorithm, ocvb)
        If ActiveAlgorithm Is Nothing And parms.activeAlgorithm.EndsWith(".py") Then
            parms.activeAlgorithm = parms.activeAlgorithm.Substring(0, Len(parms.activeAlgorithm) - 3)
            ActiveAlgorithm = algoList.createAlgorithm(parms.activeAlgorithm, ocvb)
        End If
        slidersOffset = New cv.Point
        radioOffset = New cv.Point
        If parms.useRecordedData Then recordedData = New Replay_Play(ocvb)
    End Sub
    Public Sub UpdateHostLocation(left As Int32, top As Int32, height As Int32)
        appLocation = New cv.Rect(left, top, 0, height)
    End Sub
    Public Sub RunAlgorithm()
        Try
            If ocvb.parms.useRecordedData Then recordedData.Run(ocvb)
            ActiveAlgorithm.Run(ocvb)
            ocvb.frameCount += 1

            MakeSureImage8uC3(ocvb.result1)
            MakeSureImage8uC3(ocvb.result2)

        Catch ex As Exception
            Console.WriteLine("Active Algorithm exception occurred: " + ex.Message)
        End Try
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If ActiveAlgorithm IsNot Nothing Then ActiveAlgorithm.dispose()
        If recordedData IsNot Nothing Then recordedData.Dispose()
    End Sub
End Class
