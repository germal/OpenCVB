Imports cv = OpenCvSharp
' https://www.codeproject.com/Articles/1093960/D-Polyline-Vertex-Smoothing
Public Class Smoothing_Exterior
	Inherits ocvbClass
	Dim hull As Hull_Basics
	Private Function getSplineInterpolationCatmullRom(points As List(Of cv.Point), nrOfInterpolatedPoints As Integer) As List(Of cv.Point)
		Dim spline As New List(Of cv.Point)
		' Create a new pointlist to spline.  If you don't do this, the original pointlist is included with the extrapolated points
		Dim spoints As New List(Of cv.Point)
		spoints = points

		Dim startPt = (spoints(1) + spoints(0)) * 0.5
		spoints.Insert(0, startPt)
		Dim endPt = (spoints(spoints.Count - 1) + spoints(spoints.Count - 2)) * 0.5
		spoints.Insert(spoints.Count, endPt)

		' Note the nrOfInterpolatedPoints acts as a kind of tension factor between 0 and 1 because it is normalised
		' to 1/nrOfInterpolatedPoints. It can never be 0
		Dim t As Double = 0
		Dim spoint As cv.Point
		For i = 0 To spoints.Count - 4
			spoint = New cv.Point()
			For j = 0 To nrOfInterpolatedPoints - 1
				Dim x0 = spoints.ElementAt((i) Mod spoints.Count)
				Dim x1 = spoints.ElementAt((i + 1) Mod spoints.Count)
				Dim x2 = spoints.ElementAt((i + 2) Mod spoints.Count)
				Dim x3 = spoints.ElementAt((i + 3) Mod spoints.Count)
				t = 1 / nrOfInterpolatedPoints * j
				spoint.X = 0.5 * (2 * x1.X + (-1 * x0.X + x2.X) * t + (2 * x0.X - 5 * x1.X + 4 * x2.X - x3.X) * t ^ 2 +
						   (-1 * x0.X + 3 * x1.X - 3 * x2.X + x3.X) * t ^ 3)
				spoint.Y = 0.5 * (2 * x1.Y + (-1 * x0.Y + x2.Y) * t + (2 * x0.Y - 5 * x1.Y + 4 * x2.Y - x3.Y) * t ^ 2 +
						   (-1 * x0.Y + 3 * x1.Y - 3 * x2.Y + x3.Y) * t ^ 3)
				spline.Add(spoint)
			Next
		Next

		'add the last point, but skip the interpolated last point, so second last...
		spline.Add(spoints(spoints.Count - 2))
		Return spline
	End Function
	Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
		setCaller(callerRaw)
		hull = New Hull_Basics(ocvb, caller)
		hull.sliders.TrackBar1.Minimum = 4 ' required minimum number of points for the algorithm.

		sliders.setupTrackBar1(ocvb, caller, "Smoothing iterations", 1, 20, 10)

		ocvb.label1 = "Original Points (white) Smoothed (yellow)"
		ocvb.label2 = ""
		ocvb.desc = "Smoothing the line connecting a series of points."
	End Sub
	Public Sub Run(ocvb As AlgorithmData)
		If ocvb.frameCount Mod 30 Then Exit Sub

		hull.Run(ocvb)
		Dim nextHull = hull.hull

		dst1.SetTo(0)
		Dim points = drawPoly(dst1, nextHull, cv.Scalar.White)

		Dim smoothPoints = getSplineInterpolationCatmullRom(points, sliders.TrackBar1.Value)
		If smoothPoints.Count > 0 Then drawPoly(dst1, smoothPoints.ToArray, cv.Scalar.Yellow)
	End Sub
End Class





' https://www.codeproject.com/Articles/1093960/D-Polyline-Vertex-Smoothing
Public Class Smoothing_Interior
	Inherits ocvbClass
	Dim hull As Hull_Basics
	Private Function getCurveSmoothingChaikin(points As List(Of cv.Point), tension As Double, nrOfIterations As Integer) As List(Of cv.Point2d)
		'the tension factor defines a scale between corner cutting distance in segment half length, i.e. between 0.05 and 0.45
		'the opposite corner will be cut by the inverse (i.e. 1-cutting distance) to keep symmetry
		'with a tension value of 0.5 this amounts to 0.25 = 1/4 and 0.75 = 3/4 the original Chaikin values
		Dim cutdist As Double = 0.05 + (tension * 0.4)

		'make a copy of the pointlist and feed it to the iteration
		Dim nl As New List(Of cv.Point2d)
		For i = 0 To points.Count - 1
			nl.Add(New cv.Point2d(CDbl(points.ElementAt(i).X), CDbl(points.ElementAt(i).Y)))
		Next

		For i = 1 To nrOfIterations
			nl = getSmootherChaikin(nl, cutdist)
		Next

		Return nl
	End Function

	Private Function getSmootherChaikin(points As List(Of cv.Point2d), cuttingDist As Double) As List(Of cv.Point2d)
		Dim nl As New List(Of cv.Point2d)
		'always add the first point
		nl.Add(points(0))

		For i = 0 To points.Count - 2
			Dim pt1 = New cv.Point2d((1 - cuttingDist) * points.ElementAt(i).X, (1 - cuttingDist) * points.ElementAt(i).Y)
			Dim pt2 = New cv.Point2d(cuttingDist * points.ElementAt(i + 1).X, cuttingDist * points.ElementAt(i + 1).Y)
			nl.Add(pt1 + pt2)
			pt1 = New cv.Point2d(cuttingDist * points.ElementAt(i).X, cuttingDist * points.ElementAt(i).Y)
			pt2 = New cv.Point2d((1 - cuttingDist) * points.ElementAt(i + 1).X, (1 - cuttingDist) * points.ElementAt(i + 1).Y)
			nl.Add(pt1 + pt2)
		Next

		'always add the last point
		nl.Add(points(points.Count - 1))
		Return nl
	End Function

	Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
		setCaller(callerRaw)
		hull = New Hull_Basics(ocvb, caller)
		hull.sliders.TrackBar1.Minimum = 4 ' required minimum number of points for the algorithm.
		hull.sliders.TrackBar1.Value = 16

		sliders.setupTrackBar1(ocvb, caller, "Smoothing iterations", 1, 20, 1)
		sliders.setupTrackBar2(ocvb, caller, "Smoothing tension X100", 1, 100, 50)

		ocvb.label1 = "Original Points (white) Smoothed (yellow)"
		ocvb.label2 = ""
		ocvb.desc = "Smoothing the line connecting a series of points staying inside the outline."
	End Sub
	Public Sub Run(ocvb As AlgorithmData)
		If ocvb.frameCount Mod 30 Then Exit Sub

		hull.Run(ocvb)
		Dim nextHull = hull.hull

		dst1.SetTo(0)
		Dim points = drawPoly(dst1, nextHull, cv.Scalar.White)

		Dim smoothPoints2d = getCurveSmoothingChaikin(points, sliders.TrackBar2.Value / 100, sliders.TrackBar1.Value)
		Dim smoothPoints As New List(Of cv.Point)
		For i = 0 To smoothPoints2d.Count - 1
			smoothPoints.Add(New cv.Point(CInt(smoothPoints2d.ElementAt(i).X), CInt(smoothPoints2d.ElementAt(i).Y)))
		Next
		If smoothPoints.Count > 0 Then drawPoly(dst1, smoothPoints.ToArray, cv.Scalar.Yellow)
	End Sub
End Class