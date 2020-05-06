Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class knn_Basics
    Inherits VB_Class
        Dim random As Random_Points
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        random = New Random_Points(ocvb, "knn_Basics")
        sliders.setupTrackBar1(ocvb, "knn Query Points", 1, 10000, 10)
        sliders.setupTrackBar2(ocvb, "knn Known Points", 1, 10, 3)
                ocvb.desc = "Test knn with random points in the image.  Find the nearest to a random point."
        ocvb.label2 = "Search Input"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim knn = cv.ML.KNearest.Create()
        random.Run(ocvb)
        Dim trainData = New cv.Mat(random.Points.Length, 2, cv.MatType.CV_32F, random.Points2f)
        Dim responses(random.Points.Length - 1) As Int32
        For i = 0 To responses.Length - 1
            responses(i) = i
        Next
        knn.Train(trainData, cv.ML.SampleTypes.RowSample, New cv.Mat(responses.Length, 1, cv.MatType.CV_32S, responses))

        Dim queryPoints(sliders.TrackBar1.Value) As cv.Point2f
        For i = 0 To queryPoints.Length - 1
            queryPoints(i) = New cv.Point2f(ocvb.ms_rng.Next(0, ocvb.result1.Cols), ocvb.ms_rng.Next(0, ocvb.result1.Rows))
        Next

        Dim bluePoints = sliders.TrackBar2.Value
        ocvb.label1 = "Yellow is random, blue nearest " + CStr(bluePoints)
        ocvb.result1.CopyTo(ocvb.result2)
        Dim results As New cv.Mat, neighbors As New cv.Mat, query As New cv.Mat(1, 2, cv.MatType.CV_32F)
        For i = 0 To queryPoints.Length - 1
            query.Set(Of cv.Point2f)(0, 0, queryPoints(i))
            knn.FindNearest(query, bluePoints, results, neighbors)
            For j = 0 To bluePoints - 1
                Dim index = CInt(neighbors.Get(Of Single)(0, j))
                cv.Cv2.Circle(ocvb.result1, random.Points(index), 3, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias, 0)
                ocvb.result1.Line(random.Points(index), queryPoints(i), cv.Scalar.Red, 1, cv.LineTypes.AntiAlias)
            Next
            cv.Cv2.Circle(ocvb.result1, queryPoints(i), 3, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias, 0)
            cv.Cv2.Circle(ocvb.result2, queryPoints(i), 3, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias, 0)
        Next
    End Sub
    Public Sub VBdispose()
                random.Dispose()
    End Sub
End Class






