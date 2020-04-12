Imports cv = OpenCvSharp
Imports System.Collections.Concurrent
Public Class kMeans_Clusters : Implements IDisposable
    Dim Mats As Mat_4to1
    Dim km As kMeans_Basics
    Public Sub New(ocvb As AlgorithmData)
        Mats = New Mat_4to1(ocvb)
        Mats.externalUse = True

        km = New kMeans_Basics(ocvb)

        ocvb.label1 = "kmeans - k=10"
        ocvb.label2 = "kmeans - k=2,4,6,8"
        ocvb.desc = "Show clustering with various settings for cluster count.  Draw to select region of interest."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static saveRect = ocvb.drawRect
        ocvb.drawRect = saveRect
        For i = 0 To 3
            km.sliders.TrackBar1.Value = (i + 1) * 2
            km.Run(ocvb)
            Mats.mat(i) = ocvb.result1.Resize(New cv.Size(ocvb.result1.Cols / 2, ocvb.result1.Rows / 2))
        Next
        Mats.Run(ocvb)
        km.sliders.TrackBar1.Value = 10 ' this will show kmeans with 10 clusters in Result1.
        km.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        km.Dispose()
        Mats.Dispose()
    End Sub
End Class



Public Class kMeans_Basics : Implements IDisposable
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "kMeans k", 2, 32, 4)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Cluster the rgb image using kMeans."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim small = ocvb.color.Resize(New cv.Size(ocvb.color.Width / 4, ocvb.color.Height / 4))
        Dim rectMat = small.Clone
        Dim columnVector As New cv.Mat
        columnVector = rectMat.Reshape(ocvb.color.Channels, small.Height * small.Width)
        Dim rgb32f As New cv.Mat
        columnVector.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
        Dim clusterCount = sliders.TrackBar1.Value
        Dim labels = New cv.Mat()
        Dim colors As New cv.Mat

        cv.Cv2.Kmeans(rgb32f, clusterCount, labels, term, 1, cv.KMeansFlags.PpCenters, colors)
        labels.Reshape(1, small.Height).ConvertTo(labels, cv.MatType.CV_8U)
        labels = labels.Resize(New cv.Size(ocvb.color.Width, ocvb.color.Height))

        For i = 0 To clusterCount - 1
            Dim mask = labels.InRange(i, i)
            Dim mean = ocvb.RGBDepth.Mean(mask)
            ocvb.result1.SetTo(mean, mask)
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class kMeans_RGBFast : Implements IDisposable
    Public sliders As New OptionsSliders
    Public clusterColors() As cv.Vec3b
    Public resizeFactor = 2
    Public clusterCount = 6
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "kMeans k", 2, 32, 4)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Cluster a small rgb image using kMeans.  Specify clusterCount value."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim small8uC3 = ocvb.color.Resize(New cv.Size(CInt(ocvb.color.Rows / resizeFactor), CInt(ocvb.color.Cols / resizeFactor)))
        Dim columnVector As New cv.Mat
        columnVector = small8uC3.Reshape(small8uC3.Channels, small8uC3.Rows * small8uC3.Cols)
        Dim columnVectorRGB32f As New cv.Mat
        columnVector.ConvertTo(columnVectorRGB32f, cv.MatType.CV_32FC3)
        Dim labels = New cv.Mat()
        Dim centers As New cv.Mat
        Dim clusterCount = sliders.TrackBar1.Value

        cv.Cv2.Kmeans(columnVectorRGB32f, clusterCount, labels, term, 3, cv.KMeansFlags.PpCenters, centers)
        Dim labelImage = labels.Reshape(1, small8uC3.Rows)

        ReDim clusterColors(clusterCount - 1)
        For i = 0 To clusterCount - 1
            Dim c = centers.Get(Of cv.Vec3f)(i)
            clusterColors(i) = New cv.Vec3b(CInt(c(0)), CInt(c(1)), CInt(c(2)))
        Next
        For y = 0 To labelImage.Rows - 1
            For x = 0 To labelImage.Cols - 1
                Dim cIndex = labelImage.Get(Of Byte)(y, x)
                small8uC3.Set(Of cv.Vec3b)(y, x, clusterColors(cIndex))
            Next
        Next
        ocvb.result1 = small8uC3.Resize(ocvb.result1.Size())
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class kMeans_RGB_Plus_XYDepth : Implements IDisposable
    Dim km As kMeans_Basics
    Private clusterColors() As cv.Vec6i
    Private sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "kMeans k", 2, 32, 4)
        If ocvb.parms.ShowOptions Then sliders.Show()
        km = New kMeans_Basics(ocvb)
        ocvb.label1 = "kmeans - RGB, XY, and Depth Raw"
        ocvb.desc = "Cluster with kMeans RGB, x, y, and depth."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        km.Run(ocvb) ' cluster the rgb image - output is in ocvb.result2
        Dim rgb32f As New cv.Mat
        ocvb.result1.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
        Dim xyDepth32f As New cv.Mat(rgb32f.Size(), cv.MatType.CV_32FC3, 0)
        Dim depth32f = getDepth32f(ocvb)
        For y = 0 To xyDepth32f.Rows - 1
            For x = 0 To xyDepth32f.Cols - 1
                Dim nextVal = depth32f.At(Of Single)(y, x)
                If nextVal Then xyDepth32f.Set(Of cv.Vec3f)(y, x, New cv.Vec3f(x, y, nextVal))
            Next
        Next
        Dim src() = New cv.Mat() {rgb32f, xyDepth32f}
        Dim all32f = New cv.Mat(rgb32f.Size(), cv.MatType.CV_32FC(6)) ' output will have 6 channels!
        Dim dst() = New cv.Mat() {all32f}
        Dim from_to() = New Int32() {0, 0, 0, 1, 0, 2, 3, 3, 4, 4, 5, 5}
        cv.Cv2.MixChannels(src, dst, from_to)

        Dim columnVector As New cv.Mat
        columnVector = all32f.Reshape(all32f.Channels, all32f.Rows * all32f.Cols)
        Dim labels = New cv.Mat()
        Dim centers As New cv.Mat
        Dim clusterCount = sliders.TrackBar1.Value

        cv.Cv2.Kmeans(columnVector, clusterCount, labels, term, 3, cv.KMeansFlags.PpCenters, centers)
        Dim labelImage = labels.Reshape(1, all32f.Rows)

        ReDim clusterColors(clusterCount - 1)
        For i = 0 To clusterCount - 1
            Dim c = centers.Get(Of cv.Vec6f)(i)
            clusterColors(i) = New cv.Vec6i(CInt(c(0)), CInt(c(1)), CInt(c(2)), CInt(c(3)), CInt(c(4)), CInt(c(5)))
        Next
        For y = 0 To labelImage.Rows - 1
            For x = 0 To labelImage.Cols - 1
                Dim cIndex = labelImage.Get(Of Byte)(y, x)
                With clusterColors(cIndex)
                    ocvb.result1.Set(Of cv.Vec3b)(y, x, New cv.Vec3b(10 * .Item0 Mod 255, 10 * .Item1 Mod 255, 10 * .Item2 Mod 255))
                End With
            Next
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        km.Dispose()
    End Sub
