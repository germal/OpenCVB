Imports cv = OpenCvSharp
Public Class Tracker_Basics : Implements IDisposable
    Public check As New OptionsCheckbox
    Public tracker As cv.Tracking.MultiTracker
    Public bbox As cv.Rect2d
    Public boxObject() As cv.Rect2d
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        check.Setup(ocvb, 1)
        check.Box(0).Text = "Stop tracking selected object"
        check.Show()
        ocvb.desc = "Track an object using cv.Tracking API"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If check.Box(0).Checked Then
            ocvb.result1.SetTo(0)
            check.Box(0).Checked = False
            If tracker IsNot Nothing Then tracker.Dispose()
            tracker = Nothing
        End If
        If ocvb.drawRect.Width <> 0 Then
            tracker = cv.Tracking.MultiTracker.Create()
            Dim r = ocvb.drawRect
            bbox = New cv.Rect2d(r.X, r.Y, r.Width, r.Height) ' silly that this isn't the same as rect.
            ocvb.drawRectClear = True
            tracker.Add(cv.Tracking.TrackerMIL.Create(), ocvb.color, bbox)
        End If

        If tracker IsNot Nothing Then
            tracker.Update(ocvb.color)
            boxObject = tracker.GetObjects() ' just track one.  Tracking multiple is buggy.  Returns a lot of 0 width/height rect2d's.
            If externalUse = False Then
                ocvb.result1 = ocvb.color.Clone()
                Dim p1 = New cv.Point(boxObject(0).X, boxObject(0).Y)
                Dim p2 = New cv.Point(boxObject(0).X + bbox.Width, boxObject(0).Y + bbox.Height)
                ocvb.result1.Rectangle(p1, p2, cv.Scalar.Blue, 2)
                ocvb.putText(New ActiveClass.TrueType("Draw a rectangle around object to be tracked in color image above left.", 10, 140, RESULT2))
            End If
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If tracker IsNot Nothing Then tracker.Dispose()
        check.Dispose()
    End Sub
End Class





Public Class Tracker_MultiObject : Implements IDisposable
    Dim trackers As New List(Of Tracker_Basics)
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Track any number of objects simultaneously"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.drawRect.Width <> 0 Then
            Dim tr = New Tracker_Basics(ocvb)
            tr.externalUse = True
            tr.Run(ocvb)
            ocvb.drawRect = New cv.Rect
            trackers.Add(tr)
        End If
        ocvb.result1 = ocvb.color.Clone()
        For Each tr In trackers
            Dim closeIt As Boolean
            If tr.check.Box(0).Checked Then closeIt = True
            tr.Run(ocvb)
            If closeIt Then tr.check.Dispose()
            If tr.tracker IsNot Nothing Then
                Dim p1 = New cv.Point(tr.boxObject(0).X, tr.boxObject(0).Y)
                Dim p2 = New cv.Point(tr.boxObject(0).X + tr.bbox.Width, tr.boxObject(0).Y + tr.bbox.Height)
                ocvb.result1.Rectangle(p1, p2, cv.Scalar.Blue, 2)
            End If
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        For Each tr In trackers
            tr.Dispose()
        Next
    End Sub
End Class