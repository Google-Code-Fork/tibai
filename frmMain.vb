
Public Class frmMain
    Public myController As New Controller

    Private Sub frmMain_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load


    End Sub
    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        myController.Test()
    End Sub
    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        myController.RouteRepeat()
    End Sub
    Private Sub GetXYZ_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GetXYZ.Click
        myController.GetXYZ()
    End Sub

    
End Class

