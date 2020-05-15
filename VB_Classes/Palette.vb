Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO

Public Class Palette_Color
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "blue", 0, 255, ocvb.ms_rng.Next(0, 255))
        sliders.setupTrackBar2(ocvb, caller, "green", 0, 255, ocvb.ms_rng.Next(0, 255))
        sliders.setupTrackBar3(ocvb, caller, "red", 0, 255, ocvb.ms_rng.Next(0, 255))
        ocvb.desc = "Define a color using sliders."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim b = sliders.TrackBar1.Value
        Dim g = sliders.TrackBar2.Value
        Dim r = sliders.TrackBar3.Value
        dst1.SetTo(New cv.Scalar(b, g, r))
        dst2.SetTo(New cv.Scalar(255 - b, 255 - g, 255 - r))
        label1 = "Color (RGB) = " + CStr(b) + " " + CStr(g) + " " + CStr(r)
        label2 = "Color (255 - RGB) = " + CStr(255 - b) + " " + CStr(255 - g) + " " + CStr(255 - r)
    End Sub
End Class




Public Class Palette_LinearPolar
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Use LinearPolar to create gradient image"
        SetInterpolationRadioButtons(ocvb, caller, radio, "LinearPolar")

        sliders.setupTrackBar1(ocvb, caller, "LinearPolar radius", 0, ocvb.color.Cols, ocvb.color.Cols / 2)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        For i = 0 To dst1.Rows - 1
            Dim c = i * 255 / dst1.Rows
            dst1.Row(i).SetTo(New cv.Scalar(c, c, c))
        Next
        Dim iFlag = getInterpolationRadioButtons(radio)
        Static pt = New cv.Point2f(ocvb.ms_rng.Next(0, dst1.Cols - 1), ocvb.ms_rng.Next(0, dst1.Rows - 1))
        Dim radius = sliders.TrackBar1.Value ' ocvb.ms_rng.next(0, dst1.Cols)
        dst2 = ocvb.Color.EmptyClone.SetTo(0)
        cv.Cv2.LinearPolar(dst1, dst1, pt, radius, iFlag)
        cv.Cv2.LinearPolar(ocvb.color, dst2, pt, radius, iFlag)
    End Sub
End Class




Module Palette_Custom_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Palette_Custom(img As IntPtr, map As IntPtr, dst1 As IntPtr, rows As Int32, cols As Int32, channels As Int32)
    End Sub
    Public mapNames() As String = {"Autumn", "Bone", "Cool", "Hot", "Hsv", "Jet", "Ocean", "Pink", "Rainbow", "Spring", "Summer", "Winter", "Parula", "Magma", "Inferno", "Viridis", "Cividis", "Twilight", "Twilight_Shifted", "Random", "None"}
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
    Public Function colorTransition(color1 As cv.Scalar, color2 As cv.Scalar, width As Int32) As cv.Mat
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





Public Class Palette_Map
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders = New OptionsSliders
        sliders.setupTrackBar1(ocvb, caller, "inRange offset", 1, 100, 10)
        ocvb.desc = "Map colors to different palette - Painterly Effect."
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

    Public Sub Run(ocvb As AlgorithmData)
        dst1 = ocvb.color / 64
        dst1 *= 64
        Dim palette As New SortedList(Of cv.Vec3b, Integer)(New CompareVec3b)
        Dim black As New cv.Vec3b(0, 0, 0)
        For y = 0 To dst1.Height - 1
            For x = 0 To dst1.Width - 1
                Dim nextVec = dst1.Get(Of cv.Vec3b)(y, x)
                If nextVec <> black Then
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
            Dim offset = sliders.TrackBar1.Value
            Dim loValue As New cv.Scalar(c(0) - offset, c(1) - offset, c(2) - offset)
            Dim hiValue As New cv.Scalar(c(0) + offset, c(1) + offset, c(2) + offset)
            If loValue.Item(0) < 0 Then loValue.Item(0) = 0
            If loValue.Item(1) < 0 Then loValue.Item(1) = 0
            If loValue.Item(2) < 0 Then loValue.Item(2) = 0
            If hiValue.Item(0) > 255 Then hiValue.Item(0) = 255
            If hiValue.Item(1) > 255 Then hiValue.Item(1) = 255
            If hiValue.Item(2) > 255 Then hiValue.Item(2) = 255

            Dim mask As New cv.Mat
            cv.Cv2.InRange(ocvb.color, loValue, hiValue, mask)

            Dim maxCount = cv.Cv2.CountNonZero(mask)

            dst2 = ocvb.Color.EmptyClone.SetTo(0)
            dst2.SetTo(cv.Scalar.All(255), mask)
            label2 = "Most Common Color +- " + CStr(offset) + " count = " + CStr(maxCount)
        End If
    End Sub