Public Class knn_Cluster2D
    Inherits VB_Class
        Dim knn As knn_Point2d
    Public cityPositions() As cv.Point
    Public cityOrder() As Int32
    Public distances() As Int32
    Dim numberofCities As Int32
    Dim closedRegions As Int32
    Dim totalClusters As Int32
    Public Sub drawMap(result As cv.Mat)
        For i = 0 To cityOrder.Length - 1
            result.Circle(cityPositions(i), 5, cv.Scalar.White, -1)
            result.Line(cityPositions(i), cityPositions(cityOrder(i)), cv.Scalar.White, 2)
        Next
    End Sub
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        knn = New knn_Point2d(ocvb, "knn_Cluster2D")
        knn.sliders.Visible = False
        knn.externalUse = True

        sliders.setupTrackBar1(ocvb, "knn - number of cities", 10, 1000, 100)
        
        ocvb.label1 = ""
        ocvb.label2 = ""
        ocvb.desc = "Use knn to cluster cities as preparation for a solution to the traveling salesman problem."
    End Sub
    Private Sub cluster(rColors() As cv.Vec3b, result As cv.Mat)
        Dim alreadyTaken As New List(Of Int32)
        For i = 0 To numberofCities - 1
            For j = 1 To numberofCities - 1
                Dim nearestCity = knn.responseSet(i * knn.findXnearest + j)
                ' the last entry will never have a city to connect to so just connect with the nearest.
                If i = numberofCities - 1 Then
                    cityOrder(i) = nearestCity
                    Exit For
                End If
                If alreadyTaken.Contains(nearestCity) = False Then
                    cityOrder(i) = nearestCity
                    alreadyTaken.Add(nearestCity)
                    Exit For
                End If
            Next
        Next
        drawMap(result)
        Dim tmp As cv.Mat
        tmp = result.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim black As New cv.Vec3b(0, 0, 0)
        Dim white As New cv.Vec3b(0, 0, 0)
        Dim hitBlack As Int32
        For y = 0 To result.Rows - 1
            For x = 0 To result.Cols - 1
                Dim blackTest = result.Get(Of cv.Vec3b)(y, x)
                If blackTest = black Then
                    If rColors(closedRegions Mod rColors.Length) = black Then
                        hitBlack += 1
                        closedRegions += 1 ' skip the randomly generated black color as that is our key.
                    End If
                    Dim byteCount = cv.Cv2.FloodFill(result, New cv.Point(x, y), rColors(closedRegions Mod rColors.Length))
                    If byteCount > 10 Then closedRegions += 1 ' there are fake regions due to anti-alias like features that appear when drawing.
                End If
                Dim whiteTest = tmp.Get(Of Byte)(y, x)
                If whiteTest = 255 Then
                    cv.Cv2.FloodFill(tmp, New cv.Point(x, y), black)
                    totalClusters += 1
                End If
            Next
        Next
        If hitBlack Then closedRegions -= hitBlack
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ' If they changed Then number of elements in the set
        If sliders.TrackBar1.Value <> numberofCities Then
            numberofCities = sliders.TrackBar1.Value
            knn.findXnearest = numberofCities

            ReDim cityPositions(numberofCities - 1)
            ReDim cityOrder(numberofCities - 1)

            Dim gen As New System.Random()
            Dim r As New cv.RNG(gen.Next(0, 1000000))
            For i = 0 To numberofCities - 1
                cityPositions(i).X = r.Uniform(0, ocvb.color.Width)
                cityPositions(i).Y = r.Uniform(0, ocvb.color.Height)
            Next

            ' find the nearest neighbor for each city - first will be the current city, next will be nearest real neighbors in order
            ReDim knn.lastSet(numberofCities - 1)
            ReDim knn.querySet(numberofCities - 1)
            For i = 0 To numberofCities - 1
                knn.lastSet(i) = New cv.Point2f(CSng(cityPositions(i).X), CSng(cityPositions(i).Y))
                knn.querySet(i) = New cv.Point2f(CSng(cityPositions(i).X), CSng(cityPositions(i).Y))
            Next
            knn.Run(ocvb) ' run only one time.
            ocvb.result1.SetTo(0)
            cluster(ocvb.rColors, ocvb.result1)
            ocvb.label1 = "knn clusters total=" + CStr(totalClusters) + " closedRegions=" + CStr(closedRegions)
        End If
    End Sub
    Public Sub VBdispose()
                knn.Dispose()
    End Sub
End Class






