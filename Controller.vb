Imports Tibia
Imports Tibia.Objects
Imports Tibia.Packets
Imports Tibia.Util
Public Class Controller
    Public myClient As Client = ClientChooser.ShowBox()
    Public myPlayer As Player
    Public myEvents As tibAI.EventHandler
    Public myMotion As Movement
    Public Sub New()
        'Terminates if a client is not selected
        If myClient Is Nothing Then
            MsgBox("No client selected!")
            Application.Exit()
            Exit Sub
        End If
        'If the selected client is not logged in it starts the proxy 
        'and event handler
        If myClient.LoggedIn = False Then
            myClient.StartProxy()
            myEvents = New tibAI.EventHandler(Me)
        End If
    End Sub

    Public Sub GetXYZ()
        If myPlayer Is Nothing Then Exit Sub
        MsgBox(myPlayer.X & ", " & myPlayer.Y & ", " & myPlayer.Z)
    End Sub
    Public Sub SubmitQueue(ByVal WPQueue As Integer(,))
        myMotion.FollowWaypoints(WPQueue)
    End Sub
 

End Class
