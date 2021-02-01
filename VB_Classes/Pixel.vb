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

            If task.mousePicTag < 2 Then Exit Sub
            dst1 = Choose(task.mousePicTag - 2 + 1, task.algorithmObject.dst1.clone, task.algorithmObject.dst2.clone)

            Dim displayType = -1 ' default is 8uc3
            If dst1.Type = cv.MatType.CV_8UC3 Then displayType = 0
            If dst1.Type = cv.MatType.CV_8U Then displayType = 1
            If dst1.Type = cv.MatType.CV_32F Then displayType = 2
            If dst1.Type = cv.MatType.CV_32FC3 Then displayType = 3
            If displayType < 0 Or dst1.Channels > 4 Then
                ocvb.trueText("The pixel Viewer does not support this cv.Mat!")
                Exit Sub
            End If

            Dim formatType = Choose(displayType + 1, "8UC3", "8UC1", "32FC1", "32FC3")
            pixels.Text = "Pixel Viewer for " + Choose(task.mousePicTag + 1, "Color", "RGB Depth", "dst1", "dst2") + " " + formatType + " - updates are no more than 1 per second"

            Dim drWidth = Choose(displayType + 1, 7, 22, 10, 10, 5) * pixels.Width / 650
            Dim drHeight = CInt(pixels.Height / 16) + If(pixels.Height < 400, -3, If(pixels.Height < 800, -1, 1))
            If drHeight < 20 Then drHeight = 20
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

            Dim testChange As cv.Mat = If(dst1.Channels = 1, dst1(dw).Clone, dst1(dw).CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            Dim diff As New cv.Mat
            Static savePixels As cv.Mat = testChange
            If savePixels.Size <> testChange.Size Or savePixels.Type <> testChange.Type Then
                savePixels = testChange.Clone
                saveDrawRect = New cv.Rect  ' force the refresh
            Else
                cv.Cv2.Absdiff(savePixels, testChange, diff)
            End If

            If saveDrawRect <> dw Or pixels.pixelResized Or diff.CountNonZero() Then
                savePixels = testChange.Clone
                pixels.pixelResized = False

                Select Case displayType

                    Case 0
                        pixels.line = " col " + If(dw.X Mod 5, "  ", "    ")
                        Dim colDup = If(dw.X < 1000, 26, 25)
                        Dim extraPad = If(dw.X < 1000, "", "  ")
                        For i = 0 To dw.Width - 1
                            If (dw.X + i) Mod 5 Then pixels.line += StrDup(colDup, " ") Else pixels.line += Format(dw.X + i, "#000") + "         " + extraPad
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

                pixels.pixelDataChanged = True
                savedisplayType = displayType
                saveDrawRect = dw
            End If

            ' some algorithms may not update their dst1 or dst2, so preserve it here.  Otherwise, the rectangle will be permanent...
            ' It is extra work but only because the pixel viewer is active.
            'Static saveDst1 = task.algorithmObject.dst1.clone
            'task.algorithmObject.dst1 = saveDst1.clone
            'Static saveDst2 = task.algorithmObject.dst2.clone
            'task.algorithmObject.dst2 = saveDst2.Clone

            If task.mousePicTag = 2 Then
                task.algorithmObject.dst1.Rectangle(saveDrawRect, cv.Scalar.White, If(dst1.Width = 1280, 2, 1))
            Else
                task.algorithmObject.dst2.Rectangle(saveDrawRect, cv.Scalar.White, If(dst1.Width = 1280, 2, 1))
            End If
        Else
            If task.pixelCheck Then
                pixels.Close()
                task.pixelCheck = False
            End If
        End If
    End Sub
    Public Sub closeViewer()
        If check.Box(0).Checked <> GetSetting("OpenCVB", "PixelViewerActive", "PixelViewerActive", False) Then
            SaveSetting("OpenCVB", "PixelViewerActive", "PixelViewerActive", task.pixelCheck)
        End If
        If task.pixelCheck And pixels IsNot Nothing Then pixels.Close()
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

