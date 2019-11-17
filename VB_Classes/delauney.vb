Imports cv = OpenCvSharp

Module Delaunay_Exports
    Public colorScalar(255) As cv.Scalar
    Public Sub draw_line(img As cv.Mat, org As cv.Point, dst As cv.Point, active_color As cv.Scalar)
        If org.X >= 0 And org.X <= img.Width Then
            If org.Y >= 0 And org.Y <= img.Height Then
                If dst.X >= 0 And dst.X <= img.Width Then
                    If dst.Y >= 0 And dst.Y <= img.Height Then
                        cv.Cv2.Line(img, org, dst, active_color, 1, cv.LineTypes.AntiAlias, 0)
                    End If
                End If
            End If
        End If
    End Sub

    Public Sub draw_subdiv(img As cv.Mat, subdiv As cv.Subdiv2D, delaunay_color As cv.Scalar, testval As Int32)
        If testval Then
            Dim trianglelist() = subdiv.GetTriangleList()
            Dim pt(3) As cv.Point

            For i = 0 To trianglelist.Length - 1
                Dim t = trianglelist(i)
                pt(0) = New cv.Point(Math.Round(t(0)), Math.Round(t(1)))
                pt(1) = New cv.Point(Math.Round(t(2)), Math.Round(t(3)))
                pt(2) = New cv.Point(Math.Round(t(4)), Math.Round(t(5)))
                draw_line(img, pt(0), pt(1), delaunay_color)
                draw_line(img, pt(1), pt(2), delaunay_color)
                draw_line(img, pt(2), pt(0), delaunay_color)
            Next
        Else
            Dim edgeList = subdiv.GetEdgeList()
            For i = 0 To edgeList.Length - 1
                Dim e = edgeList(i)
                Dim pt0 = New cv.Point(Math.Round(e(0)), Math.Round(e(1)))
                Dim pt1 = New cv.Point(Math.Round(e(2)), Math.Round(e(3)))
                draw_line(img, pt0, pt1, delaunay_color)
            Next
        End If
    End Sub
    Public Sub locate_point(img As cv.Mat, subdiv As cv.Subdiv2D, fp As cv.Point2f, active_color As cv.Scalar)
        Dim e0 As Int32, vector As Int32

        subdiv.Locate(fp, e0, vector)
        If e0 > 0 Then
            Dim e = e0
            Do
                Dim org As cv.Point2f, dst As cv.Point2f
                If subdiv.EdgeOrg(e, org) > 0 And subdiv.EdgeDst(e, dst) > 0 Then
                    'draw_line(img, org, dst, active_color)
                End If

                e = subdiv.GetEdge(e, 19) ' next_around_left const missing ?
                If e = e0 Then Exit Do
            Loop
        End If

        cv.Cv2.Circle(img, fp, 10, active_color, -1, cv.LineTypes.AntiAlias, 0)
    End Sub
    Public Sub paint_voronoi(img As cv.Mat, subdiv As cv.Subdiv2D)
        Dim facets = New cv.Point2f()() {Nothing}
        Dim centers() As cv.Point2f = Nothing
        subdiv.GetVoronoiFacetList(New List(Of Int32)(), facets, centers)

        Dim ifacet() As cv.Point = Nothing
        Dim ifacets = New cv.Point()() {Nothing}

        For i = 0 To facets.Length - 1
            ReDim ifacet(facets(i).Length - 1)
            For j = 0 To facets(i).Length - 1
                ifacet(j) = New cv.Point(Math.Round(facets(i)(j).X), Math.Round(facets(i)(j).Y))
            Next

            cv.Cv2.FillConvexPoly(img, ifacet, colorScalar(i Mod colorScalar.Length), cv.LineTypes.AntiAlias)
            ifacets(0) = ifacet
            cv.Cv2.Polylines(img, ifacets, True, New cv.Scalar(0), 1, cv.LineTypes.AntiAlias, 0)
        Next
    End Sub
