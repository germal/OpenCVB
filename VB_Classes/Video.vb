Imports cv = OpenCvSharp
Imports System.IO
' https://stackoverflow.com/questions/47706339/car-counting-and-classification-using-emgucv-and-vb-net
Public Class Video_Basics
    Inherits VB_Class
    Public srcVideo As String
    Dim currVideo As String
    Public image As New cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        If srcVideo = "" Then srcVideo = ocvb.parms.HomeDir + "Data\CarsDrivingUnderBridge.mp4" ' default video...
        currVideo = srcVideo
        videoOptions.NewVideo(ocvb, srcVideo)

        ocvb.label1 = srcVideo
        ocvb.desc = "Show a video file"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If srcVideo <> currVideo Then
            currVideo = srcVideo
            videoOptions.NewVideo(ocvb, currVideo)
        End If
        image = videoOptions.nextImage
        If image.Empty() = False Then ocvb.result1 = image.Resize(ocvb.color.Size())
    End Sub
    Public Sub MyDispose()
    End Sub
End Class




' https://stackoverflow.com/questions/47706339/car-counting-and-classification-using-emgucv-and-vb-net
Public Class Video_CarCounting
    Inherits VB_Class
    Dim flow As Font_FlowText
    Dim video As Video_Basics
    Dim mog As BGSubtract_MOG
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        mog = New BGSubtract_MOG(ocvb, "Video_CarCounting")
        mog.externalUse = True

        video = New Video_Basics(ocvb, "Video_CarCounting")

        flow = New Font_FlowText(ocvb, "Video_CarCounting")
        flow.externalUse = True
        flow.result1or2 = RESULT1

        ocvb.desc = "Count cars in a video file"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        video.Run(ocvb)
        If video.image.Empty() = False Then
            mog.src = video.image.Clone()
            mog.Run(ocvb)

            ocvb.result2 = video.image.Clone()

            ' there are 5 lanes of traffic so setup 5 regions
            ' NOTE: if long shadows are present this approach will not work without provision for the width of a car.  Needs more sample data.
            Dim activeHeight = 30
            Dim finishLine = mog.gray.Height - activeHeight * 8 ' the video is different size than the camera images...
            Static activeState(5) As Boolean
            Static carCount As Int32
            For i = 1 To activeState.Length - 1
                Dim lane = New cv.Rect(Choose(i, 230, 460, 680, 900, 1110), finishLine, 40, activeHeight)
                Dim cellCount = ocvb.result1(lane).CountNonZero()
                If cellCount Then
                    activeState(i) = True
                    ocvb.result1.Rectangle(lane, cv.Scalar.Red, -1)
                    ocvb.result2.Rectangle(lane, cv.Scalar.Red, -1)
                End If
                If cellCount = 0 And activeState(i) = True Then
                    activeState(i) = False
                    carCount += 1
                End If
                ocvb.result2.Rectangle(lane, cv.Scalar.White, 2)
            Next

            Dim tmp = ocvb.result1.Clone()
            flow.msgs.Add("  Cars " + CStr(carCount))
            flow.Run(ocvb)
            cv.Cv2.BitwiseOr(ocvb.result1, tmp, ocvb.result1)
        End If
    End Sub
    Public Sub MyDispose()
        video.Dispose()
        mog.Dispose()
        flow.Dispose()
    End Sub
End Class




' https://stackoverflow.com/questions/47706339/car-counting-and-classification-using-emgucv-and-vb-net
Public Class Video_CarCComp
    Inherits VB_Class
    Dim cc As CComp_Basics
    Dim flow As Font_FlowText
    Dim video As Video_Basics
    Dim mog As BGSubtract_MOG
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        mog = New BGSubtract_MOG(ocvb, "Video_CarCComp")
        mog.externalUse = True

        cc = New CComp_Basics(ocvb, "Video_CarCComp")
        cc.externalUse = True

        video = New Video_Basics(ocvb, "Video_CarCComp")

        flow = New Font_FlowText(ocvb, "Video_CarCComp")
        flow.externalUse = True
        flow.result1or2 = RESULT1

        ocvb.desc = "Outline cars with a rectangle"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        video.Run(ocvb)
        If video.image.Empty() = False Then
            mog.src = video.image.Clone()
            mog.Run(ocvb)

            cc.srcGray = ocvb.result1.Clone()
            cc.Run(ocvb)
        End If
    End Sub
    Public Sub MyDispose()
        cc.Dispose()
        video.Dispose()
        mog.Dispose()
        flow.Dispose()
    End Sub
End Class




' https://stackoverflow.com/questions/47706339/car-counting-and-classification-using-emgucv-and-vb-net
Public Class Video_MinRect
    Inherits VB_Class
    Public video As Video_Basics
    Public mog As BGSubtract_MOG
    Public externalUse As Boolean
    Public contours As cv.Point()()
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        video = New Video_Basics(ocvb, "Video_MinRect")
        video.srcVideo = ocvb.parms.HomeDir + "Data/CarsDrivingUnderBridge.mp4"
        video.Run(ocvb)

        mog = New BGSubtract_MOG(ocvb, "Video_MinRect")
        mog.externalUse = True
        ocvb.desc = "Find area of car outline - example of using minAreaRect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        video.Run(ocvb)
        If video.image.Empty() = False Then
            mog.src = video.image.Resize(ocvb.color.Size())
            mog.Run(ocvb)

            contours = cv.Cv2.FindContoursAsArray(ocvb.result1, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
            ocvb.result1 = ocvb.result1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            If externalUse = False Then
                For i = 0 To contours.Length - 1
                    Dim minRect = cv.Cv2.MinAreaRect(contours(i))
                    drawRotatedRectangle(minRect, ocvb.result1, cv.Scalar.Red)
                Next
            End If
            ocvb.result2 = video.image.Resize(ocvb.color.Size())
        End If
    End Sub
    Public Sub MyDispose()
        video.Dispose()
        mog.Dispose()
    End Sub
End Class





Public Class Video_MinCircle
    Inherits VB_Class
    Dim input As Video_MinRect
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        input = New Video_MinRect(ocvb, "Video_MinCircle")
        input.externalUse = True
        ocvb.desc = "Find area of car outline - example of using MinEnclosingCircle"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        input.Run(ocvb)

        Dim center As New cv.Point2f
        Dim radius As Single
        If input.contours IsNot Nothing Then
            For i = 0 To input.contours.Length - 1
                cv.Cv2.MinEnclosingCircle(input.contours(i), center, radius)
                ocvb.result1.Circle(center, radius, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
            Next
        End If
    End Sub
    Public Sub MyDispose()
        input.Dispose()
    End Sub
End Class