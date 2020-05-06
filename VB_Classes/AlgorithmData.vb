Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Public Class AlgorithmData
    Public bestOpenCVFont = cv.HersheyFonts.HersheyComplex
    Public bestOpenCVFontSize = 1.5
    Public color As cv.Mat
    Public colorScalar(255) As cv.Scalar
    Public RGBDepth As cv.Mat
    Public desc As String
    Public drawRect As cv.Rect ' filled in if the user draws on any of the images.
    Public drawRectClear As Boolean ' used to remove the drawing rectangle when it has been used to initialize a camshift or mean shift.
    Public fontName As String
    Public fontSize As Int32
    Public frameCount As Int32 = 0
    Public label1 As String
    Public label2 As String
    Public mouseClickFlag As Boolean
    Public mouseClickPoint As cv.Point
    Public mousePicTag As Int32 ' which image was the mouse in?
    Public mousePoint As cv.Point ' trace any mouse movements using this.
    Public ms_rng As System.Random
    Public name As String
    Public openGLHeight = 1200
    Public openGLWidth = 1500
    Public optionsLeft As Int32
    Public optionsTop As Int32
    Public parms As ActiveClass.algorithmParameters
    Public depth16 As cv.Mat
    Public pointCloud As cv.Mat
    Public PythonExe As String
    Public PythonFileName As String
    Public rColors(255) As cv.Vec3b
    Public leftView As cv.Mat
    Public rightView As cv.Mat
    Public result1 As New cv.Mat
    Public result2 As New cv.Mat
    Public TTtextData(3) As List(Of ActiveClass.TrueType)
    Public Sub New(parms As ActiveClass.algorithmParameters)
        optionsTop = parms.mainFormLoc.Y + parms.mainFormHeight
        optionsLeft = parms.mainFormLoc.X
        If parms.width < 1000 Then bestOpenCVFontSize = 0.5 ' this is better for the smaller resolutions.
        color = New cv.Mat(parms.height, parms.width, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        RGBDepth = New cv.Mat(parms.height, parms.width, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        result1 = New cv.Mat(parms.height, parms.width, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        result2 = New cv.Mat(parms.height, parms.width, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        label1 = ""
        label2 = ""
        ms_rng = New System.Random
        For i = 0 To rColors.Length - 1
            rColors(i) = New cv.Vec3b(ms_rng.Next(100, 255), ms_rng.Next(100, 255), ms_rng.Next(100, 255))
            colorScalar(i) = New cv.Scalar(rColors(i).Item0, rColors(i).Item1, rColors(i).Item2)
        Next
        For i = 0 To TTtextData.Count - 1
            TTtextData(i) = New List(Of ActiveClass.TrueType)
        Next
        fontSize = GetSetting("OpenCVB", "FontSize", "FontSize", 12)
        fontName = GetSetting("OpenCVB", "FontName", "FontName", "Tahoma")
    End Sub
    Public Sub vtkInstructions()
        putText(New ActiveClass.TrueType("VTK support is disabled. " + vbCrLf + "Enable VTK with the following steps:" + vbCrLf + vbCrLf +
                                         "Step 1) Run 'PrepareVTK.bat' in <OpenCVB_Home>" + vbCrLf +
                                         "Step 2) Build VTK for both Debug and Release" + vbCrLf +
                                         "Step 3) Build OpenCV for both Debug and Release" + vbCrLf +
                                         "Step 4) Edit mainVTK.cpp (project VTKDataExample) and modify the first line", 10, 125))
    End Sub
    Public Sub putText(tt As ActiveClass.TrueType)
        TTtextData(tt.picTag).Add(tt)
    End Sub
End Class