End Class



Public Class kMeans_RGB1_MT : Implements IDisposable
    Public clusterColors() As cv.Vec3b
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "kMeans k", 2, 32, 4)
        sliders.setupTrackBar2(ocvb, "Thread Count", 1, 32, 4)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.label1 = "kmeans - raw labels"
        ocvb.label2 = "kmeans - clusterColors"
        ocvb.desc = "Cluster the segmented rgb image using kMeans with multiple threads.  Select the desired number of clusters/threads."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim threadCount = sliders.TrackBar2.Value
        Select Case threadCount
            Case 1
                threadCount = 1
            Case 2 To 3
                threadCount = 2
            Case 4 To 7
                threadCount = 4
            Case 8 To 15
                threadCount = 8
            Case 16 To 31
                threadCount = 16
            Case 32
                threadCount = 32
            Case Else
                Exit Sub
        End Select
        Dim w = ocvb.color.Width, h = ocvb.color.Height / threadCount
        Dim taskArray(threadCount - 1) As Task
        Dim clusterCount = sliders.TrackBar1.Value
        ReDim clusterColors(clusterCount - 1)
        Dim allLabels As New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC1)
        For i = 0 To threadCount - 1
            Dim section = i
            taskArray(i) = Task.Factory.StartNew(
                Sub()
                    Dim roi = New cv.Rect(0, h * section, ocvb.color.Width, h)
                    Dim src As New cv.Mat(New cv.Size(roi.Width, roi.Height), cv.MatType.CV_8UC3)
                    ocvb.color(roi).CopyTo(src)
                    Dim columnVector As New cv.Mat
                    columnVector = src.Reshape(ocvb.color.Channels, roi.Height * roi.Width)
                    Dim rgb32f As New cv.Mat
                    columnVector.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
                    Dim labels = New cv.Mat()
                    Dim centers As New cv.Mat

                    cv.Cv2.Kmeans(rgb32f, clusterCount, labels, term, 3, cv.KMeansFlags.PpCenters, centers)
                    Dim labelImage = labels.Reshape(1, roi.Height)
                    Dim labels8uC1 As New cv.Mat
                    labelImage.ConvertTo(labels8uC1, cv.MatType.CV_8UC1)
                    labels8uC1.CopyTo(allLabels(roi))

                    If section = 0 Then
                        For j = 0 To clusterCount - 1
                            Dim c = centers.Get(Of cv.Vec3f)(j)
                            clusterColors(j) = New cv.Vec3b(CInt(c(0)), CInt(c(1)), CInt(c(2)))
                        Next
                    End If
                End Sub)
        Next
        Task.WaitAll(taskArray)

        If ocvb.frameCount Mod 30 = 0 Then
            ' only the clusterColors for task 0 are used to color all the labeled data
            For y = 0 To allLabels.Rows - 1
                For x = 0 To allLabels.Cols - 1
                    Dim cIndex = allLabels.Get(Of Byte)(y, x)
                    If cIndex < clusterColors.Count Then
                        ocvb.result2.Set(Of cv.Vec3b)(y, x, clusterColors(cIndex))
                    End If
                Next
            Next

            Dim factor = CInt(255.0 / clusterCount)
            allLabels = factor * allLabels
            cv.Cv2.CvtColor(allLabels, ocvb.result1, cv.ColorConversionCodes.GRAY2BGR)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



