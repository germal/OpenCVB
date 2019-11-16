Imports cv = OpenCvSharp
Imports OpenCvSharp.Dnn
Imports System.Net
Imports System.Linq
Imports System.IO

Public Class DNN_Test : Implements IDisposable
    Dim net As Net
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label2 = "Input Image"
        ocvb.desc = "Download and use a Caffe database"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim modelFile As New FileInfo(ocvb.parms.HomeDir + "Data/bvlc_googlenet.caffemodel")
        If File.Exists(modelFile.FullName) = False Then
            ' this site is apparently gone.  caffemodel is in the OpenCVSharp distribution.
            Dim client = HttpWebRequest.CreateHttp("http://dl.caffe.berkeleyvision.org/bvlc_googlenet.caffemodel")
            Dim response = client.GetResponse()
            Dim responseStream = response.GetResponseStream()
            Dim memory As New MemoryStream()
            responseStream.CopyTo(memory)
            File.WriteAllBytes(modelFile.FullName, memory.ToArray)
        End If
        net = Net.ReadNetFromCaffe(ocvb.parms.HomeDir + "Data/bvlc_googlenet.prototxt")

        Dim image = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/space_shuttle.jpg")
        ocvb.result2 = image.Resize(ocvb.result2.Size())
        Dim inputBlob = CvDnn.BlobFromImage(image, 1, New cv.Size(224, 224), New cv.Scalar(104, 117, 123))
        net.SetInput(inputBlob, "data")
        If ocvb.parms.AvoidDNNCrashes = False Then
            ocvb.putText(New ActiveClass.TrueType("This example is not working.  Forward fails with 'blobs.size() != 0'.", 10, 100))
            'Dim prob = net.Forward("prob")
            ' finish this ...
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class





Public Class DNN_Caffe_CS : Implements IDisposable
    Dim caffeCS As New CS_Classes.DNN
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label2 = "Input Image"
        ocvb.desc = "Download and use a Caffe database"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim protoTxt = ocvb.parms.HomeDir + "Data/bvlc_googlenet.prototxt"
        Dim modelFile = ocvb.parms.HomeDir + "Data/bvlc_googlenet.caffemodel"
        Dim synsetWords = ocvb.parms.HomeDir + "Data/synset_words.txt"
        Dim image = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/space_shuttle.jpg")
        caffeCS.Run(protoTxt, modelFile, synsetWords, image, ocvb.parms.AvoidDNNCrashes)
        ocvb.result2 = image.Resize(ocvb.result2.Size())
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class





' https://github.com/twMr7/rscvdnn
Public Class DNN_Basics : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim net As Net
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
        If ocvb.parms.ShowOptions Then sliders.Show()

        dnnWidth = ocvb.color.Height ' height is always smaller than width...
        dnnHeight = ocvb.color.Height
        crop = New cv.Rect(ocvb.color.Width / 2 - dnnWidth / 2, ocvb.color.Height / 2 - dnnHeight / 2, dnnWidth, dnnHeight)

        Dim infoText As New FileInfo(ocvb.parms.HomeDir + "Data/MobileNetSSD_deploy.prototxt")
        If infoText.Exists Then
            Dim infoModel As New FileInfo(ocvb.parms.HomeDir + "Data/MobileNetSSD_deploy.caffemodel")
            If infoModel.Exists Then
                net = CvDnn.ReadNetFromCaffe(infoText.FullName, infoModel.FullName)
                dnnPrepared = True
            End If
        End If
        ocvb.result1.SetTo(0)
        If dnnPrepared = False Then
            ocvb.putText(New ActiveClass.TrueType("librealsense caffe databases not found.", 10, 100))
            ocvb.putText(New ActiveClass.TrueType("Run CMake on librealsense and select BUILD_CV_EXAMPLES.", 10, 125))
        End If
        ocvb.desc = "Use OpenCV's dnn from Caffe file."
        ocvb.label1 = "Cropped Input Image - must be square!"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If dnnPrepared Then
            Dim inScaleFactor = sliders.TrackBar1.Value / sliders.TrackBar1.Maximum ' should be 0.0078 by default...
            Dim meanVal = CSng(sliders.TrackBar2.Value)
            Dim inputBlob = CvDnn.BlobFromImage(ocvb.color(crop), inScaleFactor, New cv.Size(300, 300), meanVal, False)
            ocvb.color.CopyTo(ocvb.result2)
            ocvb.color(crop).CopyTo(ocvb.result1(crop))
            net.SetInput(inputBlob, "data")

            ' The Forward method fails or blue-screen's my main machine so it is conditional here.
            ' The test machines I have do not fail.  The blue screen is WHEA_UNCORRECTABLE_ERROR.

            If ocvb.parms.AvoidDNNCrashes = False Then
                Dim detection = net.Forward("detection_out")
                Dim detectionMat = New cv.Mat(detection.Size(2), detection.Size(3), cv.MatType.CV_32F, detection.Data)

                Dim confidenceThreshold = 0.8F
                Dim rows = ocvb.color(crop).Rows
                Dim cols = ocvb.color(crop).Cols
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
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        If net IsNot Nothing Then net.Dispose()
    End Sub
End Class
