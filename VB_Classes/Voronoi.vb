'https://www.codeproject.com/Articles/882739/Simple-approach-to-Voronoi-diagrams
Imports cv = OpenCvSharp
Public Class Voronoi_CSharp
    Inherits VBparent
    Dim vDemo As New CS_Classes.VoronoiDemo
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        label1 = "Brute Force method"
        label2 = "Ordered List method"
        ocvb.desc = "C# implementations of the BruteForce and OrderedList Voronoi algorithms"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        vDemo.Run(dst1, True, True)
        dst1 = dst1.Normalize(255).ConvertScaleAbs(255)
        dst1 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        vDemo.Run(dst2, False, False)  ' run a second time with the same input (randomize = false)
        dst2 = dst2.Normalize(255).ConvertScaleAbs(255)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class