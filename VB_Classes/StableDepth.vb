Imports cv = OpenCvSharp
Public Class StableDepth_Clusters
    Inherits VBparent
    Dim clusters As Histogram_DepthClusters
    Dim stableD As Depth_Stable
    Public Sub New()
        initParent()

        clusters = New Histogram_DepthClusters
        stableD = New Depth_Stable
        task.desc = "Use the stable depth to identify the depth_clusters using histogram valleys"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        stableD.Run()
        clusters.src = stableD.dst2
        clusters.Run()
        dst1 = clusters.dst1
        dst2 = clusters.dst2
    End Sub
End Class
