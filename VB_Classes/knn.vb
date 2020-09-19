Imports cv = OpenCvSharp

Public Class KNN_QueryTrain
    Inherits VBparent
    Public trainingPoints As New List(Of cv.Point2f)
    Public queryPoints As New List(Of cv.Point2f)
    Public randomTrain As Random_Points
    Public randomQuery As Random_Points
    Public useRandomData As Boolean
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "KNN Query count", 1, 100, 10)
        sliders.setupTrackBar(1, "KNN Train count", 1, 100, 20)
        sliders.setupTrackBar(2, "KNN k nearest points", 1, 5, 1)

        If standalone Then
            check.Setup(ocvb, caller, 1)
            check.Box(0).Text = "Reuse the training and query data"
        End If

        randomTrain = New Random_Points(ocvb)
        randomTrain.sliders.Visible = False
        randomQuery = New Random_Points(ocvb)
        randomQuery.sliders.Visible = False

        label1 = "Random training points"
        label2 = "Random query points"
        desc = "Source of query/train points - generate points if standalone.  Reuse points if requested."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        ' algorithm does nothing but provide a location for query/train points when not running standalone.
        If standalone Or useRandomData Then
            Static reuseCheck = findCheckBox("Reuse the training and query data")
            If reuseCheck.Checked = False Then
                Static trainSlider = findSlider("KNN Train count")
                randomTrain.sliders.trackbar(0).Value = trainSlider.Value
                randomTrain.Run(ocvb)

                Static querySlider = findSlider("KNN Query count")
                randomQuery.sliders.trackbar(0).Value = querySlider.Value
                randomQuery.Run(ocvb)
            End If

            If standalone Then
                ' query/train points need to be manufactured when standalone
                trainingPoints = New List(Of cv.Point2f)(randomTrain.Points2f)
                queryPoints = New List(Of cv.Point2f)(randomQuery.Points2f)

                dst1.SetTo(cv.Scalar.White)
                dst2.SetTo(cv.Scalar.White)
                For i = 0 To randomTrain.Points2f.Count - 1
                    Dim pt = randomTrain.Points2f(i)
                    cv.Cv2.Circle(dst1, pt, 5, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias, 0)
                Next
                For i = 0 To randomQuery.Points2f.Count - 1
                    Dim pt = randomQuery.Points2f(i)
                    cv.Cv2.Circle(dst2, pt, 5, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias, 0)
                Next
            End If
        End If
    End Sub
End Class







Public Class KNN_Basics
    Inherits VBparent
    Public neighbors As New cv.Mat
    Public testMode As Boolean
    Public desiredMatches = 1
    Public knn As cv.ML.KNearest
    Public knnQT As KNN_QueryTrain
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        knnQT = New KNN_QueryTrain(ocvb)

        label1 = "White=TrainingData, Red=queries"
        knn = cv.ML.KNearest.Create()
        desc = "Test knn with random points in the image.  Find the nearest n points."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        dst1.SetTo(cv.Scalar.Black)

        If standalone Or knnQT.useRandomData Then
            knnQT.Run(ocvb)
            knnQT.trainingPoints = New List(Of cv.Point2f)(knnQT.randomTrain.Points2f)
            knnQT.queryPoints = New List(Of cv.Point2f)(knnQT.randomQuery.Points2f)
        Else
            If knnQT.queryPoints.Count = 0 Then Exit Sub ' nothing to do on this generation...
        End If
        ' The first generation may not have any training data, only queries.  (Queries move to training on subsequent generations.)
        If knnQT.trainingPoints.Count = 0 Then knnQT.trainingPoints = New List(Of cv.Point2f)(knnQT.queryPoints)

        Dim queries = New cv.Mat(knnQT.queryPoints.Count, 2, cv.MatType.CV_32F, knnQT.queryPoints.ToArray)
        Dim trainData = New cv.Mat(knnQT.trainingPoints.Count, 2, cv.MatType.CV_32F, knnQT.trainingPoints.ToArray)

        Dim response = New cv.Mat(trainData.Rows, 1, cv.MatType.CV_32S)
        For i = 0 To trainData.Rows - 1
            response.Set(Of Integer)(i, 0, i)
            cv.Cv2.Circle(dst1, trainData.Get(Of cv.Point2f)(i, 0), 5, cv.Scalar.White, -1, cv.LineTypes.AntiAlias, 0)
        Next
        knn.Train(trainData, cv.ML.SampleTypes.RowSample, response)
        knn.FindNearest(queries, desiredMatches, New cv.Mat, neighbors)

        If standalone Or testMode Then
            For i = 0 To neighbors.Rows - 1
                Dim qPoint = queries.Get(Of cv.Point2f)(i, 0)
                cv.Cv2.Circle(dst1, qPoint, 3, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias, 0)
                Dim pt = trainData.Get(Of cv.Point2f)(neighbors.Get(Of Single)(i, 0), 0)
                dst1.Line(pt, qPoint, cv.Scalar.Red, 1, cv.LineTypes.AntiAlias)
            Next
        End If
    End Sub
