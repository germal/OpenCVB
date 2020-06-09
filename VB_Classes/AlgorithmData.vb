Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Public Class AlgorithmData
    ' all the items here are used to communicate to/from the host user interface.  Other variables common to all algorithms should be ocvbClass.vb
    Public color As cv.Mat
    Public RGBDepth As cv.Mat
    Public result1 As New cv.Mat
    Public result2 As New cv.Mat
    Public pointCloud As cv.Mat
    Public depth16 As cv.Mat
    Public leftView As cv.Mat
    Public rightView As cv.Mat

    Public desc As String
    Public drawRect As cv.Rect ' filled in if the user draws on any of the images.
    Public drawRectClear As Boolean ' used to remove the drawing rectangle when it has been used to initialize a camshift or mean shift.
    Public frameCount As Int32 = 0
    Public label1 As String
    Public label2 As String
    Public parms As ActiveClass.algorithmParameters

    Public mouseClickFlag As Boolean
    Public mouseClickPoint As cv.Point
    Public mousePicTag As Int32 ' which image was the mouse in?
    Public mousePoint As cv.Point ' trace any mouse movements using this.

    Public PythonExe As String
    Public PythonFileName As String
    Public suppressOptions As Boolean
    Public TTtextData(4 - 1) As List(Of oTrueType)

    Public caller As String
    Public Sub New(parms As ActiveClass.algorithmParameters, width As Integer, height As Integer)
        color = New cv.Mat(height, width, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        RGBDepth = New cv.Mat(height, width, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        result1 = New cv.Mat(height, width, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        result2 = New cv.Mat(height, width, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        For i = 0 To TTtextData.Count - 1
            TTtextData(i) = New List(Of oTrueType)
        Next
    End Sub
    Public Sub putText(tt As oTrueType)
        TTtextData(tt.picTag).Add(tt)
    End Sub
End Class
