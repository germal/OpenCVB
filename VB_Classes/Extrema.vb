Imports cv = OpenCvSharp
Public Class Extrema_Min
    Inherits VBparent
    Dim colorize As Depth_ColorizerFastFade_CPP
    Dim motion As Motion_Basics
    Dim mOverlap As Rectangle_MultiOverlap
    Dim stable As IMU_IscameraStable
    Public minDepth As cv.Mat
    Public resetAll As Boolean
    Public Sub New()
        initParent()
        stable = New IMU_IscameraStable
        colorize = New Depth_ColorizerFastFade_CPP
        motion = New Motion_Basics
        mOverlap = New Rectangle_MultiOverlap

        minDepth = New cv.Mat(src.Size(), cv.MatType.CV_32F, ocvb.maxZ)

        label1 = "Colorized min depth data"
        task.desc = "Keep a tally of minimum depth data at every location"
    End Sub

    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        If standalone Then src = getDepth32f()

        stable.Run()
        If stable.cameraStable = False Then
            resetAll = True
            minDepth = getDepth32f()
        Else
            resetAll = False

            cv.Cv2.Min(getDepth32f(), minDepth, minDepth)

            motion.src = minDepth.Clone
            motion.Run()

            If motion.rectList.Count > 0 Then
                mOverlap.inputRects = New List(Of cv.Rect)(motion.rectList)
                mOverlap.Run()

                For Each r In mOverlap.enclosingRects
                    r.Inflate(If(r.X - 10 > 0 And r.X + r.Width + 10 < src.Width, 10, 0), If(r.Y - 10 > 0 And r.Y + r.Height + 10 < src.Height, 10, 0))
                    src(r).CopyTo(minDepth(r))
                Next

                dst2 = motion.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                For Each r In mOverlap.enclosingRects
                    dst2.Rectangle(r, cv.Scalar.Red, 2)
                Next
                mOverlap.enclosingRects.Clear()
            End If
            colorize.src = minDepth
            colorize.Run()
            dst1 = colorize.dst1
        End If
    End Sub
End Class