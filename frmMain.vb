
Public Class frmMain

    Public XQueue(0), YQueue(0), ZQueue(0) As Integer

    Private Sub frmMain_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        myController = New Controller()
    End Sub
    Private Sub frmMain_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        If Not DataMiner Is Nothing Then DataMiner.Abort()
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
        txtX.Text = CStr(myPlayer.X)
        txtY.Text = CStr(myPlayer.Y)
        txtZ.Text = CStr(myPlayer.Z)

    End Sub


    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        ListBox1.Items.Clear()
        ReDim XQueue(0)
        ReDim YQueue(0)
        ReDim ZQueue(0)
    End Sub


    Private Sub btnTest1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnTest1.Click
        ListBox1.Items.Add("32606, 31967, 7")
        ListBox1.Items.Add("32209, 31755, 7")
        If Not XQueue(0) = 0 Then
            ReDim Preserve XQueue(UBound(XQueue) + 1)
            ReDim Preserve YQueue(UBound(YQueue) + 1)
            ReDim Preserve ZQueue(UBound(ZQueue) + 1)
        End If
        XQueue(UBound(XQueue)) = 32606
        YQueue(UBound(YQueue)) = 31967
        ZQueue(UBound(ZQueue)) = 7
        If Not XQueue(0) = 0 Then
            ReDim Preserve XQueue(UBound(XQueue) + 1)
            ReDim Preserve YQueue(UBound(YQueue) + 1)
            ReDim Preserve ZQueue(UBound(ZQueue) + 1)
        End If
        XQueue(UBound(XQueue)) = 32209
        YQueue(UBound(YQueue)) = 31755
        ZQueue(UBound(ZQueue)) = 7
    End Sub


    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        Button3.Enabled = False
        myMovement.Explore()
        Button3.Enabled = True
    End Sub


    Private Sub cmdtest1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdtest1.Click
        myCartographer.myMapData.SchedulePlayersmTile()
    End Sub
End Class

