Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Pixel_Viewer
    Inherits VBparent
    Dim keys As Keyboard_Basics
    Public pixels As PixelViewerForm
    Public Sub New()
        initParent()
        keys = New Keyboard_Basics()

        task.callTrace.Clear() ' special line to clear the tree view otherwise Options_Common is standalone (it is always present, not standalone)
        standalone = False

        check.Setup(caller, 1)
        check.Box(0).Text = "Open Pixel Viewer"
        check.Box(0).Checked = GetSetting("OpenCVB", "PixelViewerActive", "PixelViewerActive", False)

        task.desc = "Display pixels under the cursor"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Static pixelCheck = findCheckBox("Open Pixel Viewer")
        If pixelCheck.Checked Then
            If task.pixelCheck = False Then
                pixels = New PixelViewerForm
                pixels.Show()
            End If
            task.pixelCheck = True

            keys.Run()
            Dim keyInput = New List(Of String)(keys.keyInput)

            dst1 = Choose(task.mousePicTag + 1, task.color, task.RGBDepth, task.algorithmObject.dst1.clone, task.algorithmObject.dst2.clone)

            Dim displayType = 0 ' default is 8uc3
            If dst1.Type = cv.MatType.CV_8U Then displayType = 1
            If dst1.Type = cv.MatType.CV_32F Then displayType = 2
            If dst1.Type = cv.MatType.CV_32FC3 Then displayType = 3

            Dim formatType = Choose(displayType + 1, "8UC3", "8UC1", "32FC1", "32FC3")
            pixels.Text = "Pixel Viewer for " + Choose(task.mousePicTag + 1, "Color", "RGB Depth", "dst1", "dst2") + " " + formatType

            Dim ratio = task.ratioImageToCampic
            Dim drWidth = Choose(displayType + 1, 7, 22, 10, 10, 5) * pixels.Width / 650
            Dim drHeight = pixels.Height / 17
            If src.Width = 1280 Then drHeight -= 4
            Static mouseLoc = New cv.Point(100, 100) ' assume 
            If task.mousePoint.X Or task.mousePoint.Y Then
                For i = 0 To keyInput.Count - 1
                    Select Case keyInput(i)
                        Case "Down"
                            task.mousePoint.Y += 1
                        Case "Up"
                            task.mousePoint.Y -= 1
                        Case "Left"
                            task.mousePoint.X -= 1
                        Case "Right"
                            task.mousePoint.X += 1
                    End Select
                Next

                Dim x = If(task.mousePoint.X >= drWidth, CInt(task.mousePoint.X - drWidth), 0)
                Dim y = If(task.mousePoint.Y >= drHeight, task.mousePoint.Y - drHeight, 0)
                mouseLoc = New cv.Point(CInt(x), CInt(y))
            End If

            Static savedisplayType = -1
            Static saveDrawRect = New cv.Rect(0, 0, -1, -1)
            If savedisplayType <> displayType Then saveDrawRect = New cv.Rect(0, 0, -1, -1)

            Dim dw = New cv.Rect(mouseLoc.x, mouseLoc.y, drWidth, drHeight)

            If dw.X < 0 Then dw.X = 0
            If dw.Y < 0 Then dw.Y = 0
            If dw.X + dw.Width > dst1.Width Then
                dw.X = dst1.Width - dw.Width
                dw.Width = dw.Width
            End If
            If dw.Y + dw.Height > dst1.Height Then
                dw.Y = dst1.Height - dw.Height
                dw.Height = dw.Height
            End If

            task.drawRect = New cv.Rect(CInt(dw.X / ratio), CInt(dw.Y / ratio), CInt(dw.Width / ratio), CInt(dw.Height / ratio))
            If saveDrawRect = task.drawRect And pixels.pixelResized = False Then Exit Sub
            pixels.pixelResized = False

            Select Case displayType

                Case 0
                    pixels.line = " col " + If(dw.X Mod 5, "  ", "    ")
                    Dim colDup = If(dw.X < 1000, 26, 25)
                    For i = 0 To dw.Width - 1
                        If (dw.X + i) Mod 5 Then pixels.line += StrDup(colDup, " ") Else pixels.line += Format(dw.X + i, "#000") + "         "
                    Next
                    pixels.line += vbCrLf
                    For y = dw.Y To Math.Min(dw.Y + dw.Height, dst1.Height) - 1
                        pixels.line += "r" + Format(y, "000") + "   "
                        For x = dw.X To Math.Min(dw.X + dw.Width, dst1.Width) - 1
                            pixels.line += Format(dst1.Get(Of Byte)(y, x), "000") + " "
                            pixels.line += Format(dst1.Get(Of Byte)(y, x + 1), "000") + " "
                            pixels.line += Format(dst1.Get(Of Byte)(y, x + 2), "000") + "   "
                        Next
                        pixels.line += vbCrLf
                    Next

                Case 1
                    pixels.line = " col" + If(dw.X Mod 5, "        ", "     ")
                    Dim colDup = If(dw.X < 1000, 7, 6)
                    For i = 0 To dw.Width - 1
                        If (dw.X + i) Mod 5 = 0 Then pixels.line += Format(dw.X + i, "#000") + "    " Else pixels.line += StrDup(colDup, " ")
                    Next
                    pixels.line += vbCrLf
                    For y = dw.Y To Math.Min(dw.Y + dw.Height, dst1.Height) - 1
                        pixels.line += "r" + Format(y, "000") + "   "
                        For x = dw.X To Math.Min(dw.X + dw.Width, dst1.Width) - 1
                            pixels.line += Format(dst1.Get(Of Byte)(y, x), "000") + If(x Mod 5 = 4, "   ", " ")
                        Next
                        pixels.line += vbCrLf
                    Next


                Case 2


                Case 3
                    pixels.line = " col  "
                    For i = 0 To dw.Width - 1
                        pixels.line += Format(dw.X + i, "000") + " "
                    Next
                    pixels.line += vbCrLf
                    For y = dw.Y To Math.Min(dw.Y + dw.Height, dst1.Height) - 1
                        pixels.line += "r" + CStr(y) + " "
                        For x = dw.X To Math.Min(dw.X + dw.Width - 1, dst1.Width)
                            pixels.line += Format(task.depth32f.Get(Of Byte)(y, x), "0000") + " "
                        Next
                    Next
                Case 4

            End Select
            pixels.Refresh()
            savedisplayType = displayType
            saveDrawRect = task.drawRect
        Else
            If task.pixelCheck Then
                pixels.Close()
                task.pixelCheck = False
            End If
        End If
    End Sub
    Public Sub closeViewer()
        SaveSetting("OpenCVB", "PixelViewerActive", "PixelViewerActive", task.pixelCheck)
        If task.pixelCheck And pixels IsNot Nothing Then pixels.Close()
    End Sub
End Class



Public Class Pixel_Explorer
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
                If drawRect.X >= 1000 Then colDup -= 2
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

