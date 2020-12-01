Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO

Public Class Palette_Basics
    Inherits VBparent
    Public gradMap As Palette_BuildGradientColorMap
    Public colormap As cv.ColormapTypes
    Public colorMat As cv.Mat
    Public Sub New()
        initParent()
        gradMap = New Palette_BuildGradientColorMap()

        radio.Setup(caller, 21)
        Static frm = findForm("Palette_Basics Radio Options")
        For i = 0 To frm.check.length - 1
            frm.check(i).Text = mapNames(i)
        Next
        Dim hsvRadio = findRadio("Hot")
        hsvRadio.Checked = True
        task.desc = "Apply the different color maps in OpenCV - Painterly Effect"
    End Sub
    Public Function checkRadios() As cv.ColormapTypes
        Static frm = findForm("Palette_Basics Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                Dim scheme = Choose(i + 1, cv.ColormapTypes.Autumn, cv.ColormapTypes.Bone, cv.ColormapTypes.Cividis, cv.ColormapTypes.Cool,
                                           cv.ColormapTypes.Hot, cv.ColormapTypes.Hsv, cv.ColormapTypes.Inferno, cv.ColormapTypes.Jet,
                                           cv.ColormapTypes.Magma, cv.ColormapTypes.Ocean, cv.ColormapTypes.Parula, cv.ColormapTypes.Pink,
                                           cv.ColormapTypes.Plasma, cv.ColormapTypes.Rainbow, cv.ColormapTypes.Spring, cv.ColormapTypes.Summer,
                                           cv.ColormapTypes.Twilight, cv.ColormapTypes.TwilightShifted, cv.ColormapTypes.Viridis,
                                           cv.ColormapTypes.Winter, 20) ' The last = placeholder for Random...
                Return scheme
            End If
        Next
        Return 0
    End Function
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        colormap = checkRadios()
        label1 = "ColorMap = " + mapNames(colormap)

        Static cMapDir As New DirectoryInfo(ocvb.parms.homeDir + "opencv/modules/imgproc/doc/pics/colormaps")
        Static saveColorMap As Integer = -1
        If saveColorMap <> colormap Then
            saveColorMap = colormap
            Dim str = cMapDir.FullName + "/colorscale_" + mapNames(colormap) + ".jpg"
            ' Something is flipped - Ocean is actually HSV and vice versa.  This addresses it but check in future OpenCVSharp releases...
            If str.Contains("Ocean") Then
                str = str.Replace("Ocean", "Hsv")
            ElseIf str.Contains("Hsv") Then
                str = str.Replace("Hsv", "Ocean")
            End If
            Dim mapFile = New FileInfo(str)
            If mapFile.Exists Then
                colorMat = cv.Cv2.ImRead(mapFile.FullName)
                If standalone Then dst2 = colorMat.Resize(src.Size())
            End If
        End If

        ' special case the random color map!
        If colormap = 20 Then
            gradMap.Run()

            ' Uncomment this to test if the .NET interface for ApplyColorMap for custom color maps is working
            ' cv.Cv2.ApplyColorMap(src, dst2, gradMap.gradientColorMap) 

            ' In the meantime, this will work!
            dst1 = Palette_Custom_Apply(src, gradMap.gradientColorMap)
            dst2 = gradMap.gradientColorMap.Resize(dst2.Size)
        Else
            cv.Cv2.ApplyColorMap(src, dst1, colormap)
        End If
    End Sub
End Class





Public Class Palette_Color
    Inherits VBparent
    Public Sub New()
        initParent()
        sliders.Setup(caller)
        sliders.setupTrackBar(0, "blue", 0, 255, msRNG.Next(0, 255))
        sliders.setupTrackBar(1, "green", 0, 255, msRNG.Next(0, 255))
        sliders.setupTrackBar(2, "red", 0, 255, msRNG.Next(0, 255))
        task.desc = "Define a color using sliders."
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim b = sliders.trackbar(0).Value
        Dim g = sliders.trackbar(1).Value
        Dim r = sliders.trackbar(2).Value
        dst1.SetTo(New cv.Scalar(b, g, r))
        dst2.SetTo(New cv.Scalar(255 - b, 255 - g, 255 - r))
        label1 = "Color (RGB) = " + CStr(b) + " " + CStr(g) + " " + CStr(r)
        label2 = "Color (255 - RGB) = " + CStr(255 - b) + " " + CStr(255 - g) + " " + CStr(255 - r)
    End Sub
End Class




