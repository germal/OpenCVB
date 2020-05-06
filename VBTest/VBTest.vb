Imports cv = OpenCvSharp
Imports VB_Classes



Public Class VBTest_Basics : Implements IDisposable
    Dim sobel As VB_Classes.Edges_Canny
    Public Sub New(ocvb As AlgorithmData, byVal caller as string)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        ocvb.name = "VBTest_Basics"
        ocvb.label1 = "VBTest_Basics"
        ocvb.desc = "Insert and debug new experiments here and then migrate them to the VB_Classes which is compiled in Release mode."
        sobel = New VB_Classes.Edges_Canny(ocvb, "VBTest_Basics")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        sobel.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sobel.Dispose()
    End Sub
End Class