End Class





Public Class KNN_1_to_1
    Inherits VBparent
    Public matchedPoints() As cv.Point2f
    Public unmatchedPoints As New List(Of cv.Point2f)
    Public basics As KNN_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        basics = New KNN_Basics(ocvb)
        If standalone Then basics.knnQT.useRandomData = True Else basics.knnQT.sliders.Visible = False ' with 1:1, no need to adjust train/query counts.
        basics.desiredMatches = 4 ' more than 1 to insure there are secondary choices below for 1:1 matching below.

        label1 = "White=TrainingData, Red=queries, yellow=unmatched"
        desc = "Use knn to find the nearest n points but use only the best and no duplicates - 1:1 mapping."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        basics.Run(ocvb)
        dst1 = basics.dst1

        ReDim matchedPoints(basics.knnQT.queryPoints.Count - 1)
        Dim neighborOffset(basics.knnQT.queryPoints.Count - 1) As Integer
        For i = 0 To matchedPoints.Count - 1
            matchedPoints(i) = basics.knnQT.trainingPoints(basics.neighbors.Get(Of Single)(i, 0))
        Next

        ' map the points 1 to 1: find duplicate best fits, choose which is better.
        ' loser must relinquish the training data element And use its next neighbor
        Dim changedNeighbors As Boolean = True
        While changedNeighbors
            changedNeighbors = False
            For i = 0 To matchedPoints.Count - 1
                Dim m1 = matchedPoints(i)
                For j = i + 1 To matchedPoints.Count - 1
                    Dim m2 = matchedPoints(j)
                    If m1.X = -1 Or m2.X = -1 Then Continue For
                    If m1 = m2 Then
                        changedNeighbors = True
                        Dim pt1 = basics.knnQT.queryPoints(i)
                        Dim pt2 = basics.knnQT.queryPoints(j)
                        Dim distance1 = Math.Sqrt((pt1.X - m1.X) * (pt1.X - m1.X) + (pt1.Y - m1.Y) * (pt1.Y - m1.Y))
                        Dim distance2 = Math.Sqrt((pt2.X - m1.X) * (pt2.X - m1.X) + (pt2.Y - m1.Y) * (pt2.Y - m1.Y))
                        Dim ij = If(distance1 > distance2, i, j)
                        Dim unresolved = True
                        If ij < neighborOffset.Length Then
                            If neighborOffset(ij) < basics.neighbors.Rows - 1 Then
                                neighborOffset(ij) += 1
                                Dim index = basics.neighbors.Get(Of Single)(neighborOffset(ij))
                                If index < basics.knnQT.trainingPoints.Count And index >= 0 Then
                                    unresolved = False
                                    matchedPoints(ij) = basics.knnQT.trainingPoints(index)
                                End If
                            End If
                        End If
                        If unresolved Then
                            matchedPoints(ij) = New cv.Point2f(-1, -1)
                            Exit For
                        End If
                    End If
                Next
            Next
        End While

        unmatchedPoints.Clear()
        For i = 0 To matchedPoints.Count - 1
            Dim mpt = matchedPoints(i)
            Dim qPoint = basics.knnQT.queryPoints(i)
            If mpt.X >= 0 Then
                cv.Cv2.Circle(dst1, qPoint, 3, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias, 0)
                dst1.Line(mpt, qPoint, cv.Scalar.Red, 1, cv.LineTypes.AntiAlias)
            Else
                unmatchedPoints.Add(qPoint)
                cv.Cv2.Circle(dst1, qPoint, 3, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias, 0)
            End If
        Next
    End Sub
