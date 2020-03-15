﻿Imports cv = OpenCvSharp
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
                gyroAngle = ocvb.parms.imuGyro
                Dim dt_gyro = (ocvb.parms.IMU_TimeStamp - lastTimeStamp) / 1000
                If ocvb.parms.cameraIndex <> D400Cam Then dt_gyro /= 1000 ' different units in the timestamp?
                lastTimeStamp = ocvb.parms.IMU_TimeStamp
                gyroAngle = gyroAngle * dt_gyro
                theta += New cv.Point3f(-gyroAngle.Z, -gyroAngle.Y, gyroAngle.X)
            End If

            ' NOTE: Initialize the angle around the y-axis to zero.
            Dim accelAngle = New cv.Point3f(Math.Atan2(ocvb.parms.imuAccel.X, Math.Sqrt(ocvb.parms.imuAccel.Y * ocvb.parms.imuAccel.Y + ocvb.parms.imuAccel.Z * ocvb.parms.imuAccel.Z)), 0,
                                                Math.Atan2(ocvb.parms.imuAccel.Y, ocvb.parms.imuAccel.Z))
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
                flow.msgs.Add("ts = " + Format(ocvb.parms.IMU_TimeStamp, "#0.00") + " Gravity (m/sec^2) x = " + Format(ocvb.parms.imuAccel.X, "#0.00") +
                              " y = " + Format(ocvb.parms.imuAccel.Y, "#0.00") + " z = " + Format(ocvb.parms.imuAccel.Z, "#0.00") + vbTab +
                              " Motion (rads/sec) pitch = " + Format(ocvb.parms.imuGyro.X, "#0.00") +
                              " Yaw = " + Format(ocvb.parms.imuGyro.Y, "#0.00") + " Roll = " + Format(ocvb.parms.imuGyro.Z, "#0.00"))
            End If
            ocvb.label1 = "theta.x " + Format(theta.X, "#0.000") + " y " + Format(theta.Y, "#0.000") + " z " + Format(theta.Z, "#0.000")
        Else
            If ocvb.frameCount = 0 Then flow.msgs.Add("No IMU present on this RealSense device")
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
        If ocvb.parms.ShowOptions Then check.Show()

        kalman.plot.sliders.Hide()
        kalman.kPlot.sliders.Hide()

        ocvb.desc = "Stabilize the image with the IMU data."
        ocvb.label1 = "IMU Stabilize (Move Camera + Select Kalman)"
        ocvb.label2 = "Difference from Color Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.IMU_Present Then
            Static savedKalmanCheck = check.Box(0).Checked
            If savedKalmanCheck <> check.Box(0).Checked Then
                kalman.restartRequested = True
                savedKalmanCheck = check.Box(0).Checked
            End If
            Dim borderCrop = 5
            Dim vert_Border = borderCrop * ocvb.color.Rows / ocvb.color.Cols
            Dim dx = ocvb.parms.imuGyro.X
            Dim dy = ocvb.parms.imuGyro.Y
            Dim da = ocvb.parms.imuGyro.Z
            Dim sx = 1 ' assume no scaling is taking place.
            Dim sy = 1 ' assume no scaling is taking place.

            If ocvb.frameCount > 1 And check.Box(0).Checked Then
                kalman.inputReal = New cv.Point3f(dx, dy, da)
                kalman.Run(ocvb)
                dx = kalman.statePoint.X
                dy = kalman.statePoint.Y
                da = kalman.statePoint.Z
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
            ocvb.putText(New ActiveClass.TrueType("Uncalibrated IMU Magnetometer reading:  x = " + CStr(ocvb.parms.IMU_Magnetometer.X), 10, 60))
            ocvb.putText(New ActiveClass.TrueType("Uncalibrated IMU Magnetometer reading:  y = " + CStr(ocvb.parms.IMU_Magnetometer.Y), 10, 80))
            ocvb.putText(New ActiveClass.TrueType("Uncalibrated IMU Magnetometer reading:  z = " + CStr(ocvb.parms.IMU_Magnetometer.Z), 10, 100))
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
            ocvb.putText(New ActiveClass.TrueType("Barometric pressure is " + CStr(ocvb.parms.IMU_Barometer) + " hectopascal.", 10, 60))
            ocvb.putText(New ActiveClass.TrueType("Barometric pressure is " + Format(ocvb.parms.IMU_Barometer * 0.02953, "#0.00") + " inches of mercury.", 10, 90))
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
            ocvb.putText(New ActiveClass.TrueType("IMU Temperature is " + Format(ocvb.parms.IMU_Temperature, "#0.00") + " degrees Celsius.", 10, 60))
            ocvb.putText(New ActiveClass.TrueType("IMU Temperature is " + Format(ocvb.parms.IMU_Temperature * 9 / 5 + 32, "#0.00") + " degrees Fahrenheit.", 10, 80))
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
        IMUtoCaptureEstimate = IMUanchor - imuFrameTime + sliders.TrackBar1.Value
        If IMUtoCaptureEstimate > IMUanchor Then IMUtoCaptureEstimate -= IMUanchor
        If IMUtoCaptureEstimate < 0 Then IMUtoCaptureEstimate = sliders.TrackBar1.Value

        Static sampledIMUFrameTime = ocvb.parms.IMU_FrameTime
        If ocvb.frameCount Mod 10 = 0 Then sampledIMUFrameTime = ocvb.parms.IMU_FrameTime

        histogramIMU(CInt(ocvb.parms.IMU_FrameTime)) += 1

        If externalUse = False Then
            ocvb.putText(New ActiveClass.TrueType("IMU_TimeStamp (ms) " + Format(ocvb.parms.IMU_TimeStamp, "00"), 10, 40))
            ocvb.putText(New ActiveClass.TrueType("CPU TimeStamp (ms) " + Format(ocvb.parms.CPU_TimeStamp, "00"), 10, 60))
            ocvb.putText(New ActiveClass.TrueType("IMU Frametime (ms, sampled) " + Format(sampledIMUFrameTime, "000.00") +
                                                          " IMUanchor = " + Format(IMUanchor, "00") +
                                                          " latest = " + Format(ocvb.parms.IMU_FrameTime, "00.00"), 10, 80))
            ocvb.putText(New ActiveClass.TrueType("IMUtoCapture (ms, sampled, in red) " + Format(IMUtoCaptureEstimate, "00"), 10, 100))

            plot.plotData = New cv.Scalar(ocvb.parms.IMU_FrameTime, ocvb.parms.CPU_FrameTime, IMUtoCaptureEstimate, IMUanchor)
            plot.Run(ocvb)

            If plot.maxScale - plot.minScale > histogramIMU.Count Then ReDim histogramIMU(plot.maxScale - plot.minScale)

            Dim plotLastX = sliders.TrackBar2.Value
            If plot.lastXdelta.Count > plotLastX Then
                Dim y = 200
                For i = 0 To plot.plotCount - 1
                    Dim outStr = "Last " + CStr(plotLastX) + Choose(i + 1, " IMU FrameTime", " CPU Frame Time", " IMUtoCapture ms", " IMU Center time") + vbTab
                    For j = plot.lastXdelta.Count - plotLastX - 1 To plot.lastXdelta.Count - 1
                        outStr += Format(plot.lastXdelta.Item(j).Item(i), "00") + ", "
                    Next
                    ocvb.putText(New ActiveClass.TrueType(outStr, 10, y))
                    y += 20
                Next
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
        HostInterruptDelayEstimate = CPUanchor - cpuFrameTime + sliders.TrackBar1.Value
        If HostInterruptDelayEstimate > CPUanchor Then HostInterruptDelayEstimate -= CPUanchor
        If HostInterruptDelayEstimate < 0 Then HostInterruptDelayEstimate = sliders.TrackBar1.Value

        Static sampledCPUFrameTime = ocvb.parms.CPU_FrameTime
        If ocvb.frameCount Mod 10 = 0 Then sampledCPUFrameTime = ocvb.parms.CPU_FrameTime

        hist(CInt(ocvb.parms.CPU_FrameTime)) += 1

        If externalUse = False Then
            ocvb.putText(New ActiveClass.TrueType("IMU_TimeStamp (ms) " + Format(ocvb.parms.IMU_TimeStamp, "00"), 10, 40))
            ocvb.putText(New ActiveClass.TrueType("CPU TimeStamp (ms) " + Format(ocvb.parms.CPU_TimeStamp, "00"), 10, 60))
            ocvb.putText(New ActiveClass.TrueType("CPU Frametime (ms, sampled) " + Format(sampledCPUFrameTime, "000.00") +
                                                          " CPUanchor = " + Format(CPUanchor, "00") +
                                                          " latest = " + Format(ocvb.parms.CPU_FrameTime, "00.00"), 10, 80))
            ocvb.putText(New ActiveClass.TrueType("Host Interrupt Delay (ms, sampled, in red) " + Format(HostInterruptDelayEstimate, "00"), 10, 100))

            plot.plotData = New cv.Scalar(ocvb.parms.IMU_FrameTime, ocvb.parms.CPU_FrameTime, HostInterruptDelayEstimate, CPUanchor)
            plot.Run(ocvb)

            If plot.maxScale - plot.minScale > hist.Count Then ReDim hist(plot.maxScale - plot.minScale)

            Dim plotLastX = sliders.TrackBar2.Value
            If plot.lastXdelta.Count > plotLastX Then
                Dim y = 200
                For i = 0 To plot.plotCount - 1
                    Dim outStr = "Last " + CStr(plotLastX) + Choose(i + 1, " IMU FrameTime", " CPU Frametime", " Host Delay ms", " CPUanchor FT") + vbTab
                    For j = plot.lastXdelta.Count - plotLastX - 1 To plot.lastXdelta.Count - 1
                        outStr += Format(plot.lastXdelta.Item(j).Item(i), "00") + ", "
                    Next
                    ocvb.putText(New ActiveClass.TrueType(outStr, 10, y))
                    y += 20
                Next
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
    Dim k1 As Kalman_Single
    Dim externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        ocvb.parms.ShowOptions = False

        host = New IMU_HostFrameTimes(ocvb)
        host.externalUse = True
        imu = New IMU_FrameTime(ocvb)
        imu.externalUse = True
        k1 = New Kalman_Single(ocvb)

        ocvb.parms.ShowOptions = True ' just show plot options...

        plot = New Plot_OverTime(ocvb)
        plot.externalUse = True
        plot.dst = ocvb.result2
        plot.maxScale = 50
        plot.minScale = 0
        plot.backColor = cv.Scalar.Aquamarine
        plot.plotCount = 4

        ocvb.label1 = "Timing data - total (white) right image"
        ocvb.label2 = "IMU (blue) host (green) Total delay est. (red)"
        ocvb.desc = "Estimate time from IMU capture to host processing to allow predicting effect of camera motion."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        host.Run(ocvb)
        imu.Run(ocvb)
        Dim totaldelay = host.HostInterruptDelayEstimate + imu.IMUtoCaptureEstimate

        k1.inputReal = totaldelay
        k1.Run(ocvb)

        Static sampledCPUDelay = host.HostInterruptDelayEstimate
        Static sampledIMUDelay = imu.IMUtoCaptureEstimate
        Static sampledTotalDelay = totaldelay
        Static sampledSmooth = k1.stateResult
        If ocvb.frameCount Mod 10 = 0 Then
            sampledCPUDelay = host.HostInterruptDelayEstimate
            sampledIMUDelay = imu.IMUtoCaptureEstimate
            sampledTotalDelay = totaldelay
            sampledSmooth = k1.stateResult
        End If

        ocvb.putText(New ActiveClass.TrueType("Estimated host delay (ms, sampled) " + Format(sampledCPUDelay, "00"), 10, 40))
        ocvb.putText(New ActiveClass.TrueType("Estimated IMU delay (ms, sampled) " + Format(sampledIMUDelay, "00"), 10, 60))
        ocvb.putText(New ActiveClass.TrueType("Estimated Total delay (ms, sampled) " + Format(sampledTotalDelay, "00"), 10, 80))
        ocvb.putText(New ActiveClass.TrueType("Estimated Total delay Smoothed (ms, sampled, in White) " + Format(sampledSmooth, "00"), 10, 100))

        plot.plotData = New cv.Scalar(imu.IMUtoCaptureEstimate, host.HostInterruptDelayEstimate, totaldelay, k1.stateResult)
        plot.Run(ocvb)

        Dim plotLastX = 25
        If plot.lastXdelta.Count > plotLastX Then
            Dim y = 200
            For i = 0 To plot.plotCount - 1
                Dim outStr = "Last " + CStr(plotLastX) + Choose(i + 1, " IMU Delay ", " Host Delay", " Total Delay ms", " Smoothed Total") + vbTab
                For j = plot.lastXdelta.Count - plotLastX - 1 To plot.lastXdelta.Count - 1
                    outStr += Format(plot.lastXdelta.Item(j).Item(i), "00") + ", "
                Next
                ocvb.putText(New ActiveClass.TrueType(outStr, 10, y))
                y += 20
            Next
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        host.Dispose()
        imu.Dispose()
    End Sub
End Class