End Class




Public Class Palette_DrawTest
    Inherits ocvbClass
    Dim palette As Palette_ColorMap
    Dim draw As Draw_RngImage
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        palette = New Palette_ColorMap(ocvb, caller)

        draw = New Draw_RngImage(ocvb, caller)
        palette.src = dst1

        ocvb.desc = "Experiment with palette using a drawn image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        draw.Run(ocvb)
        palette.Run(ocvb)
    End Sub
End Class





Public Class Palette_Gradient
    Inherits ocvbClass
    Public frameModulo As Int32 = 30 ' every 30 frames try a different pair of random colors.
    Public color1 As cv.Scalar
    Public color2 As cv.Scalar
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        label2 = "From and To colors"
        ocvb.desc = "Create gradient image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod frameModulo = 0 Then
            If standalone Then
                color1 = New cv.Scalar(ocvb.ms_rng.Next(0, 255), ocvb.ms_rng.Next(0, 255), ocvb.ms_rng.Next(0, 255))
                color2 = New cv.Scalar(ocvb.ms_rng.Next(0, 255), ocvb.ms_rng.Next(0, 255), ocvb.ms_rng.Next(0, 255))
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
    Inherits ocvbClass
    Public gradientColorMap As New cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Number of color transitions (Used only with Random)", 1, 30, 5)

        label2 = "Generated colormap"
        ocvb.desc = "Build a random colormap that smoothly transitions colors - Painterly Effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Then If ocvb.frameCount Mod 100 Then Exit Sub
        Dim color1 = New cv.Scalar(ocvb.ms_rng.Next(0, 255), ocvb.ms_rng.Next(0, 255), ocvb.ms_rng.Next(0, 255))
        Dim color2 = New cv.Scalar(ocvb.ms_rng.Next(0, 255), ocvb.ms_rng.Next(0, 255), ocvb.ms_rng.Next(0, 255))
        Dim gradCount = sliders.TrackBar1.Value
        Dim gradMat As New cv.Mat
        For i = 0 To gradCount - 1
            gradMat = colorTransition(color1, color2, ocvb.color.Width)
            color2 = color1
            color1 = New cv.Scalar(ocvb.ms_rng.Next(0, 255), ocvb.ms_rng.Next(0, 255), ocvb.ms_rng.Next(0, 255))
            If i = 0 Then gradientColorMap = gradMat Else cv.Cv2.HConcat(gradientColorMap, gradMat, gradientColorMap)
        Next
        gradientColorMap = gradientColorMap.Resize(New cv.Size(255, 1))
        Dim r As New cv.Rect(0, 0, 255, 1)
        For i = 0 To dst2.Height - 1
            r.Y = i
            dst2(r) = gradientColorMap
        Next
        If standalone Then dst1 = Palette_Custom_Apply(ocvb.color, gradientColorMap)
    End Sub
End Class





