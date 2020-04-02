Imports cv = OpenCvSharp
' https://github.com/IntelRealSense/librealsense/tree/master/examples/motion
Public Class IMU_Basics : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim lastTimeStamp As Double
    Dim flow As Font_FlowText
    Public theta As cv.Point3f ' this is the description - x, y, and z - of the axes centered in the camera.
    Public gyroAngle As cv.Point3f ' this is the orientation of the gyro.
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "IMU_Basics: Alpha x 1000", 0, 1000, 980)
        If ocvb.parms.ShowOptions Then sliders.Show()

        flow = New Font_FlowText(ocvb)
        flow.externalUse = True
        flow.result1or2 = RESULT1

        ocvb.desc = "Read and display the IMU coordinates"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.IMU_Present Then
            Dim alpha As Double = sliders.TrackBar1.Value / 1000
            If ocvb.frameCount = 0 Then
                lastTimeStamp = ocvb.parms.IMU_TimeStamp
            Else
                gyroAngle = ocvb.parms.IMU_AngularVelocity
                Dim dt_gyro = (ocvb.parms.IMU_TimeStamp - lastTimeStamp) / 1000
                If ocvb.parms.cameraIndex <> D400Cam Then dt_gyro /= 1000 ' different units in the timestamp?
                lastTimeStamp = ocvb.parms.IMU_TimeStamp
                gyroAngle = gyroAngle * dt_gyro
                theta += New cv.Point3f(-gyroAngle.Z, -gyroAngle.Y, gyroAngle.X)
            End If

            ' NOTE: Initialize the angle around the y-axis to zero.
            Dim accelAngle = New cv.Point3f(Math.Atan2(ocvb.parms.IMU_Acceleration.X, Math.Sqrt(ocvb.parms.IMU_Acceleration.Y * ocvb.parms.IMU_Acceleration.Y + ocvb.parms.IMU_Acceleration.Z * ocvb.parms.IMU_Acceleration.Z)), 0,
                                                Math.Atan2(ocvb.parms.IMU_Acceleration.Y, ocvb.parms.IMU_Acceleration.Z))
            If ocvb.frameCount = 0 Then
                theta = accelAngle
            Else
                ' Apply the Complementary Filter:
                '  - high-pass filter = theta * alpha: allows short-duration signals to pass while filtering steady signals (trying to cancel drift)
                '  - low-pass filter = accel * (1 - alpha): lets the long-term changes through, filtering out short term fluctuations
                theta.X = theta.X * alpha + accelAngle.X * (1 - alpha)
                theta.Z = theta.Z * alpha + accelAngle.Z * (1 - alpha)
            End If
            If externalUse = False Then
                flow.msgs.Add("ts = " + Format(ocvb.parms.IMU_TimeStamp, "#0.00") + " Acceleration (m/sec^2) x = " + Format(ocvb.parms.IMU_Acceleration.X, "#0.00") +
                              " y = " + Format(ocvb.parms.IMU_Acceleration.Y, "#0.00") + " z = " + Format(ocvb.parms.IMU_Acceleration.Z, "#0.00") + vbTab +
                              " Motion (rads/sec) pitch = " + Format(ocvb.parms.IMU_AngularVelocity.X, "#0.00") +
                              " Yaw = " + Format(ocvb.parms.IMU_AngularVelocity.Y, "#0.00") + " Roll = " + Format(ocvb.parms.IMU_AngularVelocity.Z, "#0.00"))
            End If
            ocvb.label1 = "theta.x " + Format(theta.X, "#0.000") + " y " + Format(theta.Y, "#0.000") + " z " + Format(theta.Z, "#0.000")
        Else
            If ocvb.frameCount = 0 Then flow.msgs.Add("No IMU present on this device")
        End If
        flow.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        flow.Dispose()
        sliders.Dispose()
    End Sub
End Class






