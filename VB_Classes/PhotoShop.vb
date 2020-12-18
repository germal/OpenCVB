Imports cv = OpenCvSharp
' https://github.com/spmallick/learnopencv/tree/master/Photoshop-Filters-in-OpenCV
Public Class PhotoShop_Sepia
    Inherits VBparent
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Sepia Multiplier X100", 0, 1000, 100)
        End If

        task.desc = "Create a sepia image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2RGB)
        Static multSlider = findSlider("Sepia Multiplier X100")
        Dim m As Double = multSlider.value / 100
        Dim tMatrix = New cv.Mat(3, 3, cv.MatType.CV_64F, {{0.393 * m, 0.769 * m, 0.189 * m}, {0.349 * m, 0.686 * m, 0.168 * m}, {0.272 * m, 0.534 * m, 0.131 * m}})
        dst1 = dst1.Transform(tMatrix).Threshold(255, 255, cv.ThresholdTypes.Trunc)
    End Sub
End Class








Public Class PhotoShop_Emboss
    Inherits VBparent
    Dim gray128 As cv.Mat
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Emboss Kernel Size", 2, 10, 2)
        End If

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 4)
            radio.check(0).Text = "Bottom Left"
            radio.check(1).Text = "Bottom Right"
            radio.check(2).Text = "Top Left"
            radio.check(3).Text = "Top Right"
            radio.check(0).Checked = True
        End If

        gray128 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 128)
        label2 = "Embossed output"
        task.desc = "Use the video stream to make it appear like an embossed paper image."
    End Sub
    Private Function kernelGenerator(size As Integer) As cv.Mat
        Dim kernel As New cv.Mat(size, size, cv.MatType.CV_8S, 0)
        For i = 0 To size - 1
            For j = 0 To size - 1
                If i < j Then kernel.Set(Of SByte)(j, i, -1) Else If i > j Then kernel.Set(Of SByte)(j, i, 1)
            Next
        Next
        Return kernel
    End Function
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static sizeSlider = findSlider("Emboss Kernel Size")
        Dim kernel = kernelGenerator(sizeSlider.value)

        Dim direction As Integer
        For direction = 0 To radio.check.Count - 1
            If radio.check(direction).Checked Then Exit For
        Next

        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Select Case direction
            Case 0 ' do nothing!
            Case 1 ' flip vertically
                cv.Cv2.Flip(kernel, kernel, cv.FlipMode.Y)
            Case 2 ' flip horizontally
                cv.Cv2.Flip(kernel, kernel, cv.FlipMode.X)
            Case 3 ' flip horizontally and vertically
                cv.Cv2.Flip(kernel, kernel, cv.FlipMode.XY)
        End Select

        dst2 = dst1.Filter2D(-1, kernel)
        cv.Cv2.Add(dst2, gray128, dst2)
    End Sub
End Class