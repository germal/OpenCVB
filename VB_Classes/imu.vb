Imports cv = OpenCvSharp
' https://github.com/IntelRealSense/librealsense/tree/master/examples/motion
Public Class IMU_Basics
    Inherits ocvbClass
    Dim lastTimeStamp As Double
    Dim flow As Font_FlowText
    Public theta As cv.Point3f ' this is the description - x, y, and z - of the axes centered in the camera.
    Public gyroAngle As cv.Point3f ' this is the orientation of the gyro.
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "IMU_Basics: Alpha x 1000", 0, 1000, 980)

        flow = New Font_FlowText(ocvb)
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
                If ocvb.parms.cameraIndex <> D435i Then dt_gyro /= 1000 ' different units in the timestamp?
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
            If standalone Then
                flow.msgs.Add("ts = " + Format(ocvb.parms.IMU_TimeStamp, "#0.00") + " Acceleration (m/sec^2) x = " + Format(ocvb.parms.IMU_Acceleration.X, "#0.00") +
                              " y = " + Format(ocvb.parms.IMU_Acceleration.Y, "#0.00") + " z = " + Format(ocvb.parms.IMU_Acceleration.Z, "#0.00") + vbTab +
                              " Motion (rads/sec) pitch = " + Format(ocvb.parms.IMU_AngularVelocity.X, "#0.00") +
                              " Yaw = " + Format(ocvb.parms.IMU_AngularVelocity.Y, "#0.00") + " Roll = " + Format(ocvb.parms.IMU_AngularVelocity.Z, "#0.00"))
            End If
            label1 = "theta.x " + Format(theta.X, "#0.000") + " y " + Format(theta.Y, "#0.000") + " z " + Format(theta.Z, "#0.000")
        Else
            If ocvb.frameCount = 0 Then flow.msgs.Add("No IMU present on this device")
        End If
        flow.Run(ocvb)
    End Sub
End Class






Public Class IMU_Stabilizer
    Inherits ocvbClass
    Dim kalman As Kalman_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        kalman = New Kalman_Basics(ocvb)

        ocvb.desc = "Stabilize the image with the IMU data."
        label1 = "IMU Stabilize (Move Camera + Select Kalman)"
        label2 = "Difference from Color Image"
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

            kalman.input = {dx, dy, da}
            kalman.Run(ocvb)
            dx = kalman.output(0)
            dy = kalman.output(1)
            da = kalman.output(2)

            Dim smoothedMat = New cv.Mat(2, 3, cv.MatType.CV_64F)
            smoothedMat.Set(Of Double)(0, 0, sx * Math.Cos(da))
            smoothedMat.Set(Of Double)(0, 1, sx * -Math.Sin(da))
            smoothedMat.Set(Of Double)(1, 0, sy * Math.Sin(da))
            smoothedMat.Set(Of Double)(1, 1, sy * Math.Cos(da))
            smoothedMat.Set(Of Double)(0, 2, dx)
            smoothedMat.Set(Of Double)(1, 2, dy)

            Dim smoothedFrame = ocvb.color.WarpAffine(smoothedMat, ocvb.color.Size())
            smoothedFrame = smoothedFrame(New cv.Range(borderCrop, smoothedFrame.Rows - borderCrop), New cv.Range(borderCrop, smoothedFrame.Cols - borderCrop))
            dst1 = smoothedFrame.Resize(ocvb.color.Size())
            cv.Cv2.Subtract(ocvb.color, dst1, dst2)

            dst1(New cv.Rect(10, 95, 50, 50)).SetTo(0)
            Dim Text = "dx = " + Format(dx, "#0.00") + vbNewLine + "dy = " + Format(dy, "#0.00") + vbNewLine + "da = " + Format(da, "#0.00")
            ocvb.putText(New TTtext(Text, 10, 100, RESULT1))
        Else
            ocvb.putText(New TTtext("No IMU present on this RealSense device", 20, 100))
        End If
    End Sub
End Class






