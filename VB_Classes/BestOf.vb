Imports cv = OpenCvSharp
Public Class BestOf_Binarize
    Inherits VBparent
    Dim binarize As Binarize_Basics
    Public Sub New()
        initParent()
        binarize = New Binarize_Basics
        task.desc = "Best way to binarize an image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        binarize.src = src
        binarize.Run()
        dst1 = binarize.dst1
    End Sub
End Class







Public Class BestOf_Edges
    Inherits VBparent
    Dim edges As Edges_BinarizedSobel
    Public Sub New()
        initParent()
        edges = New Edges_BinarizedSobel
        task.desc = "Best way to get edges from an image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        edges.src = src
        edges.Run()
        dst1 = edges.dst1
        dst2 = edges.dst2
    End Sub
End Class








Public Class BestOf_Contours
    Inherits VBparent
    Dim contours As Contours_FindandDraw
    Public Sub New()
        initParent()
        contours = New Contours_FindandDraw
        task.desc = "Best example of how to use contours"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        contours.src = src
        contours.Run()
        dst1 = contours.dst1
        dst2 = contours.dst2
    End Sub
End Class







Public Class BestOf_Blobs
    Inherits VBparent
    Dim blobs As Blob_DepthClusters
    Public Sub New()
        initParent()
        blobs = New Blob_DepthClusters
        task.desc = "Best example of using depth to identify blobs"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        blobs.src = src
        blobs.Run()
        dst1 = blobs.dst1
        dst2 = blobs.dst2
    End Sub
End Class







Public Class BestOf_CComp
    Inherits VBparent
    Dim ccomp As CComp_Binarized
    Public Sub New()
        initParent()
        ccomp = New CComp_Binarized
        task.desc = "Best example of using the connected components feature"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        ccomp.src = src
        ccomp.Run()
        dst1 = ccomp.dst1
        dst2 = ccomp.dst2
    End Sub
End Class








Public Class BestOf_FloodFill
    Inherits VBparent
    Dim flood As FloodFill_FullImage
    Public Sub New()
        initParent()
        flood = New FloodFill_FullImage
        task.desc = "Best example of using the FloodFill feature"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        flood.src = src
        flood.Run()
        dst1 = flood.dst1
        dst2 = flood.dst2
    End Sub
End Class








Public Class BestOf_KNN
    Inherits VBparent
    Dim myTopView As PointCloud_Kalman_TopView
    Public Sub New()
        initParent()
        myTopView = New PointCloud_Kalman_TopView
        task.desc = "Best example of using KNN to track objects"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        myTopView.src = task.pointCloud
        myTopView.Run()
        dst1 = myTopView.dst1

        myTopView.topView.cmat.src = dst1.Clone
        myTopView.topView.cmat.Run()
        dst2 = myTopView.topView.cmat.dst1
    End Sub
End Class







Public Class BestOf_Kalman
    Inherits VBparent
    Dim knnKalman As BestOf_KNN
    Public Sub New()
        initParent()
        knnKalman = New BestOf_KNN
        task.desc = "Best example of using Kalman to calm the points in the point tracker"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        knnKalman.Run()
        dst1 = knnKalman.dst1
        dst2 = knnKalman.dst2
    End Sub
End Class