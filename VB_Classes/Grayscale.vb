Imports cv = OpenCvSharp
Public Class Grayscale_Basics : Implements IDisposable
    Dim check As New OptionsCheckbox
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        check.Setup(ocvb, 1)
        check.Box(0).Text = "Use OpenCV to create grayscale image"
        check.Box(0).Checked = True
        If ocvb.parms.ShowOptions Then check.Show()

        ocvb.desc = "Manually create a grayscale image.  The only reason for this example is to show how slow it can be to do the work manually in VB.Net"
        ocvb.label1 = "Grayscale_Basics"
        ocvb.label2 = ""
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If check.Box(0).Checked Then
            ocvb.result1 = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Else
            ocvb.result1 = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8U)
            For y = 0 To ocvb.color.Rows - 1
                For x = 0 To ocvb.color.Cols - 1
                    Dim cc = ocvb.color.Get(of cv.Vec3b)(y, x)
                    ocvb.result1.Set(Of Byte)(y, x, CByte((cc.Item0 * 1140 + cc.Item1 * 5870 + cc.Item2 * 2989) / 10000))
                Next
            Next
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        check.Dispose()
    End Sub
End Class