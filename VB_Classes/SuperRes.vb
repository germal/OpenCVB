Imports cv = OpenCvSharp
Imports System.IO
' https://github.com/opencv/opencv/blob/master/samples/gpu/super_resolution.cpp
' https://www.csharpcodi.com/vs2/?source=4752/opencvsharp_samples/SamplesCS/Samples/SuperResolutionSample.cs
Public Class SuperRes_Basics : Implements IDisposable
    Public videoOptions As New OptionsVideoName
    Dim fs As cv.FrameSource
    Dim sr As cv.SuperResolution
    Public Sub New(ocvb As AlgorithmData)
        videoOptions.fileinfo = New FileInfo(ocvb.parms.HomeDir + "Data/CarsDrivingUnderBridge.mp4")
        fs = cv.FrameSource.CreateFrameSource_Video(videoOptions.fileinfo.FullName)
        sr = cv.SuperResolution.CreateBTVL1()
        sr.SetInput(fs)
        ocvb.desc = "Enhance resolution with SuperRes API in OpenCV"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim normalFrame As New cv.Mat
        Dim srFrame As New cv.Mat
        'fs.NextFrame(ocvb.color)
        'Dim test As New cv.Mat
        'sr.NextFrame(test)

    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class