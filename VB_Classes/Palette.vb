Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO

Public Class Palette_Color : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "blue", 0, 255, ocvb.rng.uniform(0, 255))
        sliders.setupTrackBar2(ocvb, "green", 0, 255, ocvb.rng.uniform(0, 255))
        sliders.setupTrackBar3(ocvb, "red", 0, 255, ocvb.rng.uniform(0, 255))
        If ocvb.parms.ShowOptions Then sliders.show()
        ocvb.desc = "Define a color using sliders."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim b = sliders.TrackBar1.Value
        Dim g = sliders.TrackBar2.Value
        Dim r = sliders.TrackBar3.Value
        ocvb.result1.SetTo(New cv.Scalar(b, g, r))
        ocvb.result2.SetTo(New cv.Scalar(255 - b, 255 - g, 255 - r))
        ocvb.label1 = "Color (RGB) = " + CStr(b) + " " + CStr(g) + " " + CStr(r)
        ocvb.label2 = "Color (255 - RGB) = " + CStr(255 - b) + " " + CStr(255 - g) + " " + CStr(255 - r)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class Palette_LinearPolar : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Use LinearPolar to create gradient image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod 30 = 0 Then
            For i = 0 To ocvb.result1.Rows - 1
                Dim c = i * 255 / ocvb.result1.Rows
                ocvb.result1.Row(i).SetTo(New cv.Scalar(c, c, c))
            Next
            Static choices As Int32
            Dim iFlagName = Choose(choices Mod 7 + 1, "Area", "Cubic", "lanczos4", "Linear", "Nearest", "WarpFillOutliers", "WarpInverseMap")
            Dim iFlag = Choose(choices Mod 7 + 1, cv.InterpolationFlags.Area, cv.InterpolationFlags.Cubic, cv.InterpolationFlags.Lanczos4, cv.InterpolationFlags.Linear,
                                                      cv.InterpolationFlags.Nearest, cv.InterpolationFlags.WarpFillOutliers, cv.InterpolationFlags.WarpInverseMap)
            ocvb.label1 = "LinearPolar " + iFlagName
            ocvb.label2 = "LinearPolar RGB image"

            Static pt = New cv.Point2f(ocvb.rng.uniform(0, ocvb.result1.Cols - 1), ocvb.rng.uniform(0, ocvb.result1.Rows - 1))
            Dim radius = ocvb.rng.uniform(0, ocvb.result1.Cols)
            ocvb.result2.SetTo(0)
            cv.Cv2.LinearPolar(ocvb.result1, ocvb.result1, pt, radius, iFlag)
            cv.Cv2.LinearPolar(ocvb.color, ocvb.result2, pt, radius, iFlag)
            choices += 1
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class


Module Palette_Custom_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Palette_Custom(img As IntPtr, map As IntPtr, dst As IntPtr, rows As Int32, cols As Int32)
    End Sub
    Public mapNames() As String = {"Autumn", "Bone", "Cool", "Hot", "Hsv", "Jet", "Ocean", "Pink", "Rainbow", "Spring", "Summer", "Winter", "Parula", "Magma", "Inferno", "Viridis", "Cividis", "Twilight", "Twilight_Shifted", "Random", "None"}
    Public Sub Palette_ApplyCustom(ocvb As AlgorithmData, src As cv.Mat, randomColorMap As cv.Mat)
        ' the VB.Net interface to OpenCV doesn't support adding a random lookup table to ApplyColorMap API.  It is available in C++ though.
        src /= 64
        src *= 64
        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)

        Dim mapData(randomColorMap.Total * randomColorMap.ElemSize - 1) As Byte
        Dim handleMap = GCHandle.Alloc(mapData, GCHandleType.Pinned)
        Marshal.Copy(randomColorMap.Data, mapData, 0, mapData.Length)

        Dim dstData(src.Total * src.ElemSize - 1) As Byte
        Dim handleDst = GCHandle.Alloc(dstData, GCHandleType.Pinned)

        Palette_Custom(handleSrc.AddrOfPinnedObject, handleMap.AddrOfPinnedObject, handleDst.AddrOfPinnedObject, src.Rows, src.Cols)

        Marshal.Copy(dstData, 0, ocvb.result1.Data, dstData.Length)
        handleSrc.Free()
        handleMap.Free()
        handleDst.Free()
    End Sub

End Module


