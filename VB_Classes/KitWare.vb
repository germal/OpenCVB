Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports Kitware.VTK

' https://lorensen.github.io/VTKExamples/site/CSharp/SimpleOperations/DistancePointToLine/
Public Class KitWare_DistancePointToLine
    Inherits VBparent
    Public Sub New()
        initParent()
        task.desc = "Test the use of the Kitware VTK interface"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim lineP0() As Double = {0.0, 0.0, 0.0}
        Dim lineP1() As Double = {2.0, 0.0, 0.0}
        Dim p0() As Double = {1.0, 0, 0}
        Dim p1() As Double = {1.0, 2.0, 0}

        Dim pP0 = Marshal.AllocHGlobal(Marshal.SizeOf(Of Double) * 3)
        Dim pP1 = Marshal.AllocHGlobal(Marshal.SizeOf(Of Double) * 3)
        Dim pLineP0 = Marshal.AllocHGlobal(Marshal.SizeOf(Of Double) * 3)
        Dim pLineP1 = Marshal.AllocHGlobal(Marshal.SizeOf(Of Double) * 3)
        Marshal.Copy(p0, 0, pP0, 0)
        Marshal.Copy(p1, 0, pP1, 0)
        Marshal.Copy(lineP0, 0, pLineP0, lineP0.Length)
        Marshal.Copy(lineP1, 0, pLineP0, lineP1.Length)

        Dim dist0 = vtkLine.DistanceToLine(pP0, pLineP0, pLineP1)
        Dim dist1 = vtkLine.DistanceToLine(pP1, pLineP0, pLineP1)

        Dim parametricCoord As Double = 0.0
        Dim closest(3 - 1) As Double
        Dim pClosest = Marshal.AllocHGlobal(Marshal.SizeOf(Of Double) * 3)
        Marshal.Copy(closest, 0, pClosest, closest.Length)

        dist0 = vtkLine.DistanceToLine(pP0, pLineP0, pLineP1, parametricCoord, pClosest)
        Marshal.Copy(pClosest, closest, 0, closest.Length)

        dist1 = vtkLine.DistanceToLine(pP1, pLineP0, pLineP1, parametricCoord, pClosest)
        Marshal.Copy(pClosest, closest, 0, closest.Length)

        Marshal.FreeHGlobal(pP0)
        Marshal.FreeHGlobal(pP1)
        Marshal.FreeHGlobal(pLineP0)
        Marshal.FreeHGlobal(pLineP1)
    End Sub
End Class
