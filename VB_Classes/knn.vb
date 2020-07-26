Imports cv = OpenCvSharp
Public Class KNN_Basics
    Inherits ocvbClass
    Dim random As Random_Points
    Public trainingPoints As New List(Of cv.Point2f)
    Public trainPointsUsed() As Integer
    Public queryPoints As New List(Of cv.Point2f)
    Public matchedPoints() As cv.Point2f
    Public matchedIndex() As Integer
    Public responseColor As cv.Mat
    Dim knn As cv.ML.KNearest
    Public retrainNeeded As Boolean = True
    Public noDuplicates As Boolean
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        If standalone Then
            random = New Random_Points(ocvb)
        End If
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "knn Query Points", 1, 10000, 10)
        sliders.setupTrackBar(1, "knn output Points", 1, 1000, 1)
        label1 = "Query=Red nearest=white blue=unmatched"
        label2 = "Query points"
        knn = cv.ML.KNearest.Create()
        ocvb.desc = "Test knn with random points in the image.  Find the nearest to a random point."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim trainData As cv.Mat
        Dim queries As cv.Mat
        dst1.SetTo(cv.Scalar.Black)
        If trainingPoints.Count <> queryPoints.Count Then
            Dim i = 0
        End If
        If standalone Then
            random.Run(ocvb)
            trainData = New cv.Mat(random.Points2f.Count, 2, cv.MatType.CV_32F, random.Points2f).Clone()

            random.Run(ocvb)
            queries = New cv.Mat(sliders.trackbar(0).Value, 2, cv.MatType.CV_32F, random.Points2f)
        Else
            queries = New cv.Mat(queryPoints.Count, 2, cv.MatType.CV_32F, queryPoints.ToArray)
            trainData = New cv.Mat(trainingPoints.Count, 2, cv.MatType.CV_32F, trainingPoints.ToArray)
        End If

        If retrainNeeded Then
            Dim response = New cv.Mat(trainData.Rows, 1, cv.MatType.CV_32S)
            For i = 0 To trainData.Rows - 1
                response.Set(Of Integer)(i, 0, i)
                cv.Cv2.Circle(dst1, trainData.Get(Of cv.Point2f)(i, 0), 3, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias, 0)
            Next
            knn.Train(trainData, cv.ML.SampleTypes.RowSample, response)
        End If
        ReDim matchedPoints(queries.Rows - 1)
        ReDim matchedIndex(queries.Rows - 1)
        ReDim trainPointsUsed(trainingPoints.Count - 1)

        Dim desiredMatches = sliders.trackbar(1).Value

        Dim results As New cv.Mat
        Dim neighbors As New cv.Mat
        For i = 0 To queries.Rows - 1
            Dim query = queries(New cv.Rect(0, i, 2, 1))
            knn.FindNearest(query, desiredMatches, results, neighbors)
            Dim qPoint = query.Get(Of cv.Point2f)(0, 0)
            For j = 0 To Math.Min(desiredMatches, neighbors.Cols) - 1
                matchedIndex(i) = CInt(neighbors.Get(Of Single)(0, j))
                trainPointsUsed(matchedIndex(i)) = True
                matchedPoints(i) = trainData.Get(Of cv.Point2f)(matchedIndex(i), 0)
                If noDuplicates Then
                    For k = 0 To i - 1
                        If matchedIndex(i) = matchedIndex(k) Then
                            ' pick the closest one 
                            Dim distance1 = Math.Sqrt((matchedPoints(i).X - qPoint.X) * (matchedPoints(i).X - qPoint.X) + (matchedPoints(i).Y - qPoint.Y) * (matchedPoints(i).Y - qPoint.Y))
                            Dim pt2 = queryPoints.ElementAt(k)
                            Dim distance2 = Math.Sqrt((matchedPoints(i).X - pt2.X) * (matchedPoints(i).X - pt2.X) + (matchedPoints(i).Y - pt2.Y) * (matchedPoints(i).Y - pt2.Y))
                            If distance1 < distance2 Then matchedIndex(k) = -1 Else matchedIndex(i) = -1
                        End If
                    Next
                End If
            Next
        Next

        For i = 0 To matchedPoints.Count - 1
            If matchedIndex(i) >= 0 Then
                Dim qPoint = queryPoints.ElementAt(i)
                cv.Cv2.Circle(dst1, qPoint, 3, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias, 0)
                Dim result = trainingPoints.ElementAt(matchedIndex(i))
                cv.Cv2.Circle(dst1, result, 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias, 0)
                dst1.Line(result, qPoint, cv.Scalar.Red, 1, cv.LineTypes.AntiAlias)
            End If
        Next
    End Sub
