Imports cv = OpenCvSharp
Public Class DCT_RGB : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Remove Frequencies < x", 0, 100, 1)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.desc = "Apply OpenCV's Discrete Cosine Transform to an RGB image and use slider to remove the highest frequencies."
        ocvb.label1 = "Reconstituted RGB image"
        ocvb.label2 = "Difference from original"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim srcPlanes() As cv.Mat = Nothing
        cv.Cv2.Split(ocvb.color, srcPlanes)

        Dim freqPlanes(2) As cv.Mat
        For i = 0 To 2
            Dim src32f As New cv.Mat
            srcPlanes(i).ConvertTo(src32f, cv.MatType.CV_32FC3, 1 / 255)
            freqPlanes(i) = New cv.Mat
            cv.Cv2.Dct(src32f, freqPlanes(i), cv.DctFlags.None)

            Dim roi As New cv.Rect(0, 0, sliders.TrackBar1.Value, src32f.Height)
            If roi.Width > 0 Then freqPlanes(i)(roi).SetTo(0)

            cv.Cv2.Dct(freqPlanes(i), src32f, cv.DctFlags.Inverse)
            src32f.ConvertTo(srcPlanes(i), cv.MatType.CV_8UC1, 255)
        Next
        ocvb.label1 = "Highest " + CStr(sliders.TrackBar1.Value) + " frequencies removed"

        cv.Cv2.Merge(srcPlanes, ocvb.result1)

        cv.Cv2.Subtract(ocvb.color, ocvb.result1, ocvb.result2)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class DCT_RGBDepth : Implements IDisposable
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Remove Frequencies < x", 0, 100, 1)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.label2 = "Subtract DCT inverse from Grayscale depth"
        ocvb.desc = "Find featureless surfaces in the depth data - expected to be useful only on the Kinect for Azure camera."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim frequencies As New cv.Mat
        Dim src32f As New cv.Mat
        gray.ConvertTo(src32f, cv.MatType.CV_32F, 1 / 255)
        cv.Cv2.Dct(src32f, frequencies, cv.DctFlags.None)

        Dim roi As New cv.Rect(0, 0, sliders.TrackBar1.Value, src32f.Height)
        If roi.Width > 0 Then frequencies(roi).SetTo(0)
        ocvb.label1 = "Highest " + CStr(sliders.TrackBar1.Value) + " frequencies removed"

        cv.Cv2.Dct(frequencies, src32f, cv.DctFlags.Inverse)
        src32f.ConvertTo(ocvb.result1, cv.MatType.CV_8UC1, 255)

        cv.Cv2.Subtract(gray, ocvb.result1, ocvb.result2)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



Public Class DCT_Grayscale : Implements IDisposable
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Remove Frequencies < x", 0, 100, 1)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.desc = "Apply OpenCV's Discrete Cosine Transform to a grayscale image and use slider to remove the highest frequencies."
        ocvb.label2 = "Difference from original"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim frequencies As New cv.Mat
        Dim src32f As New cv.Mat
        gray.ConvertTo(src32f, cv.MatType.CV_32F, 1 / 255)
        cv.Cv2.Dct(src32f, frequencies, cv.DctFlags.None)

        Dim roi As New cv.Rect(0, 0, sliders.TrackBar1.Value, src32f.Height)
        If roi.Width > 0 Then frequencies(roi).SetTo(0)
        ocvb.label1 = "Highest " + CStr(sliders.TrackBar1.Value) + " frequencies removed"

        cv.Cv2.Dct(frequencies, src32f, cv.DctFlags.Inverse)
        src32f.ConvertTo(ocvb.result1, cv.MatType.CV_8UC1, 255)

        cv.Cv2.Subtract(gray, ocvb.result1, ocvb.result2)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class DCT_FeatureLess_MT : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public dct As DCT_Grayscale
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Run Length Minimum", 1, 100, 15)
        If ocvb.parms.ShowOptions Then sliders.Show()

        dct = New DCT_Grayscale(ocvb)
        dct.sliders.TrackBar1.Value = 1
        ocvb.desc = "Find surfaces that lack any texture.  Remove just the highest frequency from the DCT to get horizontal lines through the image."
        ocvb.label2 = "FeatureLess RGB regions"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dct.Run(ocvb)
        ocvb.result1.SetTo(0)
        Dim runLenMin = sliders.TrackBar1.Value

        ' Result2 contain the RGB image with highest frequency removed.
        Parallel.For(0, ocvb.result2.Rows - 1,
        Sub(i)
            Dim runLen As Int32 = 0
            Dim runStart As Int32 = 0
            For j = 1 To ocvb.result2.Cols - 1
                If ocvb.result2.At(Of Byte)(i, j) = ocvb.result2.At(Of Byte)(i, j - 1) Then
                    runLen += 1
                Else
                    If runLen > runLenMin Then
                        Dim roi = New cv.Rect(runStart, i, runLen, 1)
                        ocvb.result1(roi).SetTo(255)
                    End If
                    runStart = j
                    runLen = 1
                End If
            Next
        End Sub)
        ocvb.result2.SetTo(0)
        ocvb.color.CopyTo(ocvb.result2, ocvb.result1)
        ocvb.label1 = "Mask of DCT with highest frequency removed"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        dct.Dispose()
    End Sub
End Class





