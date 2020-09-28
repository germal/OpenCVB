Imports cv = OpenCvSharp
Public Class Fuzzy_Basics
    Inherits VBparent
    Dim reduction As Reduction_KNN_Color
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        reduction = New Reduction_KNN_Color(ocvb)
        ocvb.desc = "That which is not solid is fuzzy."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        reduction.src = src
        reduction.Run(ocvb)
        dst1 = reduction.dst1
        dst2 = dst1.Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
    End Sub
End Class