End Class





Public Class KNN_Test
    Inherits ocvbClass
    Public grid As Thread_Grid
    Dim knn As KNN_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        grid = New Thread_Grid(ocvb)
        grid.sliders.trackbar(0).Value = 100
        grid.sliders.trackbar(1).Value = 100

        knn = New KNN_Basics(ocvb)
        knn.sliders.trackbar(1).Value = 1 ' query one point at a time.

        label1 = "KNN_Basics: black=nearest, query=red, unmatched=blue"
        ocvb.desc = "Assign random values inside a thread grid to test that KNN is properly tracking them."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)

        Dim queries As New List(Of cv.Point2f)
        For i = 0 To grid.roiList.Count - 1
            Dim roi = grid.roiList.ElementAt(i)
            Dim pt = New cv.Point2f(roi.X + msRNG.Next(roi.Width), roi.Y + msRNG.Next(roi.Height))
            queries.Add(pt)
        Next
        knn.queryPoints = queries

        If ocvb.frameCount > 0 Then
            knn.retrainNeeded = True
            knn.dst1.SetTo(cv.Scalar.Beige)
            knn.Run(ocvb)
            dst1 = knn.dst1
            dst1.SetTo(cv.Scalar.Black, grid.gridMask)
        End If
        knn.trainingPoints = knn.queryPoints
    End Sub
End Class





Public Class KNN_Centroids
    Inherits ocvbClass
    Dim emax As EMax_Centroids
    Dim knn As KNN_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        emax = New EMax_Centroids(ocvb)
        emax.Run(ocvb)
        dst1 = emax.emaxCPP.dst2.Clone()

        knn = New KNN_Basics(ocvb)
        knn.retrainNeeded = True
        knn.sliders.trackbar(1).Value = 1
        knn.responseColor = New cv.Mat(scalarColors.Count, 3, cv.MatType.CV_32F, scalarColors.ToArray)

        label1 = "Current image"
        label2 = "Query=Red nearest=white unmatched=blue"
        ocvb.desc = "Map the current centroids to the previous generation and match the color used."
    End Sub
    Private Sub copyList(a As List(Of cv.Point2f), b As List(Of cv.Point2f))
        b.Clear()
        For i = 0 To a.Count - 1
            b.Add(a.ElementAt(i))
        Next
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dst1.SetTo(0)

        copyList(emax.centroids, knn.trainingPoints)

        emax.Run(ocvb)
        dst1 = emax.emaxCPP.dst2.Clone

        copyList(emax.centroids, knn.queryPoints)

        knn.Run(ocvb)

        Dim maskRect = New cv.Rect(1, 1, dst1.Width, dst1.Height)
        Dim maskPlus = New cv.Mat(New cv.Size(dst1.Width + 2, dst1.Height + 2), cv.MatType.CV_8UC1, 0)
        maskPlus.Rectangle(maskRect, cv.Scalar.White, 2)

        Dim rect As New cv.Rect
        Static lastImage = emax.emaxCPP.dst2.Clone
        For i = 0 To knn.matchedPoints.Count - 1
            knn.matchedPoints(i).X += 10
            Dim nextVec = lastImage.Get(Of cv.Vec3b)(knn.matchedPoints(i).Y, knn.matchedPoints(i).X)
            Dim nextColor = New cv.Scalar(nextVec.Item0, nextVec.Item1, nextVec.Item2)
            cv.Cv2.FloodFill(dst1, maskPlus, knn.matchedPoints(i), nextColor, rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8) Or 4)
            Dim fontSize = If(ocvb.parms.resolution = resHigh, 0.8, 0.5)
        Next
        lastImage = dst1.Clone
        dst2 = lastImage.Clone()
        cv.Cv2.BitwiseOr(dst2, knn.dst1, dst2)
    End Sub