Public Class kMeans_RGB2_MT : Implements IDisposable
    Public clusterColors() As cv.Vec3b
    Public sliders As New OptionsSliders
    Public radio As New OptionsRadioButtons
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "kMeans k", 2, 32, 10)
        If ocvb.parms.ShowOptions Then sliders.Show()
        radio.Setup(ocvb, 6)
        For i = 0 To radio.check.Count - 1
            radio.check(i).Text = CStr(2 ^ i) + " threads"
        Next
        radio.check(0).Text = "1 thread"
        radio.check(5).Checked = True
        If ocvb.parms.ShowOptions Then radio.Show()
        ocvb.label1 = "kmeans - raw labels"
        ocvb.desc = "Cluster the segmented rgb image using kMeans with multiple threads.  Select the desired number of clusters/threads."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim threadData As New cv.Vec3i
        Dim w = ocvb.color.Width, h = ocvb.color.Height
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                threadData = Choose(i + 1, New cv.Vec3i(1, w, h), New cv.Vec3i(2, w / 2, h), New cv.Vec3i(4, w / 2, h / 2), New cv.Vec3i(8, w / 4, h / 2),
                                    New cv.Vec3i(16, w / 4, h / 4), New cv.Vec3i(32, w / 8, h / 4))
                Exit For
            End If
        Next
        w = threadData(1)
        h = threadData(2)
        Dim taskArray(threadData(0) - 1) As Task
        Dim clusterCount = sliders.TrackBar1.Value
        ReDim clusterColors(clusterCount - 1)
        Dim allLabels As New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC1)
        For i = 0 To threadData(0) - 1
            Dim section = i
            taskArray(i) = Task.Factory.StartNew(
                Sub()
                    Dim xfactor = CInt(ocvb.color.Width / w)
                    Dim yfactor = Math.Max(CInt(ocvb.color.Height / h), CInt(ocvb.color.Width / w))
                    Dim roi = New cv.Rect((section Mod xfactor) * w, h * Math.Floor(section / yfactor), w, h)
                    Dim src As New cv.Mat(New cv.Size(roi.Width, roi.Height), cv.MatType.CV_8UC3)
                    ocvb.color(roi).CopyTo(src)
                    Dim columnVector As New cv.Mat
                    columnVector = src.Reshape(ocvb.color.Channels, roi.Height * roi.Width)
                    Dim rgb32f As New cv.Mat
                    columnVector.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
                    Dim labels = New cv.Mat()
                    Dim centers As New cv.Mat

                    cv.Cv2.Kmeans(rgb32f, clusterCount, labels, term, 3, cv.KMeansFlags.PpCenters, centers)
                    Dim labelImage = labels.Reshape(1, roi.Height)
                    Dim labels8uC1 As New cv.Mat
                    labelImage.ConvertTo(labels8uC1, cv.MatType.CV_8UC1)
                    labels8uC1.CopyTo(allLabels(roi))

                    If section = 0 Then
                        For j = 0 To clusterCount - 1
                            Dim c = centers.Get(Of cv.Vec3f)(j)
                            clusterColors(j) = New cv.Vec3b(CInt(c(0)), CInt(c(1)), CInt(c(2)))
                        Next
                    End If
                End Sub)
        Next
        Task.WaitAll(taskArray)

        Dim factor = CInt(255.0 / clusterCount)
        allLabels = factor * allLabels
        cv.Cv2.CvtColor(allLabels, ocvb.result1, cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        radio.Dispose()
    End Sub
End Class



Public Class kMeans_RGB3_MT : Implements IDisposable
    Public sliders As New OptionsSliders
    Public radio As New OptionsRadioButtons
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "kMeans k", 2, 32, 14)
        If ocvb.parms.ShowOptions Then sliders.Show()
        radio.Setup(ocvb, 6)
        For i = 0 To radio.check.Count - 1
            radio.check(i).Text = CStr(2 ^ i) + " threads"
        Next
        radio.check(0).Text = "1 thread"
        radio.check(5).Checked = True
        If ocvb.parms.ShowOptions Then radio.Show()
        ocvb.label1 = "kmeans - raw labels"
        ocvb.label2 = "Synchronized colors from raw labels"
        ocvb.desc = "Cluster the segmented rgb image using kMeans with multiple threads.  Select the desired number of clusters/threads."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim threadData As New cv.Vec3i
        Dim w = ocvb.color.Width, h = ocvb.color.Height
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                threadData = Choose(i + 1, New cv.Vec3i(1, w, h), New cv.Vec3i(2, w / 2, h), New cv.Vec3i(4, w / 2, h / 2), New cv.Vec3i(8, w / 4, h / 2),
                                    New cv.Vec3i(16, w / 4, h / 4), New cv.Vec3i(32, w / 8, h / 4))
                Exit For
            End If
        Next
        Dim threadCount As Int32 = threadData(0)
        w = threadData(1)
        h = threadData(2)
        Dim taskArray(threadCount - 1) As Task
        Dim clusterCount = sliders.TrackBar1.Value
        Dim clusterColors = New cv.Mat(New cv.Size(3, clusterCount * threadCount), cv.MatType.CV_32FC1)
        Dim allLabels As New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC1)
        Dim xfactor = CInt(ocvb.color.Width / w)
        Dim yfactor = Math.Max(CInt(ocvb.color.Height / h), CInt(ocvb.color.Width / w))
        For i = 0 To threadCount - 1
            Dim section = i
            taskArray(i) = Task.Factory.StartNew(
                Sub()
                    Dim roi = New cv.Rect((section Mod xfactor) * w, h * Math.Floor(section / yfactor), w, h)
                    Dim src As New cv.Mat(New cv.Size(roi.Width, roi.Height), cv.MatType.CV_8UC3)
                    ocvb.color(roi).CopyTo(src)
                    Dim columnVector As New cv.Mat
                    columnVector = src.Reshape(ocvb.color.Channels, roi.Height * roi.Width)
                    Dim rgb32f As New cv.Mat
                    columnVector.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
                    Dim labels = New cv.Mat()
                    Dim centers As New cv.Mat

                    cv.Cv2.Kmeans(rgb32f, clusterCount, labels, term, 3, cv.KMeansFlags.PpCenters, centers)
                    Dim labelImage = labels.Reshape(1, roi.Height)
                    Dim labels8uC1 As New cv.Mat
                    labelImage.ConvertTo(labels8uC1, cv.MatType.CV_8UC1)
                    labels8uC1.CopyTo(allLabels(roi))

                    For j = 0 To clusterCount - 1
                        clusterColors.Set(Of cv.Vec3f)(j + section * clusterCount, centers.Get(Of cv.Vec3f)(j))
                    Next
                End Sub)
        Next
        Task.WaitAll(taskArray)

        Dim finalColorLabels As New cv.Mat
        Dim finalColorCenters As New cv.Mat
        cv.Cv2.Kmeans(clusterColors, clusterCount, finalColorLabels, term, 3, cv.KMeansFlags.PpCenters, finalColorCenters)
        For i = 0 To threadCount - 1
            Dim section = i
            taskArray(i) = Task.Factory.StartNew(
                    Sub()
                        Dim roi = New cv.Rect((section Mod xfactor) * w, h * Math.Floor(section / yfactor), w, h)
                        Dim val As Int32, colorLabel As Int32, finalColor As cv.Vec3f
                        Dim lSrc As New cv.Mat
                        allLabels(roi).CopyTo(lSrc)
                        Dim lDst As New cv.Mat(lSrc.Size(), cv.MatType.CV_8UC3)
                        For y = 0 To roi.Height - 1
                            For x = 0 To roi.Width - 1
                                val = lSrc.Get(Of Byte)(y, x)
                                colorLabel = finalColorLabels.Get(Of Int32)(section * clusterCount + val)
                                finalColor = finalColorCenters.Get(Of cv.Vec3f)(colorLabel)
                                lDst.Set(y, x, New cv.Vec3b(finalColor(0), finalColor(1), finalColor(2)))
                            Next
                        Next
                        lDst.CopyTo(ocvb.result2(roi))
                    End Sub)
        Next
        Task.WaitAll(taskArray)

        Dim factor = CInt(255.0 / clusterCount)
        allLabels = factor * allLabels
        cv.Cv2.CvtColor(allLabels, ocvb.result1, cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        radio.Dispose()
    End Sub
End Class



Public Class kMeans_ReducedRGB : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Reduction factor", 2, 64, 64)
        sliders.setupTrackBar2(ocvb, "kmeans k", 2, 64, 4)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.label2 = "Reduced color image."
        ocvb.desc = "Reduce each pixel by the reduction factor and then run kmeans."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result2 = ocvb.color / sliders.TrackBar1.Value
        ocvb.result2 *= sliders.TrackBar1.Value

        Dim src = ocvb.result2
        Dim k = sliders.TrackBar2.Value
        Dim n = src.Rows * src.Cols
        Dim data = src.Reshape(1, n)
        data.ConvertTo(data, cv.MatType.CV_32F)

        Dim labels As New cv.Mat
        Dim colors As New cv.Mat
        cv.Cv2.Kmeans(data, k, labels, term, 1, cv.KMeansFlags.PpCenters, colors)

        For i = 0 To n - 1
            data.Set(Of cv.Vec3f)(i, 0, colors.Get(Of cv.Vec3f)(labels.Get(Of Int32)(i)))
        Next
        data.Reshape(3, src.Rows).ConvertTo(ocvb.result1, cv.MatType.CV_8U)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class kMeans_XYDepth : Implements IDisposable
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "kMeans k", 2, 32, 4)
        If ocvb.parms.ShowOptions Then sliders.Show()
        Dim w = ocvb.color.Width / 4
        Dim h = ocvb.color.Height / 4
        ocvb.drawRect = New cv.Rect(w, h, w * 2, h * 2)
        ocvb.desc = "Cluster with x, y, and depth using kMeans.  Draw on the image to select a region."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim roi = ocvb.drawRect
        Dim depth32f = getDepth32f(ocvb)
        Dim xyDepth32f As New cv.Mat(depth32f(roi).Size(), cv.MatType.CV_32FC3, 0)
        For y = 0 To xyDepth32f.Rows - 1
            For x = 0 To xyDepth32f.Cols - 1
                Dim nextVal = depth32f(roi).At(Of Single)(y, x)
                If nextVal Then xyDepth32f.Set(Of cv.Vec3f)(y, x, New cv.Vec3f(x, y, nextVal))
            Next
        Next
        Dim columnVector As New cv.Mat
        columnVector = xyDepth32f.Reshape(xyDepth32f.Channels, xyDepth32f.Rows * xyDepth32f.Cols)
        Dim labels = New cv.Mat()
        Dim colors As New cv.Mat
        cv.Cv2.Kmeans(columnVector, sliders.TrackBar1.Value, labels, term, 3, cv.KMeansFlags.PpCenters, colors)
        For i = 0 To columnVector.Rows - 1
            columnVector.Set(Of cv.Vec3f)(i, 0, colors.Get(Of cv.Vec3f)(labels.Get(Of Int32)(i)))
        Next
        ocvb.RGBDepth.CopyTo(ocvb.result1)
        columnVector.Reshape(3, ocvb.result1(roi).Height).ConvertTo(ocvb.result1(roi), cv.MatType.CV_8U)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class kMeans_Depth_FG_BG : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Separate foreground and background using Kmeans (with k=2) using the depth value of center point."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim columnVector As New cv.Mat
        Dim depth32f = getDepth32f(ocvb)
        columnVector = depth32f.Reshape(1, depth32f.Rows * depth32f.Cols)
        columnVector.ConvertTo(columnVector, cv.MatType.CV_32FC1)
        Dim labels = New cv.Mat()
        Dim depthCenters As New cv.Mat
        cv.Cv2.Kmeans(columnVector, 2, labels, term, 3, cv.KMeansFlags.PpCenters, depthCenters)
        labels = labels.Reshape(1, depth32f.Rows)

        Dim foregroundLabel = 0
        If depthCenters.At(Of Single)(0, 0) > depthCenters.At(Of Single)(1, 0) Then foregroundLabel = 1

        ' if one of the centers is way out there, leave the mask alone.  KMeans clustered an unreasonably small cluster.
        ' If depthCenters.At(Of Single)(0, 0) > 20000 Or depthCenters.At(Of Single)(1, 0) > 20000 Then Exit Sub

        Dim mask = labels.InRange(foregroundLabel, foregroundLabel)
        Dim shadowMask = depth32f.Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()
        mask.SetTo(0, shadowMask)
        ocvb.result1 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class kMeans_LAB : Implements IDisposable
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "kMeans k", 2, 32, 4)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.label1 = "kMeans_LAB - draw to select region"
        Dim w = ocvb.color.Width / 4
        Dim h = ocvb.color.Height / 4
        ocvb.drawRect = New cv.Rect(w, h, w * 2, h * 2)
        ocvb.desc = "Cluster the LAB image using kMeans.  Is it better?  Optionally draw on the image and select k."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim roi = ocvb.drawRect
        Dim labMat = ocvb.color(roi).CvtColor(cv.ColorConversionCodes.RGB2Lab)
        Dim columnVector As New cv.Mat
        columnVector = labMat.Reshape(ocvb.color.Channels, roi.Height * roi.Width)
        Dim lab32f As New cv.Mat
        columnVector.ConvertTo(lab32f, cv.MatType.CV_32FC3)
        Dim clusterCount = sliders.TrackBar1.Value
        Dim labels = New cv.Mat()
        Dim colors As New cv.Mat

        cv.Cv2.Kmeans(lab32f, clusterCount, labels, term, 1, cv.KMeansFlags.PpCenters, colors)

        For i = 0 To columnVector.Rows - 1
            lab32f.Set(Of cv.Vec3f)(i, 0, colors.Get(Of cv.Vec3f)(labels.Get(Of Int32)(i)))
        Next
        ocvb.color.CopyTo(ocvb.result1)
        lab32f.Reshape(3, roi.Height).ConvertTo(ocvb.result1(roi), cv.MatType.CV_8UC3)
        ocvb.result1(roi) = ocvb.result1(roi).CvtColor(cv.ColorConversionCodes.Lab2RGB)
        ocvb.result1.Rectangle(ocvb.drawRect, cv.Scalar.White, 1)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



