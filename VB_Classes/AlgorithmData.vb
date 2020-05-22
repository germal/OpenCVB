Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Public Class AlgorithmData
    Public color As cv.Mat
    Public RGBDepth As cv.Mat

    Public scalarColors(255) As cv.Scalar
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
    Public suppressOptions As Boolean
    Public TTtextData(3) As List(Of ActiveClass.TrueType)
    Public w As Integer
    Public h As Integer
    Public Sub New(parms As ActiveClass.algorithmParameters, width As Integer, height As Integer)
        w = width
        h = height
        optionsTop = parms.mainFormLoc.Y + parms.mainFormHeight
        optionsLeft = parms.mainFormLoc.X
        color = New cv.Mat(h, w, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        RGBDepth = New cv.Mat(h, w, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        result1 = New cv.Mat(h, w, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        result2 = New cv.Mat(h, w, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        label1 = ""
        label2 = ""
        ms_rng = New System.Random
        For i = 0 To rColors.Length - 1
            rColors(i) = New cv.Vec3b(ms_rng.Next(100, 255), ms_rng.Next(100, 255), ms_rng.Next(100, 255))
            scalarColors(i) = New cv.Scalar(rColors(i).Item0, rColors(i).Item1, rColors(i).Item2)
        Next
        For i = 0 To TTtextData.Count - 1
            TTtextData(i) = New List(Of ActiveClass.TrueType)
        Next
        fontSize = GetSetting("OpenCVB", "FontSize", "FontSize", 12)
        fontName = GetSetting("OpenCVB", "FontName", "FontName", "Tahoma")
    End Sub
    Public Sub putText(tt As ActiveClass.TrueType)
        TTtextData(tt.picTag).Add(tt)
    End Sub
End Class
