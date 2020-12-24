Imports cv = OpenCvSharp
' https://github.com/spmallick/learnopencv/tree/master/
Public Class PhotoShop_Sepia
    Inherits VBparent
    Public Sub New()
        initParent()
        task.desc = "Create a sepia image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2RGB)
        Dim tMatrix = New cv.Mat(3, 3, cv.MatType.CV_64F, {{0.393, 0.769, 0.189}, {0.349, 0.686, 0.168}, {0.272, 0.534, 0.131}})
        dst1 = dst1.Transform(tMatrix).Threshold(255, 255, cv.ThresholdTypes.Trunc)
    End Sub
End Class







' https://github.com/spmallick/learnopencv/tree/master/
Public Class PhotoShop_Emboss
    Inherits VBparent
    Public gray128 As cv.Mat
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
    Public Function kernelGenerator(size As Integer) As cv.Mat
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






' https://github.com/spmallick/learnopencv/tree/master/
Public Class PhotoShop_EmbossAll
    Inherits VBparent
    Dim emboss As PhotoShop_Emboss
    Dim mats As Mat_4to1
    Dim sizeSlider As Windows.Forms.TrackBar
    Public Sub New()
        initParent()
        mats = New Mat_4to1
        emboss = New PhotoShop_Emboss

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Emboss threshold", 0, 255, 200)
        End If
        sizeSlider = findSlider("Emboss Kernel Size")
        sizeSlider.Value = 5
        hideForm("PhotoShop_Emboss Radio Options")

        label1 = "The combination of all angles"
        label2 = "bottom left, bottom right, top left, top right"
        task.desc = "Emboss using all the directions provided"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim kernel = emboss.kernelGenerator(sizeSlider.Value)

        Static threshSlider = findSlider("Emboss threshold")

        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = dst1.Filter2D(-1, kernel)
        cv.Cv2.Add(dst2, emboss.gray128, mats.mat(0))
        mats.mat(0) = mats.mat(0).Threshold(threshSlider.value, 255, cv.ThresholdTypes.Binary)

        cv.Cv2.Flip(kernel, kernel, cv.FlipMode.Y)
        dst2 = dst1.Filter2D(-1, kernel)
        cv.Cv2.Add(dst2, emboss.gray128, mats.mat(1))
        mats.mat(1) = mats.mat(1).Threshold(threshSlider.value, 255, cv.ThresholdTypes.Binary)

        cv.Cv2.Flip(kernel, kernel, cv.FlipMode.X)
        dst2 = dst1.Filter2D(-1, kernel)
        cv.Cv2.Add(dst2, emboss.gray128, mats.mat(2))
        mats.mat(2) = mats.mat(2).Threshold(threshSlider.value, 255, cv.ThresholdTypes.Binary)

        cv.Cv2.Flip(kernel, kernel, cv.FlipMode.XY)
        dst2 = dst1.Filter2D(-1, kernel)
        cv.Cv2.Add(dst2, emboss.gray128, mats.mat(3))
        mats.mat(3) = mats.mat(3).Threshold(threshSlider.value, 255, cv.ThresholdTypes.Binary)

        dst1.SetTo(0)
        For i = 0 To mats.mat.Count - 1
            cv.Cv2.BitwiseOr(mats.mat(i), dst1, dst1)
        Next

        mats.Run()
        dst2 = mats.dst1
    End Sub
End Class







' https://github.com/spmallick/learnopencv/tree/master/
Public Class PhotoShop_DuoTone
    Inherits VBparent
    Public Sub New()
        initParent()

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "First DuoTone Blue"
            radio.check(1).Text = "First DuoTone Green"
            radio.check(2).Text = "First DuoTone Red"
            radio.check(1).Checked = True

            radio1.Setup(caller + " ContourApproximation Mode", 4)
            radio1.check(0).Text = "Second DuoTone Blue"
            radio1.check(1).Text = "Second DuoTone Green"
            radio1.check(2).Text = "Second DuoTone Red"
            radio1.check(3).Text = "Second DuoTone None"
            radio1.check(3).Checked = True
        End If

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "DuoTone Dark if checked, Light otherwise"
        End If

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "DuoTone Exponent", 0, 50, 0)
        End If

        task.desc = "Create a DuoTone image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Static expSlider = findSlider("DuoTone Exponent")
        Dim exp = 1 + expSlider.value / 100
        Dim expMat As New cv.Mat(256, 1, cv.MatType.CV_8U)
        Dim expDark As New cv.Mat(256, 1, cv.MatType.CV_8U)
        For i = 0 To expMat.Rows - 1
            expMat.Set(Of Byte)(0, i, Math.Min(Math.Pow(i, exp), 255))
            expDark.Set(Of Byte)(0, i, Math.Min(Math.Pow(i, 2 - exp), 255))
        Next

        Dim split = src.Split()

        Dim sw1 As Integer
        For sw1 = 0 To radio.check.Count - 1
            If radio.check(sw1).Checked Then Exit For
        Next

        Dim sw2 As Integer
        For sw2 = 0 To radio1.check.Count - 1
            If radio1.check(sw2).Checked Then Exit For
        Next

        For i = 0 To split.Count - 1
            If i = sw1 Or i = sw2 Then
                split(i) = split(i).LUT(expMat)
            ElseIf check.Box(0).Checked Then
                split(i) = split(i).LUT(expDark)
            Else
                split(i).setto(0)
            End If
        Next

        cv.Cv2.Merge(split, dst1)
    End Sub
End Class