Public Class kMeans_RGB4_MT : Implements IDisposable
    Public sliders As New OptionsSliders
    Dim grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "kMeans k", 2, 32, 10)
        If ocvb.parms.ShowOptions Then sliders.Show()

        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 48
        grid.externalUse = True ' we don't need any results.
        ocvb.label1 = "kmeans - raw labels"
        ocvb.label2 = "Synchronized colors from raw labels"
        ocvb.desc = "Cluster a grid of segments individual and combine results.  Select the desired number of clusters/threads."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        ocvb.color.CopyTo(ocvb.result1)
        ocvb.result1.SetTo(cv.Scalar.White, grid.gridMask)

        Dim clusterCount = sliders.TrackBar1.Value
        Dim allLabels As New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC1)
        Dim clusterColors As New cv.Mat(New cv.Size(3, clusterCount * (grid.roiList.Count - 1)), cv.MatType.CV_32FC1)
        Dim roiList As New List(Of cv.Rect)
        For i = 0 To grid.roiList.Count - 1
            ' if the number of pixels is less than the clusterCount kmeans will fail!  (Segments near boundaries can be tiny.)
            If grid.roiList(i).Width * grid.roiList(i).Height >= clusterCount Then
                roiList.Add(grid.roiList(i))
            End If
        Next

        Parallel.For(0, roiList.Count - 1,
         Sub(i)
             Dim roi = roiList(i)
             Dim src As New cv.Mat(roi.Width, roi.Height, cv.MatType.CV_8UC3)
             ocvb.color(roi).CopyTo(src)
             Dim columnVector As New cv.Mat
             columnVector = src.Reshape(ocvb.color.Channels, roi.Height * roi.Width)
             Dim rgb32f As New cv.Mat
             columnVector.ConvertTo(rgb32f, cv.MatType.CV_32FC3)

             Dim labels = New cv.Mat()
             Dim centers As New cv.Mat
             cv.Cv2.Kmeans(rgb32f, clusterCount, labels, term, 3, cv.KMeansFlags.PpCenters, centers)

             Dim labelImage = labels.Reshape(1, roi.Height)
             Dim labels8uC1 As New cv.Mat
             labelImage.ConvertTo(labels8uC1, cv.MatType.CV_8UC1)
             labels8uC1.CopyTo(allLabels(roi))
             For j = 0 To clusterCount - 1
                 clusterColors.Set(Of cv.Vec3f)(j + i * clusterCount, centers.Get(Of cv.Vec3f)(j))
             Next
         End Sub)

        Dim finalColorLabels As New cv.Mat
        Dim finalColorCenters As New cv.Mat

        cv.Cv2.Kmeans(clusterColors, clusterCount, finalColorLabels, term, 3, cv.KMeansFlags.PpCenters, finalColorCenters)
        If finalColorCenters.Rows = 0 Then Exit Sub ' failure.  Problem moving slider?
        Parallel.For(0, roiList.Count - 1,
                Sub(i)
                    Dim roi = roiList(i)
                    Dim val As Int32, colorLabel As Int32, finalColor As cv.Vec3f
                    Dim lSrc As New cv.Mat
                    allLabels(roi).CopyTo(lSrc)
                    Dim lDst As New cv.Mat(lSrc.Size(), cv.MatType.CV_8UC3)
                    For y = 0 To roi.Height - 1
                        For x = 0 To roi.Width - 1
                            val = lSrc.Get(Of Byte)(y, x)
                            colorLabel = finalColorLabels.Get(Of Int32)(i * clusterCount + val)
                            finalColor = finalColorCenters.Get(Of cv.Vec3f)(colorLabel)
                            lDst.Set(y, x, New cv.Vec3b(finalColor(0), finalColor(1), finalColor(2)))
                        Next
                    Next
                    lDst.CopyTo(ocvb.result2(roi))
                End Sub)
        Dim factor = CInt(255.0 / clusterCount)
        allLabels = factor * allLabels
        cv.Cv2.CvtColor(allLabels, ocvb.result1, cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        grid.Dispose()
    End Sub
End Class





Public Class kMeans_Color : Implements IDisposable
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "kMeans k", 2, 32, 3)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Cluster the rgb image using kMeans.  Color each cluster by average depth."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim columnVector = ocvb.color.Reshape(ocvb.color.Channels, ocvb.color.Height * ocvb.color.Width)
        Dim rgb32f As New cv.Mat
        columnVector.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
        Dim clusterCount = sliders.TrackBar1.Value
        Dim labels = New cv.Mat()
        Dim colors As New cv.Mat

        cv.Cv2.Kmeans(rgb32f, clusterCount, labels, term, 1, cv.KMeansFlags.PpCenters, colors)
        labels.Reshape(1, ocvb.color.Height).ConvertTo(labels, cv.MatType.CV_8U)

        For i = 0 To clusterCount - 1
            Dim mask = labels.InRange(i, i)
            Dim mean = ocvb.RGBDepth.Mean(mask)
            ocvb.result1.SetTo(mean, mask)
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class