End Module




Public Class Delaunay_Basics : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        For i = 0 To colorScalar.Length - 1
            colorScalar(i) = New cv.Scalar(ocvb.rng.uniform(0, 255), ocvb.rng.uniform(0, 255), ocvb.rng.uniform(0, 255))
        Next
        ocvb.desc = "Use Delaunay to subdivide an image into triangles."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim active_facet_color = New cv.Scalar(0, 0, 255)
        Dim rect = New cv.Rect(0, 0, ocvb.color.Width, ocvb.color.Height)
        ocvb.result1.SetTo(0)

        Dim subdiv As New cv.Subdiv2D(rect)

        For i = 0 To 100
            Dim fp = New cv.Point2f(ocvb.rng.uniform(0, rect.Width), ocvb.rng.uniform(0, rect.Height))
            locate_point(ocvb.result1, subdiv, fp, active_facet_color)
            subdiv.Insert(fp)
            'draw_subdiv(ocvb.result1, subdiv, cv.scalar.white, ocvb.frameCount Mod 2)
        Next

        paint_voronoi(ocvb.result1, subdiv)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class Delaunay_GoodFeatures : Implements IDisposable
    Dim features As Features_GoodFeatures
    Public Sub New(ocvb As AlgorithmData)
        features = New Features_GoodFeatures(ocvb)
        ocvb.desc = "Use Delaunay with the points provided by GoodFeaturesToTrack."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        features.Run(ocvb)

        Dim active_facet_color = New cv.Scalar(0, 0, 255)
        Dim subdiv As New cv.Subdiv2D(New cv.Rect(0, 0, ocvb.color.Width, ocvb.color.Height))
        ocvb.result2.SetTo(0)
        For i = 0 To features.goodFeatures.Count - 1
            locate_point(ocvb.result2, subdiv, features.goodFeatures(i), active_facet_color)
            subdiv.Insert(features.goodFeatures(i))
        Next

        paint_voronoi(ocvb.result2, subdiv)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        features.Dispose()
    End Sub
End Class




' https://github.com/shimat/opencvsharp/wiki/Subdiv2D
Public Class Delauney_Subdiv2D : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label2 = "Voronoi facets for the same subdiv2D"
        ocvb.desc = "Generate random points and divide the image around those points."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod 100 <> 0 Then Exit Sub ' too fast otherwise...
        Dim rand As New Random()
        Dim points = Enumerable.Range(0, 100).Select(Of cv.Point2f)(
            Function(i)
                Return New cv.Point2f(rand.Next(0, ocvb.color.Width), rand.Next(0, ocvb.color.Height))
            End Function).ToArray()
        ocvb.result1.SetTo(0)
        For Each p In points
            ocvb.result1.Circle(p, 4, cv.Scalar.Red, -1)
        Next

        Dim subdiv = New cv.Subdiv2D()
        subdiv.InitDelaunay(New cv.Rect(0, 0, ocvb.result2.Width, ocvb.result2.Height))
        subdiv.Insert(points)

        ' draw voronoi diagram
        Dim facetList()() As cv.Point2f = Nothing
        Dim facetCenters() As cv.Point2f = Nothing
        subdiv.GetVoronoiFacetList(Nothing, facetList, facetCenters)

        ocvb.result2 = ocvb.result1.Clone()
        For Each list In facetList
            Dim before = list.Last()
            For Each p In list
                ocvb.result2.Line(before, p, cv.Scalar.Green, 1)
                before = p
            Next
        Next

        ' draw the delauney diagram
        Dim edgelist = subdiv.GetEdgeList()
        For Each edge In edgelist
            Dim p1 = New cv.Point(edge.Item0, edge.Item1)
            Dim p2 = New cv.Point(edge.Item2, edge.Item3)
            ocvb.result1.Line(p1, p2, cv.Scalar.Green, 1)
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class