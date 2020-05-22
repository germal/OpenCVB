Imports cv = OpenCvSharp
Imports System.IO
' https://stackoverflow.com/questions/47706339/car-counting-and-classification-using-emgucv-and-vb-net
Public Class Video_Basics
    Inherits ocvbClass
    Public srcVideo As String
    Public image As New cv.Mat
    Dim currVideo As String
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        If srcVideo = "" Then srcVideo = ocvb.parms.HomeDir + "Data\CarsDrivingUnderBridge.mp4" ' default video...
        currVideo = srcVideo
        videoOptions.NewVideo(ocvb, srcVideo)

        label1 = srcVideo
        ocvb.desc = "Show a video file"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If srcVideo <> currVideo Then
            currVideo = srcVideo
            videoOptions.NewVideo(ocvb, currVideo)
        End If
        image = videoOptions.nextImage
        If image.Empty() = False Then dst1 = image.Resize(ocvb.color.Size())
    End Sub
End Class




' https://stackoverflow.com/questions/47706339/car-counting-and-classification-using-emgucv-and-vb-net
Public Class Video_CarCounting
    Inherits ocvbClass
    Dim flow As Font_FlowText
    Dim video As Video_Basics
    Dim mog As BGSubtract_MOG
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        mog = New BGSubtract_MOG(ocvb, caller)

        video = New Video_Basics(ocvb, caller)

        flow = New Font_FlowText(ocvb, caller)
        flow.result1or2 = RESULT1

        ocvb.desc = "Count cars in a video file"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        video.Run(ocvb)
        If video.dst1.Empty() = False Then
            mog.src = video.image
            mog.Run(ocvb)
            dst1 = mog.gray
            dst2 = video.dst1

            ' there are 5 lanes of traffic so setup 5 regions
            ' NOTE: if long shadows are present this approach will not work without provision for the width of a car.  Needs more sample data.
            Dim activeHeight = 30
            Dim finishLine = mog.dst1.Height - activeHeight * 8
            Static activeState(5) As Boolean
            Static carCount As Int32
            For i = 1 To activeState.Length - 1
                Dim lane = New cv.Rect(Choose(i, 230, 460, 680, 900, 1110), finishLine, 40, activeHeight)
                Dim cellCount = dst1(lane).CountNonZero()
                If cellCount Then
                    activeState(i) = True
                    dst1.Rectangle(lane, cv.Scalar.Red, -1)
                    dst2.Rectangle(lane, cv.Scalar.Red, -1)
                End If
                If cellCount = 0 And activeState(i) = True Then
                    activeState(i) = False
                    carCount += 1
                End If
                dst2.Rectangle(lane, cv.Scalar.White, 2)
            Next

            Dim tmp = dst1.Clone()
            flow.msgs.Add("  Cars " + CStr(carCount))
            flow.Run(ocvb)
            cv.Cv2.BitwiseOr(dst1, tmp, dst1)
        End If
    End Sub
End Class




' https://stackoverflow.com/questions/47706339/car-counting-and-classification-using-emgucv-and-vb-net
Public Class Video_CarCComp
    Inherits ocvbClass
    Dim cc As CComp_Basics
    Dim flow As Font_FlowText
    Dim video As Video_Basics
    Dim mog As BGSubtract_MOG
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        mog = New BGSubtract_MOG(ocvb, caller)

        cc = New CComp_Basics(ocvb, caller)

        video = New Video_Basics(ocvb, caller)

        flow = New Font_FlowText(ocvb, caller)
        flow.result1or2 = RESULT1

        ocvb.desc = "Outline cars with a rectangle"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        video.Run(ocvb)
        If video.dst1.Empty() = False Then
            mog.src = video.dst1
            mog.Run(ocvb)
            cc.src = mog.dst1
            cc.Run(ocvb)
            dst1 = cc.dst1
            dst2 = cc.dst2
        End If
    End Sub
End Class




' https://stackoverflow.com/questions/47706339/car-counting-and-classification-using-emgucv-and-vb-net
Public Class Video_MinRect
    Inherits ocvbClass
    Public video As Video_Basics
    Public mog As BGSubtract_MOG
    Public contours As cv.Point()()
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        video = New Video_Basics(ocvb, caller)
        video.srcVideo = ocvb.parms.HomeDir + "Data/CarsDrivingUnderBridge.mp4"
        video.Run(ocvb)

        mog = New BGSubtract_MOG(ocvb, caller)
        ocvb.desc = "Find area of car outline - example of using minAreaRect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        video.Run(ocvb)
        If video.dst1.Empty() = False Then
            mog.src = video.dst1
            mog.Run(ocvb)

            contours = cv.Cv2.FindContoursAsArray(mog.dst1, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
            dst1 = mog.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            If standalone Then
                For i = 0 To contours.Length - 1
                    Dim minRect = cv.Cv2.MinAreaRect(contours(i))
                    drawRotatedRectangle(minRect, dst1, cv.Scalar.Red)
                Next
            End If
            dst2 = video.dst1
        End If
    End Sub
End Class





Public Class Video_MinCircle
    Inherits ocvbClass
    Dim input As Video_MinRect
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        input = New Video_MinRect(ocvb, caller)
        ocvb.desc = "Find area of car outline - example of using MinEnclosingCircle"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        input.Run(ocvb)
        dst1 = input.dst1
        dst2 = input.dst2

        Dim center As New cv.Point2f
        Dim radius As Single
        If input.contours IsNot Nothing Then
            For i = 0 To input.contours.Length - 1
                cv.Cv2.MinEnclosingCircle(input.contours(i), center, radius)
                dst1.Circle(center, radius, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
            Next
        End If
    End Sub
End Class
