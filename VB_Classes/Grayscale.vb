Imports cv = OpenCvSharp
Public Class Grayscale_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Use OpenCV to create grayscale image"
        check.Box(0).Checked = True

        ocvb.desc = "Manually create a grayscale image.  The only reason for this example is to show how slow it can be to do the work manually in VB.Net"
        label1 = "Grayscale_Basics"
        label2 = ""
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If check.Box(0).Checked Then
            dst1 = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Else
            dst1 = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8U)
            For y = 0 To ocvb.color.Rows - 1
                For x = 0 To ocvb.color.Cols - 1
                    Dim cc = ocvb.color.Get(Of cv.Vec3b)(y, x)
                    dst1.Set(Of Byte)(y, x, CByte((cc.Item0 * 1140 + cc.Item1 * 5870 + cc.Item2 * 2989) / 10000))
                Next
            Next
        End If
    End Sub
End Class