Public Class IMU_Stabilizer : Implements IDisposable
    Dim kalman As Kalman_Basics
    Dim check As New OptionsCheckbox
    Public Sub New(ocvb As AlgorithmData)
        kalman = New Kalman_Basics(ocvb)
        kalman.externalUse = True

        check.Setup(ocvb, 1)
        check.Box(0).Text = "Turn on/off Kalman filtering of IMU data."
        check.Box(0).Checked = True
        If ocvb.parms.ShowOptions Then check.Show()

        ocvb.desc = "Stabilize the image with the IMU data."
        ocvb.label1 = "IMU Stabilize (Move Camera + Select Kalman)"
        ocvb.label2 = "Difference from Color Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.IMU_Present Then
            Dim borderCrop = 5
            Dim vert_Border = borderCrop * ocvb.color.Rows / ocvb.color.Cols
            Dim dx = ocvb.parms.IMU_AngularVelocity.X
            Dim dy = ocvb.parms.IMU_AngularVelocity.Y
            Dim da = ocvb.parms.IMU_AngularVelocity.Z
            Dim sx = 1 ' assume no scaling is taking place.
            Dim sy = 1 ' assume no scaling is taking place.

            If check.Box(0).Checked Then
                kalman.src = {dx, dy, da}
                kalman.Run(ocvb)
                dx = kalman.dst(0)
                dy = kalman.dst(1)
                da = kalman.dst(2)
            End If

            Dim smoothedMat = New cv.Mat(2, 3, cv.MatType.CV_64F)
            smoothedMat.Set(Of Double)(0, 0, sx * Math.Cos(da))
            smoothedMat.Set(Of Double)(0, 1, sx * -Math.Sin(da))
            smoothedMat.Set(Of Double)(1, 0, sy * Math.Sin(da))
            smoothedMat.Set(Of Double)(1, 1, sy * Math.Cos(da))
            smoothedMat.Set(Of Double)(0, 2, dx)
            smoothedMat.Set(Of Double)(1, 2, dy)

            Dim smoothedFrame = ocvb.color.WarpAffine(smoothedMat, ocvb.color.Size())
            smoothedFrame = smoothedFrame(New cv.Range(borderCrop, smoothedFrame.Rows - borderCrop), New cv.Range(borderCrop, smoothedFrame.Cols - borderCrop))
            ocvb.result1 = smoothedFrame.Resize(ocvb.color.Size())
            cv.Cv2.Subtract(ocvb.color, ocvb.result1, ocvb.result2)

            ocvb.result1(New cv.Rect(10, 95, 50, 50)).SetTo(0)
            Dim Text = "dx = " + Format(dx, "#0.00") + vbNewLine + "dy = " + Format(dy, "#0.00") + vbNewLine + "da = " + Format(da, "#0.00")
            ocvb.putText(New ActiveClass.TrueType(Text, 10, 100, RESULT1))
        Else
            ocvb.putText(New ActiveClass.TrueType("No IMU present on this RealSense device", 20, 100))
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        kalman.Dispose()
        check.Dispose()
    End Sub
End Class






Public Class IMU_Magnetometer : Implements IDisposable
    Public plot As Plot_OverTime
    Public Sub New(ocvb As AlgorithmData)
        plot = New Plot_OverTime(ocvb)
        plot.externalUse = True
        plot.dst = ocvb.result2
        plot.maxScale = 10
        plot.minScale = -10

        ocvb.desc = "Get the IMU_Magnetometer values from the IMU (if available)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.IMU_Magnetometer = New cv.Point3f Then
            ocvb.putText(New ActiveClass.TrueType("The IMU for this camera does not have Magnetometer readings.", 10, 125))
        Else
            ocvb.putText(New ActiveClass.TrueType("Uncalibrated IMU Magnetometer reading:  x = " + CStr(ocvb.parms.IMU_Magnetometer.X) + vbCrLf +
                                                  "Uncalibrated IMU Magnetometer reading:  y = " + CStr(ocvb.parms.IMU_Magnetometer.Y) + vbCrLf +
                                                  "Uncalibrated IMU Magnetometer reading:  z = " + CStr(ocvb.parms.IMU_Magnetometer.Z), 10, 60))
            plot.plotData = New cv.Scalar(ocvb.parms.IMU_Magnetometer.X, ocvb.parms.IMU_Magnetometer.Y, ocvb.parms.IMU_Magnetometer.Z)
            plot.Run(ocvb)
            ocvb.label2 = "x (blue) = " + Format(plot.plotData.Item(0), "#0.00") + " y (green) = " + Format(plot.plotData.Item(1), "#0.00") +
                          " z (red) = " + Format(plot.plotData.Item(2), "#0.00")
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        plot.Dispose()
    End Sub
