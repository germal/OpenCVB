Imports cv = OpenCvSharp
Imports OpenCvSharp.Dnn
Imports System.Net
Imports System.Linq
Imports System.IO

Public Class DNN_Test
    Inherits ocvbClass
    Dim net As Net
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        label2 = "Input Image"
        ocvb.desc = "Download and use a Caffe database"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim modelFile As New FileInfo(ocvb.parms.HomeDir + "Data/bvlc_googlenet.caffemodel")
        If File.Exists(modelFile.FullName) = False Then
            ' this site is apparently gone.  caffemodel is in the Data directory in OpenCVB_HomeDir
            Dim client = HttpWebRequest.CreateHttp("http://dl.caffe.berkeleyvision.org/bvlc_googlenet.caffemodel")
            Dim response = client.GetResponse()
            Dim responseStream = response.GetResponseStream()
            Dim memory As New MemoryStream()
            responseStream.CopyTo(memory)
            File.WriteAllBytes(modelFile.FullName, memory.ToArray)
        End If
        net = Net.ReadNetFromCaffe(ocvb.parms.HomeDir + "Data/bvlc_googlenet.prototxt")

        Dim image = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/space_shuttle.jpg")
        dst2 = image.Resize(dst2.Size())
        Dim inputBlob = CvDnn.BlobFromImage(image, 1, New cv.Size(224, 224), New cv.Scalar(104, 117, 123))
        net.SetInput(inputBlob, "data")
        ocvb.trueText(New TTtext("This example is not working.  Forward fails with 'blobs.size() != 0'.", 10, 100))
        'Dim prob = net.Forward("prob") ' <--- this fails in VB.Net but works in C# (below)
        ' finish this ...
    End Sub
End Class





Public Class DNN_Caffe_CS
    Inherits ocvbClass
    Dim caffeCS As CS_Classes.DNN
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        label2 = "Input Image"
        ocvb.desc = "Download and use a Caffe database"

        Dim protoTxt = ocvb.parms.HomeDir + "Data/bvlc_googlenet.prototxt"
        Dim modelFile = ocvb.parms.HomeDir + "Data/bvlc_googlenet.caffemodel"
        Dim synsetWords = ocvb.parms.HomeDir + "Data/synset_words.txt"
        caffeCS = New CS_Classes.DNN(protoTxt, modelFile, synsetWords)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim image = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/space_shuttle.jpg")
        Dim str = caffeCS.Run(image)
        dst2 = image.Resize(dst2.Size())
        ocvb.trueText(New TTtext(str, 10, 100))
    End Sub
End Class





' https://github.com/twMr7/rscvdnn
Public Class DNN_Basics
    Inherits ocvbClass
    Dim net As Net
    Dim dnnPrepared As Boolean
    Dim crop As cv.Rect
    Dim dnnWidth As Int32, dnnHeight As Int32
    Dim testImage As cv.Mat
    Public rect As cv.Rect
    Dim classNames() = {"background", "aeroplane", "bicycle", "bird", "boat", "bottle", "bus", "car", "cat", "chair", "cow", "diningtable", "dog", "horse",
                        "motorbike", "person", "pottedplant", "sheep", "sofa", "train", "tvmonitor"}
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "dnn Scale Factor", 1, 10000, 78)
        sliders.setupTrackBar(1, "dnn MeanVal", 1, 255, 127)

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
        If dnnPrepared = False Then
            ocvb.trueText(New TTtext("Caffe databases not found.  It should be in <OpenCVB_HomeDir>/Data.", 10, 100))
        End If
        ocvb.desc = "Use OpenCV's dnn from Caffe file."
        label1 = "Cropped Input Image - must be square!"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If dnnPrepared Then
            Dim inScaleFactor = sliders.trackbar(0).Value / sliders.trackbar(0).Maximum ' should be 0.0078 by default...
            Dim meanVal = CSng(sliders.trackbar(1).Value)
            Dim inputBlob = CvDnn.BlobFromImage(ocvb.color(crop), inScaleFactor, New cv.Size(300, 300), meanVal, False)
            ocvb.color.CopyTo(dst2)
            ocvb.color(crop).CopyTo(dst1(crop))
            net.SetInput(inputBlob, "data")

            Dim detection = net.Forward("detection_out")
            Dim detectionMat = New cv.Mat(detection.Size(2), detection.Size(3), cv.MatType.CV_32F, detection.Data)

            Dim confidenceThreshold = 0.8F
            Dim rows = ocvb.color(crop).Rows
            Dim cols = ocvb.color(crop).Cols
            label2 = ""
            For i = 0 To detectionMat.Rows - 1
                Dim confidence = detectionMat.Get(Of Single)(i, 2)
                If confidence > confidenceThreshold Then
                    Dim nextName = classNames(CInt(detectionMat.Get(Of Single)(i, 1)))
                    label2 += nextName + " "  ' display the name of what we found.
                    Dim vec = detectionMat.Get(Of cv.Vec4f)(i, 3)
                    rect = New cv.Rect(vec.Item0 * cols + crop.Left, vec.Item1 * rows + crop.Top, (vec.Item2 - vec.Item0) * cols, (vec.Item3 - vec.Item1) * rows)
                    rect = New cv.Rect(rect.X, rect.Y, Math.Min(dnnWidth, rect.Width), Math.Min(dnnHeight, rect.Height))
                    dst2.Rectangle(rect, cv.Scalar.Yellow, 3, cv.LineTypes.AntiAlias)
                    rect.Width = 100
                    rect.Height = 30
                    dst2.Rectangle(rect, cv.Scalar.Black, -1)
                    ocvb.trueText(New TTtext(nextName, CInt(rect.X * ocvb.parms.trueTextLoc), CInt(rect.Y * ocvb.parms.trueTextLoc)))
                End If
            Next
        End If
    End Sub
End Class

