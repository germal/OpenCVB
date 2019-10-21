Imports cv = OpenCvSharp
Imports dnn = OpenCvSharp.Dnn
Imports System.IO
' https://github.com/twMr7/rscvdnn
Public Class DNN_Basics : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim net As dnn.Net
    Dim dnnPrepared As Boolean
    Dim crop As cv.Rect
    Dim dnnWidth As Int32, dnnHeight As Int32
    Dim testImage As cv.Mat
    Public rect As cv.Rect
    Dim classNames() = {"background", "aeroplane", "bicycle", "bird", "boat", "bottle", "bus", "car", "cat", "chair", "cow", "diningtable", "dog", "horse",
                        "motorbike", "person", "pottedplant", "sheep", "sofa", "train", "tvmonitor"}
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "dnn Scale Factor", 1, 10000, 78)
        sliders.setupTrackBar2(ocvb, "dnn MeanVal", 1, 255, 127)
        If ocvb.parms.ShowOptions Then sliders.show()

        dnnWidth = ocvb.color.Height ' height is always smaller than width...
        dnnHeight = ocvb.color.Height
        crop = New cv.Rect(ocvb.color.Width / 2 - dnnWidth / 2, ocvb.color.Height / 2 - dnnHeight / 2, dnnWidth, dnnHeight)

        Dim PERCdir = ocvb.parms.HomeDir + "librealsense/build/"
        Dim infoText As New FileInfo(PERCdir + "\wrappers\opencv\dnn\MobileNetSSD_deploy.prototxt")
        If infoText.Exists Then
            Dim infoModel As New FileInfo(PERCdir + "\wrappers\opencv\dnn\MobileNetSSD_deploy.caffemodel")
            If infoModel.Exists Then
                net = dnn.CvDnn.ReadNetFromCaffe(infoText.FullName, infoModel.FullName)
                dnnPrepared = True
            End If
        End If
        ocvb.result1.SetTo(0)
        If dnnPrepared = False Then
            ocvb.putText(New ActiveClass.TrueType("librealsense caffe databases not found.", 10, 100))
            ocvb.putText(New ActiveClass.TrueType("Run CMake on librealsense and select BUILD_CV_EXAMPLES.", 10, 125))
        End If
        ocvb.desc = "Use OpenCV's dnn from Caffe file."
        ocvb.label1 = "Input Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If dnnPrepared Then
            Dim inScaleFactor = sliders.TrackBar1.Value / sliders.TrackBar1.Maximum ' should be 0.0078 by default...
            Dim meanVal = CSng(sliders.TrackBar2.Value)
            Dim inputBlob = dnn.CvDnn.BlobFromImage(ocvb.color(crop), inScaleFactor, New cv.Size(300, 300), meanVal, False)
            ocvb.color.CopyTo(ocvb.result2)
            ocvb.color(crop).CopyTo(ocvb.result1(crop))
            net.SetInput(inputBlob, "data")
            Dim detection = net.Forward("detection_out")
            Dim detectionMat = New cv.Mat(detection.Size(2), detection.Size(3), cv.MatType.CV_32F, detection.Data)

            Dim confidenceThreshold = 0.8F
            Dim rows = ocvb.color.Rows
            Dim cols = ocvb.color.Cols
            ocvb.label2 = ""
            For i = 0 To detectionMat.Rows - 1
                Dim confidence = detectionMat.At(Of Single)(i, 2)
                If confidence > confidenceThreshold Then
                    Dim nextName = classNames(CInt(detectionMat.At(Of Single)(i, 1)))
                    ocvb.label2 += nextName + " "  ' display the name of what we found.
                    Dim vec = detectionMat.At(Of cv.Vec4f)(i, 3)
                    rect = New cv.Rect(vec.Item0 * cols + crop.Left, vec.Item1 * rows + crop.Top, (vec.Item2 - vec.Item0) * cols, (vec.Item3 - vec.Item1) * rows)
                    rect = New cv.Rect(rect.X, rect.Y, Math.Min(dnnWidth, rect.Width), Math.Min(dnnHeight, rect.Height))
                    ocvb.result2.Rectangle(rect, cv.Scalar.Yellow, 3, cv.LineTypes.AntiAlias)
                    rect.Width = 100
                    rect.Height = 30
                    ocvb.result2.Rectangle(rect, cv.Scalar.Black, -1)
                    ocvb.putText(New ActiveClass.TrueType(nextName, CInt(rect.X * ocvb.parms.imageToTrueTypeLoc), CInt(rect.Y * ocvb.parms.imageToTrueTypeLoc), RESULT2))
                End If
            Next
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        If net IsNot Nothing Then net.Dispose()
    End Sub
End Class