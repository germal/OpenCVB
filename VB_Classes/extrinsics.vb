Imports cv = OpenCvSharp
Public Class Extrinsics_Basics
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        ocvb.desc = "Show the depth camera extrinsics."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim ttStart = 40
        ocvb.putText(New ActiveClass.TrueType("Rotation Matrix                                             Translation", 10, ttStart, RESULT1))
        Dim fmt = "#0.0000"
        Dim nextLine = Format(ocvb.parms.extrinsics.rotation(0), fmt) + vbTab + Format(ocvb.parms.extrinsics.rotation(1), fmt) + vbTab +
                       Format(ocvb.parms.extrinsics.rotation(2), fmt) + vbTab + vbTab + vbTab + Format(ocvb.parms.extrinsics.translation(0), fmt)
        ocvb.putText(New ActiveClass.TrueType(nextLine, 10, ttStart + 20, RESULT1))

        nextLine = Format(ocvb.parms.extrinsics.rotation(3), fmt) + vbTab + Format(ocvb.parms.extrinsics.rotation(4), fmt) + vbTab +
                       Format(ocvb.parms.extrinsics.rotation(5), fmt) + vbTab + vbTab + vbTab + Format(ocvb.parms.extrinsics.translation(1), fmt)
        ocvb.putText(New ActiveClass.TrueType(nextLine, 10, ttStart + 40, RESULT1))

        nextLine = Format(ocvb.parms.extrinsics.rotation(6), fmt) + vbTab + Format(ocvb.parms.extrinsics.rotation(7), fmt) + vbTab +
                       Format(ocvb.parms.extrinsics.rotation(8), fmt) + vbTab + vbTab + vbTab + Format(ocvb.parms.extrinsics.translation(2), fmt)
        ocvb.putText(New ActiveClass.TrueType(nextLine, 10, ttStart + 60, RESULT1))
    End Sub
End Class