Public Class DCT_Surfaces_debug : Implements IDisposable
    Dim Mats As Mat_4to1
    Dim grid As Thread_Grid
    Dim dct As DCT_FeatureLess_MT
    Dim flow As Font_FlowText
    Public Sub New(ocvb As AlgorithmData)
        flow = New Font_FlowText(ocvb)
        flow.externalUse = True
        flow.result1or2 = RESULT1

        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 100
        grid.sliders.TrackBar2.Value = 150
        dct = New DCT_FeatureLess_MT(ocvb)
        dct.dct.sliders.TrackBar1.Value = 1
        Mats = New Mat_4to1(ocvb)
        Mats.externalUse = True

        ocvb.desc = "Find plane equation for a featureless surface - debugging one region for now."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        mats.mat(0) = ocvb.result1.Clone()

        dct.Run(ocvb)
        mats.mat(1) = ocvb.result1.CvtColor(cv.ColorConversionCodes.GRAY2BGR).Clone()
        mats.mat(2) = ocvb.result2.Clone()

        Dim mask = ocvb.result1.Clone() ' result1 contains the DCT mask of featureless surfaces.
        Dim notMask As New cv.Mat
        cv.Cv2.BitwiseNot(mask, notMask)
        ocvb.depth16.SetTo(0, notMask) ' remove non-featureless surface depth data.

        ' find the most featureless roi
        Dim maxIndex As Int32
        Dim roiCounts(grid.roiList.Count - 1)
        For i = 0 To grid.roiList.Count - 1
            roiCounts(i) = mask(grid.roiList(i)).CountNonZero()
            If roiCounts(i) > roiCounts(maxIndex) Then maxIndex = i
        Next

        mats.mat(3) = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC3, 0)
        ocvb.color(grid.roiList(maxIndex)).CopyTo(mats.mat(3)(grid.roiList(maxIndex)), mask(grid.roiList(maxIndex)))
        Mats.Run(ocvb)

        Dim world As New cv.Mat(ocvb.color.Size(), cv.MatType.CV_32FC3, 0)
        Dim roi = grid.roiList(maxIndex) ' this is where the debug comes in.  We just want to look at one region which is hopefully on a single plane.
        If roi.X = grid.roiList(maxIndex).X And roi.Y = grid.roiList(maxIndex).Y Then
            If roiCounts(maxIndex) > roi.Width * roi.Height / 4 Then
                Dim worldPoints As New List(Of cv.Point3f)
                Dim minDepth As Int32 = 100000, maxDepth As Int32
                For j = 0 To roi.Height - 1
                    For i = 0 To roi.Width - 1
                        Dim nextD = ocvb.depth16(roi).At(Of Short)(j, i)
                        If nextD <> 0 Then
                            If minDepth > nextD Then minDepth = nextD
                            If maxDepth < nextD Then maxDepth = nextD
                            Dim wpt = New cv.Point3f(roi.X + i, roi.Y + j, nextD)
                            worldPoints.Add(getWorldCoordinates(ocvb, wpt))
                        End If
                    Next
                Next
                Dim plane = computePlaneEquation(worldPoints)
                If Single.IsNaN(plane.Item0) = False Then
                    flow.msgs.Add("a=" + Format(plane.Item0, "#0.00") + " b=" + Format(plane.Item1, "#0.00") + " c=" + Format(Math.Abs(plane.Item2), "#0.00") +
                              vbTab + "depth=" + Format(-plane.Item3 / 1000, "#0.00") + "m" + vbTab + "roi.x = " + Format(roi.X, "000") + vbTab + " roi.y = " +
                              Format(roi.Y, "000") + vbTab + "MinDepth = " + Format(minDepth / 1000, "#0.00") + "m" + vbTab + " MaxDepth = " + Format(maxDepth / 1000, "#0.00") + "m")
                End If
            End If
            End If
        flow.Run(ocvb)
        ocvb.label1 = "Largest flat surface segment stats"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        Mats.Dispose()
        dct.Dispose()
        grid.Dispose()
        flow.Dispose()
    End Sub
End Class




Public Class DCT_CCompenents : Implements IDisposable
    Dim dct As DCT_FeatureLess_MT
    Dim cc As CComp_Basics
    Public Sub New(ocvb As AlgorithmData)
        dct = New DCT_FeatureLess_MT(ocvb)
        cc = New CComp_Basics(ocvb)
        cc.externalUse = True

        ocvb.desc = "Find surfaces that lack any texture with DCT (less highest frequency) and use connected components to isolate those surfaces."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dct.Run(ocvb)

        cc.srcGray = ocvb.result1.Clone()
        cc.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        dct.Dispose()
        cc.Dispose()
    End Sub
End Class






Public Class DCT_Rows : Implements IDisposable
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Remove Frequencies < x", 0, 100, 1)
        sliders.setupTrackBar2(ocvb, "Threshold after removal", 1, 255, 30)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.desc = "Find featureless surfaces in the depth data - expected to be useful only on the Kinect for Azure camera."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim frequencies As New cv.Mat
        Dim src32f As New cv.Mat
        gray.ConvertTo(src32f, cv.MatType.CV_32F, 1 / 255)
        cv.Cv2.Dct(src32f, frequencies, cv.DctFlags.Rows)

        Dim roi As New cv.Rect(0, 0, sliders.TrackBar1.Value, src32f.Height)
        If roi.Width > 0 Then frequencies(roi).SetTo(0)
        ocvb.label1 = "Highest " + CStr(sliders.TrackBar1.Value) + " frequencies removed"

        cv.Cv2.Dct(frequencies, src32f, cv.DctFlags.Inverse + cv.DctFlags.Rows)
        src32f.ConvertTo(ocvb.result1, cv.MatType.CV_8UC1, 255)
        ocvb.result2 = ocvb.result1.Threshold(sliders.TrackBar2.Value, 255, cv.ThresholdTypes.Binary)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class