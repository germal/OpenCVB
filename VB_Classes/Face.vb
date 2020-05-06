Imports cv = OpenCvSharp
Module FaceDetection_Exports
    Public Sub detectFace(ByRef src As cv.Mat, cascade As cv.CascadeClassifier)
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim faces() = cascade.DetectMultiScale(gray, 1.08, 3, cv.HaarDetectionType.ScaleImage, New cv.Size(30, 30))
        For Each face In faces
            src.Rectangle(face, cv.Scalar.Red, 1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Module

' https://docs.opencv.org/2.4/doc/tutorials/objdetect/cascade_classifier/cascade_classifier.html
Public Class Face_Haar_LBP : Implements IDisposable
    Dim haarCascade As cv.CascadeClassifier
    Dim lbpCascade As cv.CascadeClassifier
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        haarCascade = New cv.CascadeClassifier(ocvb.parms.HomeDir + "Data/haarcascade_frontalface_default.xml")
        lbpCascade = New cv.CascadeClassifier(ocvb.parms.HomeDir + "Data/lbpcascade_frontalface.xml")
        ocvb.desc = "Detect faces in the video stream."
        ocvb.label1 = "Faces detected with Haar"
        ocvb.label2 = "Faces detected with LBP"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result1 = ocvb.color.Clone()
        detectFace(ocvb.result1, haarCascade)
        ocvb.result2 = ocvb.color.Clone()
        detectFace(ocvb.result2, lbpCascade)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class



Public Class Face_Haar_Alt : Implements IDisposable
    Dim haarCascade As cv.CascadeClassifier
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        haarCascade = New cv.CascadeClassifier(ocvb.parms.HomeDir + "Data/haarcascade_frontalface_alt.xml")
        ocvb.desc = "Detect faces Haar_alt database."
        ocvb.label1 = "Faces detected with Haar_Alt"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result1 = ocvb.color.Clone()
        detectFace(ocvb.result1, haarCascade)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