End Class






Public Class KNN_Emax
    Inherits VBparent
    Public knn As KNN_1_to_1
    Dim emax As EMax_Centroids
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        If standalone Then
            emax = New EMax_Centroids(ocvb)
            emax.Run(ocvb) ' set the first generation of points.
        End If

        check.Setup(ocvb, caller, 3)
        check.Box(0).Text = "Map queries to training data 1:1 (Off means many:1)"
        check.Box(1).Text = "Display queries"
        check.Box(2).Text = "Display training input and connecting line"
        check.Box(0).Checked = True
        check.Box(1).Checked = True
        check.Box(2).Checked = True

        knn = New KNN_1_to_1(ocvb)
        knn.basics.knnQT.useRandomData = False

        label1 = "Output from Emax"
        label2 = "White=TrainingData, Red=queries yellow=unmatched"
        desc = "Emax centroids move but here KNN is used to matched the old and new locations and keep the colors the same."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If standalone Then
            knn.basics.knnQT.trainingPoints = New List(Of cv.Point2f)(emax.centroids)
            emax.Run(ocvb)
            knn.basics.knnQT.queryPoints = New List(Of cv.Point2f)(emax.centroids)
        End If

        knn.Run(ocvb)
        If standalone Then
            dst1 = emax.dst1 + knn.dst1
            dst2 = knn.dst1
        Else
            dst1 = knn.dst1
        End If
    End Sub
End Class




Public Class KNN_CentroidsEMax
    Inherits VBparent
    Public emax As EMax_Centroids
    Public knnEmax As KNN_Emax
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        emax = New EMax_Centroids(ocvb)
        knnEmax = New KNN_Emax(ocvb)

        label1 = "Colors are much more consistent across generations"
        label2 = "Raw EMax output with random coloring."
        desc = "Map the current centroids to the previous generation to match the color used."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If standalone Then knnEmax.knn.basics.knnQT.trainingPoints = New List(Of cv.Point2f)(emax.centroids)

        emax.Run(ocvb)

        knnEmax.knn.basics.knnQT.queryPoints = New List(Of cv.Point2f)(emax.centroids)
        knnEmax.Run(ocvb)

        Dim maskPlus = New cv.Mat(New cv.Size(dst1.Width + 2, dst1.Height + 2), cv.MatType.CV_8UC1, 0)
        Dim rect As New cv.Rect
        Static lastImage = emax.emaxCPP.dst2.Clone
        dst1 = emax.emaxCPP.dst2.Clone()
        If knnEmax.knn.matchedPoints IsNot Nothing Then ' no centroids are matched on the first pass.
            For Each pt In knnEmax.knn.matchedPoints
                If pt.X >= 0 Then
                    Dim nextVec = lastImage.Get(Of cv.Vec3b)(pt.Y, pt.X)
                    Dim nextColor = New cv.Scalar(nextVec.item0, nextVec.item1, nextVec.item2)
                    cv.Cv2.FloodFill(dst1, maskPlus, pt, nextColor, rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8) Or 4)
                End If
            Next
        End If
        lastImage = dst1.Clone
        dst2 = emax.emaxCPP.dst2
    End Sub
End Class





Public Class KNN_Test
    Inherits VBparent
    Public grid As Thread_Grid
    Dim knn As KNN_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        grid = New Thread_Grid(ocvb)
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Minimum = 50 ' limit the number of centroids - KNN can't handle more than a few thousand without rework.
        gridHeightSlider.Minimum = 50
        gridWidthSlider.Value = 100
        gridHeightSlider.Value = 100

        knn = New KNN_Basics(ocvb)
        knn.sliders.Visible = False
        knn.testMode = True

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Show grid mask"

        desc = "Assign random values inside a thread grid to test that KNN is properly tracking them."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        grid.Run(ocvb)

        knn.knnQT.queryPoints.Clear()
        For i = 0 To grid.roiList.Count - 1
            Dim roi = grid.roiList.ElementAt(i)
            Dim pt = New cv.Point2f(roi.X + msRNG.Next(roi.Width), roi.Y + msRNG.Next(roi.Height))
            knn.knnQT.queryPoints.Add(pt)
        Next

        knn.Run(ocvb)
        dst1 = knn.dst1
        knn.knnQT.trainingPoints = New List(Of cv.Point2f)(knn.knnQT.queryPoints)
        label1 = knn.label1
        If check.Box(0).Checked Then dst1.SetTo(cv.Scalar.White, grid.gridMask)
    End Sub