Public Class Palette_ColorMap
    Inherits ocvbClass
    Public gradMap As Palette_BuildGradientColorMap
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        gradMap = New Palette_BuildGradientColorMap(ocvb, caller)

        radio.Setup(ocvb, caller, 21)
        For i = 0 To radio.check.Count - 1
            radio.check(i).Text = mapNames(i)
        Next
        radio.check(4).Checked = True
        ocvb.desc = "Apply the different color maps in OpenCV - Painterly Effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim colormap = cv.ColormapTypes.Autumn
        Static buildNewRandomMap = False
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                colormap = Choose(i + 1, cv.ColormapTypes.Autumn, cv.ColormapTypes.Bone, cv.ColormapTypes.Cool, cv.ColormapTypes.Hot,
                                             cv.ColormapTypes.Hsv, cv.ColormapTypes.Jet, cv.ColormapTypes.Ocean, cv.ColormapTypes.Pink,
                                             cv.ColormapTypes.Rainbow, cv.ColormapTypes.Spring, cv.ColormapTypes.Summer, cv.ColormapTypes.Winter,
                                             12, 13, 14, 15, 16, 17, 18, 19, 20) ' missing some colorMapType definitions but they are there...
                label1 = "ColorMap = " + mapNames(i)

                Static cMapDir As New DirectoryInfo(ocvb.parms.OpenCVfullPath + "/../../../modules/imgproc/doc/pics/colormaps")
                Dim mapFile = New FileInfo(cMapDir.FullName + "/colorscale_" + mapNames(i) + ".jpg")
                If mapFile.Exists Then
                    Dim cmap = cv.Cv2.ImRead(mapFile.FullName)
                    dst2 = cmap.Resize(ocvb.color.Size())
                End If

                ' special case the random color map!
                If colormap = 19 Then
                    Static saveTransitionCount = gradMap.sliders.TrackBar1.Value
                    If buildNewRandomMap = False Or saveTransitionCount <> gradMap.sliders.TrackBar1.Value Then
                        saveTransitionCount = gradMap.sliders.TrackBar1.Value
                        buildNewRandomMap = True
                        gradMap.Run(ocvb)
                    End If
                    dst1 = Palette_Custom_Apply(src, gradMap.gradientColorMap)
                    dst2 = gradMap.dst2
                    Exit For
                End If
                buildNewRandomMap = False ' if they select something other than random, then next random request will rebuild the map.
                If colormap = 20 Then
                    dst1 = src.Clone()
                    dst2 = dst2.SetTo(0)
                    Exit For
                End If
                cv.Cv2.ApplyColorMap(src, dst1, colormap)
                Exit For
            End If
        Next
    End Sub
End Class





Public Class Palette_DepthColorMap
    Inherits ocvbClass
    Public gradientColorMap As New cv.Mat
    Dim holes As Depth_Holes
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        holes = New Depth_Holes(ocvb, caller)

        ocvb.desc = "Build a colormap that best shows the depth.  NOTE: custom color maps need to use C++ ApplyColorMap."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount = 0 Then
            Dim color1 = cv.Scalar.Yellow
            Dim color2 = cv.Scalar.Red
            Dim color3 = cv.Scalar.Blue
            Dim gradMat As New cv.Mat
            gradMat = colorTransition(color1, color2, ocvb.color.Width)
            gradientColorMap = gradMat
            gradMat = colorTransition(color3, color1, ocvb.color.Width)
            cv.Cv2.HConcat(gradientColorMap, gradMat, gradientColorMap)
            gradientColorMap = gradientColorMap.Resize(New cv.Size(255, 1))
        End If
        Dim depth8u = getDepth32f(ocvb).ConvertScaleAbs(0.1)
        If depth8u.Width <> ocvb.color.Width Then depth8u = depth8u.Resize(ocvb.color.Size())
        dst1 = Palette_Custom_Apply(depth8u, gradientColorMap)

        holes.Run(ocvb)
        dst1.SetTo(0, holes.holeMask)

        Dim r As New cv.Rect(100, 0, 255, 1)
        For i = 0 To dst2.Height - 1
            r.Y = i
            dst2(r) = gradientColorMap
        Next
    End Sub
End Class





Public Class Palette_DepthColorMapJet
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Use the Jet colormap to display depth. "
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim depth8u = getDepth32f(ocvb).ConvertScaleAbs(0.03)
        If depth8u.Width <> ocvb.color.Width Then depth8u = depth8u.Resize(ocvb.color.Size())
        cv.Cv2.ApplyColorMap(255 - depth8u, dst1, cv.ColormapTypes.Jet)
    End Sub
End Class