Public Class IMU_Magnetometer
    Inherits ocvbClass
    Public plot As Plot_OverTime
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        plot = New Plot_OverTime(ocvb)
        plot.dst1 = dst2
        plot.maxScale = 10
        plot.minScale = -10

        ocvb.desc = "Get the IMU_Magnetometer values from the IMU (if available)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.IMU_Magnetometer = New cv.Point3f Then
            ocvb.putText(New TTtext("The IMU for this camera does not have Magnetometer readings.", 10, 125))
        Else
            ocvb.putText(New TTtext("Uncalibrated IMU Magnetometer reading:  x = " + CStr(ocvb.parms.IMU_Magnetometer.X) + vbCrLf +
                                                  "Uncalibrated IMU Magnetometer reading:  y = " + CStr(ocvb.parms.IMU_Magnetometer.Y) + vbCrLf +
                                                  "Uncalibrated IMU Magnetometer reading:  z = " + CStr(ocvb.parms.IMU_Magnetometer.Z), 10, 60))
            plot.plotData = New cv.Scalar(ocvb.parms.IMU_Magnetometer.X, ocvb.parms.IMU_Magnetometer.Y, ocvb.parms.IMU_Magnetometer.Z)
            plot.Run(ocvb)
            label2 = "x (blue) = " + Format(plot.plotData.Item(0), "#0.00") + " y (green) = " + Format(plot.plotData.Item(1), "#0.00") +
                          " z (red) = " + Format(plot.plotData.Item(2), "#0.00")
        End If
    End Sub
End Class




Public Class IMU_Barometer
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        ocvb.desc = "Get the barometric pressure from the IMU (if available)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.IMU_Barometer = 0 Then
            ocvb.putText(New TTtext("The IMU for this camera does not have barometric pressure.", 10, 125))
        Else
            ocvb.putText(New TTtext("Barometric pressure is " + CStr(ocvb.parms.IMU_Barometer) + " hectopascal." + vbCrLf +
                                                  "Barometric pressure is " + Format(ocvb.parms.IMU_Barometer * 0.02953, "#0.00") + " inches of mercury.", 10, 60))
        End If
    End Sub
End Class




Public Class IMU_Temperature
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        ocvb.desc = "Get the temperature of the IMU (if available)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.IMU_Present Then
            ocvb.putText(New TTtext("IMU Temperature is " + Format(ocvb.parms.IMU_Temperature, "#0.00") + " degrees Celsius." + vbCrLf +
                                                  "IMU Temperature is " + Format(ocvb.parms.IMU_Temperature * 9 / 5 + 32, "#0.00") + " degrees Fahrenheit.", 10, 60))
        End If
    End Sub
End Class




