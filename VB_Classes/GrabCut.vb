Imports cv = OpenCvSharp
' https://docs.opencv.org/3.3.1/de/dd0/grabcut_8cpp-example.html
Public Class GrabCut_Basics
    Inherits VBparent
    Dim contours As Contours_Depth
    Public Sub New()
        initParent()
        contours = New Contours_Depth()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Erode iterations", 1, 20, 3)
            sliders.setupTrackBar(1, "Erode kernel size", 1, 21, 3)
        End If
        task.desc = "Use grabcut to isolate what is in the foreground and background.  "
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        contours.src = src
        contours.Run()
        dst2 = contours.dst2
        Dim iterations = sliders.trackbar(0).Value
        Dim kernelsize = sliders.trackbar(1).Value
        If kernelsize Mod 2 = 0 Then kernelsize += 1
        Dim morphShape = cv.MorphShapes.Cross

        Dim element = cv.Cv2.GetStructuringElement(morphShape, New cv.Size(kernelsize, kernelsize))
        dst1 = dst2.Erode(element, Nothing, iterations)

        Dim gray = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim grayEroded = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim fg = gray.Threshold(1, cv.GrabCutClasses.FGD, cv.ThresholdTypes.Binary)
        Dim bg = gray.Threshold(1, cv.GrabCutClasses.BGD, cv.ThresholdTypes.BinaryInv)
        gray.SetTo(0, grayEroded)
        Dim prFG = gray.Threshold(1, cv.GrabCutClasses.PR_FGD, cv.ThresholdTypes.Binary)

        Dim mask As New cv.Mat
        cv.Cv2.BitwiseOr(bg, fg, mask)
        cv.Cv2.BitwiseOr(prFG, mask, mask)

        Static bgModel As New cv.Mat, fgModel As New cv.Mat
        Dim rect As New cv.Rect
        If fg.CountNonZero() > 100 And bg.CountNonZero() > 100 Then
            cv.Cv2.GrabCut(src, mask, rect, bgModel, fgModel, 1, cv.GrabCutModes.InitWithMask)
        End If
        src.CopyTo(dst2, mask)
    End Sub
End Class


