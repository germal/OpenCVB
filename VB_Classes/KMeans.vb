Imports cv = OpenCvSharp
Public Class kMeans_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "kMeans k", 2, 32, 4)
        ocvb.desc = "Cluster the rgb image pixels using kMeans."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim small = src.Resize(New cv.Size(src.Width / 4, src.Height / 4))
        Dim rectMat = small.Clone
        Dim columnVector As New cv.Mat
        columnVector = rectMat.Reshape(src.Channels, small.Height * small.Width)
        Dim rgb32f As New cv.Mat
        columnVector.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
        Dim clusterCount = sliders.TrackBar1.Value
        Dim labels = New cv.Mat()
        Dim colors As New cv.Mat

        cv.Cv2.Kmeans(rgb32f, clusterCount, labels, term, 1, cv.KMeansFlags.PpCenters, colors)
        labels.Reshape(1, small.Height).ConvertTo(labels, cv.MatType.CV_8U)
        labels = labels.Resize(New cv.Size(src.Width, src.Height))

        For i = 0 To clusterCount - 1
            Dim mask = labels.InRange(i, i)
            Dim mean = ocvb.RGBDepth.Mean(mask)
            dst1.SetTo(mean, mask)
        Next
    End Sub
End Class





Public Class kMeans_Clusters
    Inherits ocvbClass
    Dim Mats As Mat_4to1
    Dim km As kMeans_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        Mats = New Mat_4to1(ocvb, caller)

        km = New kMeans_Basics(ocvb, caller)

        label1 = "kmeans - k=10"
        label2 = "kmeans - k=2,4,6,8"
        ocvb.desc = "Show clustering with various settings for cluster count.  Draw to select region of interest."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static saveRect = ocvb.drawRect
        ocvb.drawRect = saveRect
        For i = 0 To 3
            km.sliders.TrackBar1.Value = (i + 1) * 2
            km.Run(ocvb)
            Mats.mat(i) = dst1.Resize(New cv.Size(dst1.Cols / 2, dst1.Rows / 2))
        Next
        Mats.Run(ocvb)
        km.sliders.TrackBar1.Value = 10 ' this will show kmeans with 10 clusters in Result1.
        km.Run(ocvb)
    End Sub
End Class





Public Class kMeans_RGBFast
    Inherits ocvbClass
    Public clusterColors() As cv.Vec3b
    Public resizeFactor = 2
    Public clusterCount = 6
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "kMeans k", 2, 32, 4)
        ocvb.desc = "Cluster a small rgb image using kMeans.  Specify clusterCount value."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim small8uC3 = src.Resize(New cv.Size(CInt(src.Rows / resizeFactor), CInt(src.Cols / resizeFactor)))
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
        dst1 = small8uC3.Resize(dst1.Size())
    End Sub
End Class




Public Class kMeans_RGB_Plus_XYDepth
    Inherits ocvbClass
    Dim km As kMeans_Basics
    Dim clusterColors() As cv.Vec6i
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "kMeans k", 2, 32, 4)
        km = New kMeans_Basics(ocvb, caller)
        label1 = "kmeans - RGB, XY, and Depth Raw"
        ocvb.desc = "Cluster with kMeans RGB, x, y, and depth."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        km.Run(ocvb) ' cluster the rgb image - output is in dst2
        Dim rgb32f As New cv.Mat
        km.dst1.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
        Dim xyDepth32f As New cv.Mat(rgb32f.Size(), cv.MatType.CV_32FC3, 0)
        Dim depth32f = getDepth32f(ocvb)
        For y = 0 To xyDepth32f.Rows - 1
            For x = 0 To xyDepth32f.Cols - 1
                Dim nextVal = depth32f.Get(Of Single)(y, x)
                If nextVal Then xyDepth32f.Set(Of cv.Vec3f)(y, x, New cv.Vec3f(x, y, nextVal))
            Next
        Next
        Dim src() = New cv.Mat() {rgb32f, xyDepth32f}
        Dim all32f = New cv.Mat(rgb32f.Size(), cv.MatType.CV_32FC(6)) ' output will have 6 channels!
        Dim mixed() = New cv.Mat() {all32f}
        Dim from_to() = New Int32() {0, 0, 0, 1, 0, 2, 3, 3, 4, 4, 5, 5}
        cv.Cv2.MixChannels(src, mixed, from_to)

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
                    dst1.Set(Of cv.Vec3b)(y, x, New cv.Vec3b(10 * .Item0 Mod 255, 10 * .Item1 Mod 255, 10 * .Item2 Mod 255))
                End With
            Next
        Next
    End Sub
End Class







