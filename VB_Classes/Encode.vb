Imports cv = OpenCvSharp
Public Class Encode_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Encode Quality Level", 1, 100, 1) ' make it low quality to highlight how different it can be.
        sliders.setupTrackBar(1, "Encode Output Scaling", 1, 100, 7)

        setDescription(ocvb, "Error Level Analysis - to verify a jpg image has not been modified.")
        label1 = "absDiff with original"
        label2 = "Original decompressed"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim buf(ocvb.color.Width * ocvb.color.Height * ocvb.color.ElemSize) As Byte
        Dim encodeParams() As Int32 = {cv.ImwriteFlags.JpegQuality, sliders.trackbar(0).Value}

        cv.Cv2.ImEncode(".jpg", ocvb.color, buf, encodeParams)
        dst2 = cv.Cv2.ImDecode(buf, 1)

        Dim output As New cv.Mat
        cv.Cv2.Absdiff(ocvb.color, dst2, output)

        Dim scale = sliders.trackbar(1).Value
        output.ConvertTo(dst1, cv.MatType.CV_8UC3, scale)
        Dim compressionRatio = buf.Length / (ocvb.color.Rows * ocvb.color.Cols * ocvb.color.ElemSize)
        label2 = "Original compressed to len=" + CStr(buf.Length) + " (" + Format(compressionRatio, "0.1%") + ")"
    End Sub
End Class



Public Class Encode_Options
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Encode Quality Level", 1, 100, 1) ' make it low quality to highlight how different it can be.
        sliders.setupTrackBar(1, "Encode Output Scaling", 1, 100, 85)

        radio.Setup(ocvb, caller, 6)
        radio.check(0).Text = "JpegChromaQuality"
        radio.check(1).Text = "JpegLumaQuality"
        radio.check(2).Text = "JpegOptimize"
        radio.check(3).Text = "JpegProgressive"
        radio.check(4).Text = "JpegQuality"
        radio.check(5).Text = "WebPQuality"
        radio.check(0).Checked = True

        setDescription(ocvb, "Encode options that affect quality.")
        label1 = "absDiff with original image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim buf(ocvb.color.Width * ocvb.color.Height * ocvb.color.ElemSize) As Byte
        Dim encodeOption As Int32
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                encodeOption = Choose(i + 1, cv.ImwriteFlags.JpegChromaQuality, cv.ImwriteFlags.JpegLumaQuality, cv.ImwriteFlags.JpegOptimize, cv.ImwriteFlags.JpegProgressive,
                                              cv.ImwriteFlags.JpegQuality, cv.ImwriteFlags.WebPQuality)
                Exit For
            End If
        Next

        Dim fileExtension = ".jpg"
        Dim qualityLevel = sliders.trackbar(0).Value
        If encodeOption = cv.ImwriteFlags.JpegProgressive Then qualityLevel = 1 ' just on or off
        If encodeOption = cv.ImwriteFlags.JpegOptimize Then qualityLevel = 1 ' just on or off
        Dim encodeParams() As Int32 = {encodeOption, qualityLevel}

        cv.Cv2.ImEncode(fileExtension, ocvb.color, buf, encodeParams)
        dst2 = cv.Cv2.ImDecode(buf, 1)

        Dim output As New cv.Mat
        cv.Cv2.Absdiff(ocvb.color, dst2, output)

        Dim scale = sliders.trackbar(1).Value
        output.ConvertTo(dst1, cv.MatType.CV_8UC3, scale)
        Dim compressionRatio = buf.Length / (ocvb.color.Rows * ocvb.color.Cols * ocvb.color.ElemSize)
        label2 = "Original compressed to len=" + CStr(buf.Length) + " (" + Format(compressionRatio, "0.0%") + ")"
    End Sub
End Class

