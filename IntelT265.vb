Public Class IntelT265
    Function DetectDevice(DeviceName As String) As Boolean
        Try
            ' See if the desired device shows up in the device manager.'
            Dim info As Management.ManagementObject
            Dim search As System.Management.ManagementObjectSearcher
            Dim Name As String
            search = New System.Management.ManagementObjectSearcher("SELECT * From Win32_PnPEntity")
            For Each info In search.Get()
                ' Go through each device detected.'
                Name = CType(info("Caption"), String) ' Get the name of the device.'
                console.writeline(Name)
                If InStr(Name, DeviceName, CompareMethod.Text) > 0 Then
                    Return True
                End If
            Next
        Catch ex As Exception
        End Try
        'We did not find the device we were looking for'
        Return False
    End Function
End Class