Public Class Palette_ColorMap : Implements IDisposable
    Public src As New cv.Mat
    Public externalUse As Boolean
    Public radio As New OptionsRadioButtons
    Public randomMap = New cv.Mat(256, 1, cv.MatType.CV_8UC3, New cv.Scalar(255, 255, 255))
    Public Sub New(ocvb As AlgorithmData)
        radio.Setup(ocvb, 21)
        For i = 0 To radio.check.Count - 1
            radio.check(i).Text = mapNames(i)
        Next
        radio.check(4).Checked = True
        If ocvb.parms.ShowOptions Then radio.Show()
        ocvb.desc = "Apply the different color maps in OpenCV - Painterly Effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim colormap = cv.ColormapTypes.Autumn
        Dim randomActive As Boolean
        If externalUse = False Then src = ocvb.color.Clone()
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                colormap = Choose(i + 1, cv.ColormapTypes.Autumn, cv.ColormapTypes.Bone, cv.ColormapTypes.Cool, cv.ColormapTypes.Hot,
                                         cv.ColormapTypes.Hsv, cv.ColormapTypes.Jet, cv.ColormapTypes.Ocean, cv.ColormapTypes.Pink,
                                         cv.ColormapTypes.Rainbow, cv.ColormapTypes.Spring, cv.ColormapTypes.Summer, cv.ColormapTypes.Winter, 12, 13, 14, 15, 16, 17, 18, 19, 20)
                If colormap = 19 Then randomActive = True
                ocvb.label1 = mapNames(i)
                If colormap = 20 Then
                    ocvb.result1 = src.Clone()
                    Exit Sub
                End If
                Exit For
            End If
        Next
        If randomActive Then
            Static frameCount = ocvb.frameCount
            Static randomColorMap = New cv.Mat(256, 1, cv.MatType.CV_8UC3, New cv.Scalar(255, 255, 255))
            If frameCount = ocvb.frameCount Then
                cv.Cv2.Randu(randomColorMap, New cv.Scalar(0, 0, 0), New cv.Scalar(255, 255, 255))
                randomColorMap.copyTo(randomMap)
            End If
            Palette_ApplyCustom(ocvb, src, randomColorMap)
        Else
            cv.Cv2.ApplyColorMap(src, ocvb.result1, colormap)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        radio.Dispose()
    End Sub
End Class




Public Class Palette_Map : Implements IDisposable
    Dim sliders As OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders = New OptionsSliders
        sliders.setupTrackBar1(ocvb, "inRange offset", 1, 100, 10)
        If ocvb.parms.ShowOptions Then sliders.show()
        ocvb.desc = "Map colors to different palette - Painterly Effect."
        ocvb.label1 = "Reduced Colors"
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
        ocvb.result1 = ocvb.color / 64
        ocvb.result1 *= 64
        Dim palette As New SortedList(Of cv.Vec3b, Integer)(New CompareVec3b)
        Dim black As New cv.Vec3b(0, 0, 0)
        For y = 0 To ocvb.result1.Height - 1
            For x = 0 To ocvb.result1.Width - 1
                Dim nextVec = ocvb.result1.At(Of cv.Vec3b)(y, x)
                If nextVec <> black Then
                    If palette.ContainsKey(nextVec) Then
                        palette(nextVec) = palette(nextVec) + 1
                    Else
                        palette.Add(nextVec, 1)
                    End If
                End If
            Next
        Next

        ocvb.label1 = "palette count = " + CStr(palette.Count)
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

            ocvb.result2.SetTo(0)
            ocvb.result2.SetTo(cv.Scalar.All(255), mask)
            ocvb.label2 = "Most Common Color +- " + CStr(offset) + " count = " + CStr(maxCount)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class