End Class





Public Class KNN_Cluster2D
    Inherits ocvbClass
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
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        knn = New knn_Point2d(ocvb)
        knn.sliders.Visible = False

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "knn - number of cities", 10, 1000, 100)
        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Demo Mode (continuous update)"
        If ocvb.parms.testAllRunning Then check.Box(0).Checked = True

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
        If sliders.trackbar(0).Value <> numberofCities Or check.Box(0).Checked Then
            numberofCities = sliders.trackbar(0).Value
            knn.findXnearest = numberofCities

            ReDim cityPositions(numberofCities - 1)
            ReDim cityOrder(numberofCities - 1)

            Dim gen As New System.Random()
            Dim r As New cv.RNG(gen.Next(0, 1000000))
            For i = 0 To numberofCities - 1
                cityPositions(i).X = r.Uniform(0, src.Width)
                cityPositions(i).Y = r.Uniform(0, src.Height)
            Next

            ' find the nearest neighbor for each city - first will be the current city, next will be nearest real neighbors in order
            ReDim knn.lastSet(numberofCities - 1)
            ReDim knn.querySet(numberofCities - 1)
            For i = 0 To numberofCities - 1
                knn.lastSet(i) = New cv.Point2f(CSng(cityPositions(i).X), CSng(cityPositions(i).Y))
                knn.querySet(i) = New cv.Point2f(CSng(cityPositions(i).X), CSng(cityPositions(i).Y))
            Next
            knn.Run(ocvb) ' run only one time.
            dst1.SetTo(0)
            totalClusters = 0
            closedRegions = 0
            cluster(rColors, dst1)
            label1 = "knn clusters total=" + CStr(totalClusters) + " closedRegions=" + CStr(closedRegions)
        End If
    End Sub
End Class




