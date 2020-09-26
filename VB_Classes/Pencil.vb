Imports cv = OpenCvSharp
' https://www.learnopencv.com/non-photorealistic-rendering-using-opencv-python-c/
Public Class Pencil_Basics
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Pencil Sigma_s", 0, 200, 60)
        sliders.setupTrackBar(1, "Pencil Sigma_r", 1, 100, 7)
        sliders.setupTrackBar(2, "Pencil Shade Factor", 1, 200, 40)
        ocvb.desc = "Convert image to a pencil sketch - Painterly Effect"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim sigma_s = sliders.trackbar(0).Value
        Dim sigma_r = sliders.trackbar(1).Value / sliders.trackbar(1).Maximum
        Dim shadowFactor = sliders.trackbar(2).Value / 1000
        cv.Cv2.PencilSketch(src, dst2, dst1, sigma_s, sigma_r, shadowFactor)
    End Sub
End Class




' https://cppsecrets.com/users/2582658986657266505064717765737646677977/Convert-photo-to-sketch-using-python.php?fbclid=IwAR3pOtiqxeOPiqouii7tmN9Q7yA5vG4dFdXGqA0XgZqcMB87w5a1PEMzGOw
Public Class Pencil_Manual
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Blur kernel size", 2, 100, 10)
        ocvb.desc = "Break down the process of converting an image to a sketch - Painterly Effect"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim grayinv As New cv.Mat
        cv.Cv2.BitwiseNot(src, grayinv)
        Dim ksize = sliders.trackbar(0).Value
        If ksize Mod 2 = 0 Then ksize += 1
        Dim blur = grayinv.Blur(New cv.Size(ksize, ksize), New cv.Point(ksize / 2, ksize / 2))
        cv.Cv2.Divide(src, 255 - blur, dst1, 256)

        Static index As Integer
        label2 = "Intermediate result: " + Choose(index + 1, "gray", "grayinv", "blur")
        dst2 = Choose(index + 1, src, grayinv, blur)
        If ocvb.frameCount Mod 30 = 0 Then
            index += 1
            If index >= 3 Then index = 0
        End If
    End Sub
End Class