End Class




Public Class IMU_Barometer : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Get the barometric pressure from the IMU (if available)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.IMU_Barometer = 0 Then
            ocvb.putText(New ActiveClass.TrueType("The IMU for this camera does not have barometric pressure.", 10, 125))
        Else
            ocvb.putText(New ActiveClass.TrueType("Barometric pressure is " + CStr(ocvb.parms.IMU_Barometer) + " hectopascal." + vbCrLf +
                                                  "Barometric pressure is " + Format(ocvb.parms.IMU_Barometer * 0.02953, "#0.00") + " inches of mercury.", 10, 60))
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class IMU_Temperature : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Get the temperature of the IMU (if available)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.IMU_Present Then
            ocvb.putText(New ActiveClass.TrueType("IMU Temperature is " + Format(ocvb.parms.IMU_Temperature, "#0.00") + " degrees Celsius." + vbCrLf +
                                                  "IMU Temperature is " + Format(ocvb.parms.IMU_Temperature * 9 / 5 + 32, "#0.00") + " degrees Fahrenheit.", 10, 60))
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class IMU_FrameTime : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public plot As Plot_OverTime
    Public CPUInterval As Double
    Public externalUse As Boolean
    Public IMUtoCaptureEstimate As Double
    Public Sub New(ocvb As AlgorithmData)
        plot = New Plot_OverTime(ocvb)
        plot.externalUse = True
        plot.dst = ocvb.result2
        plot.maxScale = 150
        plot.minScale = 0
        plot.backColor = cv.Scalar.Aquamarine
        plot.plotCount = 4

        sliders.setupTrackBar1(ocvb, "Minimum IMU to Capture time (ms)", 1, 10, 2)
        sliders.setupTrackBar2(ocvb, "Number of Plot Values", 5, 30, 25)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.label2 = "IMU FT (blue) CPU FT (green) Latency est. (red)"
        ocvb.desc = "Use the IMU timestamp to estimate the delay from IMU capture to image capture.  Just an estimate!"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static IMUanchor As Integer = ocvb.parms.IMU_FrameTime
        Static histogramIMU(plot.maxScale) As Integer
        ' there can be some errant times at startup.
        If ocvb.parms.IMU_FrameTime > plot.maxScale Or ocvb.parms.IMU_FrameTime < 0 Then Exit Sub ' skip the crazy values.
        Static imuTotalTime As Double
        imuTotalTime += ocvb.parms.IMU_FrameTime
        If imuTotalTime = 0 Then
            Static allZeroCount As Integer
            allZeroCount += 1
            If allZeroCount > 20 Then
                ocvb.putText(New ActiveClass.TrueType("Is IMU present?  No IMU FrameTimes", 10, 40))
                allZeroCount = Integer.MinValue ' don't show message again.
            End If
            Exit Sub ' if the IMU frametime was 0, then no new IMU data was generated (or it is unsupported!)
        End If

        Dim maxval = Integer.MinValue
        For i = 0 To histogramIMU.Count - 1
            If maxval < histogramIMU(i) Then
                maxval = histogramIMU(i)
                IMUanchor = i
            End If
        Next

        Dim imuFrameTime = CInt(ocvb.parms.IMU_FrameTime)
        If IMUanchor <> 0 Then imuFrameTime = imuFrameTime Mod IMUanchor
        Dim minDelay = sliders.TrackBar1.Value
        IMUtoCaptureEstimate = IMUanchor - imuFrameTime + minDelay
        If IMUtoCaptureEstimate > IMUanchor Then IMUtoCaptureEstimate -= IMUanchor
        If IMUtoCaptureEstimate < minDelay Then IMUtoCaptureEstimate = minDelay

        Static sampledIMUFrameTime = ocvb.parms.IMU_FrameTime
        If ocvb.frameCount Mod 10 = 0 Then sampledIMUFrameTime = ocvb.parms.IMU_FrameTime

        histogramIMU(CInt(ocvb.parms.IMU_FrameTime)) += 1

        If externalUse = False Then
            ocvb.putText(New ActiveClass.TrueType("IMU_TimeStamp (ms) " + Format(ocvb.parms.IMU_TimeStamp, "00") + vbCrLf +
                                                  "CPU TimeStamp (ms) " + Format(ocvb.parms.CPU_TimeStamp, "00") + vbCrLf +
                                                  "IMU Frametime (ms, sampled) " + Format(sampledIMUFrameTime, "000.00") +
                                                  " IMUanchor = " + Format(IMUanchor, "00") +
                                                  " latest = " + Format(ocvb.parms.IMU_FrameTime, "00.00") + vbCrLf +
                                                  "IMUtoCapture (ms, sampled, in red) " + Format(IMUtoCaptureEstimate, "00") + vbCrLf + vbCrLf +
                                                  "IMU Frame Time = Blue" + vbCrLf +
                                                  "Host Frame Time = Green" + vbCrLf +
                                                  "IMU Total Delay = Red" + vbCrLf +
                                                  "IMU Anchor Frame Time = White (IMU Frame Time that occurs most often", 10, 40))

            plot.plotData = New cv.Scalar(ocvb.parms.IMU_FrameTime, ocvb.parms.CPU_FrameTime, IMUtoCaptureEstimate, IMUanchor)
            plot.Run(ocvb)

            If plot.maxScale - plot.minScale > histogramIMU.Count Then ReDim histogramIMU(plot.maxScale - plot.minScale)

            Dim plotLastX = sliders.TrackBar2.Value
            If plot.lastXdelta.Count > plotLastX Then
                Dim allText As String = ""
                For i = 0 To plot.plotCount - 1
                    Dim outStr = "Last " + CStr(plotLastX) + Choose(i + 1, " IMU FrameTime", " CPU Frame Time", " IMUtoCapture ms", " IMU Center time") + vbTab
                    For j = plot.lastXdelta.Count - plotLastX - 1 To plot.lastXdelta.Count - 1
                        outStr += Format(plot.lastXdelta.Item(j).Item(i), "00") + ", "
                    Next
                    allText += outStr + vbCrLf
                Next
                ocvb.putText(New ActiveClass.TrueType(allText, 10, 180))
            End If
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        plot.Dispose()
    End Sub