Public Class kMeans_Color_MT : Implements IDisposable
    Public grid As Thread_Grid
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "kMeans k", 2, 32, 2)
        If ocvb.parms.ShowOptions Then sliders.Show()

        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 32
        grid.sliders.TrackBar2.Value = 32
        grid.externalUse = True ' we don't need any results.

        ocvb.desc = "Cluster the rgb image using kMeans.  Color each cluster by average depth."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        Dim clusterCount = sliders.TrackBar1.Value
        Dim depth32f = getDepth32f(ocvb)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim zeroDepth = depth32f(roi).Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()
            Dim color = ocvb.color(roi).Clone()
            Dim columnVector = color.Reshape(ocvb.color.Channels, roi.Height * roi.Width)
            Dim rgb32f As New cv.Mat
            columnVector.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
            Dim labels = New cv.Mat()
            Dim colors As New cv.Mat

            cv.Cv2.Kmeans(rgb32f, clusterCount, labels, term, 1, cv.KMeansFlags.PpCenters, colors)
            labels.Reshape(1, roi.Height).ConvertTo(labels, cv.MatType.CV_8U)

            For i = 0 To clusterCount - 1
                Dim mask = labels.InRange(i, i)
                mask.SetTo(0, zeroDepth) ' don't include the zeros in the mean depth computation.
                Dim mean = ocvb.RGBDepth(roi).Mean(mask)
                ocvb.result1(roi).SetTo(mean, mask)
            Next
        End Sub)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        grid.Dispose()
    End Sub
