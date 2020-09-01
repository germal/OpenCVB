Imports System.ComponentModel
Imports System.Windows.Forms
Public Class TreeviewForm
    Dim botDistance As Integer
    Private Sub OK_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OK_Button.Click
        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()
    End Sub
    Private Sub Cancel_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Cancel_Button.Click
        Me.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Close()
    End Sub
    Private Sub TreeView1_AfterSelect(sender As Object, e As TreeViewEventArgs) Handles TreeView1.AfterSelect
        Me.Timer1.Enabled = False
        OpenCVB.AvailableAlgorithms.Text = e.Node.Text
        If OpenCVB.AvailableAlgorithms.Text <> e.Node.Text Then
            ' the list of active algorithms for this group does not contain the algorithm requested so just add it!
            OpenCVB.AvailableAlgorithms.Items.Add(e.Node.Text)
            OpenCVB.AvailableAlgorithms.Text = e.Node.Text
        End If
        Console.WriteLine(OpenCVB.AvailableAlgorithms.Text + " should be " + e.Node.Text)
    End Sub
    Private Sub TreeviewForm_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        Dim split() = Me.Text.Split()
        OpenCVB.AvailableAlgorithms.Text = split(0)
    End Sub
    Public Sub TreeviewForm_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        If botDistance = 0 Then botDistance = Me.Height - Label1.Top
        Label1.Top = Me.Height - botDistance
        TreeView1.Height = Label1.Top - 5
    End Sub
    Private Function FindRecursive(ByVal tNode As TreeNode, name As String) As TreeNode
        Dim tn As TreeNode
        For Each tn In tNode.Nodes
            If tn.Text = name Then Return tn
            Dim rnode = FindRecursive(tn, name)
            If rnode IsNot Nothing Then Return rnode
        Next
        Return Nothing
    End Function
    Private Function getNode(tv As TreeView, name As String) As TreeNode
        For Each n In tv.Nodes
            If n.text = name Then Return n
            Dim rnode = FindRecursive(n, name)
            If rnode IsNot Nothing Then Return rnode
        Next
        Return Nothing
    End Function
    Private Function modifyCallTrace(calls As List(Of String), name As String) As List(Of String)
        Dim result As New List(Of String)
        For i = 0 To calls.Count - 1
            If calls(i).StartsWith(name) Then result.Add(Trim(Mid(name, Len(name) + 1)))
        Next
        Return result
    End Function
    Public Sub updateTree()
        Dim tv = TreeView1
        tv.Nodes.Clear()
        Dim rootcall = Trim(OpenCVB.callTrace(0))
        Me.Text = rootcall + " - Click on any node to review the algorithm's input and output."
        tv.Nodes.Add(rootcall)

        For nodeLevel = 0 To 100 ' this loop will terminate after the depth of the nesting.  100 is excessive insurance deep nesting may occur.
            Dim alldone = True
            For i = 1 To OpenCVB.callTrace.Count - 1
                Dim split() = OpenCVB.callTrace(i).Split
                If split.Count = nodeLevel + 3 Then
                    alldone = False
                    Dim node = getNode(tv, split(nodeLevel))
                    If node Is Nothing Then
                        If nodeLevel = 0 Then
                            tv.Nodes(nodeLevel).Nodes.Add(split(nodeLevel))
                        Else
                            node = getNode(tv, split(nodeLevel - 1))
                            node.Nodes.Add(split(nodeLevel))
                        End If
                    Else
                        node.Nodes.Add(split(nodeLevel + 1))
                    End If
                End If
            Next
            If alldone Then Exit For ' we didn't find any more nodes to add.
        Next
        tv.ExpandAll()
    End Sub
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If Me.Text.StartsWith(OpenCVB.callTrace(0)) = False Then updateTree()
    End Sub
End Class