End Class





Public Class IMU_HostFrameTimes : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public plot As Plot_OverTime
    Public CPUInterval As Double
    Public externalUse As Boolean
    Public HostInterruptDelayEstimate As Double
    Public Sub New(ocvb As AlgorithmData)
        plot = New Plot_OverTime(ocvb)
        plot.externalUse = True
        plot.dst = ocvb.result2
        plot.maxScale = 150
        plot.minScale = 0
        plot.backColor = cv.Scalar.Aquamarine
        plot.plotCount = 4

        sliders.setupTrackBar1(ocvb, "Minimum Host interrupt delay (ms)", 1, 10, 4)
        sliders.setupTrackBar2(ocvb, "Number of Plot Values", 5, 30, 25)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.label2 = "IMU FT (blue) CPU FT (green) Latency est. (red)"
        ocvb.desc = "Use the Host timestamp to estimate the delay from image capture to host interrupt.  Just an estimate!"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static CPUanchor As Integer = ocvb.parms.CPU_FrameTime
        Static hist(plot.maxScale) As Integer
        ' there can be some errant times at startup.
        If ocvb.parms.CPU_FrameTime > plot.maxScale Or ocvb.parms.CPU_FrameTime < 0 Then Exit Sub ' skip the crazy values.

        Dim maxval = Integer.MinValue
        For i = 0 To hist.Count - 1
            If maxval < hist(i) Then
                maxval = hist(i)
                CPUanchor = i
            End If
        Next

        Dim cpuFrameTime = CInt(ocvb.parms.CPU_FrameTime)
        If CPUanchor <> 0 Then cpuFrameTime = cpuFrameTime Mod CPUanchor
        Dim minDelay = sliders.TrackBar1.Value
        HostInterruptDelayEstimate = CPUanchor - cpuFrameTime + minDelay
        If HostInterruptDelayEstimate > CPUanchor Then HostInterruptDelayEstimate -= CPUanchor
        If HostInterruptDelayEstimate < 0 Then HostInterruptDelayEstimate = minDelay

        Static sampledCPUFrameTime = ocvb.parms.CPU_FrameTime
        If ocvb.frameCount Mod 10 = 0 Then sampledCPUFrameTime = ocvb.parms.CPU_FrameTime

        hist(CInt(ocvb.parms.CPU_FrameTime)) += 1

        If externalUse = False Then
            ocvb.putText(New ActiveClass.TrueType("IMU_TimeStamp (ms) " + Format(ocvb.parms.IMU_TimeStamp, "00") + vbCrLf +
                                                  "CPU TimeStamp (ms) " + Format(ocvb.parms.CPU_TimeStamp, "00") + vbCrLf +
                                                  "CPU Frametime (ms, sampled) " + Format(sampledCPUFrameTime, "000.00") +
                                                  " CPUanchor = " + Format(CPUanchor, "00") +
                                                  " latest = " + Format(ocvb.parms.CPU_FrameTime, "00.00") + vbCrLf +
                                                  "Host Interrupt Delay (ms, sampled, in red) " + Format(HostInterruptDelayEstimate, "00") + vbCrLf + vbCrLf +
                                                  "IMU Frame Time = Blue" + vbCrLf +
                                                  "Host Frame Time = Green" + vbCrLf +
                                                  "Host Total Delay (latency) = Red" + vbCrLf +
                                                  "Host Anchor Frame Time = White (Host Frame Time that occurs most often", 10, 40))

            plot.plotData = New cv.Scalar(ocvb.parms.IMU_FrameTime, ocvb.parms.CPU_FrameTime, HostInterruptDelayEstimate, CPUanchor)
            plot.Run(ocvb)

            If plot.maxScale - plot.minScale > hist.Count Then ReDim hist(plot.maxScale - plot.minScale)

            Dim plotLastX = sliders.TrackBar2.Value
            If plot.lastXdelta.Count > plotLastX Then
                Dim allText As String = ""
                For i = 0 To plot.plotCount - 1
                    Dim outStr = "Last " + CStr(plotLastX) + Choose(i + 1, " IMU FrameTime", " CPU Frametime", " Host Delay ms", " CPUanchor FT") + vbTab
                    For j = plot.lastXdelta.Count - plotLastX - 1 To plot.lastXdelta.Count - 1
                        outStr += Format(plot.lastXdelta.Item(j).Item(i), "00") + ", "
                    Next
                    allText += outStr + vbCrLf
                Next
                ocvb.putText(New ActiveClass.TrueType(allText, 10, 180))
            End If
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        plot.Dispose()
    End Sub