End Class





Public Class KNN_Test_1_to_1
    Inherits VBparent
    Public grid As Thread_Grid
    Dim knn As KNN_1_to_1
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        grid = New Thread_Grid(ocvb)
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Minimum = 50 ' limit the number of centroids - KNN can't handle more than a few thousand without rework.
        gridHeightSlider.Minimum = 50
        gridWidthSlider.Value = 100
        gridHeightSlider.Value = 100

        knn = New KNN_1_to_1(ocvb)
        knn.basics.sliders.Visible = False

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Show grid mask"

        desc = "Assign random values inside a thread grid to test that KNN is properly tracking them."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        grid.Run(ocvb)

        knn.basics.knnQT.queryPoints.Clear()
        For i = 0 To grid.roiList.Count - 1
            Dim roi = grid.roiList.ElementAt(i)
            Dim pt = New cv.Point2f(roi.X + msRNG.Next(roi.Width), roi.Y + msRNG.Next(roi.Height))
            knn.basics.knnQT.queryPoints.Add(pt)
        Next

        knn.Run(ocvb)
        dst1 = knn.dst1
        knn.basics.knnQT.trainingPoints = New List(Of cv.Point2f)(knn.basics.knnQT.queryPoints)
        label1 = knn.label1
        If check.Box(0).Checked Then dst1.SetTo(cv.Scalar.White, grid.gridMask)
    End Sub
End Class







Public Class KNN_Cluster2D
    Inherits VBparent
    Dim knn As KNN_Point2d
    Public cityPositions() As cv.Point
    Public cityOrder() As Int32
    Public distances() As Int32
    Dim numberOfCities As Int32
    Dim closedRegions As Int32
    Dim totalClusters As Int32
    Public Sub drawMap(result As cv.Mat)
        For i = 0 To cityOrder.Length - 1
            result.Circle(cityPositions(i), 5, cv.Scalar.White, -1)
            result.Line(cityPositions(i), cityPositions(cityOrder(i)), cv.Scalar.White, 2)
        Next
    End Sub
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        knn = New KNN_Point2d(ocvb)
        knn.sliders.Visible = False

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "KNN - number of cities", 10, 1000, 100)
        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Demo Mode (continuous update)"
        If ocvb.testAllRunning Then check.Box(0).Checked = True

        desc = "Use knn to cluster cities as preparation for a solution to the traveling salesman problem."
    End Sub
    Private Sub cluster(result As cv.Mat)
        Dim alreadyTaken As New List(Of Int32)
        For i = 0 To numberOfCities - 1
            For j = 1 To numberOfCities - 1
                Dim nearestCity = knn.responseSet(i * knn.findXnearest + j)
                ' the last entry will never have a city to connect to so just connect with the nearest.
                If i = numberOfCities - 1 Then
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
        Dim hitBlack As Int32
        For y = 0 To result.Rows - 1
            For x = 0 To result.Cols - 1
                Dim blackTest = result.Get(Of cv.Vec3b)(y, x)
                If blackTest = cv.Scalar.Black Then
                    If rColors(closedRegions Mod rColors.Length) = cv.Scalar.Black Then
                        hitBlack += 1
                        closedRegions += 1 ' skip the randomly generated black color as that is our key.
                    End If
                    Dim byteCount = cv.Cv2.FloodFill(result, New cv.Point(x, y), rColors(closedRegions Mod rColors.Length))
                    If byteCount > 10 Then closedRegions += 1 ' there are fake regions due to anti-alias like features that appear when drawing.
                End If
                If tmp.Get(Of Byte)(y, x) = 255 Then
                    cv.Cv2.FloodFill(tmp, New cv.Point(x, y), cv.Scalar.Black)
                    totalClusters += 1
                End If
            Next
        Next
        If hitBlack Then closedRegions -= hitBlack
    End Sub
    Public Sub Run(ocvb As VBocvb)
        ' If they changed Then number of elements in the set
        Static demoModeCheck = findCheckBox("Demo Mode")
        Static cityCountSlider = findSlider("KNN - number of cities")

        If cityCountSlider.Value <> numberOfCities Or demoModeCheck.Checked Then
            numberOfCities = cityCountSlider.Value
            knn.findXnearest = numberOfCities

            ReDim cityPositions(numberOfCities - 1)
            ReDim cityOrder(numberOfCities - 1)

            Dim gen As New System.Random()
            Dim r As New cv.RNG(gen.Next(0, 1000000))
            For i = 0 To numberOfCities - 1
                cityPositions(i).X = r.Uniform(0, src.Width)
                cityPositions(i).Y = r.Uniform(0, src.Height)
            Next

            ' find the nearest neighbor for each city - first will be the current city, next will be nearest real neighbors in order
            Dim trainingSlider = findSlider("KNN Train count")
            Dim querySlider = findSlider("KNN Query count")
            knn.knn.knnQT.trainingPoints.Clear()
            knn.knn.knnQT.queryPoints.Clear()
            For i = 0 To numberOfCities - 1
                knn.knn.knnQT.trainingPoints.Add(New cv.Point2f(CSng(cityPositions(i).X), CSng(cityPositions(i).Y)))
                knn.knn.knnQT.queryPoints.Add(New cv.Point2f(CSng(cityPositions(i).X), CSng(cityPositions(i).Y)))
            Next
            knn.Run(ocvb)
            dst1.SetTo(0)
            totalClusters = 0
            closedRegions = 0
            cluster(dst1)
            label1 = "knn clusters total=" + CStr(totalClusters) + " closedRegions=" + CStr(closedRegions)
        End If
    End Sub
