Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

' https://github.com/masaddev/OpenCVParticleFilter/tree/master/OpenCVParticleFilter
Public Class ParticleFilter_Example
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        ocvb.desc = "Particle Filter example downloaded from github - link in the code."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
    End Sub
    Public Sub Close()
        ' ParticleFilter_Close(pfPtr)
    End Sub
End Class







Module ParticleFilter
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function ParticleFilter_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ParticleFilter_Close(pfPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function ParticleFilter_Run(pfPtr As IntPtr, rows As Integer, cols As Integer, channels As Integer) As IntPtr
    End Function
End Module