Public Class Palette_LinearPolar
    Inherits VBparent
    Public Sub New()
        initParent()
        task.desc = "Use LinearPolar to create gradient image"
        SetInterpolationRadioButtons(caller, radio, "LinearPolar")

        sliders.Setup(caller)
        sliders.setupTrackBar(0, "LinearPolar radius", 0, src.Cols, src.Cols / 2)
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        dst1.SetTo(0)
        For i = 0 To dst1.Rows - 1
            Dim c = i * 255 / dst1.Rows
            dst1.Row(i).SetTo(New cv.Scalar(c, c, c))
        Next
        Static frm = findForm("Palette_LinearPolar Radio Options")
        Dim iFlag = getInterpolationRadioButtons(radio, frm)
        Static pt = New cv.Point2f(msRNG.Next(0, dst1.Cols - 1), msRNG.Next(0, dst1.Rows - 1))
        Dim radius = sliders.trackbar(0).Value ' msRNG.next(0, dst1.Cols)
        dst2.SetTo(0)
        If iFlag = cv.InterpolationFlags.WarpInverseMap Then sliders.trackbar(0).Value = sliders.trackbar(0).Maximum
        cv.Cv2.LinearPolar(dst1, dst1, pt, radius, iFlag)
        cv.Cv2.LinearPolar(src, dst2, pt, radius, iFlag)
    End Sub
End Class




Module Palette_Custom_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Palette_Custom(img As IntPtr, map As IntPtr, dst1 As IntPtr, rows As Integer, cols As Integer, channels As Integer)
    End Sub
    Public mapNames() As String = {"Autumn", "Bone", "Cividis", "Cool", "Hot", "Hsv", "Inferno", "Jet", "Magma", "Ocean", "Parula", "Pink",
                                   "Plasma", "Rainbow", "Spring", "Summer", "Twilight", "TwilightShifted", "Viridis", "Winter", "Random - use slider to adjust"}
    Public Function Palette_Custom_Apply(src As cv.Mat, customColorMap As cv.Mat) As cv.Mat
        ' the VB.Net interface to OpenCV doesn't support adding a random lookup table to ApplyColorMap API.  It is available in C++ though.
        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)

        Dim mapData(customColorMap.Total * customColorMap.ElemSize - 1) As Byte
        Dim handleMap = GCHandle.Alloc(mapData, GCHandleType.Pinned)
        Marshal.Copy(customColorMap.Data, mapData, 0, mapData.Length)

        Dim dstData(src.Total * 3 - 1) As Byte ' it always comes back in color...
        Dim handledst1 = GCHandle.Alloc(dstData, GCHandleType.Pinned)

        ' the custom colormap API is not implemented for custom color maps.  Only colormapTypes can be provided.
        Palette_Custom(handleSrc.AddrOfPinnedObject, handleMap.AddrOfPinnedObject, handledst1.AddrOfPinnedObject, src.Rows, src.Cols, src.Channels)

        Dim output = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
        Marshal.Copy(dstData, 0, output.Data, dstData.Length)
        handleSrc.Free()
        handleMap.Free()
        handledst1.Free()
        Return output
    End Function
    Public Function colorTransition(color1 As cv.Scalar, color2 As cv.Scalar, width As Integer) As cv.Mat
        Dim f As Double = 1.0
        Dim gradientColors As New cv.Mat(1, width, cv.MatType.CV_64FC3)
        For i = 0 To width - 1
            gradientColors.Set(Of cv.Scalar)(0, i, New cv.Scalar(f * color2(0) + (1 - f) * color1(0), f * color2(1) + (1 - f) * color1(1),
                                                                     f * color2(2) + (1 - f) * color1(2)))
            f -= 1 / width
        Next
        Dim result = New cv.Mat(1, width, cv.MatType.CV_8UC3)
        For i = 0 To width - 1
            result.Col(i).SetTo(gradientColors.Get(Of cv.Scalar)(0, i))
        Next
        Return result
    End Function
End Module







