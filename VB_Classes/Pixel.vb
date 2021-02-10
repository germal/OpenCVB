Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Pixel_Viewer
    Inherits VBparent
    Public pixels As PixelViewerForm
    Public Sub New()
        initParent()

        task.callTrace.Clear() ' special line to clear the tree view otherwise Options_Common is standalone (it is always present, not standalone)
        standalone = False

        task.desc = "Display pixels under the cursor"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        If task.pixelViewerOn Then
            If pixels Is Nothing Then pixels = New PixelViewerForm
            If pixels.Visible = False Then pixels = New PixelViewerForm
            pixels.Show()

            dst1 = Choose(task.mousePicTag + 1, task.color, task.RGBDepth, task.algorithmObject.dst1, task.algorithmObject.dst2)

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

            Dim drWidth = Choose(displayType + 1, 7, 22, 13, 4) * pixels.Width / 650 + 3
            Dim drHeight = CInt(pixels.Height / 16) + If(pixels.Height < 400, -3, If(pixels.Height < 800, -1, 1))
            If drHeight < 20 Then drHeight = 20

            If pixels.mousePoint <> New cv.Point Then
                task.mousePoint += pixels.mousePoint
                task.mousePointUpdated = True
                pixels.mousePoint = New cv.Point
            End If
            Static mouseLoc = New cv.Point(100, 100) ' assume 
            If task.mousePoint.X Or task.mousePoint.Y Then
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

            Dim img = dst1(dw)
            Dim minVal As Single = 0, maxVal As Single = 255
            Dim format32f = "0000.0"
            If img.Type = cv.MatType.CV_32F Or img.Type = cv.MatType.CV_32FC3 Then
                img.MinMaxLoc(minVal, maxVal)
                If minVal >= 0 Then
                    If maxVal < 1000 Then format32f = "000.00"
                    If maxVal < 100 Then format32f = "00.000"
                    If maxVal < 10 Then format32f = "0.0000"
                Else
                    maxVal = Math.Max(-minVal, maxVal)
                    format32f = " 0.000;-0.000"
                    If maxVal < 1000 Then format32f = " 000.0;-000.0"
                    If maxVal < 100 Then format32f = " 00.00;-00.00"
                    If maxVal < 10 Then format32f = " 0.000;-0.000"
                End If
            End If

            Static saveMousePoint = task.mousePoint
            If ((saveDrawRect <> dw Or pixels.pixelResized Or pixels.updateReady) And diff.CountNonZero()) Or task.mousePoint <> saveMousePoint Then
                pixels.updateReady = False
                savePixels = testChange.Clone
                pixels.pixelResized = False
                Dim imgText = ""
                Select Case displayType

                    Case 0
                        imgText += If(dw.X + drWidth > 1000, " col    ", " col    ") + CStr(dw.X) + " through " + CStr(CInt(dw.X + drWidth)) + vbCrLf
                        For y = 0 To img.Height - 1
                            imgText += "r" + Format(dw.Y + y, "000") + "   "
                            For x = 0 To img.Width - 1
                                Dim vec = img.Get(Of cv.Vec3b)(y, x)
                                imgText += Format(vec.Item0, "000") + " " + Format(vec.Item1, "000") + " " + Format(vec.Item2, "000") + "   "
                            Next
                            imgText += vbCrLf
                        Next

                    Case 1
                        imgText += If(dw.X + drWidth > 1000, " col    ", " col    ") + CStr(dw.X) + " through " + CStr(CInt(dw.X + drWidth)) + vbCrLf
                        For y = 0 To img.Height - 1
                            imgText += "r" + Format(dw.Y + y, "000") + "   "
                            For x = 0 To img.Width - 1
                                imgText += Format(img.Get(Of Byte)(y, x), "000") + If((dw.X + x) Mod 5 = 4, "   ", " ")
                            Next
                            imgText += vbCrLf
                        Next

                    Case 2
                        imgText += If(dw.X + drWidth > 1000, " col    ", " col    ") + CStr(dw.X) + " through " + CStr(CInt(dw.X + drWidth)) + vbCrLf
                        For y = 0 To img.Height - 1
                            imgText += "r" + Format(dw.Y + y, "000") + "   "
                            For x = 0 To img.Width - 1
                                imgText += Format(img.Get(Of Single)(y, x), format32f) + If((dw.X + x) Mod 5 = 4, "   ", " ")
                            Next
                            imgText += vbCrLf
                        Next

                    Case 3
                        imgText += If(dw.X + drWidth > 1000, " col    ", " col    ") + CStr(dw.X) + " through " + CStr(CInt(dw.X + drWidth)) + vbCrLf
                        For y = 0 To img.Height - 1
                            imgText += "r" + Format(dw.Y + y, "000") + "   "
                            For x = 0 To img.Width - 1
                                Dim vec = img.Get(Of cv.Vec3f)(y, x)
                                imgText += Format(vec.Item0, format32f) + " " + Format(vec.Item1, format32f) + " " + Format(vec.Item2, format32f) + "   "
                            Next
                            imgText += vbCrLf
                        Next
                    Case 4

                End Select
                saveMousePoint = task.mousePoint
                savedisplayType = displayType
                saveDrawRect = dw
                pixels.rtb.Text = imgText
                pixels.Refresh()
            End If

            ' Dim outImg As cv.Mat = If(task.mousePicTag = 2, task.algorithmObject.dst1, task.algorithmObject.dst2)
            dst1.MinMaxLoc(minVal, maxVal)
            dst1.Rectangle(saveDrawRect, cv.Scalar.All(maxVal), If(dst1.Width = 1280, 3, 2))
            dst1.Rectangle(saveDrawRect, cv.Scalar.All(minVal), If(dst1.Width = 1280, 2, 1))
        Else
            If pixels IsNot Nothing Then
                pixels.Close()
                pixels = Nothing
            End If
        End If
    End Sub
    Public Sub closeViewer()
        If pixels IsNot Nothing Then pixels.Close()
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

