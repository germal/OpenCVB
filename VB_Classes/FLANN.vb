Imports cv = OpenCvSharp
' https://docs.opencv.org/2.4/modules/flann/doc/flann_fast_approximate_nearest_neighbor_search.html#
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/FlannSample.vb
Public Class FLANN_Test
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        ocvb.desc = "Test basics of FLANN - Fast Library for Approximate Nearest Neighbor. "
        ocvb.label1 = "FLANN Basics"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount > 0 Then Exit Sub ' we are already done - it is a one-pass algorithm.
        Console.WriteLine(Environment.NewLine & String.Format(("===== FlannTest =====")))

        ' creates data set
        Using features As New cv.Mat(10000, 2, cv.MatType.CV_32FC1)
            cv.Cv2.Randu(features, 0, ocvb.ms_rng.Next(9900, 10000))

            ' query
            Dim queryPoint As New cv.Point2f(ocvb.ms_rng.Next(0, 10000), ocvb.ms_rng.Next(0, 10000))
            Dim queries As New cv.Mat(1, 2, cv.MatType.CV_32FC1, queryPoint)
            Console.WriteLine(String.Format("query:({0}, {1})", queryPoint.X, queryPoint.Y))
            Console.WriteLine(String.Format("-----"))

            ' knnSearch
            Using nnIndex As New cv.Flann.Index(features, New cv.Flann.KDTreeIndexParams(4))
                Dim knn As Integer = 1
                Dim indices() As Integer = Nothing
                Dim dists() As Single = Nothing
                nnIndex.KnnSearch(queries, indices, dists, knn, New cv.Flann.SearchParams(32))

                For i As Integer = 0 To knn - 1
                    Dim index As Integer = indices(i)
                    Dim dist As Single = dists(i)
                    Dim pt As New cv.Point2f(features.Get(Of Single)(index, 0), features.Get(Of Single)(index, 1))
                    ocvb.putText(New ActiveClass.TrueType(String.Format("No.{0}" & vbTab, i), 10 + i * 30, 30 + i * 15))
                    ocvb.putText(New ActiveClass.TrueType(String.Format("index:{0}", index), 10 + i * 30, 30 + i * 15 + 30))
                    ocvb.putText(New ActiveClass.TrueType(String.Format("distance:{0}", dist), 10 + i * 30, 30 + i * 15 + 60))
                    ocvb.putText(New ActiveClass.TrueType(String.Format("data:({0}, {1})", pt.X, pt.Y), 10, 30 + i * 15 + 90))
                Next i
            End Using
        End Using
    End Sub
End Class



Public Class FLANN_Basics
    Inherits VB_Class
        Dim random As Random_Points
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        random = New Random_Points(ocvb, callerName)

        sliders.setupTrackBar1(ocvb, callerName, "Query Count", 1, 10000, 10)

        ocvb.desc = "FLANN - Fast Library for Approximate Nearest Neighbor.  Find nearest neighbor"
        ocvb.label1 = "Yellow is query, Nearest points blue"
        ocvb.label2 = "FLANN Search Input"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        random.Run(ocvb) ' fill result1 with random points in x and y range of the image.
        Dim features As New cv.Mat(random.Points2f.Length, 2, cv.MatType.CV_32F, random.Points2f)

        Dim knnCount = sliders.TrackBar1.Value
        ocvb.result1.CopyTo(ocvb.result2)
        ' knnSearch
        Using nnIndex As New cv.Flann.Index(features, New cv.Flann.KDTreeIndexParams(4))
            Dim indices() As Integer = Nothing
            Dim distances() As Single = Nothing
            For i = 0 To knnCount - 1
                Dim query As New cv.Mat(1, 2, cv.MatType.CV_32F)
                query.Set(Of cv.Point2f)(0, 0, New cv.Point2f(random.Points2f(i).X, random.Points2f(i).Y))
                Dim displayCount = 3
                nnIndex.KnnSearch(query, indices, distances, displayCount, New cv.Flann.SearchParams(32)) ' 4 nearest neighbors

                Dim pt2 = random.Points2f(i)
                For j = 0 To displayCount - 1
                    Dim pt1 = random.Points(indices(j))
                    cv.Cv2.Circle(ocvb.result1, pt1, 5, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias, 0)
                    ocvb.result1.Line(pt1, pt2, cv.Scalar.Red, 1, cv.LineTypes.AntiAlias)
                Next
                cv.Cv2.Circle(ocvb.result1, pt2, 5, cv.Scalar.GreenYellow, -1, cv.LineTypes.AntiAlias, 0)
                cv.Cv2.Circle(ocvb.result2, pt2, 5, cv.Scalar.GreenYellow, -1, cv.LineTypes.AntiAlias, 0)
            Next
        End Using
    End Sub
    Public Sub MyDispose()
                random.Dispose()
    End Sub
End Class