Public Class Palette_Reduction
    Inherits VBparent
    Dim reduction As Reduction_Basics
    Public Sub New()
        initParent()
        reduction = New Reduction_Basics()
        reduction.radio.check(0).Checked = True
        reduction.radio.check(2).Enabled = False ' must have some reduction for this to work...

        sliders.Setup(caller)
        sliders.setupTrackBar(0, "InRange offset from specific color", 1, 100, 10)
        task.desc = "Map colors to different palette - Painterly Effect."
        label1 = "Reduced Colors"
    End Sub
    Private Class CompareVec3b : Implements IComparer(Of cv.Vec3b)
        Public Function Compare(ByVal a As cv.Vec3b, ByVal b As cv.Vec3b) As Integer Implements IComparer(Of cv.Vec3b).Compare
            If a(0) <> b(0) Then
                Return If(a(0) < b(0), -1, 1)
            ElseIf a(1) <> b(1) Then
                Return If(a(1) < b(1), -1, 1)
            End If
            If a(2) = b(2) Then Return 0
            Return If(a(2) < b(2), -1, 1)
        End Function
    End Class
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static reductionSlider = findSlider("Reduction factor")
        If reductionSlider.value < 32 Then
            reductionSlider.value = 32
            Console.WriteLine("This algorithm gets very slow unless there is lots of reduction.  Resetting reduction slider value to 2^^5")
        End If
        reduction.src = src
        reduction.Run()
        dst1 = reduction.dst1

        Dim palette As New SortedList(Of cv.Vec3b, Integer)(New CompareVec3b)
        For y = 0 To dst1.Height - 1
            For x = 0 To dst1.Width - 1
                Dim nextVec = dst1.Get(Of cv.Vec3b)(y, x)
                If nextVec <> cv.Scalar.Black Then
                    If palette.ContainsKey(nextVec) Then
                        palette(nextVec) = palette(nextVec) + 1
                    Else
                        palette.Add(nextVec, 1)
                    End If
                End If
            Next
        Next

        label1 = "palette count = " + CStr(palette.Count)
        Dim max As Integer
        Dim maxIndex As Integer
        For i = 0 To palette.Count - 1
            If palette.ElementAt(i).Value > max Then
                max = palette.ElementAt(i).Value
                maxIndex = i
            End If
        Next

        If palette.Count > 0 Then
            Dim c = palette.ElementAt(maxIndex).Key
            Dim offset = sliders.trackbar(0).Value
            Dim loValue As New cv.Scalar(c(0) - offset, c(1) - offset, c(2) - offset)
            Dim hiValue As New cv.Scalar(c(0) + offset, c(1) + offset, c(2) + offset)
            If loValue.Item(0) < 0 Then loValue.Item(0) = 0
            If loValue.Item(1) < 0 Then loValue.Item(1) = 0
            If loValue.Item(2) < 0 Then loValue.Item(2) = 0
            If hiValue.Item(0) > 255 Then hiValue.Item(0) = 255
            If hiValue.Item(1) > 255 Then hiValue.Item(1) = 255
            If hiValue.Item(2) > 255 Then hiValue.Item(2) = 255

            Dim mask As New cv.Mat
            cv.Cv2.InRange(src, loValue, hiValue, mask)

            Dim maxCount = cv.Cv2.CountNonZero(mask)

            dst2 = src.EmptyClone.SetTo(0)
            dst2.SetTo(cv.Scalar.All(255), mask)
            label2 = "Most Common Color +- " + CStr(offset) + " count = " + CStr(maxCount)
        End If
    End Sub
End Class




Public Class Palette_DrawTest
    Inherits VBparent
    Dim palette As Palette_Basics
    Dim draw As Draw_RngImage
    Public Sub New()
        initParent()
        palette = New Palette_Basics()

        draw = New Draw_RngImage()
        palette.src = dst1

        task.desc = "Experiment with palette using a drawn image"
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        draw.Run()
        palette.src = draw.dst1
        palette.Run()
        dst1 = palette.dst1
    End Sub
End Class





Public Class Palette_Gradient
    Inherits VBparent
    Public frameModulo As Integer = 30 ' every 30 frames try a different pair of random colors.
    Public color1 As cv.Scalar
    Public color2 As cv.Scalar
    Public Sub New()
        initParent()
        label2 = "From and To colors"
        task.desc = "Create gradient image"
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        If ocvb.frameCount Mod frameModulo = 0 Then
            If standalone Then
                color1 = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
                color2 = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
            End If
            dst2.SetTo(color1)
            dst2(New cv.Rect(0, 0, dst2.Width, dst2.Height / 2)).SetTo(color2)

            Dim gradientColors As New cv.Mat(dst1.Rows, 1, cv.MatType.CV_64FC3)
            Dim f As Double = 1.0
            For i = 0 To dst1.Rows - 1
                gradientColors.Set(Of cv.Scalar)(i, 0, New cv.Scalar(f * color2(0) + (1 - f) * color1(0), f * color2(1) + (1 - f) * color1(1),
                                                                         f * color2(2) + (1 - f) * color1(2)))
                f -= 1 / dst1.Rows
            Next

            For i = 0 To dst1.Rows - 1
                dst1.Row(i).SetTo(gradientColors.Get(Of cv.Scalar)(i))
            Next
        End If
    End Sub
