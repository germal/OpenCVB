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







