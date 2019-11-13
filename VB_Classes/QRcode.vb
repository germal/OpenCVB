Imports cv = OpenCvSharp
Imports System.IO
Public Class QRcode_Basics : Implements IDisposable
    Dim qrDecoder As New cv.QRCodeDetector
    Dim qrInput1 As New cv.Mat
    Dim qrInput2 As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        Dim fileInfo = New FileInfo(ocvb.parms.HomeDir + "data/QRcode1.png")
        If fileInfo.Exists Then qrInput1 = cv.Cv2.ImRead(fileInfo.FullName)
        fileInfo = New FileInfo(ocvb.parms.HomeDir + "Data/QRCode2.png")
        If fileInfo.Exists Then qrInput2 = cv.Cv2.ImRead(fileInfo.FullName)
        If ocvb.color.Width < 480 Then ' for the smallest configurations the default size can be too big!
            qrInput1 = qrInput1.Resize(New cv.Size(120, 160))
            qrInput2 = qrInput2.Resize(New cv.Size(120, 160))
        End If
        ocvb.desc = "Read a QR code"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim input = ocvb.color.Clone()
        Dim x = ocvb.rng.uniform(0, input.Width - Math.Max(qrInput1.Width, qrInput2.Width))
        Dim y = ocvb.rng.uniform(0, input.Height - Math.Max(qrInput1.Height, qrInput2.Height))
        If CInt(ocvb.frameCount / 50) Mod 2 = 0 Then
            Dim roi = New cv.Rect(x, y, qrInput1.Width, qrInput1.Height)
            input(roi) = qrInput1
        Else
            Dim roi = New cv.Rect(x, y, qrInput2.Width, qrInput2.Height)
            input(roi) = qrInput2
        End If

        Dim box() As cv.Point2f = Nothing
        Dim rectifiedImage As New cv.Mat
        Dim refersTo = qrDecoder.DetectAndDecode(input, box, rectifiedImage)

        input.CopyTo(ocvb.result1)
        For i = 0 To box.Length - 1
            ocvb.result1.Line(box(i), box((i + 1) Mod 4), cv.Scalar.Red, 3, cv.LineTypes.AntiAlias)
        Next
        If refersTo <> "" Then ocvb.label1 = refersTo
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