End Class





Public Class Palette_BuildGradientColorMap
    Inherits VBparent
    Public gradientColorMap As New cv.Mat
    Public Sub New()
        initParent()
        sliders.Setup(caller)
        sliders.setupTrackBar(0, "Number of color transitions (Used only with Random)", 1, 30, 5)

        label2 = "Generated colormap"
        task.desc = "Build a random colormap that smoothly transitions colors - Painterly Effect"
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim color1 = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
        Dim color2 = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
        Static saveGradCount = -1
        Static transitionSlider = findSlider("Number of color transitions (Used only with Random)")
        Dim gradCount = transitionSlider.Value
        If saveGradCount <> gradCount Then
            saveGradCount = gradCount
            Dim gradMat As New cv.Mat
            For i = 0 To gradCount - 1
                gradMat = colorTransition(color1, color2, src.Width)
                color2 = color1
                color1 = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
                If i = 0 Then gradientColorMap = gradMat Else cv.Cv2.HConcat(gradientColorMap, gradMat, gradientColorMap)
            Next
        End If
        gradientColorMap = gradientColorMap.Resize(New cv.Size(256, 1))
        dst1 = Palette_Custom_Apply(src, gradientColorMap)
        If standalone Then dst2 = gradientColorMap
    End Sub
End Class





Public Class Palette_DepthColorMap
    Inherits VBparent
    Dim gradientColorMap As New cv.Mat
    Dim holes As Depth_Holes
    Public Sub New()
        initParent()
        holes = New Depth_Holes()
        hideForm("Depth_Holes Slider Options")

        sliders.Setup(caller)
        sliders.setupTrackBar(0, "Convert and Scale value X100", 0, 100, 8)

        label2 = "Palette used to color left image"
        task.desc = "Build a colormap that best shows the depth.  NOTE: custom color maps need to use C++ ApplyColorMap."
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        If ocvb.frameCount = 0 Then
            Dim color1 = cv.Scalar.Yellow
            Dim color2 = cv.Scalar.Red
            Dim color3 = cv.Scalar.Blue
            Dim gradMat As New cv.Mat
            gradMat = colorTransition(color1, color2, src.Width)
            gradientColorMap = gradMat
            gradMat = colorTransition(color3, color1, src.Width)
            cv.Cv2.HConcat(gradientColorMap, gradMat, gradientColorMap)
            gradientColorMap = gradientColorMap.Resize(New cv.Size(255, 1))

            Dim r As New cv.Rect(0, 0, 255, 1)
            For i = 0 To dst2.Height - 1
                r.Y = i
                dst2(r) = gradientColorMap
            Next
        End If
        Static cvtScaleSlider = findSlider("Convert and Scale value X100")
        Dim depth8u = getDepth32f().ConvertScaleAbs(cvtScaleSlider.Value / 100)
        dst1 = Palette_Custom_Apply(depth8u, gradientColorMap)

        holes.Run()
        dst1.SetTo(0, holes.holeMask)
    End Sub
End Class







Public Class Palette_Consistency
    Inherits VBparent
    Dim emax As EMax_CPP
    Public hist As Histogram_Simple
    Dim lut As LUT_Rebuild
    Private Class CompareHistCounts : Implements IComparer(Of Single)
        Public Function Compare(ByVal a As Single, ByVal b As Single) As Integer Implements IComparer(Of Single).Compare
            If a > b Then Return 1
            Return -1 ' never returns equal because duplicates can happen.
        End Function
    End Class
    Public Sub New()
        initParent()
        emax = New EMax_CPP()
        emax.basics.sliders.trackbar(1).Value = 15

        hist = New Histogram_Simple()
        hist.sliders.trackbar(0).Value = 255
        hideForm("Histogram_Simple Slider Options")

        lut = New LUT_Rebuild()

        task.desc = "Using a histogram, assign the same colors to the same areas across frames"
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        If standalone Then
            emax.Run()
            src = emax.dst2
        End If
        Dim size = New cv.Size(src.Width / 4, src.Height / 4)
        Dim img = src.Resize(size, 0, 0, cv.InterpolationFlags.Cubic)
        img = img.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        img = img.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        hist.src = img
        hist.Run()
        If standalone Then dst2 = hist.dst1.Resize(src.Size)

        Dim histogram = hist.plotHist.hist
        Dim orderedByCount As New SortedList(Of Single, Integer)(New CompareHistCounts)
        For i = 0 To histogram.Rows - 1
            Dim nextVal = histogram.Get(Of Single)(i)
            If nextVal > 500 Then orderedByCount.Add(nextVal, i)
        Next

        Dim grayIndex As Integer
        Dim grayIncr As Integer = CInt(255 / orderedByCount.Count)
        For i = orderedByCount.Count - 1 To 0 Step -1
            Dim paletteIndex = orderedByCount.ElementAt(i).Value
            lut.paletteMap(paletteIndex) = grayIndex
            grayIndex += grayIncr
        Next

        lut.src = img
        lut.Run()
        dst1 = lut.dst1.Resize(src.Size())
    End Sub
