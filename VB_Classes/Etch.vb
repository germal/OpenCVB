
Imports cv = OpenCvSharp
Public Class Etch_ASketch
    Inherits VBparent
    Dim keys As Keyboard_Basics
    Dim slateColor = New cv.Scalar(122, 122, 122)
    Dim black As New cv.Vec3b(0, 0, 0)
    Dim cursor As cv.Point
    Dim ms_rng As New System.Random
    Private Function randomCursor(ocvb As VBocvb)
        Return New cv.Point(ms_rng.Next(0, src.Width), ms_rng.Next(0, src.Height))
    End Function
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        check.Setup(ocvb, caller, 2)
        check.Box(0).Text = "Etch_ASketch clean slate"
        check.Box(1).Text = "Demo mode"
        check.Box(1).Checked = True
        If ocvb.parms.testAllRunning Then check.Box(1).Checked = True

        keys = New Keyboard_Basics(ocvb)

        cursor = randomCursor(ocvb)
        dst1.SetTo(slateColor)
        ocvb.desc = "Use OpenCV to simulate the Etch-a-Sketch Toy"
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        keys.Run(ocvb)
        Dim Input = New List(Of String)(keys.keyInput)

        If check.Box(1).Checked Then
            Input.Clear() ' ignore any keyboard input when in Demo mode.
            Dim nextKey = Choose(ms_rng.Next(1, 5), "Down", "Up", "Left", "Right")
            label1 = "Etch_ASketch demo mode - moving randomly"
            For i = 0 To ms_rng.Next(10, 50)
                Input.Add(nextKey)
            Next
        Else
            label1 = "Use Up/Down/Left/Right keys to create image"
        End If
        If check.Box(0).Checked Then
            check.Box(0).Checked = False
            cursor = randomCursor(ocvb)
            dst1.SetTo(slateColor)
        End If

        For i = 0 To Input.Count - 1
            Select Case Input(i)
                Case "Down"
                    cursor.Y += 1
                Case "Up"
                    cursor.Y -= 1
                Case "Left"
                    cursor.X -= 1
                Case "Right"
                    cursor.X += 1
            End Select
            If cursor.X < 0 Then cursor.X = 0
            If cursor.Y < 0 Then cursor.Y = 0
            If cursor.X >= src.Width Then cursor.X = src.Width - 1
            If cursor.Y >= src.Height Then cursor.Y = src.Height - 1
            dst1.Set(Of cv.Vec3b)(cursor.Y, cursor.X, black)
        Next
        If check.Box(1).Checked Then
            Static lastCursor = cursor
            If lastCursor = cursor And ocvb.frameCount <> 0 Then cursor = randomCursor(ocvb)
            lastCursor = cursor
        End If
    End Sub
End Class

