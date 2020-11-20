'https://www.codeproject.com/Articles/882739/Simple-approach-to-Voronoi-diagrams
Imports cv = OpenCvSharp

Public Class Voronoi_Basics
    Inherits VBparent
    Public vDemo As New CS_Classes.VoronoiDemo
    Public random As Random_Points
    Public inputPoints As List(Of cv.Point)
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        If standalone Then
            random = New Random_Points(ocvb)
            Dim countSlider = findSlider("Random Pixel Count")
            countSlider.Maximum = 100
        End If
        label1 = "Ordered list output for Voronoi algorithm"
        ocvb.desc = "Use the ordered list method to find the Voronoi segments"
    End Sub
    Public Sub vDisplay(ocvb As VBocvb, ByRef dst As cv.Mat, points As List(Of cv.Point))
        dst = dst.Normalize(255).ConvertScaleAbs(255)
        dst = dst.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        For Each pt In points
            dst.Circle(pt, ocvb.dotSize, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me

        If standalone Then
            random.Run(ocvb)
            inputPoints = New List(Of cv.Point)(random.Points)
        End If

        vDemo.Run(dst1, inputPoints)
        vDisplay(ocvb, dst1, inputPoints)
    End Sub
End Class






Public Class Voronoi_Compare
    Inherits VBparent
    Dim basics As Voronoi_Basics
    Public random As Random_Points
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        basics = New Voronoi_Basics(ocvb)
        random = New Random_Points(ocvb)

        label1 = "Brute Force method"
        label2 = "Ordered List method"
        ocvb.desc = "C# implementations of the BruteForce and OrderedList Voronoi algorithms"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me

        random.Run(ocvb)
        Dim points = New List(Of cv.Point)(random.Points)
        basics.vDemo.Run(dst1, points, True)
        basics.vDisplay(ocvb, dst1, points)

        basics.vDemo.Run(dst2, points, False)
        basics.vDisplay(ocvb, dst2, points)
    End Sub
End Class