Public Class IMU_FrameTime
    Inherits ocvbClass
    Public plot As Plot_OverTime
    Public CPUInterval As Double
    Public IMUtoCaptureEstimate As Double
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        plot = New Plot_OverTime(ocvb)
        plot.dst1 = dst2
        plot.maxScale = 150
        plot.minScale = 0
        plot.backColor = cv.Scalar.Aquamarine
        plot.plotCount = 4

        sliders.setupTrackBar1(ocvb, caller, "Minimum IMU to Capture time (ms)", 1, 10, 2)
        sliders.setupTrackBar2("Number of Plot Values", 5, 30, 25)

        label2 = "IMU FT (blue) Host FT (green) Latency est. (red)"
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
                ocvb.putText(New TTtext("Is IMU present?  No IMU FrameTimes", 10, 40))
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

        If standalone Then
            ocvb.putText(New TTtext("IMU_TimeStamp (ms) " + Format(ocvb.parms.IMU_TimeStamp, "00") + vbCrLf +
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
                    Dim outStr = "Last " + CStr(plotLastX) + Choose(i + 1, " IMU FrameTime", " Host Frame Time", " IMUtoCapture ms", " IMU Center time") + vbTab
                    For j = plot.lastXdelta.Count - plotLastX - 1 To plot.lastXdelta.Count - 1
                        outStr += Format(plot.lastXdelta.Item(j).Item(i), "00") + ", "
                    Next
                    allText += outStr + vbCrLf
                Next
                ocvb.putText(New TTtext(allText, 10, 180))
            End If
        End If
    End Sub
End Class





Public Class IMU_HostFrameTimes
    Inherits ocvbClass
    Public plot As Plot_OverTime
    Public CPUInterval As Double
    Public HostInterruptDelayEstimate As Double
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        plot = New Plot_OverTime(ocvb)
        plot.dst1 = dst2
        plot.maxScale = 150
        plot.minScale = 0
        plot.backColor = cv.Scalar.Aquamarine
        plot.plotCount = 4

        sliders.setupTrackBar1(ocvb, caller, "Minimum Host interrupt delay (ms)", 1, 10, 4)
        sliders.setupTrackBar2("Number of Plot Values", 5, 30, 25)

        label2 = "IMU FT (blue) Host FT (green) Latency est. (red)"
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

        If standalone Then
            ocvb.putText(New TTtext("IMU_TimeStamp (ms) " + Format(ocvb.parms.IMU_TimeStamp, "00") + vbCrLf +
                                                  "CPU TimeStamp (ms) " + Format(ocvb.parms.CPU_TimeStamp, "00") + vbCrLf +
                                                  "Host Frametime (ms, sampled) " + Format(sampledCPUFrameTime, "000.00") +
                                                  " CPUanchor = " + Format(CPUanchor, "00") +
                                                  " latest = " + Format(ocvb.parms.CPU_FrameTime, "00.00") + vbCrLf +
                                                  "Host Interrupt Delay (ms, sampled, in red) " + Format(HostInterruptDelayEstimate, "00") + vbCrLf + vbCrLf +
                                                  "Blue" + vbTab + "IMU Frame Time" + vbCrLf +
                                                  "Green" + vbTab + "Host Frame Time" + vbCrLf +
                                                  "Red" + vbTab + "Host Total Delay (latency)" + vbCrLf +
                                                  "White" + vbTab + "Host Anchor Frame Time (Host Frame Time that occurs most often", 10, 40))

            plot.plotData = New cv.Scalar(ocvb.parms.IMU_FrameTime, ocvb.parms.CPU_FrameTime, HostInterruptDelayEstimate, CPUanchor)
            plot.Run(ocvb)

            If plot.maxScale - plot.minScale > hist.Count Then ReDim hist(plot.maxScale - plot.minScale)

            Dim plotLastX = sliders.TrackBar2.Value
            If plot.lastXdelta.Count > plotLastX Then
                Dim allText As String = ""
                For i = 0 To plot.plotCount - 1
                    Dim outStr = "Last " + CStr(plotLastX) + Choose(i + 1, " IMU FrameTime", " Host Frametime", " Host Delay ms", " CPUanchor FT") + vbTab
                    For j = plot.lastXdelta.Count - plotLastX - 1 To plot.lastXdelta.Count - 1
                        outStr += Format(plot.lastXdelta.Item(j).Item(i), "00") + ", "
                    Next
                    allText += outStr + vbCrLf
                Next
                ocvb.putText(New TTtext(allText, 10, 180))
            End If
        End If
    End Sub
End Class




Public Class IMU_TotalDelay
    Inherits ocvbClass
    Dim host As IMU_HostFrameTimes
    Dim imu As IMU_FrameTime
    Dim plot As Plot_OverTime
    Dim kalman As Kalman_Single
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        ocvb.parms.ShowOptions = False

        host = New IMU_HostFrameTimes(ocvb)
        imu = New IMU_FrameTime(ocvb)
        kalman = New Kalman_Single(ocvb)

        ocvb.parms.ShowOptions = True ' just show plot options...

        plot = New Plot_OverTime(ocvb)
        plot.dst1 = dst2
        plot.maxScale = 50
        plot.minScale = 0
        plot.plotCount = 4

        label1 = "Timing data - total (white) right image"
        label2 = "IMU (blue) host (green) Total delay est. (red)"
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

        ocvb.putText(New TTtext("Estimated host delay (ms, sampled) " + Format(sampledCPUDelay, "00") + vbCrLf +
                                              "Estimated IMU delay (ms, sampled) " + Format(sampledIMUDelay, "00") + vbCrLf +
                                              "Estimated Total delay (ms, sampled) " + Format(sampledTotalDelay, "00") + vbCrLf +
                                              "Estimated Total delay Smoothed (ms, sampled, in White) " + Format(sampledSmooth, "00") + vbCrLf + vbCrLf +
                                              "Blue" + vbTab + "IMU Frame Time" + vbCrLf +
                                              "Green" + vbTab + "Host Frame Time" + vbCrLf +
                                              "Red" + vbTab + "Host+IMU Total Delay (latency)" + vbCrLf +
                                              "White" + vbTab + "Host+IMU Anchor Frame Time (Host Frame Time that occurs most often)", 10, 40))

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
            ocvb.putText(New TTtext(allText, 10, 180))
        End If
    End Sub
End Class






Public Class IMU_GVector
    Inherits ocvbClass
    Dim kalman As Kalman_Basics
    Public angleX As Single ' in radians.
    Public angleY As Single ' in radians.
    Public angleZ As Single ' in radians.
    Public result As Integer = RESULT1
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        kalman = New Kalman_Basics(ocvb)
        ReDim kalman.input(6 - 1)

        ocvb.desc = "Find the angle of tilt for the camera with respect to gravity."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gx = ocvb.parms.IMU_Acceleration.X
        Dim gy = ocvb.parms.IMU_Acceleration.Y
        Dim gz = ocvb.parms.IMU_Acceleration.Z

        angleX = Math.Atan2(gy, gx) + cv.Cv2.PI / 2
        angleY = Math.Atan2(gx, gy) - cv.Cv2.PI / 2
        angleZ = Math.Atan2(gy, gz) + cv.Cv2.PI / 2

        kalman.input = {gx, gy, gz, angleX, angleY, angleZ}

        If kalman.check.Box(0).Checked Then
            kalman.Run(ocvb)
            gx = kalman.output(0)
            gy = kalman.output(1)
            gz = kalman.output(2)

            angleX = kalman.output(3)
            angleY = kalman.output(4)
            angleZ = kalman.output(5)
        End If

        If standalone Then
            Dim outStr As String = "Acceleration and their angles are smoothed with a Kalman filters:" + vbCrLf + vbCrLf
            outStr = "IMU Acceleration in X-direction = " + vbTab + vbTab + Format(gx, "#0.0000") + vbCrLf
            outStr += "IMU Acceleration in Y-direction = " + vbTab + vbTab + Format(gy, "#0.0000") + vbCrLf
            outStr += "IMU Acceleration in Z-direction = " + vbTab + vbTab + Format(gz, "#0.0000") + vbCrLf + vbCrLf
            outStr += "X-axis Angle from horizontal (in degrees) = " + vbTab + Format(angleX * 57.2958, "#0.0000") + vbCrLf
            outStr += "Y-axis Angle from horizontal (in degrees) = " + vbTab + Format(angleY * 57.2958, "#0.0000") + vbCrLf
            outStr += "Z-axis Angle from horizontal (in degrees) = " + vbTab + Format(angleZ * 57.2958, "#0.0000") + vbCrLf + vbCrLf
            ' if there is any significant acceleration other than gravity, it will be detected here.
            If Math.Abs(Math.Sqrt(gx * gx + gy * gy + gz * gz) - 9.807) > 0.05 Then
                outStr += vbCrLf + "Camera appears to be moving because the gravity vector is not 9.8.  Results may not be valid." + vbCrLf
            End If
            ocvb.putText(New TTtext(outStr, 10, 40, result))

            ' validate the result
            Dim valstr = "sqrt (" + vbTab + Format(gx, "#0.0000") + "*" + Format(gx, "#0.0000") + vbTab +
                            vbTab + Format(gy, "#0.0000") + "*" + Format(gy, "#0.0000") + vbTab +
                            vbTab + Format(gz, "#0.0000") + "*" + Format(gz, "#0.0000") + " ) = " + vbTab +
                            vbTab + Format(Math.Sqrt(gx * gx + gy * gy + gz * gz), "#0.0000") + vbCrLf +
                            "Should be close to the earth's gravitational constant of 9.807 (or the camera was moving.)"

            ocvb.putText(New TTtext(valstr, 10, 200, result))
        End If
    End Sub
End Class

