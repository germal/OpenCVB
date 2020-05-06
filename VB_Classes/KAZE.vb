Imports cv = OpenCvSharp
Imports System.Collections.Generic

Public Class KAZE_KeypointsKAZE_CS
    Inherits VB_Class
    Dim CS_Kaze As New CS_Classes.Kaze_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        ocvb.desc = "Find keypoints using KAZE algorithm."
        ocvb.label1 = "KAZE key points"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        CS_Kaze.GetKeypoints(gray)
        ocvb.color.CopyTo(ocvb.result1)
        For i = 0 To CS_Kaze.kazeKeyPoints.Count - 1
            ocvb.result1.Circle(CS_Kaze.kazeKeyPoints.ElementAt(i).Pt, 3, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
    Public Sub MyDispose()
    End Sub
End Class




Public Class KAZE_KeypointsAKAZE_CS
    Inherits VB_Class
    Dim CS_AKaze As New CS_Classes.AKaze_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        ocvb.desc = "Find keypoints using AKAZE algorithm."
        ocvb.label1 = "AKAZE key points"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        CS_AKaze.GetKeypoints(gray)
        ocvb.color.CopyTo(ocvb.result1)
        For i = 0 To CS_AKaze.akazeKeyPoints.Count - 1
            ocvb.result1.Circle(CS_AKaze.akazeKeyPoints.ElementAt(i).Pt, 3, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
    Public Sub MyDispose()
    End Sub
End Class



Public Class KAZE_Sample_CS
    Inherits VB_Class
    Dim box As New cv.Mat
    Dim box_in_scene As New cv.Mat
    Dim CS_Kaze As New CS_Classes.Kaze_Sample
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        box = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/box.png", cv.ImreadModes.Color)
        box_in_scene = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/box_in_scene.png", cv.ImreadModes.Color)
        ocvb.desc = "Match keypoints in 2 photos."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim result = CS_Kaze.Run(box, box_in_scene)
        ocvb.result1 = result.Resize(ocvb.color.Size())
    End Sub
    Public Sub MyDispose()
    End Sub
End Class



Public Class KAZE_Match_CS
    Inherits VB_Class
    Dim red As LeftRightView_Basics
    Dim CS_Kaze As New CS_Classes.Kaze_Sample
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        red = New LeftRightView_Basics(ocvb, callerName)
        red.sliders.TrackBar1.Value = 45
        ocvb.desc = "Match keypoints in the left and right images."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        red.Run(ocvb)
        Dim result = CS_Kaze.Run(ocvb.result1, ocvb.result2)
        result(New cv.Rect(0, 0, ocvb.result1.Width, ocvb.result1.Height)).CopyTo(ocvb.result1)
        result(New cv.Rect(ocvb.result1.Width, 0, ocvb.result1.Width, ocvb.result1.Height)).CopyTo(ocvb.result2)
    End Sub
    Public Sub MyDispose()
        red.Dispose()
    End Sub
End Class




Public Class KAZE_LeftAligned_CS
    Inherits VB_Class
        Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, callerName, "Max number of points to match", 1, 300, 100)
        sliders.setupTrackBar2(ocvb, callerName, "When matching, max possible distance", 1, 200, 100)

        ocvb.desc = "Match keypoints in the left and right images but display it as movement in the right image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim CS_KazeLeft As New CS_Classes.Kaze_Basics
        CS_KazeLeft.GetKeypoints(ocvb.leftView)
        Dim CS_KazeRight As New CS_Classes.Kaze_Basics
        CS_KazeRight.GetKeypoints(ocvb.rightView)

        ocvb.result1 = ocvb.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        ocvb.result2 = ocvb.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim topDistance = sliders.TrackBar2.Value
        Dim maxPoints = sliders.TrackBar1.Value
        Dim maxCount = Math.Min(maxPoints, Math.Min(CS_KazeRight.kazeKeyPoints.Count, CS_KazeLeft.kazeKeyPoints.Count))
        For i = 0 To maxCount - 1
            Dim pt1 = CS_KazeRight.kazeKeyPoints.ElementAt(i)
            Dim minIndex As Integer
            Dim minDistance As Single = Single.MaxValue
            For j = 0 To CS_KazeLeft.kazeKeyPoints.Count - 1
                Dim pt2 = CS_KazeLeft.kazeKeyPoints.ElementAt(j)
                ' the right image point must be to the right of the left image point (pt1 X is < pt2 X) and at about the same Y
                If Math.Abs(pt2.Pt.Y - pt1.Pt.Y) < 2 And pt1.Pt.X < pt2.Pt.X Then
                    Dim distance = Math.Sqrt((pt1.Pt.X - pt2.Pt.X) * (pt1.Pt.X - pt2.Pt.X) + (pt1.Pt.Y - pt2.Pt.Y) * (pt1.Pt.Y - pt2.Pt.Y))
                    ' it is not enough to just be at the same height.  Can't be too far away!
                    If minDistance > distance And distance < topDistance Then
                        minIndex = j
                        minDistance = distance
                    End If
                End If
            Next
            If minDistance < Single.MaxValue Then
                ocvb.result2.Circle(pt1.Pt, 3, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
                ocvb.result1.Circle(pt1.Pt, 3, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
                ocvb.result2.Circle(CS_KazeLeft.kazeKeyPoints.ElementAt(minIndex).Pt, 3, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
                ocvb.result2.Line(pt1.Pt, CS_KazeLeft.kazeKeyPoints.ElementAt(minIndex).Pt, cv.Scalar.Yellow, 1, cv.LineTypes.AntiAlias)
            End If
        Next
        ocvb.label1 = "Right image has " + CStr(CS_KazeRight.kazeKeyPoints.Count) + " key points"
        ocvb.label2 = "Left image has " + CStr(CS_KazeLeft.kazeKeyPoints.Count) + " key points"
    End Sub
    Public Sub MyDispose()
    End Sub
End Class