End Class





Public Class kMeans_ColorDepth : Implements IDisposable
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "kMeans k", 2, 32, 3)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Cluster the rgb+Depth using kMeans.  Color each cluster by average depth."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim rgb32f As New cv.Mat
        ocvb.color.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
        Dim srcPlanes() As cv.Mat = Nothing
        cv.Cv2.Split(rgb32f, srcPlanes)
        ReDim Preserve srcPlanes(3)
        srcPlanes(3) = getDepth32f(ocvb)
        Dim zeroMask = srcPlanes(3).Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()

        Dim rgbDepth As New cv.Mat
        cv.Cv2.Merge(srcPlanes, rgbDepth)

        Dim columnVector = rgbDepth.Reshape(srcPlanes.Length, rgbDepth.Height * rgbDepth.Width)
        Dim clusterCount = sliders.TrackBar1.Value
        Dim labels = New cv.Mat()
        Dim colors As New cv.Mat

        cv.Cv2.Kmeans(columnVector, clusterCount, labels, term, 1, cv.KMeansFlags.PpCenters, colors)
        labels.Reshape(1, ocvb.color.Height).ConvertTo(labels, cv.MatType.CV_8U)

        For i = 0 To clusterCount - 1
            Dim mask = labels.InRange(i, i)
            Dim mean = ocvb.RGBDepth.Mean(mask)
            ocvb.result1.SetTo(mean, mask)
        Next
        ocvb.result1.SetTo(0, zeroMask)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class





