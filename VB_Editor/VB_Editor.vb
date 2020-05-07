Imports System.IO
Module VB_EditorMain
    Dim changeLines As Integer
    Private Function makeChange(line As String) As String
        If line.Contains(" = New ") And line.Contains("(ocvb, """) Then
            Console.WriteLine(line)
            line = Mid(line, 1, InStr(line, "ocvb, ") + 5) + "caller)"
            Console.WriteLine("Change to: " + line)
            changeLines += 1
        End If
        Return line
    End Function
    Sub Main()
        ' Regular expression are great but can be too complicated.  This app is just a simpler way to make global changes that 
        ' would normally be accomplished with regular expressions.
        Dim VBcodeDir As New DirectoryInfo(CurDir() + "/../../VB_Classes")
        Dim fileEntries As String() = Directory.GetFiles(VBcodeDir.FullName)

        Dim changeFiles As New List(Of String)
        For Each fileName In fileEntries
            Dim nextFile As New System.IO.StreamReader(fileName)
            Dim saveChangeLines = changeLines
            While nextFile.Peek() <> -1
                Dim line As String
                line = Trim(nextFile.ReadLine())
                makeChange(line)
            End While
            nextFile.Close()
            If saveChangeLines <> changeLines Then changeFiles.Add(fileName)
        Next
        Console.WriteLine(CStr(changeLines) + " matching lines found in " + CStr(changeFiles.Count) + " files")

        Dim response = InputBox("Respond 'Yes' to make the changes.", "Make Changes?", "")
        If response = "Yes" Then
            changeLines = 0
            For Each filename In changeFiles
                Dim sr = New StreamReader(filename)
                Dim code As String = sr.ReadToEnd
                sr.Close()
                Dim lines = code.Split(vbCrLf)
                For i = 0 To lines.Count - 1
                    lines(i) = makeChange(Trim(lines(i)))
                Next

                Dim sw = New StreamWriter(filename)
                For i = 0 To lines.Count - 1
                    sw.Write(lines(i))
                Next
                sw.Close()
            Next
        End If

    End Sub
End Module
