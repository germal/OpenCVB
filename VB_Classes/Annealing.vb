Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions

Module Annealing_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Annealing_Basics_Open(cityPositions As IntPtr, numberOfCities As Int32) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Annealing_Basics_Close(saPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Annealing_Basics_Run(saPtr As IntPtr, cityOrder As IntPtr, numberOfCities As Int32) As IntPtr
    End Function
End Module




Public Class Annealing_Basics_CPP
    Inherits ocvbClass
    Public numberOfCities As Int32 = 25
    Public restartComputation As Boolean
    Public msg As String

    Public cityPositions() As cv.Point2f
    Public cityOrder() As Int32

    Public energy As Single
    Public closed As Boolean
    Public circularPattern As Boolean = True
    Dim saPtr As IntPtr
    Public Sub drawMap(ocvb As AlgorithmData)
        For i = 0 To cityOrder.Length - 1
            dst1.Circle(cityPositions(i), 5, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
            dst1.Line(cityPositions(i), cityPositions(cityOrder(i)), cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        Next
        cv.Cv2.PutText(dst1, "Energy", New cv.Point(10, 100), ocvb.bestOpenCVFont, ocvb.bestOpenCVFontSize, cv.Scalar.Yellow, 1, cv.LineTypes.AntiAlias)
        cv.Cv2.PutText(dst1, Format(energy, "#0"), New cv.Point(10, 160), ocvb.bestOpenCVFont, ocvb.bestOpenCVFontSize, cv.Scalar.Yellow, 1, cv.LineTypes.AntiAlias)
    End Sub
    Public Sub setup(ocvb As AlgorithmData)
        ReDim cityOrder(numberOfCities - 1)

        Dim radius = ocvb.color.Height * 0.45
        Dim center = New cv.Point(ocvb.color.Width / 2, ocvb.color.Height / 2)
        If circularPattern Then
            ReDim cityPositions(numberOfCities - 1)
            Dim gen As New System.Random()
            Dim r As New cv.RNG(gen.Next(0, 100))
            For i = 0 To cityPositions.Length - 1
                Dim theta = r.Uniform(0, 360)
                cityPositions(i).X = radius * Math.Cos(theta) + center.X
                cityPositions(i).Y = radius * Math.Sin(theta) + center.Y
                cityOrder(i) = (i + 1) Mod numberOfCities
            Next
        End If
        For i = 0 To cityOrder.Length - 1
            cityOrder(i) = (i + 1) Mod numberOfCities
        Next
        dst1 = New cv.Mat(ocvb.color.Size, cv.MatType.CV_8UC3, 0)
    End Sub
    Public Sub Open()
        Dim hCityPosition = GCHandle.Alloc(cityPositions, GCHandleType.Pinned)
        saPtr = Annealing_Basics_Open(hCityPosition.AddrOfPinnedObject(), numberOfCities)
        hCityPosition.Free()
        closed = False
    End Sub
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        setup(ocvb)
        ocvb.desc = "Simulated annealing with traveling salesman.  NOTE: No guarantee simulated annealing will find the optimal solution."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Then
            If ocvb.frameCount = 0 Then Open()
        End If
        Dim saveCityOrder = cityOrder.Clone()
        Dim hCityOrder = GCHandle.Alloc(cityOrder, GCHandleType.Pinned)
        Dim out As IntPtr = Annealing_Basics_Run(saPtr, hCityOrder.AddrOfPinnedObject, cityPositions.Length)
        hCityOrder.Free()
        msg = Marshal.PtrToStringAnsi(out)
        Dim split As String() = Regex.Split(msg, "\W+")
        energy = CSng(split(split.Length - 2) + "." + split(split.Length - 1))

        Dim changed As Boolean
        For i = 0 To cityOrder.Length - 1
            If saveCityOrder(i) <> cityOrder(i) Then
                changed = True
                Exit For
            End If
        Next

        drawMap(ocvb)

        If restartComputation Or InStr(msg, "temp=0.000") Or InStr(msg, "changesApplied=0 temp") Then
            Annealing_Basics_Close(saPtr)
            restartComputation = False
            If standalone Then
                setup(ocvb)
                Open()
            End If
            closed = True
        End If
    End Sub
    Public Sub Close()
        Annealing_Basics_Close(saPtr)
    End Sub
End Class





Public Class Annealing_CPP_MT
    Inherits ocvbClass
    Dim random As Random_Points
    Dim anneal(35) As Annealing_Basics_CPP
    Dim mats As Mat_4to1
    Dim flow As Font_FlowText
    Private Class CompareEnergy : Implements IComparer(Of Single)
        Public Function Compare(ByVal a As Single, ByVal b As Single) As Integer Implements IComparer(Of Single).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return -1
            Return 1
        End Function
    End Class
    Private Sub setup(ocvb As AlgorithmData)
        random.sliders.TrackBar1.Value = sliders.TrackBar1.Value
        random.Run(ocvb) ' get the city positions (may or may not be used below.)

        anneal(0) = New Annealing_Basics_CPP(ocvb, caller)
        anneal(0).numberOfCities = sliders.TrackBar1.Value
        anneal(0).circularPattern = check.Box(2).Checked
        If check.Box(2).Checked = False Then anneal(0).cityPositions = random.Points2f.Clone()
        anneal(0).setup(ocvb)
        anneal(0).Open() ' this will initialize the C++ copy of the city positions.
        For i = 1 To anneal.Length - 1
            anneal(i) = New Annealing_Basics_CPP(ocvb, caller)
            anneal(i).numberOfCities = sliders.TrackBar1.Value
            anneal(i).setup(ocvb)
            anneal(i).cityPositions = anneal(0).cityPositions.Clone() ' duplicate for all threads - working on the same set of points.
            anneal(i).Open() ' this will initialize the C++ copy of the city positions.
        Next
        Static startTime As DateTime
        Dim timeSpent = Now.Subtract(startTime)
        If timeSpent.TotalSeconds < 10000 Then Console.WriteLine("time spent on last problem = " + Format(timeSpent.TotalSeconds, "#0.0") + " seconds.")
        startTime = Now
    End Sub

    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        random = New Random_Points(ocvb, caller)
        random.sliders.Visible = False

        mats = New Mat_4to1(ocvb, caller)

        sliders.setupTrackBar1(ocvb, caller, "Anneal Number of Cities", 5, 500, 25)
        sliders.setupTrackBar2(ocvb, caller, "Success = top X threads agree on energy level.", 2, anneal.Count, anneal.Count)

        check.Setup(ocvb, caller, 3)
        check.Box(0).Text = "Restart TravelingSalesman"
        check.Box(1).Text = "Copy Best Intermediate solutions (top half) to Bottom Half"
        check.Box(1).Checked = True
        check.Box(2).Text = "Circular pattern of cities (allows you to visually check if successful.)"
        check.Box(2).Checked = True

        flow = New Font_FlowText(ocvb, caller)
        flow.result1or2 = RESULT1

        label1 = "Log of Annealing progress"
        label2 = "Top 2 are best solutions, bottom 2 are worst."

        setup(ocvb)
        ocvb.desc = "Setup and control finding the optimal route for a traveling salesman"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount < 10 Then Exit Sub
        If anneal(0).numberOfCities <> sliders.TrackBar1.Value Or check.Box(0).Checked Or check.Box(2).Checked <> anneal(0).circularPattern Then setup(ocvb)
        check.Box(0).Checked = False
        Dim allClosed As Boolean = True
        Parallel.For(0, anneal.Length,
            Sub(i)
                If anneal(i).closed = False Then
                    anneal(i).Run(ocvb)
                    allClosed = False
                End If
            End Sub)

        ' find the best result and start all the others with it.
        Dim minEnergy As Single = Single.MaxValue
        Dim minIndex As Int32 = 0
        Dim bestList As New SortedList(Of Single, Int32)(New CompareEnergy)
        flow.msgs.Clear()
        For i = 0 To anneal.Length - 1
            bestList.Add(anneal(i).energy, i)
            flow.msgs.Add(Format(i, "00") + " " + anneal(i).msg)
        Next
        flow.Run(ocvb)

        ' if the top 4 are all the same energy, then we are done.
        If bestList.Count > 1 Then
            Dim sameEnergy As Int32 = 1
            For i = 1 To sliders.TrackBar2.Value - 1
                If anneal(CInt(bestList.ElementAt(i).Value)).energy = anneal(CInt(bestList.ElementAt(0).Value)).energy Then sameEnergy += 1
            Next
            If sameEnergy = sliders.TrackBar2.Value Then allClosed = True
            If sameEnergy = 1 Then
                label1 = "There is only " + CStr(sameEnergy) + " thread at the best energy level."
            Else
                label1 = "There are " + CStr(sameEnergy) + " threads at the best energy level."
            End If
        Else
            label1 = "Energy level is " + CStr(anneal(0).energy)
        End If

        mats.mat(0) = anneal(CInt(bestList.ElementAt(0).Value)).dst1
        If bestList.Count >= 2 Then
            mats.mat(1) = anneal(CInt(bestList.ElementAt(1).Value)).dst1
            mats.mat(2) = anneal(CInt(bestList.ElementAt(bestList.Count - 2).Value)).dst1
            mats.mat(3) = anneal(CInt(bestList.ElementAt(bestList.Count - 1).Value)).dst1
        End If
        mats.Run(ocvb)
        dst2 = mats.dst1

        ' copy the top half of the solutions to the bottom half (worst solutions)
        If check.Box(1).Checked Then
            For i = 0 To anneal.Length / 2 - 1
                anneal(bestList.ElementAt(bestList.Count - 1 - i).Value).cityOrder = anneal(bestList.ElementAt(i).Value).cityOrder
            Next
        End If

        If allClosed Then setup(ocvb)
    End Sub
End Class




Public Class Annealing_Options
    Inherits ocvbClass
    Dim random As Random_Points
    Public anneal As Annealing_Basics_CPP
    Dim flow As Font_FlowText
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        random = New Random_Points(ocvb, caller)
        random.sliders.TrackBar1.Value = 25 ' change the default number of cities here.
        random.Run(ocvb) ' get the city positions (may or may not be used below.)

        check.Setup(ocvb, caller, 2)
        check.Box(0).Text = "Restart TravelingSalesman"
        check.Box(1).Text = "Circular pattern of cities (allows you to visually check if successful.)"
        check.Box(1).Checked = True

        flow = New Font_FlowText(ocvb, caller)
        flow.result1or2 = RESULT2

        label1 = "Log of Annealing progress"


        anneal = New Annealing_Basics_CPP(ocvb, caller)
        anneal.numberOfCities = random.sliders.TrackBar1.Value
        anneal.circularPattern = check.Box(1).Checked
        If check.Box(1).Checked = False Then anneal.cityPositions = random.Points2f.Clone()
        anneal.setup(ocvb)
        anneal.Open()
        ocvb.desc = "Setup and control finding the optimal route for a traveling salesman"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim numberOfCities = random.sliders.TrackBar1.Value
        Dim circularPattern = check.Box(1).Checked ' do they want a circular pattern?
        If numberOfCities <> anneal.numberOfCities Or circularPattern <> anneal.circularPattern Then
            anneal.circularPattern = circularPattern
            anneal.numberOfCities = numberOfCities
            anneal.restartComputation = True
        Else
            anneal.restartComputation = check.Box(0).Checked
            check.Box(0).Checked = False
        End If

        anneal.Run(ocvb)
        dst1 = anneal.dst1

        If anneal.restartComputation Then
            anneal.restartComputation = False
            random.Run(ocvb) ' get the city positions (may or may not be used below.)
            If check.Box(1).Checked = False Then anneal.cityPositions = random.Points2f.Clone()
            anneal.setup(ocvb)
            anneal.Open()
            Static startTime As DateTime
            Dim timeSpent = Now.Subtract(startTime)
            If timeSpent.TotalSeconds < 10000 Then Console.WriteLine("time spent on last problem = " + Format(timeSpent.TotalSeconds, "#0.0") + " seconds.")
            startTime = Now
        End If

        flow.msgs.Add(anneal.msg)
        flow.Run(ocvb)
    End Sub
End Class
