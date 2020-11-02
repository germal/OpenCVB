Imports cv = OpenCvSharp
Public Class imShow_Basics
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        ocvb.desc = "This is just a reminder that all HighGUI methods are available in OpenCVB"
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        cv.Cv2.ImShow("color", src)
    End Sub
End Class


