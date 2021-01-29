Imports cv = OpenCvSharp
Public Class Viewer_Basics
    Inherits VBparent
    Dim flow As Font_FlowText
    Dim phase As Gradient_StableDepth
    Public Sub New()
        initParent()

        phase = New Gradient_StableDepth
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Viewer X", 0, src.Width, 50)
            sliders.setupTrackBar(1, "Viewer Y", 0, src.Height, 50)
            sliders.setupTrackBar(2, "Viewer size", 1, 100, 16)
        End If

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 5)
            radio.check(0).Text = "Display all 3 channels (regardless of format)"
            radio.check(1).Text = "Display data as 8UC1"
            radio.check(2).Text = "Display data as 32-bit"
            radio.check(3).Text = "Display depth data in drawRect location"
            radio.check(4).Text = "Display pointcloud data in drawRect location"
            radio.check(1).Checked = True
        End If

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Run Viewer with current algorithm"
            check.Box(0).Checked = True
        End If
        'static firstCheck = findCheckBox("FirstCheckBox")
        'dim testCheck = firstCheck.checked

        flow = New Font_FlowText()
        flow.dst = RESULT2
        label1 = "Draw anywhere to view pixel data"
        task.desc = "View pixel data inside the drawRect area"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        dst1.SetTo(0)

        phase.src = src
        phase.Run()
        dst1 = phase.dst2

        Static xSlider = findSlider("Viewer X")
        Static ySlider = findSlider("Viewer Y")
        Static sizeSlider = findSlider("Viewer size")
        Dim drSize = sizeSlider.value
        Static drawRect = New cv.Rect(src.Width / 2, src.Height / 2, drSize, drSize)
        If task.drawRect.Width Then
            xSlider.value = task.drawRect.X
            ySlider.value = task.drawRect.Y
            task.drawRectClear = True
        End If
        drawRect = New cv.Rect(xSlider.value, ySlider.value, drSize, drSize)

        Dim radioIndex As Integer
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then radioIndex = i
        Next

        label2 = radio.check(radioIndex).Text
        flow.msgs.Clear()
        flow.msgs.Add("Row = " + CStr(drawRect.y) + " Column = " + CStr(drawRect.x))
        Select Case radioIndex
            Case 0
            Case 1
                For y = 0 To drawRect.height - 1
                    Dim line As String = ""
                    For x = 0 To drawRect.width - 1
                        ' line += 
                    Next
                Next
            Case 2
            Case 3
            Case 4

        End Select

        flow.Run()
        dst1.Rectangle(drawRect, cv.Scalar.White, 1)
    End Sub
End Class
