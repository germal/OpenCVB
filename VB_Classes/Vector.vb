Imports cv = OpenCvSharp
' https://stackoverflow.com/questions/37492663/how-to-use-magnitude-and-absdiff-opencv-functions-to-compute-distances
Public Class Vector_Magnitude : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        ocvb.desc = "Compute Euclidian and Manhattan Distance on a single vector."
        ocvb.label1 = "Vector Magnitude"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim cVector() As Single = {1, 4, 4, 8}
        ocvb.putText(New ActiveClass.TrueType("Original vector = " + CStr(cVector(0)) + ", " + CStr(cVector(1)) + ", " + CStr(cVector(2)) + ", " + CStr(cVector(3)), 10, 30))
        Dim coordinates As New cv.Mat(1, 4, cv.MatType.CV_32F, cVector)
        Dim diff_x = coordinates.Col(0) - coordinates.Col(2)
        Dim diff_y = coordinates.Col(1) - coordinates.Col(3)

        ' sqrt((x2 - x1)^2 + (y2 - y1)^2)
        Dim euclidean_distance As New cv.Mat
        cv.Cv2.Magnitude(diff_x, diff_y, euclidean_distance)
        ocvb.putText(New ActiveClass.TrueType("euclidean_distance = " + CStr(euclidean_distance.Get(of Single)(0, 0)), 10, 60))

        Dim manhattan_distance = cv.Cv2.Abs(diff_x) + cv.Cv2.Abs(diff_y)
        ocvb.putText(New ActiveClass.TrueType("manhattan_distance = " + CStr(manhattan_distance.ToMat.Get(of Single)(0, 0)), 10, 90))

        ' Another way to compute L1 distance, with absdiff
        ' abs(x2 - x1) + abs(y2 - y1)
        Dim points1 = coordinates(cv.Range.All(), New cv.Range(0, 2))
        Dim points2 = coordinates(cv.Range.All(), New cv.Range(2, 4))
        Dim other_manhattan_distance As New cv.Mat
        cv.Cv2.Absdiff(points1, points2, other_manhattan_distance)
        other_manhattan_distance = other_manhattan_distance.Col(0) + other_manhattan_distance.Col(1)
        ocvb.putText(New ActiveClass.TrueType("other_manhattan_distance = " + CStr(other_manhattan_distance.Get(of Single)(0, 0)), 10, 120))
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
