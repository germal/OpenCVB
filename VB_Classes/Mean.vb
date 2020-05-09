Imports cv = OpenCvSharp
Public Class Mean_Basics
    Inherits ocvbClass
    Dim images As New List(Of cv.Mat)
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw as string)
        setCaller(caller)
        sliders.setupTrackBar1(ocvb, caller, "Mean - number of input images", 1, 100, 10)
        ocvb.desc = "Create an image that is the mean of x number of previous images."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static saveImageCount = sliders.TrackBar1.Value
        If sliders.TrackBar1.Value <> saveImageCount Then
            saveImageCount = sliders.TrackBar1.Value
            images.Clear()
        End If
        If standalone Then src = ocvb.color
        Dim nextImage As New cv.Mat
        If src.Type <> cv.MatType.CV_32F Then src.ConvertTo(nextImage, cv.MatType.CV_32F, 1 / saveImageCount) Else nextImage = src
        images.Add(nextImage.Clone())
        If dst Is Nothing Then dst = src.Clone()

        nextImage.SetTo(0)
        For Each img In images
            nextImage += img
        Next
        If images.Count > saveImageCount Then images.RemoveAt(0)
        If nextImage.Type <> src.Type Then nextImage.ConvertTo(dst, src.Type) Else dst = nextImage
        If standalone Then ocvb.result1 = dst
		MyBase.Finish(ocvb)
    End Sub
    Public Sub MyDispose()
    End Sub
End Class
