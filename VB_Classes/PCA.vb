Imports cv = OpenCvSharp
' https://github.com/opencv/opencv/blob/master/samples/cpp/pca.cpp
Public Class PCA_Basics
    Inherits VB_Class
        Public useDepthInput As Boolean
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, "Retained Variance", 1, 100, 95)
                ocvb.desc = "Reconstruct a video stream as a composite of X images."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static images(7) As cv.Mat
        Static images32f(images.Length) As cv.Mat
        Dim index = ocvb.frameCount Mod images.Length
        If useDepthInput Then
            images(index) = ocvb.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Else
            images(index) = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        End If
        Dim gray32f As New cv.Mat
        images(index).ConvertTo(gray32f, cv.MatType.CV_32F)
        gray32f = gray32f.Normalize(0, 255, cv.NormTypes.MinMax)
        images32f(index) = New cv.Mat
        gray32f.Clone().Reshape(1, 1).ConvertTo(images32f(index), cv.MatType.CV_32F)
        If ocvb.frameCount >= images.Length Then
            Dim data = New cv.Mat(images.Length, ocvb.color.Rows * ocvb.color.Cols, cv.MatType.CV_32F)
            For i = 0 To images.Length - 1
                images32f(i).CopyTo(data.Row(i))
            Next

            Dim retainedVariance = sliders.TrackBar1.Value / 100
            Dim pca = New cv.PCA(data, New cv.Mat, cv.PCA.Flags.DataAsRow, retainedVariance)  ' the pca inputarray cannot be static so we reallocate each time.  

            Dim point = pca.Project(data.Row(0))
            Dim reconstruction = pca.BackProject(point)
            reconstruction = reconstruction.Reshape(images(0).Channels(), images(0).Rows)
            reconstruction.ConvertTo(reconstruction, cv.MatType.CV_8UC1)
            ocvb.result1 = reconstruction.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        End If
    End Sub
    Public Sub VBdispose()
    End Sub
End Class



Public Class PCA_Depth
    Inherits VB_Class
    Dim pca As PCA_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        pca = New PCA_Basics(ocvb, "PCA_Depth")
        pca.useDepthInput = True
        ocvb.desc = "Reconstruct a depth stream as a composite of X images."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        pca.Run(ocvb)
    End Sub
    Public Sub VBdispose()
        pca.Dispose()
    End Sub
End Class




' https://docs.opencv.org/3.1.0/d1/dee/tutorial_introduction_to_pca.html
Public Class PCA_DrawImage
    Inherits VB_Class
    Dim pca As PCA_Basics
    Dim image As New cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        pca = New PCA_Basics(ocvb, "PCA_DrawImage")
        image = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/pca_test1.jpg")
        ocvb.desc = "Use PCA to find the principle direction of an object."
        ocvb.label1 = "Original image"
        ocvb.label2 = "PCA Output"
    End Sub
    Private Sub drawAxis(img As cv.Mat, p As cv.Point, q As cv.Point, color As cv.Scalar, scale As Single)
        Dim angle = Math.Atan2(p.Y - q.Y, p.X - q.X) ' angle in radians
        Dim hypotenuse = Math.Sqrt((p.Y - q.Y) * (p.Y - q.Y) + (p.X - q.X) * (p.X - q.X))
        q.X = p.X - scale * hypotenuse * Math.Cos(angle)
        q.Y = p.Y - scale * hypotenuse * Math.Sin(angle)
        img.Line(p, q, color, 1, cv.LineTypes.AntiAlias)
        p.X = q.X + 9 * Math.Cos(angle + Math.PI / 4)
        p.Y = q.Y + 9 * Math.Sin(angle + Math.PI / 4)
        img.Line(p, q, color, 1, cv.LineTypes.AntiAlias)
        p.X = q.X + 9 * Math.Cos(angle - Math.PI / 4)
        p.Y = q.Y + 9 * Math.Sin(angle - Math.PI / 4)
        img.Line(p, q, color, 1, cv.LineTypes.AntiAlias)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result1 = image.Resize(ocvb.result1.Size())
        Dim gray = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(50, 255, cv.ThresholdTypes.Binary Or cv.ThresholdTypes.Otsu)
        Dim hierarchy() As cv.HierarchyIndex = Nothing
        Dim contours As cv.Point()() = Nothing
        cv.Cv2.FindContours(gray, contours, hierarchy, cv.RetrievalModes.List, cv.ContourApproximationModes.ApproxNone)

        ocvb.result2.SetTo(0)
        For i = 0 To contours.Length - 1
            Dim area = cv.Cv2.ContourArea(contours(i))
            If area < 100 Or area > 100000 Then Continue For
            cv.Cv2.DrawContours(ocvb.result2, contours, i, cv.Scalar.Red, 1, cv.LineTypes.AntiAlias)
            Dim sz = contours(i).Length
            Dim data_pts = New cv.Mat(sz, 2, cv.MatType.CV_64FC1)
            For j = 0 To data_pts.Rows - 1
                data_pts.Set(Of Double)(j, 0, contours(i)(j).X)
                data_pts.Set(Of Double)(j, 1, contours(i)(j).Y)
            Next

            Dim pca_analysis = New cv.PCA(data_pts, New cv.Mat, cv.PCA.Flags.DataAsRow)
            Dim cntr = New cv.Point(CInt(pca_analysis.Mean.Get(of Double)(0, 0)), CInt(pca_analysis.Mean.Get(of Double)(0, 1)))
            Dim eigen_vecs(2) As cv.Point2d
            Dim eigen_val(2) As Double
            For j = 0 To 1
                eigen_vecs(j) = New cv.Point2d(pca_analysis.Eigenvectors.Get(of Double)(j, 0), pca_analysis.Eigenvectors.Get(of Double)(j, 1))
                eigen_val(j) = pca_analysis.Eigenvalues.Get(of Double)(0, j)
            Next

            ocvb.result2.Circle(cntr, 3, cv.Scalar.BlueViolet, -1, cv.LineTypes.AntiAlias)
            Dim factor As Single = 0.02 ' scaling factor for the lines depicting the principle components.
            Dim ept1 = New cv.Point(cntr.X + factor * eigen_vecs(0).X * eigen_val(0), cntr.Y + factor * eigen_vecs(0).Y * eigen_val(0))
            Dim ept2 = New cv.Point(cntr.X - factor * eigen_vecs(1).X * eigen_val(1), cntr.Y - factor * eigen_vecs(1).Y * eigen_val(1))

            drawAxis(ocvb.result2, cntr, ept1, cv.Scalar.Red, 1) ' primary principle component
            drawAxis(ocvb.result2, cntr, ept2, cv.Scalar.BlueViolet, 5) ' secondary principle component
        Next
    End Sub
    Public Sub VBdispose()
        pca.Dispose()
    End Sub
End Class
