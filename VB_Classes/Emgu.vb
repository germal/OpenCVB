Imports cv = OpenCvSharp

Public Class Emgu_Basics : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Test a sample EMGU usage."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim data(ocvb.color.Rows * ocvb.color.Cols * ocvb.color.ElemSize) As Byte
        Emgu_Classes.DrawSubdivision.Draw(ocvb.color.Rows, ocvb.color.Cols, 20, data)
        ' why not just have Draw return a Mat from Emgu?  Because an Emgu Mat is not an OpenCVSharp Mat!  But this works...
        ocvb.result1 = New cv.Mat(ocvb.color.Rows, ocvb.color.Cols, cv.MatType.CV_8UC3, data)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class Emgu_Factdetection : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Use the simplest possible face detector in Emgu examples."
        ocvb.result1.SetTo(0)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim lena = New cv.Mat(ocvb.parms.HomeDir + "VB_Classes/Python/PythonData/Lena.jpg", cv.ImreadModes.Color)
        Dim data(lena.Rows * lena.Cols * lena.ElemSize) As Byte
        Emgu_Classes.FaceDetection.Detect(ocvb.parms.HomeDir + "VB_Classes/Python/PythonData/Lena.jpg",
                                          ocvb.parms.HomeDir + "Data/haarcascade_frontalface_alt.xml", data)
        Dim tmp = New cv.Mat(lena.Rows, lena.Cols, cv.MatType.CV_8UC3, data)
        tmp = tmp.Resize(New cv.Size(ocvb.result1.Rows, ocvb.result1.Rows))
        ocvb.result1(New cv.Rect(0, 0, tmp.Rows, tmp.Cols)) = tmp
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

