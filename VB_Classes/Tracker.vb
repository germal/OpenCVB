Imports cv = OpenCvSharp
Public Class Tracker_Basics
    Inherits VBparent
    Public tracker As cv.Tracking.MultiTracker
    Public bbox As cv.Rect2d
    Public boxObject() As cv.Rect2d
    Public trackerIndex As integer = 5 ' trackerMIL by default...
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Stop tracking selected object"
        desc = "Track an object using cv.Tracking API - tracker algorithm"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        ocvb.trueText("Draw a rectangle around object to be tracked.", 10, 140)
        If check.Box(0).Checked Then
            check.Box(0).Checked = False
            If tracker IsNot Nothing Then tracker.Dispose()
            tracker = Nothing
        End If
        If ocvb.drawRect.Width <> 0 Then
            tracker = cv.Tracking.MultiTracker.Create()
            Dim r = ocvb.drawRect
            bbox = New cv.Rect2d(r.X, r.Y, r.Width, r.Height) ' silly that this isn't the same as rect.
            ocvb.drawRectClear = True
            Select Case trackerIndex
                Case 0
                    tracker.Add(cv.Tracking.TrackerBoosting.Create(), src, bbox)
                Case 1
                    tracker.Add(cv.Tracking.TrackerCSRT.Create(), src, bbox)
                Case 2
                    tracker.Add(cv.Tracking.TrackerGOTURN.Create(), src, bbox)
                Case 3
                    tracker.Add(cv.Tracking.TrackerKCF.Create(), src, bbox)
                Case 4
                    tracker.Add(cv.Tracking.TrackerMedianFlow.Create(), src, bbox)
                Case 5
                    tracker.Add(cv.Tracking.TrackerMIL.Create(), src, bbox)
                Case 6
                    tracker.Add(cv.Tracking.TrackerMOSSE.Create(), src, bbox)
                Case 7
                    tracker.Add(cv.Tracking.TrackerTLD.Create(), src, bbox)
            End Select
        End If

        dst2 = src.Clone()
        If tracker IsNot Nothing Then
            tracker.Update(src)
            boxObject = tracker.GetObjects() ' just track one.  Tracking multiple is buggy.  Returns a lot of 0 width/height rect2d's.
            Dim p1 = New cv.Point(boxObject(0).X, boxObject(0).Y)
            Dim p2 = New cv.Point(boxObject(0).X + bbox.Width, boxObject(0).Y + bbox.Height)
            dst2.Rectangle(p1, p2, cv.Scalar.Blue, 2)
        End If
    End Sub
End Class





Public Class Tracker_MultiObject
    Inherits VBparent
    Dim trackers As New List(Of Tracker_Basics)
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        desc = "Track any number of objects simultaneously - tracker algorithm"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.drawRect.Width <> 0 Then
            Dim tr = New Tracker_Basics(ocvb)
            tr.src = src
            tr.Run(ocvb)
            ocvb.drawRect = New cv.Rect
            trackers.Add(tr)
        End If
        dst1 = src.Clone()
        For Each tr In trackers
            Dim closeIt As Boolean
            If tr.check.Box(0).Checked Then closeIt = True
            tr.src = src
            tr.Run(ocvb)
            If closeIt Then tr.check.Dispose()
            If tr.tracker IsNot Nothing Then
                Dim p1 = New cv.Point(tr.boxObject(0).X, tr.boxObject(0).Y)
                Dim p2 = New cv.Point(tr.boxObject(0).X + tr.bbox.Width, tr.boxObject(0).Y + tr.bbox.Height)
                dst1.Rectangle(p1, p2, cv.Scalar.Blue, 2)
            End If
        Next
    End Sub
End Class






Public Class Tracker_Methods
    Inherits VBparent
    Dim tracker As Tracker_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        tracker = New Tracker_Basics(ocvb)

        radio.Setup(ocvb, caller, 8)
        radio.check(0).Text = "TrackerBoosting"
        radio.check(1).Text = "TrackerCSRT"
        radio.check(2).Text = "TrackerGOTURN - disabled (not working)"
        radio.check(2).Enabled = False
        radio.check(3).Text = "TrackerKCF"
        radio.check(4).Text = "TrackerMedianFlow"
        radio.check(5).Text = "TrackerMIL"
        radio.check(6).Text = "TrackerMOSSE"
        radio.check(7).Text = "TrackerTLD"
        radio.check(5).Checked = True ' TrackerMIL is the default

        desc = "Experiment with the different types of tracking methods - apparently not much difference..."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Static saveMethod As integer

        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked = True Then
                tracker.trackerIndex = i
                label1 = "Method: " + radio.check(i).Text
                Exit For
            End If
        Next
        If saveMethod <> tracker.trackerIndex Then
            tracker.check.Box(0).Checked = True
        Else
            tracker.src = src
            tracker.Run(ocvb)
            dst1 = tracker.dst1
        End If
        saveMethod = tracker.trackerIndex
    End Sub
End Class

