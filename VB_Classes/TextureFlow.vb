Imports cv = OpenCvSharp
Public Class TextureFlow_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Texture Flow Delta", 2, 100, 12)
        sliders.setupTrackBar2(ocvb, caller, "Texture Eigen BlockSize", 1, 100, 20)
        sliders.setupTrackBar3(ocvb, caller, "Texture Eigen Ksize", 1, 15, 1)

        ocvb.desc = "Find and mark the texture flow in an image - see texture_flow.py.  Painterly Effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim TFdelta = sliders.TrackBar1.Value
        Dim TFblockSize = sliders.TrackBar2.Value * 2 + 1
        Dim TFksize = sliders.TrackBar3.Value * 2 + 1
        If standalone or src.width = 0 Then src = ocvb.color
        Dim gray = src.CvtColor(OpenCvSharp.ColorConversionCodes.BGR2GRAY)
        dst1 = src.Clone
        Dim eigen = gray.CornerEigenValsAndVecs(TFblockSize, TFksize)
        Dim split = eigen.Split()
        Dim d2 = TFdelta / 2
        For y = d2 To dst1.Height - 1 Step d2
            For x = d2 To dst1.Width - 1 Step d2
                Dim delta = New cv.Point2f(split(4).Get(Of Single)(y, x), split(5).Get(Of Single)(y, x)) * TFdelta
                Dim p1 = New cv.Point(x - delta.X, y - delta.Y)
                Dim p2 = New cv.Point(x + delta.X, y + delta.Y)
                dst1.Line(p1, p2, cv.Scalar.Black, 1, cv.LineTypes.AntiAlias)
            Next
        Next
    End Sub
End Class




Public Class TextureFlow_Depth
    Inherits ocvbClass
    Dim texture As TextureFlow_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        texture = New TextureFlow_Basics(ocvb, caller)
        ocvb.desc = "Display texture flow in the depth data"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        texture.src = ocvb.RGBDepth
        texture.Run(ocvb)
    End Sub
End Class
