Imports cv = OpenCvSharp
Public Class TransformationMatrix_Basics
    Inherits ocvbClass
    Dim topLocations As New List(Of cv.Point3d)
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "TMatrix Top View multiplier", 1, 1000, 500)
        If ocvb.parms.cameraIndex = StereoLabsZED2 Then sliders.TrackBar1.Value = 1 ' need a smaller multiplier...

        ocvb.label1 = "View from above the camera"
        ocvb.label2 = "View from side of the camera"
        ocvb.desc = "Show the contents of the transformation matrix"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dst1.SetTo(0)
        ocvb.result2.SetTo(0)
        If ocvb.parms.transformationMatrix IsNot Nothing Then
            Dim t = ocvb.parms.transformationMatrix
            Dim mul = sliders.TrackBar1.Value
            topLocations.Add(New cv.Point3d(-t(12) * mul + dst1.Width / 2,
                                            -t(13) * mul + dst1.Height / 2,
                                             t(14) * mul + dst1.Height / 2))

            For i = 0 To topLocations.Count - 1
                Dim pt = topLocations.ElementAt(i)
                If pt.X > 0 And pt.X < dst1.Width And pt.Z > 0 And pt.Z < ocvb.color.Height Then
                    dst1.Circle(New cv.Point(pt.X, pt.Z), 3, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
                End If

                If pt.Z > 0 And pt.Z < dst1.Width And pt.Y > 0 And pt.Y < ocvb.color.Height Then
                    ocvb.result2.Circle(New cv.Point(pt.Z, pt.Y), 3, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
                End If
            Next

            If topLocations.Count > 20 Then topLocations.RemoveAt(0) ' just show the last x points
        Else
            ocvb.putText(New ActiveClass.TrueType("The transformation matrix for the current camera has not been set", 10, 125))
        End If
    End Sub
End Class
