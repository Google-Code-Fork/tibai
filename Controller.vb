Imports Tibia
Imports Tibia.Objects
Imports Tibia.Packets
Imports Tibia.Util
Public Class Controller

    Public Sub New()
        myClient = ClientChooser.ShowBox()
        'Terminates if a client is not selected
        If myClient Is Nothing Then
            MsgBox("No client selected!")
            Application.Exit()
            Exit Sub
        End If
        'If the selected client is not logged in it starts the proxy 
        'and event handler
        If myClient.LoggedIn = False Then
            myClient.IO.StartProxy()
            myEvents = New tibAI.EventHandler()
        End If
        myCartographer = New Cartographer(80)
        Handle = myClient.Process.Handle

    End Sub


    Public Sub GetXYZ()
        If myPlayer Is Nothing Then Exit Sub
        MsgBox(myPlayer.X & ", " & myPlayer.Y & ", " & myPlayer.Z)
    End Sub
    Public Sub SubmitQueue(ByVal WPQueue As Integer(,))
        myMovement.FollowWaypoints(WPQueue)
    End Sub
    Public Sub SendStatustoClient(ByVal Msg As String)
        Tibia.Packets.Incoming.TextMessagePacket.Send(myClient, Tibia.Packets.StatusMessage.ConsoleOrange2, Msg)
    End Sub
    Protected Overrides Sub finalize()

    End Sub

End Class