Public Class kMeans_RGB_MT
    Inherits ocvbClass
    Dim grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "kMeans k", 2, 32, 10)

        grid = New Thread_Grid(ocvb, caller)
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 48
        label1 = "kmeans - raw labels"
        label2 = "Synchronized colors from raw labels"
        ocvb.desc = "Cluster a grid of segments individual and combine results.  Select the desired number of clusters/threads."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        src.CopyTo(dst1)
        dst1.SetTo(cv.Scalar.White, grid.gridMask)

        Dim clusterCount = sliders.TrackBar1.Value
        Dim allLabels As New cv.Mat(src.Size(), cv.MatType.CV_8UC1)
        Dim clusterColors As New cv.Mat(New cv.Size(3, clusterCount * (grid.roiList.Count - 1)), cv.MatType.CV_32FC1)
        Dim roiList As New List(Of cv.Rect)
        For i = 0 To grid.roiList.Count - 1
            ' if the number of pixels is less than the clusterCount kmeans will fail!  (Segments near boundaries can be tiny.)
            If grid.roiList(i).Width * grid.roiList(i).Height >= clusterCount Then
                roiList.Add(grid.roiList(i))
            End If
        Next

        Parallel.For(0, roiList.Count,
         Sub(i)
             Dim roi = roiList(i)
             Dim src As New cv.Mat(roi.Width, roi.Height, cv.MatType.CV_8UC3)
             src(roi).CopyTo(src)
             Dim columnVector As New cv.Mat
             columnVector = src.Reshape(src.Channels, roi.Height * roi.Width)
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
        Parallel.For(0, roiList.Count,
                Sub(i)
                    Dim roi = roiList(i)
                    Dim val As Int32, colorLabel As Int32, finalColor As cv.Vec3f
                    Dim lSrc As New cv.Mat
                    allLabels(roi).CopyTo(lSrc)
                    Dim ldst1 As New cv.Mat(lSrc.Size(), cv.MatType.CV_8UC3)
                    For y = 0 To roi.Height - 1
                        For x = 0 To roi.Width - 1
                            val = lSrc.Get(Of Byte)(y, x)
                            colorLabel = finalColorLabels.Get(Of Int32)(i * clusterCount + val)
                            finalColor = finalColorCenters.Get(Of cv.Vec3f)(colorLabel)
                            ldst1.Set(y, x, New cv.Vec3b(finalColor(0), finalColor(1), finalColor(2)))
                        Next
                    Next
                    ldst1.CopyTo(dst2(roi))
                End Sub)
        Dim factor = CInt(255.0 / clusterCount)
        allLabels = factor * allLabels
        cv.Cv2.CvtColor(allLabels, dst1, cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class





Public Class kMeans_ReducedRGB
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Reduction factor", 2, 64, 64)
        sliders.setupTrackBar2(ocvb, caller, "kmeans k", 2, 64, 4)
        label2 = "Reduced color image."
        ocvb.desc = "Reduce each pixel by the reduction factor and then run kmeans."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dst2 = src / sliders.TrackBar1.Value
        dst2 *= sliders.TrackBar1.Value

        src = dst2
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
        data.Reshape(3, src.Rows).ConvertTo(dst1, cv.MatType.CV_8U)
    End Sub
End Class




Public Class kMeans_XYDepth
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "kMeans k", 2, 32, 4)
        Dim w = src.Width / 4
        Dim h = src.Height / 4
        ocvb.drawRect = New cv.Rect(w, h, w * 2, h * 2)
        ocvb.desc = "Cluster with x, y, and depth using kMeans.  Draw on the image to select a region."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim roi = ocvb.drawRect
        Dim depth32f = getDepth32f(ocvb)
        Dim xyDepth32f As New cv.Mat(depth32f(roi).Size(), cv.MatType.CV_32FC3, 0)
        For y = 0 To xyDepth32f.Rows - 1
            For x = 0 To xyDepth32f.Cols - 1
                Dim nextVal = depth32f(roi).Get(Of Single)(y, x)
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
        ocvb.RGBDepth.CopyTo(dst1)
        columnVector.Reshape(3, dst1(roi).Height).ConvertTo(dst1(roi), cv.MatType.CV_8U)
    End Sub
End Class




Public Class kMeans_Depth_FG_BG
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
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
        If depthCenters.Get(Of Single)(0, 0) > depthCenters.Get(Of Single)(1, 0) Then foregroundLabel = 1

        ' if one of the centers is way out there, leave the mask alone.  KMeans clustered an unreasonably small cluster.
        ' If depthCenters.Get(of Single)(0, 0) > 20000 Or depthCenters.Get(of Single)(1, 0) > 20000 Then Exit Sub

        Dim mask = labels.InRange(foregroundLabel, foregroundLabel)
        Dim shadowMask = depth32f.Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()
        mask.SetTo(0, shadowMask)
        dst1 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class




Public Class kMeans_LAB
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "kMeans k", 2, 32, 4)
        label1 = "kMeans_LAB - draw to select region"
        Dim w = src.Width / 4
        Dim h = src.Height / 4
        ocvb.drawRect = New cv.Rect(w, h, w * 2, h * 2)
        ocvb.desc = "Cluster the LAB image using kMeans.  Is it better?  Optionally draw on the image and select k."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim roi = ocvb.drawRect
        Dim labMat = src(roi).CvtColor(cv.ColorConversionCodes.RGB2Lab)
        Dim columnVector As New cv.Mat
        columnVector = labMat.Reshape(src.Channels, roi.Height * roi.Width)
        Dim lab32f As New cv.Mat
        columnVector.ConvertTo(lab32f, cv.MatType.CV_32FC3)
        Dim clusterCount = sliders.TrackBar1.Value
        Dim labels = New cv.Mat()
        Dim colors As New cv.Mat

        cv.Cv2.Kmeans(lab32f, clusterCount, labels, term, 1, cv.KMeansFlags.PpCenters, colors)

        For i = 0 To columnVector.Rows - 1
            lab32f.Set(Of cv.Vec3f)(i, 0, colors.Get(Of cv.Vec3f)(labels.Get(Of Int32)(i)))
        Next
        src.CopyTo(dst1)
        lab32f.Reshape(3, roi.Height).ConvertTo(dst1(roi), cv.MatType.CV_8UC3)
        dst1(roi) = dst1(roi).CvtColor(cv.ColorConversionCodes.Lab2RGB)
        dst1.Rectangle(ocvb.drawRect, cv.Scalar.White, 1)
    End Sub
