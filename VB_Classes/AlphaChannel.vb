Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
' https://www.learnopencv.com/alpha-blending-using-opencv-cpp-python/
' https://docs.microsoft.com/en-us/dotnet/api/system.drawing.bitmap.maketransparent?view=dotnet-plat-ext-3.1
Public Class AlphaChannel_Basics
    Inherits VBparent
    Dim fg As Depth_InRange
    Dim alpha As New OptionsAlphaBlend
    Public Sub New()
        initParent()

        fg = New Depth_InRange()

        alpha.Show()
        alpha.Size = New System.Drawing.Size(src.Width + 10, src.Height + 10)

        task.desc = "Use the the Windows 10 alpha channel to separate foreground and background"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        fg.Run()

        src = src.CvtColor(cv.ColorConversionCodes.BGR2BGRA)
        Dim split() = cv.Cv2.Split(src)
        split(3) = fg.depthMask
        cv.Cv2.Merge(split, src)
        alpha.AlphaPic.Image = cvext.BitmapConverter.ToBitmap(src, System.Drawing.Imaging.PixelFormat.Format32bppArgb)
    End Sub
End Class





' https://www.learnopencv.com/alpha-blending-using-opencv-cpp-python/
Public Class AlphaChannel_Blend
    Inherits VBparent
    Dim fg As Depth_InRange
    Public Sub New()
        initParent()

        fg = New Depth_InRange()

        sliders.Setup(caller)
        sliders.setupTrackBar(0, "Transparency amount", 0, 255, 100)

        task.desc = "Use alpha blending to smoothly separate background from foreground"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        fg.Run()
        dst2.SetTo(0)
        src.CopyTo(dst2, fg.noDepthMask)

        Static transparencySlider = findSlider("Transparency amount")
        Dim alpha = transparencySlider.Value / 255
        cv.Cv2.AddWeighted(src, alpha, dst2, 1.0 - alpha, 0, dst1)
    End Sub
End Class
