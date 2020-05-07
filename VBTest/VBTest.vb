Imports cv = OpenCvSharp
Imports VB_Classes



Public Class VBTest_Basics
    Inherits ocvbClass
    Dim sobel As Edges_Canny
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setcaller(callerRaw)
        If caller = "" Then caller = Me.GetType.Name Else caller += "-->" + Me.GetType.Name
        ocvb.name = "VBTest_Basics"
        ocvb.label1 = "VBTest_Basics"
        ocvb.desc = "Insert and debug new experiments here and then migrate them to the VB_Classes which is compiled in Release mode."
        sobel = New VB_Classes.Edges_Canny(ocvb, "VBTest_Basics")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        sobel.Run(ocvb)
    End Sub
    Public Sub myDispose()
        sobel.Dispose()
    End Sub
End Class