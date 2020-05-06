Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Public Class Gradient_Basics
    Inherits VB_Class
    Public src As cv.Mat
    Public externalUse As Boolean
    Dim sobel As Edges_Sobel
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sobel = New Edges_Sobel(ocvb, callerName)
        sobel.externalUse = True
        ocvb.desc = "Use phase to compute gradient"
        ocvb.label2 = "Phase Output"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then src = ocvb.color
        sobel.src = src
        sobel.Run(ocvb)
        Dim angle = New cv.Mat
        Dim x32f As New cv.Mat
        Dim y32f As New cv.Mat
        sobel.grayX.ConvertTo(x32f, cv.MatType.CV_32F)
        sobel.grayY.ConvertTo(y32f, cv.MatType.CV_32F)
        cv.Cv2.Phase(x32f, y32f, angle)
        Dim gray = angle.Normalize(255, 0, cv.NormTypes.MinMax)
        gray.ConvertTo(ocvb.result2, cv.MatType.CV_8UC1)
        ocvb.result1 = sobel.dst
    End Sub
    Public Sub MyDispose()
        sobel.Dispose()
    End Sub
End Class




Public Class Gradient_Depth
    Inherits VB_Class
    Dim sobel As Edges_Sobel
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sobel = New Edges_Sobel(ocvb, callerName)
        sobel.externalUse = True
        ocvb.desc = "Use phase to compute gradient on depth image"
        ocvb.label2 = "Phase Output"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        sobel.src = ocvb.RGBDepth.Clone()
        sobel.Run(ocvb)
        Dim angle = New cv.Mat
        Dim x32f As New cv.Mat
        Dim y32f As New cv.Mat
        sobel.grayX.ConvertTo(x32f, cv.MatType.CV_32F)
        sobel.grayY.ConvertTo(y32f, cv.MatType.CV_32F)
        cv.Cv2.Phase(x32f, y32f, angle)
        Dim gray = angle.Normalize(255, 0, cv.NormTypes.MinMax)
        gray.ConvertTo(ocvb.result2, cv.MatType.CV_8UC1)
        ocvb.result1 = sobel.dst
    End Sub
    Public Sub MyDispose()
        sobel.Dispose()
    End Sub
End Class






Public Class Gradient_Flatland
    Inherits VB_Class
    Dim grade As Gradient_Basics
        Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        grade = New Gradient_Basics(ocvb, callerName)
        sliders.setupTrackBar1(ocvb, callerName, "Reduction Factor", 1, 64, 16)
                ocvb.desc = "Reduced grayscale shows isobars in depth."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim reductionFactor = sliders.TrackBar1.Maximum - sliders.TrackBar1.Value
        ocvb.result1 = ocvb.RGBDepth.Clone()
        ocvb.result1 /= reductionFactor
        ocvb.result1 *= reductionFactor
        grade.Run(ocvb)
    End Sub
    Public Sub MyDispose()
                grade.Dispose()
    End Sub
End Class
