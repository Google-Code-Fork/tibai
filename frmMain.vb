
Public Class frmMain
    Public myController As New Controller
    Public XQueue(0), YQueue(0), ZQueue(0) As Integer

    Private Sub frmMain_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load


    End Sub


    
    Private Sub btnAddtoList_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAddtoList.Click
        If Not Val(txtX.Text) > 31744 Or Not Val(txtX.Text) < 31744 + 1792 Then Exit Sub
        If Not Val(txtY.Text) > 30976 Or Not Val(txtY.Text) < 30976 + 2048 Then Exit Sub
        If Not Val(txtZ.Text) > 0 Or Not Val(txtZ.Text) < 16 Then Exit Sub
        If Not XQueue(0) = 0 Then
            ReDim Preserve XQueue(UBound(XQueue) + 1)
            ReDim Preserve YQueue(UBound(YQueue) + 1)
            ReDim Preserve ZQueue(UBound(ZQueue) + 1)
        End If
        XQueue(UBound(XQueue)) = CInt(Val(txtX.Text))
        YQueue(UBound(YQueue)) = CInt(Val(txtY.Text))
        ZQueue(UBound(ZQueue)) = CInt(Val(txtZ.Text))
        ListBox1.Items.Add(txtX.Text & ", " & txtY.Text & ", " & txtZ.Text)
        txtX.Text = ""
        txtY.Text = ""
        txtZ.Text = ""
    End Sub


    Private Sub btnStartQueue_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnStartQueue.Click
        If XQueue(0) = 0 Then Exit Sub
        Dim wpqueue(UBound(XQueue), 2) As Integer
        Me.btnStartQueue.Enabled = False
        Me.btnAddtoList.Enabled = False
        Me.Button2.Enabled = False
        For i As Integer = 0 To UBound(XQueue)
            wpqueue(i, 0) = XQueue(i)
            wpqueue(i, 1) = YQueue(i)
            wpqueue(i, 2) = ZQueue(i)
        Next
        myController.SubmitQueue(wpqueue)
        Me.Button2.Enabled = True
        Me.btnStartQueue.Enabled = True
        Me.btnAddtoList.Enabled = True

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        txtX.Text = CStr(myController.myPlayer.X)
        txtY.Text = CStr(myController.myPlayer.Y)
        txtZ.Text = CStr(myController.myPlayer.Z)

    End Sub


    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        ListBox1.Items.Clear()
        ReDim XQueue(0)
        ReDim YQueue(0)
        ReDim ZQueue(0)
    End Sub
End Class

