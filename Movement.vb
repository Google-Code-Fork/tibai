﻿Imports Tibia.Objects
Public Class Movement
    'Houses the capacity to perform any movement based action that a human
    'player could perform and with all the flexibility a human could execute
    'it with
    '1) Improve the isPZlocked check. It's very rudimentary right now.
    '2) Create a grid based highway to minimize the search area and reduce the search time
    '3) Create a supernode based highway to facilitate tibia-wide paths
    '4) Add a high-level pathfinder to find paths between supernodes and grids
    '5) Add ability to check for other non-qualified steps such as premmy and level switches
    '7) Expand my movement class to handle all useful movements
    '8) Add the capacity to Pause a movement to be resumed later or terminate it completely
    '9) Add level-change tile mapping for searching the whole map
    Public Sub New()

    End Sub
    Public Function GotoXYZ(ByVal X As Integer, ByVal Y As Integer, ByVal Z As Integer) As Boolean
        'Goes to single XYZ coordinate
        Dim queue(0, 2) As Integer
        queue(0, 0) = X
        queue(0, 1) = Y
        queue(0, 2) = Z
        Return ProcessXYZQueue(queue)
    End Function
    Public Function FollowWaypoints(ByVal Path(,) As Integer) As Boolean
        'Follows a list of waypoints
        Return ProcessXYZQueue(Path)
    End Function
    Private Function ProcessXYZQueue(ByVal Queue(,) As Integer) As Boolean
        'Keeps walking until the current path and the next path are finished
        Dim tpath(,), n, nextpath(,) As Integer
        Dim isPrepared, skipsearch As Boolean
        Dim StatusMessage As String
        Dim QueuePtr As Integer 'the current destination

        ProcessXYZQueue = False 'action failed by default
        tpath = Nothing
        nextpath = Nothing
        isPrepared = False
        skipsearch = False
        Do
            'gets the path
            If myPlayer Is Nothing Then Return False

            'skips if a path was found from the nextdest coordinates
            If Not skipsearch = True Then
                tpath = myCartographer.GetPath(myPlayer.X, myPlayer.Y, myPlayer.Z, Queue(QueuePtr, 0), Queue(QueuePtr, 1), Queue(QueuePtr, 2))
                If tpath Is Nothing Then StatusMessage = "Path Not Found" Else StatusMessage = "Path Found"
                StatusMessage &= ": " & myCartographer.OpenedCount & " nodes searched in " & CStr(myCartographer.LastActionTime / 1000) & " seconds with " & myCartographer.HeuristicBaseCost & " as the heuristic."
                myController.SendStatustoClient(StatusMessage)
                'exits if a path isn't found
                If tpath Is Nothing Then Exit Do
            End If
            skipsearch = False

            'moves through the path
            For i As Integer = 0 To UBound(tpath, 1)
                'starts the player moving
                myPlayer.GoTo_X = tpath(i, 0)
                myPlayer.GoTo_Y = tpath(i, 1)
                myPlayer.GoTo_Z = tpath(i, 2)
                myPlayer.IsWalking = True
                n = 0

                'Waits or preprocesses the next path while the character is 
                'moving
                Do While myPlayer.GoTo_X = tpath(i, 0)

                    'prepare the next path if its loaded
                    If Not QueuePtr = UBound(Queue, 1) And Not isPrepared Then
                        If Not myPlayer.X = Queue(QueuePtr + 1, 0) Or Not myPlayer.Y = Queue(QueuePtr + 1, 1) Or Not myPlayer.Z = Queue(QueuePtr + 1, 2) Then
                            isPrepared = True
                            nextpath = myCartographer.GetPath(Queue(QueuePtr, 0), Queue(QueuePtr, 1), Queue(QueuePtr, 2), Queue(QueuePtr + 1, 0), Queue(QueuePtr + 1, 1), Queue(QueuePtr + 1, 2))
                            If nextpath Is Nothing Then StatusMessage = "Failed to Queue Next Path" Else StatusMessage = "Successfully Queued Next Path(Waiting)"
                            StatusMessage &= ": " & myCartographer.OpenedCount & " nodes searched in " & CStr(myCartographer.LastActionTime / 1000) & " seconds with " & myCartographer.HeuristicBaseCost & " as the heuristic."
                            myController.SendStatustoClient(StatusMessage)
                        End If
                    End If

                    'a 15 second wait at speed 290 is fine
                    'this if statement adjusts that according to the 
                    'players actual walking speed
                    n += 1
                    If n = CInt(Math.Truncate(30 * (CDec(myPlayer.WalkSpeed) / CDec(290)))) Then Exit For
                    Threading.Thread.Sleep(500) 'thread sleeps for .5 seconds
                Loop
                'Goto Next step or finish
            Next

            'Destination Reached

            'if the steps to the destination are less than 5
            '*This provides a buffer at the end of a path. Since tibia's 
            '*pathfinder would have already sent the packet to walk the 
            '*last 5 steps we can start doing other things. This reduces 
            '*the pause at the end of a path and results in much smoother
            '*operation.
            If Math.Abs(myPlayer.X - Queue(QueuePtr, 0)) + Math.Abs(myPlayer.Y - Queue(QueuePtr, 1)) < 3 Then
                'current destination reached
                If isPrepared = True Then
                    'Another path is available to be pursued
                    If nextpath Is Nothing Then
                        'the path search failed
                        Exit Do
                    Else
                        'if a path was queued successfully
                        'sets a new destination
                        QueuePtr += 1
                        'loads the pre-processed path
                        tpath = nextpath
                        'allows the tpath search to be skipped later on
                        skipsearch = True
                        isPrepared = False
                    End If
                Else
                    'there's no new destination 
                    ProcessXYZQueue = True 'action succeeded
                    Exit Function
                End If

            End If

        Loop
    End Function
    Public Sub Explore()
        'Goes to every available black tile on the map
        Dim StatusMessage As String
        Dim myPath(,), n As Integer
        Do
            'gets the path
            myPath = myCartographer.GetPathtoTileType(myPlayer.X, myPlayer.Y, myPlayer.Z, 0, &HFA)
            If myPath Is Nothing Then Exit Sub
            StatusMessage = ": " & myCartographer.OpenedCount & " nodes searched in " & CStr(myCartographer.LastActionTime / 1000) & " seconds with " & myCartographer.HeuristicBaseCost & " as the heuristic."
            myController.SendStatustoClient(StatusMessage)
            If myPlayer Is Nothing Then Exit Sub



            'moves through the path
            For i As Integer = 0 To UBound(myPath, 1)
                'starts the player moving
                myPlayer.GoTo_X = myPath(i, 0)
                myPlayer.GoTo_Y = myPath(i, 1)
                myPlayer.GoTo_Z = myPath(i, 2)
                myPlayer.IsWalking = True
                n = 0

                'Waits
                Do While myPlayer.GoTo_X = myPath(i, 0)
                    Threading.Thread.Sleep(100) 'thread sleeps for .1 seconds
                Loop
                'Goto Next step or finish
            Next

        Loop

    End Sub
    
End Class
