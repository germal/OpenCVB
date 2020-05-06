Imports cv = OpenCvSharp
' https://www.learnopencv.com/non-photorealistic-rendering-using-opencv-python-c/
Public Class Pencil_Basics
    Inherits VB_Class
        Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, "Pencil Sigma_s", 0, 200, 60)
        sliders.setupTrackBar2(ocvb, "Pencil Sigma_r", 1, 100, 7)
        sliders.setupTrackBar3(ocvb, "Pencil Shade Factor", 1, 200, 40)
                ocvb.desc = "Convert image to a pencil sketch - Painterly Effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim sigma_s = sliders.TrackBar1.Value
        Dim sigma_r = sliders.TrackBar2.Value / sliders.TrackBar2.Maximum
        Dim shadowFactor = sliders.TrackBar3.Value / 1000
        cv.Cv2.PencilSketch(ocvb.color, ocvb.result2, ocvb.result1, sigma_s, sigma_r, shadowFactor)
    End Sub
    Public Sub VBdispose()
            End Sub
End Class




' https://cppsecrets.com/users/2582658986657266505064717765737646677977/Convert-photo-to-sketch-using-python.php?fbclid=IwAR3pOtiqxeOPiqouii7tmN9Q7yA5vG4dFdXGqA0XgZqcMB87w5a1PEMzGOw
Public Class Pencil_Manual
    Inherits VB_Class
        Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, "Blur kernel size", 2, 100, 10)
                ocvb.desc = "Break down the process of converting an image to a sketch - Painterly Effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim grayinv As New cv.Mat
        cv.Cv2.BitwiseNot(gray, grayinv)
        Dim ksize = sliders.TrackBar1.Value
        If ksize Mod 2 = 0 Then ksize += 1
        Dim blur = grayinv.Blur(New cv.Size(ksize, ksize), New cv.Point(ksize / 2, ksize / 2))
        cv.Cv2.Divide(gray, 255 - blur, ocvb.result1, 256)

        Static index As Integer
        ocvb.label2 = "Intermediate result: " + Choose(index + 1, "gray", "grayinv", "blur")
        ocvb.result2 = Choose(index + 1, gray, grayinv, blur)
        If ocvb.frameCount Mod 30 = 0 Then
            index += 1
            If index >= 3 Then index = 0
        End If
    End Sub
    Public Sub VBdispose()
            End Sub
End Class