Public Class kMeans_ColorDepth_MT : Implements IDisposable
    Public grid As Thread_Grid
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "kMeans k", 2, 32, 3)
        If ocvb.parms.ShowOptions Then sliders.Show()

        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 32
        grid.sliders.TrackBar2.Value = 32
        grid.externalUse = True ' we don't need any results.

        ocvb.desc = "Cluster the rgb+Depth using kMeans.  Color each cluster by average depth."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)

        Dim clusterCount = sliders.TrackBar1.Value
        Dim depth32f = getDepth32f(ocvb)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
       Sub(roi)
           Dim rgb32f As New cv.Mat
           ocvb.color(roi).ConvertTo(rgb32f, cv.MatType.CV_32FC3)
           Dim srcPlanes() As cv.Mat = Nothing
           cv.Cv2.Split(rgb32f, srcPlanes)
           ReDim Preserve srcPlanes(4 - 1)
           srcPlanes(3) = depth32f(roi)

           Dim rgbDepth As New cv.Mat
           cv.Cv2.Merge(srcPlanes, rgbDepth)

           Dim columnVector = rgbDepth.Reshape(srcPlanes.Length, rgbDepth.Height * rgbDepth.Width)
           Dim labels = New cv.Mat()
           Dim colors As New cv.Mat

           cv.Cv2.Kmeans(columnVector, clusterCount, labels, term, 1, cv.KMeansFlags.PpCenters, colors)
           labels.Reshape(1, roi.Height).ConvertTo(labels, cv.MatType.CV_8U)

           For i = 0 To clusterCount - 1
               Dim mask = labels.InRange(i, i)
               Dim mean = ocvb.RGBDepth(roi).Mean(mask)
               ocvb.result1(roi).SetTo(mean, mask)
           Next
       End Sub)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        grid.Dispose()
    End Sub
End Class