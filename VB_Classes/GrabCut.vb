Imports cv = OpenCvSharp
' https://docs.opencv.org/3.3.1/de/dd0/grabcut_8cpp-example.html
Public Class GrabCut_Basics
    Inherits ocvbClass
    Dim contours As Contours_Depth
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        contours = New Contours_Depth(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Erode iterations", 1, 20, 3)
        sliders.setupTrackBar(1, "Erode kernel size", 1, 21, 3)

        ocvb.desc = "Use grabcut to isolate what is in the foreground and background.  "
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        contours.src = src
        contours.Run(ocvb)
        dst2 = contours.dst2
        Dim iterations = sliders.sliders(0).Value
        Dim kernelsize = sliders.sliders(1).Value
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

