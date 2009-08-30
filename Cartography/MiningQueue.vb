Public Class MiningQueue
    Private MyQueue As New Queue(Of mTile.tileID)
    Private MTiles(111, 127, 15) As Boolean

    Public Sub New()
    End Sub
    Public Function Count() As Integer
        Return MyQueue.Count
    End Function
    Public Function Dequeue() As mTile.tileID
        Dim mtX, mtY As Integer
        'locks the queue
        SyncLock MyQueue
            'removes an item
            Dequeue = MyQueue.Dequeue
            'updates the lookup array
            mtY = CInt(Math.Truncate(Dequeue.mTileNumber / CInt(MapX / 16)))
            mtX = Dequeue.mTileNumber - (mtY * CInt(MapX / 16))
            MTiles(mtX, mtY, Dequeue.MDO.BaseZ) = False
        End SyncLock
    End Function
    Public Sub Enqueue(ByVal mt As mTile.tileID)
        Dim mtX, mtY As Integer
        'checks to see if its enqueue
        mtY = CInt(Math.Truncate(mt.mTileNumber / CInt(MapX / 16)))
        mtX = mt.mTileNumber - (mtY * CInt(MapX / 16))
        'if it is then it exits the sub
        If MTiles(mtX, mtY, mt.MDO.BaseZ) = True Then Exit Sub
        'locks the queue and adds the item
        SyncLock MyQueue
            MyQueue.Enqueue(mt)
        End SyncLock
    End Sub
End Class
