Imports cv = OpenCvSharp
Public Class MiniPC_Basics
    Inherits VBparent
    Dim resize As Resize_Percentage
    Public Sub New()
        initParent()
        resize = New Resize_Percentage
        task.desc = "Create a mini point cloud for use with histograms"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        resize.src = task.pointCloud
        resize.Run()

        If standalone Or task.intermediateReview = caller Then
            Dim split = resize.dst1.Split()
            Dim rect = New cv.Rect(0, 0, resize.dst1.Width, resize.dst1.Height)
            If rect.Height < dst1.Height / 2 Then rect.Y = dst1.Height / 4 ' move it below the dst1 caption
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            dst1(rect) = split(2).ConvertScaleAbs(255)
            dst1.Rectangle(rect, cv.Scalar.White, 1)
            label1 = "MiniPC is " + CStr(rect.Width) + "x" + CStr(rect.Height) + " total pixels = " + CStr(rect.Width * rect.Height)
        End If
    End Sub
End Class








Public Class MiniPC_Histogram
    Inherits VBparent
    Dim mini As MiniPC_Basics
    Public Sub New()
        initParent()
        mini = New MiniPC_Basics
        task.desc = "Create a histogram for the mini point cloud"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
    End Sub
End Class