End Class




Public Class IMU_TotalDelay : Implements IDisposable
    Dim host As IMU_HostFrameTimes
    Dim imu As IMU_FrameTime
    Dim plot As Plot_OverTime
    Dim kalman As Kalman_Single
    Dim externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        ocvb.parms.ShowOptions = False

        host = New IMU_HostFrameTimes(ocvb)
        host.externalUse = True
        imu = New IMU_FrameTime(ocvb)
        imu.externalUse = True
        kalman = New Kalman_Single(ocvb)
        kalman.externalUse = True

        ocvb.parms.ShowOptions = True ' just show plot options...

        plot = New Plot_OverTime(ocvb)
        plot.externalUse = True
        plot.dst = ocvb.result2
        plot.maxScale = 50
        plot.minScale = 0
        plot.plotCount = 4

        ocvb.label1 = "Timing data - total (white) right image"
        ocvb.label2 = "IMU (blue) host (green) Total delay est. (red)"
        ocvb.desc = "Estimate time from IMU capture to host processing to allow predicting effect of camera motion."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        host.Run(ocvb)
        imu.Run(ocvb)
        Dim totaldelay = host.HostInterruptDelayEstimate + imu.IMUtoCaptureEstimate

        kalman.inputReal = totaldelay
        kalman.Run(ocvb)

        Static sampledCPUDelay = host.HostInterruptDelayEstimate
        Static sampledIMUDelay = imu.IMUtoCaptureEstimate
        Static sampledTotalDelay = totaldelay
        Static sampledSmooth = kalman.stateResult
        If ocvb.frameCount Mod 10 = 0 Then
            sampledCPUDelay = host.HostInterruptDelayEstimate
            sampledIMUDelay = imu.IMUtoCaptureEstimate
            sampledTotalDelay = totaldelay
            sampledSmooth = kalman.stateResult
        End If

        ocvb.putText(New ActiveClass.TrueType("Estimated host delay (ms, sampled) " + Format(sampledCPUDelay, "00") + vbCrLf +
                                              "Estimated IMU delay (ms, sampled) " + Format(sampledIMUDelay, "00") + vbCrLf +
                                              "Estimated Total delay (ms, sampled) " + Format(sampledTotalDelay, "00") + vbCrLf +
                                              "Estimated Total delay Smoothed (ms, sampled, in White) " + Format(sampledSmooth, "00") + vbCrLf + vbCrLf +
                                              "IMU Frame Time = Blue" + vbCrLf +
                                              "Host Frame Time = Green" + vbCrLf +
                                              "Host+IMU Total Delay (latency) = Red" + vbCrLf +
                                              "Host+IMU Anchor Frame Time = White (Host Frame Time that occurs most often", 10, 40))

        plot.plotData = New cv.Scalar(imu.IMUtoCaptureEstimate, host.HostInterruptDelayEstimate, totaldelay, kalman.stateResult)
        plot.Run(ocvb)

        Dim plotLastX = 25
        If plot.lastXdelta.Count > plotLastX Then
            Dim allText As String = ""
            For i = 0 To plot.plotCount - 1
                Dim outStr = "Last " + CStr(plotLastX) + Choose(i + 1, " IMU Delay ", " Host Delay", " Total Delay ms", " Smoothed Total") + vbTab
                For j = plot.lastXdelta.Count - plotLastX - 1 To plot.lastXdelta.Count - 1
                    outStr += Format(plot.lastXdelta.Item(j).Item(i), "00") + ", "
                Next
                allText += outStr + vbCrLf
            Next
            ocvb.putText(New ActiveClass.TrueType(allText, 10, 180))
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        host.Dispose()
        kalman.Dispose()
        imu.Dispose()
    End Sub
