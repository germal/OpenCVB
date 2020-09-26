Imports cv = OpenCvSharp
Public Class TextureFlow_Basics
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Texture Flow Delta", 2, 100, 12)
        sliders.setupTrackBar(1, "Texture Eigen BlockSize", 1, 100, 20)
        sliders.setupTrackBar(2, "Texture Eigen Ksize", 1, 15, 1)

        ocvb.desc = "Find and mark the texture flow in an image - see texture_flow.py.  Painterly Effect"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim TFdelta = sliders.trackbar(0).Value
        Dim TFblockSize = sliders.trackbar(1).Value * 2 + 1
        Dim TFksize = sliders.trackbar(2).Value * 2 + 1
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
    Inherits VBparent
    Dim texture As TextureFlow_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        texture = New TextureFlow_Basics(ocvb)
        ocvb.desc = "Display texture flow in the depth data"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        texture.src = ocvb.RGBDepth
        texture.Run(ocvb)
        dst1 = texture.dst1
    End Sub
End Class