End Class






Public Class Palette_ObjectColors
    Inherits VBparent
    Dim reduction As Reduction_KNN_Color
    Dim inrange As Depth_InRange
    Dim palette As Palette_Basics
    Public gray As cv.Mat
    Public Sub New()
        initParent()

        palette = New Palette_Basics()
        hideForm("Palette_BuildGradientColorMap Slider Options")
        reduction = New Reduction_KNN_Color()
        inrange = New Depth_InRange()

        label1 = "Consistent colors"
        label2 = "Original colors"
        task.desc = "New class description"
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        reduction.src = src
        reduction.Run()
        dst2 = reduction.dst2

        inrange.Run()
        Static minDepthSlider = findSlider("InRange Min Depth")
        Static maxDepthSlider = findSlider("InRange Max Depth")
        If ocvb.frameCount = 0 Then maxDepthSlider.value = maxDepthSlider.maximum
        Dim minDepth = minDepthSlider.value
        Dim maxDepth = maxDepthSlider.value
        If maxDepth - minDepth < 2 Then Exit Sub

        Dim depth32f = getDepth32f()
        Dim blobList As New SortedList(Of Single, Integer)
        For i = 0 To reduction.pTrack.drawRC.viewObjects.Count - 1
            Dim vo = reduction.pTrack.drawRC.viewObjects.Values(i)
            If vo.mask IsNot Nothing Then
                Dim mask = vo.mask.Clone
                Dim r = vo.preKalmanRect
                mask.SetTo(0, inrange.noDepthMask(r)) ' count only points with depth
                Dim countDepthPixels = mask.CountNonZero()
                If countDepthPixels > 30 Then
                    Dim depth = depth32f(r).Mean(mask)
                    If blobList.ContainsKey(depth.Item(0)) = False Then
                        If depth.Item(0) > minDepth And depth.Item(0) < maxDepth Then blobList.Add(depth.Item(0), i)
                    End If
                End If
            End If
        Next

        gray = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For i = 0 To blobList.Count - 1
            Dim index = blobList.ElementAt(i).Value
            Dim blob = reduction.pTrack.drawRC.viewObjects.Values(index)
            gray(blob.preKalmanRect).SetTo(i + 1, blob.mask)
        Next
        dst1 = gray * Math.Floor(255 / blobList.Count) ' map to 0-255
        Dim colormap = palette.checkRadios()
        cv.Cv2.ApplyColorMap(src, dst1, colormap)

        dst1.SetTo(0, gray.ConvertScaleAbs(255))
        For i = 0 To blobList.Count - 1
            Dim index = blobList.ElementAt(i).Value
            Dim blob = reduction.pTrack.drawRC.viewObjects.Values(index)
            dst1.Rectangle(New cv.Rect(blob.centroid.X, blob.centroid.Y, 60 * ocvb.fontSize, 30 * ocvb.fontSize), cv.Scalar.Black, -1)
            ocvb.trueText(CStr(CInt(blobList.ElementAt(i).Key)), blob.centroid)
        Next
        label1 = CStr(blobList.Count) + " regions between " + Format(minDepth / 1000, "0.0") + " and " + Format(maxDepth / 1000, "0.0") + "meters"
    End Sub
End Class






Public Class Palette_Layout2D
    Inherits VBparent
    Dim grid As Thread_Grid
    Public Sub New()
        initParent()
        grid = New Thread_Grid()
        Dim widthSlider = findSlider("ThreadGrid Width")
        Dim heightslider = findSlider("ThreadGrid Height")
        widthSlider.Value = 40
        heightslider.Value = 24
        grid.Run()
        task.desc = "Layout the available colors in a 2D grid"
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        grid.Run()
        Dim index As Integer
        For Each r In grid.roiList
            dst1(r).SetTo(ocvb.scalarColors(index Mod 255))
            index += 1
        Next
        label1 = "Palette_Layout2D - " + CStr(grid.roiList.Count) + " regions"
    End Sub
End Class