End Class






Public Class kMeans_Color
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "kMeans k", 2, 32, 3)
        ocvb.desc = "Cluster the rgb image using kMeans.  Color each cluster by average depth."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim columnVector = src.Reshape(src.Channels, src.Height * src.Width)
        Dim rgb32f As New cv.Mat
        columnVector.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
        Dim clusterCount = sliders.TrackBar1.Value
        Dim labels = New cv.Mat()
        Dim colors As New cv.Mat

        cv.Cv2.Kmeans(rgb32f, clusterCount, labels, term, 1, cv.KMeansFlags.PpCenters, colors)
        labels.Reshape(1, src.Height).ConvertTo(labels, cv.MatType.CV_8U)

        For i = 0 To clusterCount - 1
            Dim mask = labels.InRange(i, i)
            Dim mean = ocvb.RGBDepth.Mean(mask)
            dst1.SetTo(mean, mask)
        Next
    End Sub
End Class





Public Class kMeans_Color_MT
    Inherits ocvbClass
    Public grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "kMeans k", 2, 32, 2)

        grid = New Thread_Grid(ocvb, caller)
        grid.sliders.TrackBar1.Value = 32
        grid.sliders.TrackBar2.Value = 32

        ocvb.desc = "Cluster the rgb image using kMeans.  Color each cluster by average depth."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        Dim clusterCount = sliders.TrackBar1.Value
        Dim depth32f = getDepth32f(ocvb)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim zeroDepth = depth32f(roi).Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()
            Dim color = src(roi).Clone()
            Dim columnVector = color.Reshape(src.Channels, roi.Height * roi.Width)
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
                dst1(roi).SetTo(mean, mask)
            Next
        End Sub)
    End Sub
End Class





Public Class kMeans_ColorDepth
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "kMeans k", 2, 32, 3)
        ocvb.desc = "Cluster the rgb+Depth using kMeans.  Color each cluster by average depth."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim rgb32f As New cv.Mat
        src.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
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
        labels.Reshape(1, src.Height).ConvertTo(labels, cv.MatType.CV_8U)

        For i = 0 To clusterCount - 1
            Dim mask = labels.InRange(i, i)
            Dim mean = ocvb.RGBDepth.Mean(mask)
            dst1.SetTo(mean, mask)
        Next
        dst1.SetTo(0, zeroMask)
    End Sub
End Class





Public Class kMeans_ColorDepth_MT
    Inherits ocvbClass
    Public grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "kMeans k", 2, 32, 3)

        grid = New Thread_Grid(ocvb, caller)
        grid.sliders.TrackBar1.Value = 32
        grid.sliders.TrackBar2.Value = 32

        ocvb.desc = "Cluster the rgb+Depth using kMeans.  Color each cluster by average depth."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)

        Dim clusterCount = sliders.TrackBar1.Value
        Dim depth32f = getDepth32f(ocvb)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
       Sub(roi)
           Dim rgb32f As New cv.Mat
           src(roi).ConvertTo(rgb32f, cv.MatType.CV_32FC3)
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
               dst1(roi).SetTo(mean, mask)
           Next
       End Sub)
    End Sub
End Class
