Imports cv = OpenCvSharp
Public Class ImageRGB_Basics
    Inherits VBparent
    Public motion As Motion_Basics
    Public stableImage As cv.Mat
    Dim pixel As Pixel_Viewer
    Public Sub New()
        initParent()
        motion = New Motion_Basics
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "Use maximum pixel value"
            radio.check(1).Text = "Use minimum pixel value"
            radio.check(2).Text = "Use original (unchanged) pixels"
            radio.check(1).Checked = True
        End If
        label1 = "Stabilized image"
        task.desc = "Stabilize the RGB (actually BGR) image with Min/Max OpenCV API"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        motion.src = task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        motion.Run()
        label2 = motion.label2
        dst2 = If(motion.dst2.Channels = 1, motion.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR), motion.dst2.Clone)

        Dim radioVal As Integer
        Static frm As OptionsRadioButtons = findfrm(caller + " Radio Options")
        For radioVal = 0 To frm.check.Count - 1
            If frm.check(radioVal).Checked Then Exit For
        Next

        If motion.resetAll Or stableImage Is Nothing Or radioVal = 2 Then
            stableImage = src.Clone
        Else
            Dim rect = motion.uRect.allRect
            dst2.Rectangle(rect, cv.Scalar.Yellow, 2)
            If rect.Width And rect.Height Then src(rect).CopyTo(stableImage(rect))
            If radioVal = 0 Then cv.Cv2.Max(src, stableImage, stableImage) Else cv.Cv2.Min(src, stableImage, stableImage)
        End If
        dst1 = stableImage

        If standalone Or task.intermediateReview = caller Then
            If pixel Is Nothing Then pixel = New Pixel_Viewer
            pixel.src = dst1
            pixel.Run()
            dst1 = pixel.dst1
        End If
    End Sub
End Class

