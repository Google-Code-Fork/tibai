Public Class mTile
    Public id As tileID
    Public Discovered As Boolean = False 'a flag indicating that this tile has been mapped and its children are known
    Public Mapped As Boolean = False 'a flag indicating that all of its children have connected to it
    Public Connections As New Stack(Of Connection) 'a list of connections
    Public Children As New Stack(Of tileID) 'a list of known connected mTiles
    Public Structure Connection
        Dim SourceNode As Byte ' the nodeid of the node in this mTile
        Dim TargetTile As Integer 'the mTile id of the target
        Dim TargetNode As Byte 'the nodeid of the tile in the target mTile
    End Structure
    Public Structure tileID
        Dim tileID As Integer 'the position in the MDO
        Dim MDO As MDatObj 'a reference to the MDO object 
        Dim mTileNumber As Integer 'the position of the 16x16 tile in tibias map
    End Structure
    Public Sub New(ByVal mTileID As Integer, ByRef mdo As MDatObj, ByVal mtilenum As Integer)
        id.tileID = mTileID
        id.MDO = mdo
        id.mTileNumber = mtilenum

    End Sub
    Public Sub ConnectTo(ByVal srcnode As Integer, ByRef targetmtile As mTile, ByVal targetnode As Integer)

        For Each c In Connections
            'exit the sub if the connection is already there 
            If c.SourceNode = srcnode And c.TargetTile = targetmtile.id.mTileNumber And c.TargetNode = targetnode Then Exit Sub
        Next
        'otherwise 
        'build the connection
        Dim mycon As Connection
        mycon.SourceNode = CByte(srcnode)
        mycon.TargetNode = CByte(targetnode)
        mycon.TargetTile = targetmtile.id.mTileNumber
        'add the connection to the stack
        Connections.Push(mycon)
    End Sub


End Class
