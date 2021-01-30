Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Pixel_Viewer
    Inherits VBparent
    Dim flow As Font_FlowText
    Public drawRect As cv.Rect
    Public Sub New()
        initParent()

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 5)
            radio.check(0).Text = "Display all 3 channels (regardless of format)"
            radio.check(1).Text = "Display data as 8UC1"
            radio.check(2).Text = "Display data as 32-bit"
            radio.check(3).Text = "Display depth data in drawRect location"
            radio.check(4).Text = "Display pointcloud data in drawRect location"
            radio.check(0).Checked = True
        End If

        flow = New Font_FlowText()
        flow.dst = RESULT2
        label1 = "Move the mouse to the desired area"
        task.desc = "View pixel data inside the drawRect area"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        dst1 = src
        dst2.SetTo(0)

        Dim radioIndex As Integer
        For radioIndex = 0 To radio.check.Count - 1
            If radio.check(radioIndex).Checked Then Exit For
        Next

        Dim drWidth = Choose(radioIndex + 1, 7, 22, 10, 10, 5)
        Dim drHeight = flow.maxLineCount
        If src.Width = 1280 Then drHeight -= 4
        Static mouseLoc = New cv.Point(100, 100) ' assume 
        If task.mousePoint.X Or task.mousePoint.Y Then
            Dim x = If(task.mousePoint.X >= drWidth, CInt(task.mousePoint.X - drWidth), drWidth)
            Dim y = If(task.mousePoint.Y >= drHeight, task.mousePoint.Y - drHeight, drHeight)
            mouseLoc = New cv.Point(CInt(x), CInt(y))
        End If
        drawRect = New cv.Rect(mouseLoc.x, mouseLoc.y, drWidth, drHeight)

        label2 = radio.check(radioIndex).Text
        flow.msgs.Clear()
        Dim line As String = If(src.Width = 1280, vbCrLf + vbCrLf, "")
        If radioIndex = 0 And dst1.Channels = 1 Then radioIndex = 1
        If dst1.Type = cv.MatType.CV_32F Then radioIndex = 2
        Select Case radioIndex

            Case 0
                Dim colDup = 115
                Dim img = dst1(drawRect).Reshape(1)
                line += " col      "
                For i = drawRect.X To drawRect.X + drawRect.Width - 1 Step 5
                    line += Format(i, "#000") + StrDup(colDup, " ")
                Next
                flow.msgs.Add(line + vbCrLf)
                For y = 0 To img.Height - 1
                    line = "r" + CStr(drawRect.Y + y) + " "
                    For x = 0 To img.Width - 1
                        If x Mod 3 = 0 Then line += " "
                        line += Format(img.Get(Of Byte)(y, x), "000") + " "
                    Next
                    flow.msgs.Add(line)
                Next


            Case 1
                If dst1.Channels <> 1 Then dst1 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                line += vbCrLf + " col  "
                Dim colDup = 30
                If drawRect.X > 1000 Then colDup -= 2
                For i = 0 To drawRect.Width - 1 Step 5
                    line += Format(drawRect.X + i, "#000" + StrDup(colDup, " "))
                Next
                flow.msgs.Add(line + vbCrLf)
                For y = drawRect.Y To Math.Min(drawRect.Y + drawRect.Height, dst1.Height) - 1
                    line = "r" + CStr(y) + " "
                    For x = drawRect.X To Math.Min(drawRect.X + drawRect.Width - 1, dst1.Width)
                        line += Format(dst1.Get(Of Byte)(y, x), "000") + " "
                    Next
                    flow.msgs.Add(line)
                Next


            Case 2


            Case 3
                line = " col  "
                For i = 0 To drawRect.Width - 1
                    line += Format(drawRect.X + i, "000") + " "
                Next
                flow.msgs.Add(line + vbCrLf)
                For y = drawRect.Y To Math.Min(drawRect.Y + drawRect.Height, dst1.Height) - 1
                    line = "r" + CStr(y) + " "
                    For x = drawRect.X To Math.Min(drawRect.X + drawRect.Width - 1, dst1.Width)
                        line += Format(task.depth32f.Get(Of Byte)(y, x), "0000") + " "
                    Next
                    flow.msgs.Add(line)
                Next
            Case 4

        End Select

        flow.Run()
        dst1.Rectangle(drawRect, cv.Scalar.White, 1)
    End Sub
End Class








' https://github.com/shimat/opencvsharp_samples/blob/cba08badef1d5ab3c81ab158a64828a918c73df5/SamplesCS/Samples/PixelAccess.cs
Public Class Pixel_GetSet
    Inherits VBparent
    Dim mats As Mat_4to1
    Public Sub New()
        initParent()
        mats = New Mat_4to1()

        label1 = "Time to copy using get/set,Generic Index, Marshal Copy"
        label2 = "Click any quadrant at left to view it below"
        task.desc = "Perform Pixel-level operations in 3 different ways to measure efficiency."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim rows = src.Height
        Dim cols = src.Width
        Dim output As String = ""
        Dim rgb = src.CvtColor(cv.ColorConversionCodes.BGR2RGB)

        Dim watch = Stopwatch.StartNew()
        For y = 0 To rows - 1
            For x = 0 To cols - 1
                Dim color = rgb.Get(Of cv.Vec3b)(y, x)
                color.Item0.SwapWith(color.Item2)
                mats.mat(0).Set(Of cv.Vec3b)(y, x, color)
            Next
        Next
        watch.Stop()
        output += "Upper left image is GetSet and it took " + CStr(watch.ElapsedMilliseconds) + "ms" + vbCrLf + vbCrLf

        mats.mat(1) = rgb.Clone()
        watch = Stopwatch.StartNew()
        Dim indexer = mats.mat(1).GetGenericIndexer(Of cv.Vec3b)
        For y = 0 To rows - 1
            For x = 0 To cols - 1
                Dim color = indexer(y, x)
                color.Item0.SwapWith(color.Item2)
                indexer(y, x) = color
            Next
        Next
        watch.Stop()
        output += "Upper right image is Generic Indexer and it took " + CStr(watch.ElapsedMilliseconds) + "ms" + vbCrLf + vbCrLf

        watch = Stopwatch.StartNew()
        Dim colorArray(cols * rows * rgb.ElemSize - 1) As Byte
        Marshal.Copy(rgb.Data, colorArray, 0, colorArray.Length)
        For i = 0 To colorArray.Length - 3 Step 3
            colorArray(i).SwapWith(colorArray(i + 2))
        Next
        mats.mat(2) = New cv.Mat(rows, cols, cv.MatType.CV_8UC3, colorArray)
        watch.Stop()
        output += "Marshal Copy took " + CStr(watch.ElapsedMilliseconds) + "ms" + vbCrLf

        ocvb.trueText(output, src.Width / 2 + 10, src.Height / 2 + 20)

        mats.Run()
        dst1 = mats.dst1
        If task.mouseClickFlag And task.mousePicTag = RESULT1 Then setMyActiveMat()
        dst2 = mats.mat(quadrantIndex)
    End Sub
End Class

