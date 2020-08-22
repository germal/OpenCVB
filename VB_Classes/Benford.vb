Imports cv = OpenCvSharp
Imports System.Text.RegularExpressions

' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
Public Class Benford_Basics
    Inherits ocvbClass
    Public expectedDistribution(10 - 1) As Single
    Public counts(expectedDistribution.Count - 1) As Single
    Dim plot As Plot_Histogram
    Dim benford As Benford_NormalizedImage
    Dim weight As AddWeighted_Basics
    Dim weightSlider As System.Windows.Forms.TrackBar
    Dim fontSlider As System.Windows.Forms.TrackBar
    Dim use99 As Boolean
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        plot = New Plot_Histogram(ocvb)
        If standalone Then benford = New Benford_NormalizedImage(ocvb)

        For i = 1 To expectedDistribution.Count - 1
            expectedDistribution(i) = Math.Log10(1 + 1 / i) ' get the precise expected values.
        Next

        weight = New AddWeighted_Basics(ocvb)
        weightSlider = findSlider("Weight")
        If weightSlider IsNot Nothing Then weightSlider.Value = 75

        fontSlider = findSlider("Histogram Font Size x10")
        fontSlider.Value = 6

        ocvb.desc = "Build the capability to perform a Benford analysis."
    End Sub
    Public Sub setup99()
        ReDim expectedDistribution(100 - 1)
        For i = 1 To expectedDistribution.Count - 1
            expectedDistribution(i) = Math.Log10(1 + 1 / i)
        Next
        ReDim counts(expectedDistribution.Count - 1)
        use99 = True
    End Sub

    Public Sub Run(ocvb As AlgorithmData)
        If standalone Then
            benford.src = src
            benford.Run(ocvb)
            dst1 = benford.dst1
            dst2 = benford.dst2
            Exit Sub
        End If
        src = src.Reshape(1, src.Width * src.Height)
        Dim indexer = src.GetGenericIndexer(Of Single)()
        ReDim counts(expectedDistribution.Count - 1)
        If use99 = False Then
            For i = 0 To src.Rows - 1
                Dim val = indexer(i).ToString
                If val <> 0 And Single.IsNaN(val) = False Then
                    Dim firstInt = Regex.Match(val, "[1-9]{1}")
                    counts(firstInt.Value) += 1
                End If
            Next
        Else
            ' this is for the distribution 10-99
            For i = 0 To src.Rows - 1
                Dim val = indexer(i).ToString
                If val <> 0 And Single.IsNaN(val) = False Then
                    Dim firstInt = Regex.Match(val, "[1-9]{1}").ToString
                    Dim index = val.IndexOf(firstInt)
                    If index < Len(val - 2) And index > 0 Then
                        Dim val99 = Mid(val, index + 1, 2)
                        counts(val99) += 1
                    End If
                End If
            Next
        End If

        plot.hist = New cv.Mat(counts.Length, 1, cv.MatType.CV_32F, counts)
        plot.Run(ocvb)
        weight.src1 = plot.dst1.Clone

        For i = 0 To counts.Count - 1
            counts(i) = src.Rows * expectedDistribution(i)
        Next

        plot.hist = New cv.Mat(counts.Length, 1, cv.MatType.CV_32F, counts)
        plot.Run(ocvb)
        weight.src2 = plot.dst1

        weight.Run(ocvb)
        dst1 = weight.dst1

        label2 = CStr(weightSlider.Value) + "% actual distribution vs. " + CStr(100 - weightSlider.Value) + "% Benford distribution"
    End Sub
End Class






' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
Public Class Benford_NormalizedImage
    Inherits ocvbClass
    Public benford As Benford_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        benford = New Benford_Basics(ocvb)

        ocvb.desc = "Perform a Benford analysis of an image normalized to between 0 and 1"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim gray32f As New cv.Mat
        dst1.ConvertTo(gray32f, cv.MatType.CV_32F)

        benford.src = gray32f.Normalize(1)
        benford.Run(ocvb)
        dst2 = benford.dst1
        label2 = benford.label2
    End Sub
End Class






' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
Public Class Benford_NormalizedImage99
    Inherits ocvbClass
    Public benford As Benford_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        benford = New Benford_Basics(ocvb)
        benford.setup99()

        ocvb.desc = "Perform a Benford analysis for 10-99, not 1-9, of an image normalized to between 0 and 1"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim gray32f As New cv.Mat
        dst1.ConvertTo(gray32f, cv.MatType.CV_32F)

        benford.src = gray32f.Normalize(1)
        benford.Run(ocvb)
        dst2 = benford.dst1
        label2 = benford.label2
    End Sub
End Class






' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
Public Class Benford_JPEG
    Inherits ocvbClass
    Public benford As Benford_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        benford = New Benford_Basics(ocvb)

        sliders.Setup(ocvb, caller, 1)
        sliders.setupTrackBar(0, "JPEG Quality", 1, 100, 90)

        ocvb.desc = "Perform a Benford analysis for 1-9 of a JPEG compressed image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim jpeg = src.ImEncode(".jpg", New Integer() {cv.ImwriteFlags.JpegQuality, sliders.trackbar(0).Value})
        benford.src = New cv.Mat(jpeg.Count, 1, cv.MatType.CV_8U, jpeg)
        dst1 = cv.Cv2.ImDecode(jpeg, cv.ImreadModes.Color)
        benford.Run(ocvb)
        dst2 = benford.dst1
        label2 = benford.label2
    End Sub
End Class






' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
Public Class Benford_JPEG99
    Inherits ocvbClass
    Public benford As Benford_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        benford = New Benford_Basics(ocvb)
        benford.setup99()

        sliders.Setup(ocvb, caller, 1)
        sliders.setupTrackBar(0, "JPEG Quality", 1, 100, 90)

        ocvb.desc = "Perform a Benford analysis for 10-99, not 1-9, of a JPEG compressed image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim jpeg = src.ImEncode(".jpg", New Integer() {cv.ImwriteFlags.JpegQuality, sliders.trackbar(0).Value})
        benford.src = New cv.Mat(jpeg.Count, 1, cv.MatType.CV_8U, jpeg)
        dst1 = cv.Cv2.ImDecode(jpeg, cv.ImreadModes.Color)
        benford.Run(ocvb)
        dst2 = benford.dst1
        label2 = benford.label2
    End Sub
End Class

