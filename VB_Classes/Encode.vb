Imports cv = OpenCvSharp
' https://www.programcreek.com/python/example/70396/cv2.imencode
Public Class Encode_Basics
    Inherits VBparent
    Dim options As Encode_Options
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        options = New Encode_Options(ocvb)

        desc = "Error Level Analysis - to verify a jpg image has not been modified."
        label1 = "absDiff with original"
        label2 = "Original decompressed"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim buf(src.Width * src.Height * src.ElemSize) As Byte
        Dim encodeParams() As Int32 = {options.getEncodeParameter(), options.qualityLevel}

        cv.Cv2.ImEncode(".jpg", src, buf, encodeParams)
        dst2 = cv.Cv2.ImDecode(buf, 1)

        Dim output As New cv.Mat
        cv.Cv2.Absdiff(src, dst2, output)

        Static scaleSlider = findSlider("Encode Output Scaling")
        If ocvb.frameCount = 0 Then scaleSlider.value = 10

        output.ConvertTo(dst1, cv.MatType.CV_8UC3, scaleSlider.Value)
        Dim compressionRatio = buf.Length / (src.Rows * src.Cols * src.ElemSize)
        label2 = "Original compressed to len=" + CStr(buf.Length) + " (" + Format(compressionRatio, "0.1%") + ")"
    End Sub
End Class



' https://answers.opencv.org/question/31519/encode-image-in-jpg-with-opencv-avoiding-the-artifacts-effect/
Public Class Encode_Options
    Inherits VBparent
    Public qualityLevel As Integer
    Public Sub New(ocvb As VBocvb)
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
        radio.check(1).Checked = True

        desc = "Encode options that affect quality."
        label1 = "absDiff with original image"
    End Sub
    Public Function getEncodeParameter() As Integer
        Dim encodeOption As Integer
        qualityLevel = sliders.trackbar(0).Value
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                encodeOption = Choose(i + 1, cv.ImwriteFlags.JpegChromaQuality, cv.ImwriteFlags.JpegLumaQuality, cv.ImwriteFlags.JpegOptimize, cv.ImwriteFlags.JpegProgressive,
                                              cv.ImwriteFlags.JpegQuality, cv.ImwriteFlags.WebPQuality)
                Exit For
            End If
        Next
        If encodeOption = cv.ImwriteFlags.JpegProgressive Then qualityLevel = 1 ' just on or off
        If encodeOption = cv.ImwriteFlags.JpegOptimize Then qualityLevel = 1 ' just on or off
        Return encodeOption
    End Function
    Public Sub Run(ocvb As VBocvb)
        Dim buf(src.Width * src.Height * src.ElemSize) As Byte

        Dim fileExtension = ".jpg"
        Dim encodeParams() As Int32 = {getEncodeParameter(), qualityLevel}

        cv.Cv2.ImEncode(fileExtension, src, buf, encodeParams)
        dst2 = cv.Cv2.ImDecode(buf, 1)

        Dim output As New cv.Mat
        cv.Cv2.Absdiff(src, dst2, output)

        Dim scale = sliders.trackbar(1).Value
        output.ConvertTo(dst1, cv.MatType.CV_8UC3, scale)
        Dim compressionRatio = buf.Length / (src.Rows * src.Cols * src.ElemSize)
        label2 = "Original compressed to len=" + CStr(buf.Length) + " (" + Format(compressionRatio, "0.0%") + ")"
    End Sub
End Class

