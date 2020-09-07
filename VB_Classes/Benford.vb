Imports cv = OpenCvSharp
Imports System.Text.RegularExpressions
' Benford's Law is pretty cool but I don't think it is a phenomenon of nature.  It is produced from bringing real world measurements to a human scale.
' Reducing an image with compression occur because human understanding maps the data within reach of the understanding embedded in our number system.
' (Further investigation: would a base other than 10 provide the same results?)
' If real world measurements do not conform to Benford's Law, it is likely because the measurement is not a good one or has been manipulated.
' Benford's law is a good indicator that the scale for the measurement is appropriate.
' Below are 2 types of examples - one just takes the grayscale image and applies Benford's analysis, the other uses jpeg/PNG before applying Benford.
' Only the JPEG/PNG examples match Benford while the grayscale image does not.
' Note that with the 10-99 Benford JPEG example, the results match Benford and then stop matching and abruptly fall off in the middle of the plot.
' This impact is likely the result of how JPEG compression truncates values as insignificant - a definite manipulation of the data.

' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
Public Class Benford_Basics
    Inherits VBparent
    Public expectedDistribution(10 - 1) As Single
    Public counts(expectedDistribution.Count - 1) As Single
    Dim plot As Plot_Histogram
    Dim benford As Benford_NormalizedImage
    Dim weight As AddWeighted_Basics
    Dim use99 As Boolean
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        plot = New Plot_Histogram(ocvb)
        If standalone Then benford = New Benford_NormalizedImage(ocvb)

        For i = 1 To expectedDistribution.Count - 1
            expectedDistribution(i) = Math.Log10(1 + 1 / i) ' get the precise expected values.
        Next

        weight = New AddWeighted_Basics(ocvb)

        desc = "Build the capability to perform a Benford analysis."
    End Sub
    Public Sub setup99()
        ReDim expectedDistribution(100 - 1)
        For i = 1 To expectedDistribution.Count - 1
            expectedDistribution(i) = Math.Log10(1 + 1 / i)
        Next
        ReDim counts(expectedDistribution.Count - 1)
        use99 = True
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If standalone Then
            benford.src = src
            benford.Run(ocvb)
            dst1 = benford.dst1
            dst2 = benford.dst2
            label2 = benford.label2
            Exit Sub
        End If

        Dim weightSlider = findSlider("Weight")
        If ocvb.frameCount = 0 Then
            If weightSlider IsNot Nothing Then weightSlider.Value = 75
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
                        If IsNumeric(val99) Then counts(val99) += 1
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

        cv.Cv2.BitwiseNot(plot.dst1, weight.src2)
        ' weight.src2 = plot.dst1
        weight.Run(ocvb)
        dst1 = weight.dst1

        label2 = "AddWeighted: " + CStr(weightSlider.Value) + "% actual vs. " + CStr(100 - weightSlider.Value) + "% Benford distribution"
    End Sub
End Class






' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
Public Class Benford_NormalizedImage
    Inherits VBparent
    Public benford As Benford_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        benford = New Benford_Basics(ocvb)

        desc = "Perform a Benford analysis of an image normalized to between 0 and 1"
    End Sub
    Public Sub Run(ocvb As VBocvb)
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
    Inherits VBparent
    Public benford As Benford_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        benford = New Benford_Basics(ocvb)
        benford.setup99()

        desc = "Perform a Benford analysis for 10-99, not 1-9, of an image normalized to between 0 and 1"
    End Sub
    Public Sub Run(ocvb As VBocvb)
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
    Inherits VBparent
    Public benford As Benford_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        benford = New Benford_Basics(ocvb)

        sliders.Setup(ocvb, caller, 1)
        sliders.setupTrackBar(0, "JPEG Quality", 1, 100, 90)

        desc = "Perform a Benford analysis for 1-9 of a JPEG compressed image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
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
    Inherits VBparent
    Public benford As Benford_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        benford = New Benford_Basics(ocvb)
        benford.setup99()

        sliders.Setup(ocvb, caller, 1)
        sliders.setupTrackBar(0, "JPEG Quality", 1, 100, 90)

        desc = "Perform a Benford analysis for 10-99, not 1-9, of a JPEG compressed image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Static qualitySlider = findSlider("JPEG Quality")
        Dim jpeg = src.ImEncode(".jpg", New Integer() {cv.ImwriteFlags.JpegQuality, qualitySlider.Value})
        benford.src = New cv.Mat(jpeg.Count, 1, cv.MatType.CV_8U, jpeg)
        dst1 = cv.Cv2.ImDecode(jpeg, cv.ImreadModes.Color)
        benford.Run(ocvb)
        dst2 = benford.dst1
        label2 = benford.label2
    End Sub
End Class







' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
Public Class Benford_PNG
    Inherits VBparent
    Public benford As Benford_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        benford = New Benford_Basics(ocvb)

        sliders.Setup(ocvb, caller, 1)
        sliders.setupTrackBar(0, "PNG Compression", 1, 100, 90)

        desc = "Perform a Benford analysis for 1-9 of a JPEG compressed image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Static compressionSlider = findSlider("PNG Compression")
        Dim png = src.ImEncode(".png", New Integer() {cv.ImwriteFlags.PngCompression, compressionSlider.Value})
        benford.src = New cv.Mat(png.Count, 1, cv.MatType.CV_8U, png)
        dst1 = cv.Cv2.ImDecode(png, cv.ImreadModes.Color)
        benford.Run(ocvb)
        dst2 = benford.dst1
        label2 = benford.label2
    End Sub
End Class






Public Class Benford_Depth
    Inherits VBparent
    Public benford As Benford_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        benford = New Benford_Basics(ocvb)
        desc = "Apply Benford to the depth data"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        benford.src = getDepth32f(ocvb)
        benford.Run(ocvb)
        dst1 = benford.dst1
        label1 = benford.label2
    End Sub
End Class






Public Class Benford_DepthRGB
    Inherits VBparent
    Public benford As Benford_JPEG
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        benford = New Benford_JPEG(ocvb)
        desc = "Apply Benford to the depth RGB image that is compressed with JPEG"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim jpeg = ocvb.RGBDepth.ImEncode(".jpg", New Integer() {cv.ImwriteFlags.JpegQuality, benford.sliders.trackbar(0).Value})
        benford.src = ocvb.RGBDepth
        benford.Run(ocvb)
        dst1 = cv.Cv2.ImDecode(jpeg, cv.ImreadModes.Color)
        dst2 = benford.dst2
        label2 = benford.label2
    End Sub
End Class