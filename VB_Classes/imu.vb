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
        plot.maxVal = 10
        plot.minVal = -10
        plot.sliders.TrackBar1.Value = 2
        plot.sliders.TrackBar2.Value = 2

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





Public Class IMU_TimeStamp : Implements IDisposable
    Dim flow As Font_FlowText
    Public Sub New(ocvb As AlgorithmData)
        flow = New Font_FlowText(ocvb)
        flow.externalUse = True
        flow.result1or2 = RESULT1

        ocvb.desc = "Get the timestamp from the IMU"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.IMU_Present Then
            flow.msgs.Add("Timestamp = " + Format(ocvb.parms.IMU_TimeStamp / 1000, "##0.0000000000000") + " seconds")
            flow.Run(ocvb)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        flow.Dispose()
    End Sub
End Class





Public Class IMU_Time : Implements IDisposable
    Dim check As New OptionsCheckbox
    Dim k1 As Kalman_Single
    Dim k2 As Kalman_Single
    Public plot As Plot_OverTime
    Public positiveDelta As Double
    Public smoothedDelta As Double
    Public externalUse As Boolean
    Dim minVal = -20
    Dim maxVal = 20
    Dim lastXdelta As New List(Of Single)
    Public Sub New(ocvb As AlgorithmData)
        check.Setup(ocvb, 1)
        check.Box(0).Text = "Resync the IMU and CPU clocks"
        If ocvb.parms.ShowOptions Then check.Show()

        k1 = New Kalman_Single(ocvb)
        k2 = New Kalman_Single(ocvb)

        plot = New Plot_OverTime(ocvb)
        plot.externalUse = True
        plot.dst = ocvb.result2
        plot.maxVal = maxVal
        plot.minVal = minVal
        plot.sliders.TrackBar1.Value = 4
        plot.sliders.TrackBar2.Value = 4
        plot.backColor = cv.Scalar.Aquamarine
        plot.plotCount = 3

        ocvb.desc = "Measure and plot the time difference from the IMU timestamp to the current time (2 different clocks)."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static syncCount As Integer
        Static myframeCount As Integer
        Static syncCPU As Double
        Static offChartValue As Integer
        Static myStopWatch As New System.Diagnostics.Stopwatch
        If ocvb.frameCount = 0 Then myStopWatch.Start()
        Dim ms = myStopWatch.ElapsedMilliseconds - syncCPU

        Static lastIMUtime = ocvb.parms.IMU_TimeStamp
        Dim imuDelta = ocvb.parms.IMU_TimeStamp - lastIMUtime

        Static timeOffset As Double ' when the IMU clock is ahead of the cpu clock, use the average offset to bump the cpu clock
        If imuDelta > ms Then
            timeOffset = imuDelta - ms
            k1.inputReal = timeOffset
            k1.Run(ocvb)
            timeOffset = k1.stateResult
        End If

        positiveDelta = Math.Max(0, ms + timeOffset - imuDelta) ' it is essential that this value be positive
        Dim rawDelta = ms + timeOffset - imuDelta
        k2.inputReal = positiveDelta
        k2.Run(ocvb)
        smoothedDelta = k2.stateResult
        If smoothedDelta < 1 Then smoothedDelta = 1
        If externalUse = False Then
            plot.plotData = New cv.Scalar(smoothedDelta, 0, rawDelta, 0)
            plot.Run(ocvb)
            lastXdelta.Add(rawDelta)
            If lastXdelta.Count >= ocvb.color.Width Then lastXdelta.Remove(0)

            If rawDelta < minVal Or rawDelta > maxVal Then offChartValue += 1

            ' if enough points are off the charted area, then redo the scale.
            If offChartValue > 50 Then
                ocvb.result2.SetTo(0)
                minVal = Double.MaxValue
                maxVal = Double.MinValue
                For i = 0 To lastXdelta.Count - 1
                    If lastXdelta.Item(i) < minVal Then minVal = lastXdelta.Item(i)
                    If lastXdelta.Item(i) > maxVal Then maxVal = lastXdelta.Item(i)
                Next
                maxVal = CInt(maxVal + 1)
                minVal = CInt(minVal - 1)
                plot.maxVal = maxVal
                plot.minVal = minVal
                lastXdelta.Clear()
                offChartValue = 0
                plot.columnIndex = 0 ' restart at the left side of the chart
            End If

            ocvb.putText(New ActiveClass.TrueType(" IMU timestamp (ms) = " + Format(imuDelta, "#0.0"), 10, 60))
            ocvb.putText(New ActiveClass.TrueType("CPU timestamp (ms) = " + Format(ms, "#0.0"), 10, 80))
            If rawDelta < 0 Then
                ocvb.putText(New ActiveClass.TrueType("Delta ms = " + Format(rawDelta, "00.00") + " Raw data plotted in Red", 10, 100))
            Else
                ocvb.putText(New ActiveClass.TrueType("Delta ms = " + Format(rawDelta, "000.00") + " Raw data plotted in Red", 10, 100))
            End If
            ocvb.putText(New ActiveClass.TrueType("Delta ms = " + Format(smoothedDelta, "000.00") + " forced positive values are smoothed with Kalman filter and plotted in Blue", 10, 120))
            ocvb.putText(New ActiveClass.TrueType("timeOffset ms = " + Format(timeOffset, "000.00") +
                                                  " When CPU and IMU clock difference is negative, the time offset is smoothed with Kalman filter", 10, 140))
            ocvb.putText(New ActiveClass.TrueType("positiveDelta ms = " + Format(positiveDelta, "000.00") + " forced positive Delta ms plotted in Green", 10, 160))
            ocvb.putText(New ActiveClass.TrueType("Off chart count = " + CStr(offChartValue), 10, 180))
            ocvb.putText(New ActiveClass.TrueType("myFrameCount = " + CStr(myframeCount) + " - Use this to reset after x frames", 10, 200))
            syncCount -= 1
            If smoothedDelta > 10 Or myframeCount >= 1000 Or check.Box(0).Checked Or syncCount > 0 Then
                ocvb.putText(New ActiveClass.TrueType("Syncing the IMU and CPU Clocks", 10, 220))
            End If

            ocvb.label1 = "Delta ms: Raw values between " + CStr(minVal) + " and " + CStr(maxVal)
            ocvb.label2 = "Delta ms: Red (raw) Blue (smoothed) Green is zero"
        End If

        ' Clocks drift.  Here we sync up the IMU and CPU clocks by restarting the algorithm.  
        ' We could reset the Kalman object but the effect of the Kalman filter becomes quite apparent as the values shift to normal.
        myframeCount += 1
        If smoothedDelta > 10 Or myframeCount >= 1000 Or check.Box(0).Checked Then
            myframeCount = 0
            check.Box(0).Checked = False
            syncCPU += ms
            lastIMUtime = ocvb.parms.IMU_TimeStamp
            smoothedDelta = 0
            timeOffset = 0
            positiveDelta = 0
            offChartValue = 1000 ' force the update to the scale.
            syncCount = 30 ' show it for the next 30 frames.
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        plot.Dispose()
        k1.Dispose()
        k2.Dispose()
    End Sub
End Class