End Class




Public Class KNN_Point2d
    Inherits VBparent
    Public knn As KNN_Basics
    Public findXnearest As Integer = 1
    Public responseSet() As Int32
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        knn = New KNN_Basics(ocvb)
        If standalone Then knn.knnQT.useRandomData = True

        desc = "Use KNN to find n matching points for each query."
        label1 = "Yellow=Queries, Blue=Best Responses"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If standalone Then
            dst1.SetTo(0)
            For i = 0 To knn.knnQT.trainingPoints.Count - 1
                cv.Cv2.Circle(dst1, knn.knnQT.trainingPoints(i), 9, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias, 0)
            Next
            Static nearestCountSlider = findSlider("KNN k nearest points")
            findXnearest = nearestCountSlider.Value
        End If

        knn.Run(ocvb)

        ReDim responseSet(knn.knnQT.queryPoints.Count * findXnearest - 1)
        Dim results As New cv.Mat, neighbors As New cv.Mat, query As New cv.Mat(1, 2, cv.MatType.CV_32F)
        For i = 0 To knn.knnQT.queryPoints.Count - 1
            query.Set(Of cv.Point2f)(0, 0, knn.knnQT.queryPoints(i))
            knn.knn.FindNearest(query, findXnearest, results, neighbors)
            For j = 0 To neighbors.Cols - 1
                Dim index = neighbors.Get(Of Single)(0, j)
                responseSet(i * findXnearest + j) = CInt(index)
            Next
            If standalone Then
                For j = 0 To findXnearest - 1
                    dst1.Line(knn.knnQT.trainingPoints(responseSet(i * findXnearest + j)), knn.knnQT.queryPoints(i), cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                    cv.Cv2.Circle(dst1, knn.knnQT.queryPoints(i), 5, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias, 0)
                Next
            End If
        Next
    End Sub
End Class





Public Class KNN_Point3d
    Inherits VBparent
    Public querySet() As cv.Point3f
    Public responseSet() As Int32
    Public lastSet() As cv.Point3f ' default usage: find and connect points in 2D for this number of points.
    Public findXnearest As Int32
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "knn Query Points", 1, 500, 10)
        sliders.setupTrackBar(1, "knn k nearest points", 0, 500, 1)

        desc = "Use KNN to connect 3D points.  Results shown are a 2D projection of the 3D results."
        label1 = "Yellow=Query (in 3D) Blue=Best Response (in 3D)"
        label2 = "Top Down View to confirm 3D KNN is correct"
    End Sub
    Public Sub Run(ocvb As VBocvb)
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