Public Class Palette_Gradient : Implements IDisposable
    Public frameModulo As Int32 = 30 ' every 30 frames try a different pair of random colors.
    Public color1 As cv.Scalar
    Public color2 As cv.Scalar
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label2 = "From and To colors"
        ocvb.desc = "Create gradient image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod frameModulo = 0 Then
            Dim f As Double = 1.0
            If externalUse = False Then
                color1 = ocvb.colorScalar(ocvb.rng.uniform(0, 255))
                color2 = ocvb.colorScalar(ocvb.rng.uniform(0, 255))
            End If
            ocvb.result2.SetTo(color1)
            ocvb.result2(New cv.Rect(0, 0, ocvb.result2.Width, ocvb.result2.Height / 2)).SetTo(color2)

            Dim gradientColors As New cv.Mat(ocvb.result1.Rows, 1, cv.MatType.CV_64FC3)
            For i = 0 To ocvb.result1.Rows - 1
                gradientColors.Set(Of cv.Scalar)(i, 0, New cv.Scalar(f * color2(0) + (1 - f) * color1(0), f * color2(1) + (1 - f) * color1(1),
                                                                     f * color2(2) + (1 - f) * color1(2)))
                f -= 1 / ocvb.result1.Rows
            Next

            For i = 0 To ocvb.result1.Rows - 1
                ocvb.result1.Row(i).SetTo(gradientColors.At(Of cv.Scalar)(i))
            Next
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class Palette_Random : Implements IDisposable
    Dim gradient As Palette_Gradient
    Dim resized As Resize_Percentage
    Dim check As New OptionsCheckbox
    Public Sub New(ocvb As AlgorithmData)
        gradient = New Palette_Gradient(ocvb)
        gradient.frameModulo = 1

        resized = New Resize_Percentage(ocvb)
        resized.externalUse = True

        check.Setup(ocvb, 1)
        check.Box(0).Text = "Generate a new pair of random colors"
        check.Box(0).Checked = True
        If ocvb.parms.ShowOptions Then check.Show()
        ocvb.desc = "Create a random colormap and apply it - Painterly Effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If check.Box(0).Checked Then
            check.Box(0).Checked = False
            gradient.Run(ocvb)
            ' we want 256 colors from the gradient in result1
            resized.src = ocvb.result1
            resized.resizePercent = 256 / ocvb.color.Total ' we want only 256 colors
            resized.Run(ocvb)
        End If
        Palette_ApplyCustom(ocvb, ocvb.color, resized.dst)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        gradient.Dispose()
        resized.Dispose()
        check.Dispose()
    End Sub
End Class





Public Class Palette_DrawTest : Implements IDisposable
    Dim palette As Palette_ColorMap
    Dim draw As Draw_RngImage
    Public src As New cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        palette = New Palette_ColorMap(ocvb)
        palette.externalUse = True

        draw = New Draw_RngImage(ocvb)
        palette.src = ocvb.result1

        ocvb.desc = "Experiment with palette using a drawn image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        draw.Run(ocvb)
        palette.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        palette.Dispose()
        draw.Dispose()
    End Sub
End Class





Public Class Palette_Display : Implements IDisposable
    Dim palette As Palette_ColorMap
    Public Sub New(ocvb As AlgorithmData)
        palette = New Palette_ColorMap(ocvb)
        palette.externalUse = True

        ocvb.desc = "Display the requested palette and the results of its application."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        palette.src = ocvb.color.Clone()
        palette.Run(ocvb)
        Dim cMapDir As New DirectoryInfo(ocvb.parms.OpenCVfullPath + "/../../../modules/imgproc/doc/pics/colormaps")
        Dim colorMap As Int32
        For i = 0 To palette.radio.check.Count - 1
            If palette.radio.check(i).Checked Then
                colormap = Choose(i + 1, cv.ColormapTypes.Autumn, cv.ColormapTypes.Bone, cv.ColormapTypes.Cool, cv.ColormapTypes.Hot,
                                         cv.ColormapTypes.Hsv, cv.ColormapTypes.Jet, cv.ColormapTypes.Ocean, cv.ColormapTypes.Pink,
                                         cv.ColormapTypes.Rainbow, cv.ColormapTypes.Spring, cv.ColormapTypes.Summer, cv.ColormapTypes.Winter, 12, 13, 14, 15, 16, 17, 18, 19, 20)
                ocvb.label1 = mapNames(i)
                ocvb.label2 = mapNames(i)
            End If
        Next
        Dim mapFile = New FileInfo(cMapDir.FullName + "/colorscale_" + ocvb.label1 + ".jpg")
        If mapFile.Exists Then
            Dim cmap = cv.Cv2.ImRead(mapFile.FullName)
            ocvb.result2 = cmap.Resize(ocvb.color.Size())
        Else
            If colorMap = 19 Then ocvb.result2 = palette.randomMap.resize(ocvb.color.Size()) Else ocvb.result2.SetTo(0)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        palette.Dispose()
    End Sub
End Class