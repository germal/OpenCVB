
Imports cv = OpenCvSharp
Public Class Etch_ASketch
    Inherits ocvbClass
    Dim keys As Keyboard_Basics
    Dim slateColor = New cv.Scalar(122, 122, 122)
    Dim black As New cv.Vec3b(0, 0, 0)
    Dim cursor As cv.Point
    Dim ms_rng As New System.Random
    Private Function randomCursor(ocvb As AlgorithmData)
        Return New cv.Point(ms_rng.Next(0, ocvb.color.Width), ms_rng.Next(0, ocvb.color.Height))
    End Function
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)

        check.Setup(ocvb, caller, 2)
        check.Box(0).Text = "Etch_ASketch clean slate"
        check.Box(1).Text = "Demo mode"
        If ocvb.parms.testAllRunning Then check.Box(1).Checked = True

        keys = New Keyboard_Basics(ocvb, caller)

        cursor = randomCursor(ocvb)
        dst1.SetTo(slateColor)
        ocvb.desc = "Use OpenCV to simulate the Etch-a-Sketch Toy"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim directionCount = 1
        If check.Box(1).Checked Then
            keys.input = Choose(ms_rng.Next(1, 5), "down", "up", "left", "right")
            label1 = "Etch_ASketch demo mode - moving randomly"
            directionCount = ms_rng.Next(10, 50)
        Else
            label1 = "Use Up/Down/Left/Right keys to create image"
        End If
        If check.Box(0).Checked Then
            check.Box(0).Checked = False
            cursor = randomCursor(ocvb)
            dst1.SetTo(slateColor)
        End If
        keys.Run(ocvb)
        For i = 0 To directionCount - 1
            Select Case keys.input
                Case "down"
                    cursor.Y += 1
                Case "up"
                    cursor.Y -= 1
                Case "left"
                    cursor.X -= 1
                Case "right"
                    cursor.X += 1
            End Select
            If cursor.X < 0 Then cursor.X = cursor = randomCursor(ocvb)
            If cursor.Y < 0 Then cursor.Y = cursor = randomCursor(ocvb)
            If cursor.X > ocvb.color.Width Then cursor = randomCursor(ocvb)
            If cursor.Y > ocvb.color.Height Then cursor = randomCursor(ocvb)
            dst1.Set(Of cv.Vec3b)(cursor.Y, cursor.X, black)
        Next
        If check.Box(1).Checked Then
            Static lastCursor = cursor
            If lastCursor = cursor And ocvb.frameCount <> 0 Then cursor = randomCursor(ocvb)
            lastCursor = cursor
        End If
        keys.input = ""
    End Sub
End Class
