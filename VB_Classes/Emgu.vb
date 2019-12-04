Imports cv = OpenCvSharp

Public Class Emgu_Basics : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Test a sample EMGU usage."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Emgu_Classes.DrawSubdivision.Draw(ocvb.color.Rows, ocvb.color.Cols, 20)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class Emgu_Factdetection : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Use the simplest possible face detector in Emgu examples."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Emgu_Classes.FaceDetection.Detect(ocvb.parms.HomeDir + "VB_Classes/Python/PythonData/Lena.jpg",
                                          ocvb.parms.HomeDir + "Data/haarcascade_frontalface_alt.xml")
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




'Public Class Emgu_VizDraw3D : Implements IDisposable
'    Dim draw3d As Emgu_Classes.VizDraw3D
'    Public Sub New(ocvb As AlgorithmData)
'        draw3d = New Emgu_Classes.VizDraw3D()
'        ocvb.desc = "Use Emgu to draw a 3D representation of a stereo image"
'    End Sub
'    Public Sub Run(ocvb As AlgorithmData)
'        draw3d.Run(ocvb.parms.HomeDir + "Data/imL.png", ocvb.parms.HomeDir + "Data/imR.png")
'    End Sub
'    Public Sub Dispose() Implements IDisposable.Dispose
'        draw3d.Dispose()
'    End Sub
'End Class