Public Class KNN_Point2d
    Inherits ocvbClass
    Public querySet() As cv.Point2f
    Public responseSet() As Int32
    Public lastSet() As cv.Point2f ' default usage: find and connect points in 2D for this number of points.
    Public findXnearest As Int32
    Dim knn As cv.ML.KNearest
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "knn Query Points", 1, 50, 10)
        sliders.setupTrackBar(1, "knn k nearest points", 1, 5, 1)

        ocvb.desc = "Use KNN to connect 2D points."
        label1 = "Yellow=Queries, Blue=Best Responses"
        knn = cv.ML.KNearest.Create()
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Then
            ReDim lastSet(sliders.trackbar(0).Value - 1)
            ReDim querySet(sliders.trackbar(0).Value - 1)
            For i = 0 To lastSet.Count - 1
                lastSet(i) = New cv.Point2f(msRNG.Next(0, dst1.Cols), msRNG.Next(0, dst1.Rows))
            Next

            For i = 0 To querySet.Count - 1
                querySet(i) = New cv.Point2f(msRNG.Next(0, dst1.Cols), msRNG.Next(0, dst1.Rows))
            Next
        End If
        Dim responses(lastSet.Length - 1) As Int32
        For i = 0 To responses.Length - 1
            responses(i) = i
        Next

        Dim trainData = New cv.Mat(lastSet.Length, 2, cv.MatType.CV_32F, lastSet)
        knn.Train(trainData, cv.ML.SampleTypes.RowSample, New cv.Mat(responses.Length, 1, cv.MatType.CV_32S, responses))

        Dim results As New cv.Mat, neighbors As New cv.Mat, query As New cv.Mat(1, 2, cv.MatType.CV_32F)
        dst1.SetTo(0)
        If standalone Then
            For i = 0 To lastSet.Count - 1
                cv.Cv2.Circle(dst1, lastSet(i), 9, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias, 0)
            Next
        End If

        If standalone Then findXnearest = sliders.trackbar(1).Value
        ReDim responseSet(querySet.Length * findXnearest - 1)
        For i = 0 To querySet.Count - 1
            query.Set(Of cv.Point2f)(0, 0, querySet(i))
            knn.FindNearest(query, findXnearest, results, neighbors)
            For j = 0 To findXnearest - 1
                responseSet(i * findXnearest + j) = CInt(neighbors.Get(Of Single)(0, j))
            Next
            If standalone Then
                For j = 0 To findXnearest - 1
                    dst1.Line(lastSet(responseSet(i * findXnearest + j)), querySet(i), cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                    cv.Cv2.Circle(dst1, querySet(i), 5, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias, 0)
                Next
            End If
        Next
    End Sub
End Class




Public Class KNN_Point3d
    Inherits ocvbClass
    Public querySet() As cv.Point3f
    Public responseSet() As Int32
    Public lastSet() As cv.Point3f ' default usage: find and connect points in 2D for this number of points.
    Public findXnearest As Int32
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "knn Query Points", 1, 500, 10)
        sliders.setupTrackBar(1, "knn k nearest points", 0, 500, 1)

        ocvb.desc = "Use KNN to connect 3D points.  Results shown are a 2D projection of the 3D results."
        label1 = "Yellow=Query (in 3D) Blue=Best Response (in 3D)"
        label2 = "Top Down View to confirm 3D KNN is correct"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim maxDepth As Int32 = 4000 ' this is an arbitrary max dept    h
        Dim knn = cv.ML.KNearest.Create()
        If standalone Then
            ReDim lastSet(sliders.trackbar(0).Value - 1)
            ReDim querySet(lastSet.Count - 1)
            For i = 0 To lastSet.Count - 1
                lastSet(i) = New cv.Point3f(msRNG.Next(0, dst1.Cols), msRNG.Next(0, dst1.Rows), msRNG.Next(0, maxDepth))
            Next

            For i = 0 To querySet.Count - 1
                querySet(i) = New cv.Point3f(msRNG.Next(0, dst1.Cols), msRNG.Next(0, dst1.Rows), msRNG.Next(0, maxDepth))
            Next
        End If
        Dim responses(lastSet.Length - 1) As Int32
        For i = 0 To responses.Length - 1
            responses(i) = i
        Next

        Dim trainData = New cv.Mat(lastSet.Length, 2, cv.MatType.CV_32F, lastSet)
        knn.Train(trainData, cv.ML.SampleTypes.RowSample, New cv.Mat(responses.Length, 1, cv.MatType.CV_32S, responses))

        Dim results As New cv.Mat, neighbors As New cv.Mat, query As New cv.Mat(1, 2, cv.MatType.CV_32F)
        dst1.SetTo(0)
        dst2.SetTo(0)
        For i = 0 To lastSet.Count - 1
            Dim p = New cv.Point2f(lastSet(i).X, lastSet(i).Y)
            dst1.Circle(p, 9, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
            p = New cv.Point2f(lastSet(i).X, lastSet(i).Z * src.Rows / maxDepth)
            dst2.Circle(p, 9, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
        Next

        If standalone Then findXnearest = sliders.trackbar(1).Value
        ReDim responseSet(querySet.Length * findXnearest - 1)
        For i = 0 To querySet.Count - 1
            query.Set(Of cv.Point3f)(0, 0, querySet(i))
            knn.FindNearest(query, findXnearest, results, neighbors)
            For j = 0 To findXnearest - 1
                responseSet(i * findXnearest + j) = CInt(neighbors.Get(Of Single)(0, j))
            Next
            If standalone Then
                For j = 0 To findXnearest - 1
                    Dim plast = New cv.Point2f(lastSet(responseSet(i * findXnearest + j)).X, lastSet(responseSet(i * findXnearest + j)).Y)
                    Dim pQ = New cv.Point2f(querySet(i).X, querySet(i).Y)
                    dst1.Line(plast, pQ, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                    dst1.Circle(pQ, 5, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias, 0)

                    plast = New cv.Point2f(lastSet(responseSet(i * findXnearest + j)).X, lastSet(responseSet(i * findXnearest + j)).Z * src.Rows / maxDepth)
                    pQ = New cv.Point2f(querySet(i).X, querySet(i).Z * src.Rows / maxDepth)
                    dst2.Line(plast, pQ, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                    dst2.Circle(pQ, 5, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias, 0)
                Next
            End If
        Next
    End Sub
End Class




Public Class KNN_ClusterNoisyLine
    Inherits ocvbClass
    Public noisyLine As Fitline_RawInput
    Public cityOrder() As Int32
    Public knn As KNN_Point2d
    Dim numberofCities As Int32
    Public findXnearest As Int32 = 2
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        noisyLine = New Fitline_RawInput(ocvb)
        knn = New KNN_Point2d(ocvb)
        knn.sliders.Visible = False

        ocvb.desc = "Use knn to cluster the output of noisyline class."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static linePointCount As Int32
        Static lineNoise As Int32
        Static highlight As Boolean
        ' If the number of elements in the set changes, then recompute...
        If (noisyLine.sliders.trackbar(0).Value + noisyLine.sliders.trackbar(1).Value) <> numberofCities Or noisyLine.sliders.trackbar(2).Value <> lineNoise Or
            noisyLine.check.Box(0).Checked <> highlight Or noisyLine.check.Box(1).Checked = True Then

            linePointCount = noisyLine.sliders.trackbar(1).Value
            lineNoise = noisyLine.sliders.trackbar(2).Value
            highlight = noisyLine.check.Box(0).Checked
            noisyLine.check.Box(1).Checked = True
            numberofCities = noisyLine.sliders.trackbar(0).Value + linePointCount
            ReDim cityOrder(numberofCities - 1)
            noisyLine.Run(ocvb)
            dst1 = noisyLine.dst1

            knn.findXnearest = findXnearest

            ' find the nearest neighbor for each city - first will be the current city, next will be nearest real neighbors in order
            ReDim knn.lastSet(numberofCities - 1)
            ReDim knn.querySet(numberofCities - 1)
            For i = 0 To numberofCities - 1
                knn.lastSet(i) = noisyLine.points(i)
                knn.querySet(i) = noisyLine.points(i)
            Next
            knn.Run(ocvb) ' run only one time.
            dst2.SetTo(0)
            For i = 0 To numberofCities - 1
                Dim nearestCity = knn.responseSet(i * knn.findXnearest + 1)
                cityOrder(i) = nearestCity
            Next

            ' draw the map
            For i = 0 To cityOrder.Length - 1
                dst2.Circle(noisyLine.points(i), 5, cv.Scalar.White, -1)
                dst2.Line(noisyLine.points(i), noisyLine.points(cityOrder(i)), cv.Scalar.White, 2)
            Next

            Dim tmp As cv.Mat
            tmp = dst2.Clone()
            Dim black As New cv.Vec3b(0, 0, 0)
            Dim totalClusters As Int32
            For y = 0 To tmp.Rows - 1
                For x = 0 To tmp.Cols - 1
                    If tmp.Get(Of Byte)(y, x) = 255 Then
                        Dim byteCount = cv.Cv2.FloodFill(tmp, New cv.Point(x, y), black)
                        totalClusters += 1
                    End If
                Next
            Next
            label2 = "knn clusters total=" + CStr(totalClusters)
            label1 = "Input points = " + CStr(numberofCities)
        End If
    End Sub
End Class