Public Class knn_Point2d
    Inherits VB_Class
        Public querySet() As cv.Point2f
    Public responseSet() As Int32
    Public lastSet() As cv.Point2f ' default usage: find and connect points in 2D for this number of points.
    Public externalUse As Boolean
    Public findXnearest As Int32
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, "knn Query Points", 1, 50, 10)
        sliders.setupTrackBar2(ocvb, "knn k nearest points", 1, 5, 1)
        
        ocvb.desc = "Use KNN to connect 2D points."
        ocvb.label1 = "Yellow=Queries, Blue=Best Responses"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim knn = cv.ML.KNearest.Create()
        If externalUse = False Then
            ReDim lastSet(sliders.TrackBar1.Value - 1)
            ReDim querySet(sliders.TrackBar1.Value - 1)
            For i = 0 To lastSet.Count - 1
                lastSet(i) = New cv.Point2f(ocvb.ms_rng.Next(0, ocvb.result1.Cols), ocvb.ms_rng.Next(0, ocvb.result1.Rows))
            Next

            For i = 0 To querySet.Count - 1
                querySet(i) = New cv.Point2f(ocvb.ms_rng.Next(0, ocvb.result1.Cols), ocvb.ms_rng.Next(0, ocvb.result1.Rows))
            Next
            ocvb.result1.SetTo(0)
        End If
        Dim responses(lastSet.Length - 1) As Int32
        For i = 0 To responses.Length - 1
            responses(i) = i
        Next

        Dim trainData = New cv.Mat(lastSet.Length, 2, cv.MatType.CV_32F, lastSet)
        knn.Train(trainData, cv.ML.SampleTypes.RowSample, New cv.Mat(responses.Length, 1, cv.MatType.CV_32S, responses))

        Dim results As New cv.Mat, neighbors As New cv.Mat, query As New cv.Mat(1, 2, cv.MatType.CV_32F)
        If externalUse = False Then
            For i = 0 To lastSet.Count - 1
                cv.Cv2.Circle(ocvb.result1, lastSet(i), 9, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias, 0)
            Next
        End If

        If externalUse = False Then findXnearest = sliders.TrackBar2.Value
        ReDim responseSet(querySet.Length * findXnearest - 1)
        For i = 0 To querySet.Count - 1
            query.Set(Of cv.Point2f)(0, 0, querySet(i))
            knn.FindNearest(query, findXnearest, results, neighbors)
            For j = 0 To findXnearest - 1
                responseSet(i * findXnearest + j) = CInt(neighbors.Get(Of Single)(0, j))
            Next
            If externalUse = False Then
                For j = 0 To findXnearest - 1
                    ocvb.result1.Line(lastSet(responseSet(i * findXnearest + j)), querySet(i), cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                    cv.Cv2.Circle(ocvb.result1, querySet(i), 5, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias, 0)
                Next
            End If
        Next
    End Sub
    Public Sub VBdispose()
            End Sub
End Class







Public Class knn_Point3d
    Inherits VB_Class
        Public querySet() As cv.Point3f
    Public responseSet() As Int32
    Public lastSet() As cv.Point3f ' default usage: find and connect points in 2D for this number of points.
    Public externalUse As Boolean
    Public findXnearest As Int32
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, "knn Query Points", 1, 500, 10)
        sliders.setupTrackBar2(ocvb, "knn k nearest points", 0, 500, 1)
        
        ocvb.desc = "Use KNN to connect 3D points.  Results shown are a 2D projection of the 3D results."
        ocvb.label1 = "Yellow=Query (in 3D) Blue=Best Response (in 3D)"
        ocvb.label2 = "Top Down View to confirm 3D KNN is correct"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim maxDepth As Int32 = 4000 ' this is an arbitrary max dept    h
        Dim knn = cv.ML.KNearest.Create()
        If externalUse = False Then
            ReDim lastSet(sliders.TrackBar1.Value - 1)
            ReDim querySet(lastSet.Count - 1)
            For i = 0 To lastSet.Count - 1
                lastSet(i) = New cv.Point3f(ocvb.ms_rng.Next(0, ocvb.result1.Cols), ocvb.ms_rng.Next(0, ocvb.result1.Rows), ocvb.ms_rng.Next(0, maxDepth))
            Next

            For i = 0 To querySet.Count - 1
                querySet(i) = New cv.Point3f(ocvb.ms_rng.Next(0, ocvb.result1.Cols), ocvb.ms_rng.Next(0, ocvb.result1.Rows), ocvb.ms_rng.Next(0, maxDepth))
            Next

            ocvb.result1.SetTo(0)
            ocvb.result2.SetTo(0)
        End If
        Dim responses(lastSet.Length - 1) As Int32
        For i = 0 To responses.Length - 1
            responses(i) = i
        Next

        Dim trainData = New cv.Mat(lastSet.Length, 2, cv.MatType.CV_32F, lastSet)
        knn.Train(trainData, cv.ML.SampleTypes.RowSample, New cv.Mat(responses.Length, 1, cv.MatType.CV_32S, responses))

        Dim results As New cv.Mat, neighbors As New cv.Mat, query As New cv.Mat(1, 2, cv.MatType.CV_32F)
        For i = 0 To lastSet.Count - 1
            Dim p = New cv.Point2f(lastSet(i).X, lastSet(i).Y)
            ocvb.result1.Circle(p, 9, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
            p = New cv.Point2f(lastSet(i).X, lastSet(i).Z * ocvb.color.Rows / maxDepth)
            ocvb.result2.Circle(p, 9, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
        Next

        If externalUse = False Then findXnearest = sliders.TrackBar2.Value
        ReDim responseSet(querySet.Length * findXnearest - 1)
        For i = 0 To querySet.Count - 1
            query.Set(Of cv.Point3f)(0, 0, querySet(i))
            knn.FindNearest(query, findXnearest, results, neighbors)
            For j = 0 To findXnearest - 1
                responseSet(i * findXnearest + j) = CInt(neighbors.Get(Of Single)(0, j))
            Next
            If externalUse = False Then
                For j = 0 To findXnearest - 1
                    Dim plast = New cv.Point2f(lastSet(responseSet(i * findXnearest + j)).X, lastSet(responseSet(i * findXnearest + j)).Y)
                    Dim pQ = New cv.Point2f(querySet(i).X, querySet(i).Y)
                    ocvb.result1.Line(plast, pQ, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                    ocvb.result1.Circle(pQ, 5, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias, 0)

                    plast = New cv.Point2f(lastSet(responseSet(i * findXnearest + j)).X, lastSet(responseSet(i * findXnearest + j)).Z * ocvb.color.Rows / maxDepth)
                    pQ = New cv.Point2f(querySet(i).X, querySet(i).Z * ocvb.color.Rows / maxDepth)
                    ocvb.result2.Line(plast, pQ, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                    ocvb.result2.Circle(pQ, 5, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias, 0)
                Next
            End If
        Next
    End Sub
    Public Sub VBdispose()
            End Sub
End Class






Public Class knn_ClusterNoisyLine
    Inherits VB_Class
    Public noisyLine As Fitline_RawInput
    Public cityOrder() As Int32
    Public knn As knn_Point2d
    Dim numberofCities As Int32
    Public findXnearest As Int32 = 2
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        noisyLine = New Fitline_RawInput(ocvb, "knn_ClusterNoisyLine")
        knn = New knn_Point2d(ocvb, "knn_ClusterNoisyLine")
        knn.sliders.Visible = False
        knn.externalUse = True

        ocvb.desc = "Use knn to cluster the output of noisyline class."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static linePointCount As Int32
        Static lineNoise As Int32
        Static highlight As Boolean
        ' If they changed Then number of elements in the set or options, then recompute...
        If (noisyLine.sliders.TrackBar1.Value + noisyLine.sliders.TrackBar2.Value) <> numberofCities Or noisyLine.sliders.TrackBar3.Value <> lineNoise Or
            noisyLine.check.Box(0).Checked <> highlight Or noisyLine.check.Box(1).Checked = True Then

            linePointCount = noisyLine.sliders.TrackBar2.Value
            lineNoise = noisyLine.sliders.TrackBar3.Value
            highlight = noisyLine.check.Box(0).Checked
            noisyLine.check.Box(1).Checked = False
            numberofCities = noisyLine.sliders.TrackBar1.Value + linePointCount
            ReDim cityOrder(numberofCities - 1)
            noisyLine.Run(ocvb)

            knn.findXnearest = findXnearest

            ' find the nearest neighbor for each city - first will be the current city, next will be nearest real neighbors in order
            ReDim knn.lastSet(numberofCities - 1)
            ReDim knn.querySet(numberofCities - 1)
            For i = 0 To numberofCities - 1
                knn.lastSet(i) = noisyLine.points(i)
                knn.querySet(i) = noisyLine.points(i)
            Next
            knn.Run(ocvb) ' run only one time.
            ocvb.result2.SetTo(0)
            For i = 0 To numberofCities - 1
                Dim nearestCity = knn.responseSet(i * knn.findXnearest + 1)
                cityOrder(i) = nearestCity
            Next

            ' draw the map
            For i = 0 To cityOrder.Length - 1
                ocvb.result2.Circle(noisyLine.points(i), 5, cv.Scalar.White, -1)
                ocvb.result2.Line(noisyLine.points(i), noisyLine.points(cityOrder(i)), cv.Scalar.White, 2)
            Next

            Dim tmp As cv.Mat
            tmp = ocvb.result2.Clone()
            Dim black As New cv.Vec3b(0, 0, 0)
            Dim totalClusters As Int32
            For y = 0 To tmp.Rows - 1
                For x = 0 To tmp.Cols - 1
                    If tmp.Get(of Byte)(y, x) = 255 Then
                        Dim byteCount = cv.Cv2.FloodFill(tmp, New cv.Point(x, y), black)
                        totalClusters += 1
                    End If
                Next
            Next
            ocvb.label2 = "knn clusters total=" + CStr(totalClusters)
            ocvb.label1 = "Input points = " + CStr(numberofCities)
        End If
    End Sub
    Public Sub VBdispose()
        knn.Dispose()
        noisyLine.Dispose()
    End Sub
End Class