End Class






Public Class IMU_AnglesToGravity : Implements IDisposable
    Dim kalman As Kalman_Basics
    Public angleX As Single ' these are all in radians.
    Public angleY As Single
    Public angleZ As Single
    Public Sub New(ocvb As AlgorithmData)
        kalman = New Kalman_Basics(ocvb)
        kalman.externalUse = True
        ocvb.desc = "Find the angle of tilt for the camera with respect to gravity."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ReDim kalman.src(6 - 1)
        kalman.src(0) = ocvb.parms.IMU_Acceleration.X
        kalman.src(1) = ocvb.parms.IMU_Acceleration.Y
        kalman.src(2) = ocvb.parms.IMU_Acceleration.Z
        kalman.src(3) = ocvb.parms.IMU_AngularVelocity.X
        kalman.src(4) = ocvb.parms.IMU_AngularVelocity.Y
        kalman.src(5) = ocvb.parms.IMU_AngularVelocity.Z

        kalman.Run(ocvb)

        Dim rawData As String = "Smoothed Angular Velocity and Acceleration:" + vbCrLf + vbCrLf
        rawData += "ts = " + Format(ocvb.parms.IMU_TimeStamp, "#0.00") + vbCrLf + " Acceleration (m/sec^2)" + vbTab + "x = " + vbTab + Format(kalman.dst(0), "#0.00") + vbTab +
                              " y = " + vbTab + Format(kalman.dst(1), "#0.00") + vbTab + " z = " + vbTab + Format(kalman.dst(2), "#0.00") + vbCrLf +
                              " Motion (rads/sec)" + vbTab + "pitch = " + vbTab + Format(kalman.dst(3), "#0.00") + vbTab +
                              " Yaw = " + vbTab + Format(kalman.dst(4), "#0.00") + vbTab + " Roll = " + vbTab + Format(kalman.dst(4), "#0.00")
        ocvb.putText(New ActiveClass.TrueType(rawData, 10, 30))

        ' to insure that the camera is not moving, yaw, pitch, and roll must be near zero...
        Dim yaw = kalman.dst(4)
        Dim pitch = kalman.dst(3)
        Dim roll = kalman.dst(5)
        Dim outStr As String = ""
        Dim gx = kalman.dst(0)
        Dim gy = kalman.dst(1)
        Dim gz = kalman.dst(2)
        Dim angleX = -Math.Atan2(gx, Math.Sqrt(gy * gy + gz * gz))
        Dim angleY = Math.Atan2(gy, Math.Sqrt(gx * gx + gz * gz))
        Dim angleZ = -Math.Atan2(gz, Math.Sqrt(gx * gx + gy * gy))
        outStr = "IMU Acceleration in X-direction = " + vbTab + vbTab + Format(gx, "#0.0000") + vbCrLf
        outStr += "IMU Acceleration in Y-direction = " + vbTab + vbTab + Format(gy, "#0.0000") + vbCrLf
        outStr += "IMU Acceleration in Z-direction = " + vbTab + vbTab + Format(gz, "#0.0000") + vbCrLf
        outStr += "X-axis Angle from horizontal (in degrees) = " + vbTab + Format(angleX * 57.2958, "#0.0000") + vbCrLf
        outStr += "Y-axis Angle from horizontal (in degrees) = " + vbTab + Format(angleY * 57.2958, "#0.0000") + vbCrLf
        outStr += "Z-axis Angle from horizontal (in degrees) = " + vbTab + Format(angleZ * 57.2958, "#0.0000") + vbCrLf
        ' if there is any significant acceleration other than gravity, it will be detected here.
        If Math.Abs(Math.Sqrt(gx * gx + gy * gy + gz * gz) - 9.807) > 0.05 Then outStr += vbCrLf + "Camera is moving.  Results are not valid."
        ocvb.putText(New ActiveClass.TrueType(outStr, 10, 100))

        ' validate the result
        Dim valstr = "sqrt (" + vbTab + Format(gx, "#0.0000") + "*" + Format(gx, "#0.0000") + vbTab +
                                vbTab + Format(gy, "#0.0000") + "*" + Format(gy, "#0.0000") + vbTab +
                                vbTab + Format(gz, "#0.0000") + "*" + Format(gz, "#0.0000") + " ) = " + vbTab +
                                vbTab + Format(Math.Sqrt(gx * gx + gy * gy + gz * gz), "#0.0000") + vbCrLf +
                                "Should be close to the earth's gravitational constant of 9.807 (or the camera was moving.)"

        ocvb.putText(New ActiveClass.TrueType(valstr, 10, 200))
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        kalman.Dispose()
    End Sub
End Class