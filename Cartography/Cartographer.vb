Public Class Cartographer
    'The cartographer class is the master of map related functions and information.
    'It executes the data mining, A* path searches, and other map related functions
    'It utilizes the MapData class to manage the raw data and the BinaryHeap class to
    'sort the highest score nodes in path searches.

    Public myMapData As MapData 'the class used to get movement cost info
    Private OpenList As BinaryHeap 'the sorted list of open nodes
    Private NodeMap(,,) As Byte '0=Movement Cost 1=Parent, 2=status
    Private ScoreMap(,,) As Integer '0=GScore, 1=Heuristic
    Private pathfound As Boolean '=true if a path was found
    Public HeuristicBaseCost As Byte = &HA0 'heuristic movement cost
    Private StepInt As Integer 'How often a step is returned(1 is every step, 100 is every 100th step)
    Public OpenedCount As Integer 'the tally of total evaluated nodes for the previous search
    Public LastActionTime As Integer 'the milliseconds spent on the last path search
    Public RecentDiscovered, RecentMapped As Integer
    Public ActiveMining As Boolean = True 'a flag that allows the mining thread to pause
    Public StopMining As Boolean = False 'a flag that allows the mining thread to terminate

    Public Sub New(ByVal StepInterval As Integer)
        StepInt = StepInterval
        OpenList = New BinaryHeap(10000)
        myMapData = New MapData()
        ReDim NodeMap(MapX, MapY, 2)
        ReDim ScoreMap(MapX, MapY, 1)
        'sets up objects used in the data mining process and starts the data miner thread
        Mappable = New MiningQueue
        DataMiner = New System.Threading.Thread(AddressOf Me.MineMacroPaths)
        DataMiner.Priority = System.Threading.ThreadPriority.Lowest
        DataMiner.Name = "Dataminer"
        DataMiner.Start()
        'for use when changing the debugcolor
        Randomize()
    End Sub
    Protected Overrides Sub Finalize()
        DataMiner.Abort()
        StopMining = True
    End Sub
    Private Sub AddtoList(ByVal X As Integer, ByVal Y As Integer, ByVal mcost As Byte, ByVal parent As Byte, ByVal GScore As Integer, ByVal HScore As Integer)
        NodeMap(X, Y, 0) = mcost 'movement cost
        NodeMap(X, Y, 1) = parent '0-8: 4 = self
        NodeMap(X, Y, 2) = 1 'status 1=opened 2=closed, 0 = not added
        ScoreMap(X, Y, 0) = GScore 'GScore
        ScoreMap(X, Y, 1) = HScore 'HScore
        OpenList.AddItem((X * MapX) + Y, GScore + HScore)
    End Sub
    Public Function GetH(ByVal X1 As Integer, ByVal Y1 As Integer, ByVal X2 As Integer, ByVal Y2 As Integer) As Integer
        'gets the heuristic 
        ' *note: I figured this was faster to process than the Math.Abs function 
        ' *because this is only two simple comparisons of an integer and the
        ' *Math.Abs function, because its potential input is so diverse, is 
        ' *almost definately more processor intensive and I'd use 2 of them.
        ' *I might be wrong but I doubt it.
        If X1 > X2 Then
            If Y1 > Y2 Then
                GetH = (X1 - X2 + Y1 - Y2) * HeuristicBaseCost
            Else
                GetH = (X1 - X2 + Y2 - Y1) * HeuristicBaseCost
            End If
        Else
            If Y1 > Y2 Then
                GetH = (X2 - X1 + Y1 - Y2) * HeuristicBaseCost
            Else
                GetH = (X2 - X1 + Y2 - Y1) * HeuristicBaseCost
            End If
        End If
    End Function
    Private Sub SetupGlobals()
        OpenedCount = 0
        OpenList.Reset()
        pathfound = False
        ReDim NodeMap(MapX, MapY, 2)
        ReDim ScoreMap(MapX, MapY, 1)
    End Sub

    Public Function GetPath(ByVal StX As Integer, ByVal StY As Integer, ByVal StZ As Integer, ByVal EndX As Integer, ByVal EndY As Integer, ByVal EndZ As Integer) As Integer(,)
        'increment the debug color
        RotateDebugColor()

        'gets a path from one coordinate to another
        Dim sTime As DateTime
        sTime = Now 'tracks how long this action took
        Dim ReturnVar(0, 2) As Integer
        'if the start and end coordinates are the same
        If StX = EndX And StY = EndY And StZ = EndZ Then
            ReturnVar(0, 0) = StX
            ReturnVar(0, 1) = StY
            ReturnVar(0, 2) = StZ
            OpenedCount = 1
            LastActionTime = CInt(Math.Truncate((Now.Ticks - sTime.Ticks) / System.TimeSpan.TicksPerMillisecond))
            Return ReturnVar
        End If
        Dim tNode, tNodeX, tNodeY, cNodeX, cNodeY, cMCost, DiagMod As Integer
        HeuristicBaseCost = CByte((GetSample(StX, StY, StZ) + GetSample(EndX, EndY, EndZ)) / 2)
        If HeuristicBaseCost + &H10 < &HFA Then
            HeuristicBaseCost = CByte(HeuristicBaseCost + &H10)
        Else
            HeuristicBaseCost = &HFA
        End If
        SetupGlobals() 'prepares the global variables
        'create the open list of nodes, initially containing only our starting node
        '   reduces the coordinates to my 1792x2048 map
        StX = StX - MinX
        StY = StY - MinY
        EndX = EndX - MinX
        EndY = EndY - MinY
        AddtoList(StX, StY, 0, 4, 0, GetH(StX, StY, EndX, EndY))
        OpenedCount += 1
        'while (we have not reached our goal) {
        Do While pathfound = False And OpenList.length > 0
            'consider the best node in the open list (the node with the lowest f value)
            tNode = OpenList.GetFromTop
            tNodeX = CInt(Math.Truncate(tNode / MapX))
            tNodeY = tNode - (tNodeX * MapX)
            'if (this node is the goal) {
            If tNodeX = EndX And tNodeY = EndY Then
                'then we're done
                pathfound = True
                Exit Do

            End If
            'move the current node to the closed list and consider all of its neighbors
            NodeMap(tNodeX, tNodeY, 2) = 2 'closes node

            'for (each neighbor) {
            For i As Integer = 0 To 8
                If Not i = 4 Then '4 is tnode so skip it
                    'sets the diagonal modifier to either 300% or 100%
                    If i = 0 Or i = 2 Or i = 6 Or i = 8 Then DiagMod = 3 Else DiagMod = 1
                    'gets child i's x and y coordinates
                    cNodeX = tNodeX + (CInt(Math.Truncate(i / 3)) - 1)
                    cNodeY = tNodeY + (i - (CInt(Math.Truncate(i / 3)) * 3) - 1)
                    'verifies this child node is on the map
                    If cNodeX > -1 And cNodeX < MapX And cNodeY > -1 And cNodeY < MapY Then
                        'if (this neighbor is in the closed or opened list 
                        If Not NodeMap(cNodeX, cNodeY, 2) = 0 Then
                            'and our current g value is lower) {
                            If ScoreMap(tNodeX, tNodeY, 0) + (NodeMap(cNodeX, cNodeY, 0) * DiagMod) < ScoreMap(cNodeX, cNodeY, 0) And NodeMap(cNodeX, cNodeY, 2) = 1 Then
                                'update the neighbor with the new, lower, g value 
                                ScoreMap(cNodeX, cNodeY, 0) = ScoreMap(tNodeX, tNodeY, 0) + (NodeMap(cNodeX, cNodeY, 0) * DiagMod)
                                'change the neighbor's parent to our current node
                                '0,1,2 'if the child is in the parents 1 position
                                '3,X,5 'then the parent is in the childs 7 position
                                '6,7,8 'so 8-i is the appropriate parent position for child(i)
                                '      'since 8-1=7 and 8-7=1
                                NodeMap(cNodeX, cNodeY, 1) = CByte(8 - i)
                                'if the child was open then resort the binary heap
                                If NodeMap(cNodeX, cNodeY, 2) = 1 Then
                                    'the item in the heap is equal to the numerical position of the node on the map the value is the Gscore +hscore
                                    OpenList.ReSortItem((cNodeX * MapX) + cNodeY, ScoreMap(cNodeX, cNodeY, 0) + ScoreMap(cNodeX, cNodeY, 1))
                                End If
                            End If
                        Else 'else this neighbor is not in either the open or closed list {
                            'add the neighbor to the open list and set its g value

                            cMCost = myMapData.GetTileCost(cNodeX + MinX, cNodeY + MinY, StZ, True)
                            If cMCost < &HFF Then
                                OpenedCount += 1 'counts the number of nodes assessed
                                AddtoList(cNodeX, cNodeY, CByte(cMCost), CByte(8 - i), ScoreMap(tNodeX, tNodeY, 0) + (cMCost * DiagMod), GetH(cNodeX, cNodeY, EndX, EndY))
                            End If
                        End If
                    End If
                End If
            Next


        Loop
        If pathfound = True Then
            'fills the Return variable with the path 
            Dim pX, pY, pp, XArray(500), YArray(500), pathlen As Integer
            'since the last node found has to be the goal if the path was found
            'it works backwards from that node
            pX = tNodeX
            pY = tNodeY
            'Map.GetTileCost(pX + MinX, Py + MinY, StZ, &H29)
            'do while the parent node is not the starting node
            '   fills two temporary arrays with the X and Y coordinates
            XArray(0) = pX + MinX
            YArray(0) = pY + MinY
            Do While pX <> StX Or pY <> StY
                'put's every step of the path in the xarray variable
                pp = NodeMap(pX, pY, 1)
                pX = pX + (CInt(Math.Truncate(pp / 3)) - 1)
                pY = pY + (pp - (CInt(Math.Truncate(pp / 3)) * 3) - 1)
                If pX = StX And pY = StY Then Exit Do
                'expands the variables if the path is too long to fit
                If pathlen = UBound(XArray) Then
                    ReDim Preserve XArray(UBound(XArray) + 500)
                    ReDim Preserve YArray(UBound(YArray) + 500)
                End If
                'puts the coordinate in the array
                pathlen += 1
                XArray(pathlen) = pX + MinX
                YArray(pathlen) = pY + MinY

                'Map.GetTileCost(pX + MinX, pY + MinY, StZ, &H29)
            Loop
            'resizes the array properly
            If pathlen > 0 Then
                ReDim ReturnVar(CInt(Math.Truncate((Math.Ceiling(CDec(pathlen) / CDec(StepInt))))) - 1, 2)
            Else
                ReDim ReturnVar(0, 2)
            End If

            Dim nextspot As Integer
            'fills the return variable with the path at the appropriate step interval
            If pathlen < StepInt Then
                'if the path is too short for a for next loop then just
                'send the destination
                ReturnVar(nextspot, 0) = XArray(0)
                ReturnVar(nextspot, 1) = YArray(0)
                ReturnVar(nextspot, 2) = StZ
            Else
                For i As Integer = pathlen - StepInt To 0 Step -StepInt 'counts backwards to reverse the order of the steps
                    'resizes the return variable
                    ReturnVar(nextspot, 0) = XArray(i)
                    ReturnVar(nextspot, 1) = YArray(i)
                    ReturnVar(nextspot, 2) = StZ
                    nextspot += 1 'moves the pointer forward
                    If i < StepInt And i > 0 Then 'the next iteration wont add the final step
                        'sets the last step as the destination
                        ReturnVar(nextspot, 0) = XArray(0)
                        ReturnVar(nextspot, 1) = YArray(0)
                        ReturnVar(nextspot, 2) = StZ
                    End If
                Next
            End If

        Else
            'returns nothing if a path isn't found
            ReturnVar = Nothing
        End If
        'sets lastactiontime to the processing time of this function
        LastActionTime = CInt(Math.Truncate((Now.Ticks - sTime.Ticks) / System.TimeSpan.TicksPerMillisecond))
        Return ReturnVar
    End Function
    Public Function GetPathtoTileType(ByVal StX As Integer, ByVal StY As Integer, ByVal StZ As Integer, ByVal tileID As Byte, ByVal tileCost As Byte) As Integer(,)
        'increment the debug color
        RotateDebugColor()

        'gets a path from the starting point to any tile with a matching tileID and movement cost
        Dim sTime As DateTime
        sTime = Now 'tracks how long this action took
        Dim ReturnVar(0, 2) As Integer
        Dim tNode, tNodeX, tNodeY, cNodeX, cNodeY, cMCost, DiagMod As Integer
        SetupGlobals() 'prepares the global variables
        'create the open list of nodes, initially containing only our starting node
        '   reduces the coordinates to my 1792x2048 map
        StX = StX - MinX
        StY = StY - MinY
        AddtoList(StX, StY, 0, 4, 0, 0)
        OpenedCount += 1
        'while (we have not reached our goal) {
        Do While pathfound = False And OpenList.length > 0
            'consider the best node in the open list (the node with the lowest f value)
            tNode = OpenList.GetFromTop
            tNodeX = CInt(Math.Truncate(tNode / MapX))
            tNodeY = tNode - (tNodeX * MapX)
            'if (this node is a goal) {

            If myMapData.GetTileID(tNodeX + MinX, tNodeY + MinY, StZ) = tileID And myMapData.GetTileCost(tNodeX, tNodeY, StZ, True) = tileCost Then

                'then we're done
                pathfound = True
                Exit Do

            Else 'else {

                'move the current node to the closed list and consider all of its neighbors
                NodeMap(tNodeX, tNodeY, 2) = 2 'closes node
                myMapData.GetTileCost(tNodeX + MinX, tNodeY + MinY, StZ, True)
                'for (each neighbor) {
                For i As Integer = 0 To 8
                    If Not i = 4 Then '4 is tnode so skip it
                        'sets the diagonal modifier to either 300% or 100%
                        If i = 0 Or i = 2 Or i = 6 Or i = 8 Then DiagMod = 3 Else DiagMod = 1
                        'gets child i's x and y coordinates
                        cNodeX = tNodeX + (CInt(Math.Truncate(i / 3)) - 1)
                        cNodeY = tNodeY + (i - (CInt(Math.Truncate(i / 3)) * 3) - 1)
                        'verifies this child node is on the map
                        If cNodeX > -1 And cNodeX < MapX And cNodeY > -1 And cNodeY < MapY Then
                            OpenedCount += 1 'counts the number of nodes assessed
                            'if (this neighbor is in the closed or opened list 
                            If Not NodeMap(cNodeX, cNodeY, 2) = 0 Then
                                'and our current g value is lower) {
                                If ScoreMap(tNodeX, tNodeY, 0) + (NodeMap(cNodeX, cNodeY, 0) * DiagMod) < ScoreMap(cNodeX, cNodeY, 0) And NodeMap(cNodeX, cNodeY, 2) = 1 Then
                                    'update the neighbor with the new, lower, g value 
                                    ScoreMap(cNodeX, cNodeY, 0) = ScoreMap(tNodeX, tNodeY, 0) + (NodeMap(cNodeX, cNodeY, 0) * DiagMod)
                                    'change the neighbor's parent to our current node
                                    '0,1,2 'if the child is in the parents 1 position
                                    '3,X,5 'then the parent is in the childs 7 position
                                    '6,7,8 'so 8-i is the appropriate parent position for child(i)
                                    '      'since 8-1=7 and 8-7=1
                                    NodeMap(cNodeX, cNodeY, 1) = CByte(8 - i)
                                    'if the child was open then resort the binary heap
                                    If NodeMap(cNodeX, cNodeY, 2) = 1 Then
                                        'the item in the heap is equal to the numerical position of the node on the map the value is the Gscore +hscore
                                        OpenList.ReSortItem((cNodeX * MapX) + cNodeY, ScoreMap(cNodeX, cNodeY, 0) + ScoreMap(cNodeX, cNodeY, 1))
                                    End If
                                End If
                            Else 'else this neighbor is not in either the open or closed list {
                                'add the neighbor to the open list and set its g value
                                cMCost = myMapData.GetTileCost(cNodeX + MinX, cNodeY + MinY, StZ, True)

                                If cMCost < &HFF Then
                                    AddtoList(cNodeX, cNodeY, CByte(cMCost), CByte(8 - i), ScoreMap(tNodeX, tNodeY, 0) + (cMCost * DiagMod), 0)
                                Else
                                    'This is for debug purposes.
                                    myMapData.GetTileCost(cNodeX + MinX, cNodeY + MinY, StZ, True)
                                End If
                            End If
                        End If
                    End If
                Next

            End If

        Loop
        If pathfound = True Then
            'fills the Return variable with the path
            Dim pX, pY, pp, XArray(0), YArray(0) As Integer
            'since the last node found has to be the goal if the path was found
            'it works backwards from that node
            pX = tNodeX
            pY = tNodeY
            'Map.GetTileCost(pX + MinX, Py + MinY, StZ, &H29)
            'do while the parent node is not the starting node
            '   fills two temporary arrays with the X and Y coordinates
            XArray(0) = pX + MinX
            YArray(0) = pY + MinY
            Do While pX <> StX Or pY <> StY
                For i As Integer = 1 To StepInt
                    pp = NodeMap(pX, pY, 1)
                    pX = pX + (CInt(Math.Truncate(pp / 3)) - 1)
                    pY = pY + (pp - (CInt(Math.Truncate(pp / 3)) * 3) - 1)
                    If pX = StX And pY = StY Then Exit Do
                Next
                ReDim Preserve XArray(UBound(XArray) + 1)
                ReDim Preserve YArray(UBound(YArray) + 1)
                XArray(UBound(XArray)) = pX + MinX
                YArray(UBound(YArray)) = pY + MinY

                'Map.GetTileCost(pX + MinX, pY + MinY, StZ, &H29)
            Loop
            'reverses the temporary array's
            Array.Reverse(XArray)
            Array.Reverse(YArray)
            'resizes the return variable
            ReDim ReturnVar(UBound(XArray), 2)
            'fills the return variable with the path at the appropriate step interval
            For i As Integer = 0 To UBound(XArray)
                ReturnVar(i, 0) = XArray(i)
                ReturnVar(i, 1) = YArray(i)
                ReturnVar(i, 2) = StZ
            Next
        Else
            'returns nothing if a path isn't found
            ReturnVar = Nothing
        End If
        LastActionTime = CInt(Math.Truncate((Now.Ticks - sTime.Ticks) / System.TimeSpan.TicksPerMillisecond))
        Return ReturnVar
    End Function
    Public Function GetSample(ByVal StX As Integer, ByVal StY As Integer, ByVal StZ As Integer) As Integer
        'increment the debug color
        RotateDebugColor()

        'Samples nodes around the start and returns a heuristic for use
        'in the sample area
        Dim ReturnVar As Integer
        Dim SampleNodesCount As Integer
        Dim tNode, tNodeX, tNodeY, cNodeX, cNodeY, cMCost, DiagMod As Integer
        SetupGlobals() 'prepares the global variables
        'create the open list of nodes, initially containing only our starting node
        '   reduces the coordinates to my 1792x2048 map
        StX = StX - MinX
        StY = StY - MinY
        AddtoList(StX, StY, 0, 4, 0, 0)
        OpenedCount += 1
        'while (we have not reached our goal) {
        Do While pathfound = False And OpenList.length > 0
            'consider the best node in the open list (the node with the lowest f value)
            tNode = OpenList.GetFromTop
            tNodeX = CInt(Math.Truncate(tNode / MapX))
            tNodeY = tNode - (tNodeX * MapX)

            'goal
            If SampleNodesCount > 100 Then
                'then we're done
                Exit Do
            Else 'else {

                'move the current node to the closed list and consider all of its neighbors
                NodeMap(tNodeX, tNodeY, 2) = 2 'closes node
                myMapData.GetTileCost(tNodeX + MinX, tNodeY + MinY, StZ, True)
                'for (each neighbor) {
                For i As Integer = 0 To 8
                    If Not i = 4 Then '4 is tnode so skip it
                        'sets the diagonal modifier to either 300% or 100%
                        If i = 0 Or i = 2 Or i = 6 Or i = 8 Then DiagMod = 3 Else DiagMod = 1
                        'gets child i's x and y coordinates
                        cNodeX = tNodeX + (CInt(Math.Truncate(i / 3)) - 1)
                        cNodeY = tNodeY + (i - (CInt(Math.Truncate(i / 3)) * 3) - 1)
                        'verifies this child node is on the map
                        If cNodeX > -1 And cNodeX < MapX And cNodeY > -1 And cNodeY < MapY Then
                            OpenedCount += 1 'counts the number of nodes assessed
                            'if (this neighbor is in the closed or opened list 
                            If Not NodeMap(cNodeX, cNodeY, 2) = 0 Then
                                'and our current g value is lower) {
                                If ScoreMap(tNodeX, tNodeY, 0) + (NodeMap(cNodeX, cNodeY, 0) * DiagMod) < ScoreMap(cNodeX, cNodeY, 0) And NodeMap(cNodeX, cNodeY, 2) = 1 Then
                                    'update the neighbor with the new, lower, g value 
                                    ScoreMap(cNodeX, cNodeY, 0) = ScoreMap(tNodeX, tNodeY, 0) + (NodeMap(cNodeX, cNodeY, 0) * DiagMod)
                                    'change the neighbor's parent to our current node
                                    '0,1,2 'if the child is in the parents 1 position
                                    '3,X,5 'then the parent is in the childs 7 position
                                    '6,7,8 'so 8-i is the appropriate parent position for child(i)
                                    '      'since 8-1=7 and 8-7=1
                                    NodeMap(cNodeX, cNodeY, 1) = CByte(8 - i)
                                    'if the child was open then resort the binary heap
                                    If NodeMap(cNodeX, cNodeY, 2) = 1 Then
                                        'the item in the heap is equal to the numerical position of the node on the map the value is the Gscore +hscore
                                        OpenList.ReSortItem((cNodeX * MapX) + cNodeY, ScoreMap(cNodeX, cNodeY, 0) + ScoreMap(cNodeX, cNodeY, 1))
                                    End If
                                End If
                            Else 'else this neighbor is not in either the open or closed list {
                                'add the neighbor to the open list and set its g value
                                cMCost = myMapData.GetTileCost(cNodeX + MinX, cNodeY + MinY, StZ, True)
                                'if this tile is walkable then add it to the list
                                If cMCost < &HFF Then
                                    'updates the average movement cost stored in returnvar
                                    ReturnVar = CByte(((SampleNodesCount * ReturnVar) + cMCost) / (SampleNodesCount + 1))
                                    SampleNodesCount += 1

                                    AddtoList(cNodeX, cNodeY, CByte(cMCost), CByte(8 - i), ScoreMap(tNodeX, tNodeY, 0) + (cMCost * DiagMod), 0)
                                End If
                            End If
                        End If
                    End If
                Next

            End If

        Loop


        Return ReturnVar

    End Function

    'Gathering of large scale path search data for better heuristics
    Public Sub DiscoverMTile(ByVal current As mTile)
        If current.Discovered = True Then Exit Sub

        RotateDebugColor() 'changes the color written to the minimap in tibia when debugging

        Dim mNodeMap(17, 17) As Integer 'status 1=opened 2=closed, 0 = not added
        Dim mHeap As New BinaryHeap(10000)
        Dim myX, myY, myZ As Integer
        Dim cTileX, cTileY As Integer
        Dim tnode, tnodeX, tnodeY, cnodeX, cnodeY As Integer
        Dim curNodeID As Byte
        Dim childmTile As mTile
        Dim cmapx, cmapy, cid, cnodeid As Integer
        Dim dontAdd As Boolean = False
        Dim tempDebugVal As Boolean = DebugMap
        myY = (current.id.MDO.BaseY * 256) + (CInt(Math.Truncate(current.id.tileID / 16)) * 16)
        myX = (current.id.MDO.BaseX * 256) + ((current.id.tileID - (CInt(Math.Truncate(current.id.tileID / 16)) * 16)) * 16)
        myZ = current.id.MDO.BaseZ
        'converts the zcoordinate to a string
        Dim zStr As String
        If myZ < 10 Then zStr = "0" & CStr(myZ) Else zStr = CStr(myZ)
        'Do a flood search on each walkable tile in the mTile
        For x As Integer = 1 To 16
            For y As Integer = 1 To 16

                'if the tile is walkable and is not closed
                If Not mNodeMap(x, y) = 2 Then
                    DebugColor = &H1
                    If Not myMapData.GetTileCost(myX + x - 1, myY + y - 1, myZ, True) = 255 Then

                        'increment the nodecount variable for marking tiles
                        curNodeID += CByte(1)

                        RotateDebugColor() 'changes the color written to the minimap in tibia when debugging
                        '1) Add the node to the open list.
                        mNodeMap(x, y) = 1
                        mHeap.AddItem(((y - 1) * 16) + (x - 1), 0)

                        Do While mHeap.length > 0
                            'a) Get a node from the openlist. 
                            tnode = mHeap.GetFromTop
                            'get the 17x17 map coordinates
                            tnodeY = CInt(Math.Truncate(tnode / 16))
                            tnodeX = tnode - CInt(Math.Truncate(tnodeY * 16))
                            tnodeY += 1
                            tnodeX += 1
                            'mark each tile opened in the search with a node id
                            'note: perimeter tiles arent added and so cannot be opened
                            myMapData.MarkNodeId(myX + tnodeX - 1, myY + tnodeY - 1, myZ, curNodeID)
                            'b) Switch it to the closed list. 
                            mNodeMap(tnodeX, tnodeY) = 2
                            'c) For each of the 8 squares adjacent to this current square …
                            For i As Integer = 0 To 8
                                If Not i = 4 Then 'for is this square
                                    'gets child i's x and y coordinates
                                    cnodeY = tnodeY + (CInt(Math.Truncate(i / 3)) - 1)
                                    cnodeX = tnodeX + (i - (CInt(Math.Truncate(i / 3)) * 3) - 1)
                                    'disables debugging for tiles outside of our mtile
                                    If DebugMap Then
                                        If cnodeX = 0 Or cnodeX = 17 Or cnodeY = 0 Or cnodeY = 17 Then
                                            tempDebugVal = DebugMap
                                            DebugMap = False
                                        End If
                                    End If
                                    'If it is walkable and if its not open or closed and is on the tibia map            
                                    If mNodeMap(cnodeX, cnodeY) = 0 Then
                                        If myX + cnodeX - 1 > MinX And myX + cnodeX - 1 < MinX + MapX And myY + cnodeY - 1 > MinY And myY + cnodeY - 1 < MinY + MapY Then
                                            If Not myMapData.GetTileCost(myX + cnodeX - 1, myY + cnodeY - 1, myZ, True) = 255 Then

                                                'If it isn’t on the open list  
                                                If mNodeMap(cnodeX, cnodeY) = 0 Then
                                                    'its outside of this mtile
                                                    If cnodeX = 0 Or cnodeX = 17 Or cnodeY = 0 Or cnodeY = 17 Then
                                                        'determine the childs base map coordinates and its tile id
                                                        cTileY = CInt(Math.Truncate(current.id.tileID / 16))
                                                        cTileX = current.id.tileID - (cTileY * 16)
                                                        'defaults to the current basex
                                                        cmapx = current.id.MDO.BaseX
                                                        If cnodeX = 0 Then
                                                            'if the tile is on the left edge then the mapx is one less
                                                            If current.id.tileID - (CInt(Math.Truncate(current.id.tileID / 16)) * 16) = 0 Then
                                                                cmapx -= 1
                                                                cTileX = 15
                                                            Else
                                                                cTileX -= 1
                                                            End If
                                                        ElseIf cnodeX = 17 Then
                                                            'if the tile is on the right edge then the mapx is one more
                                                            If current.id.tileID - (CInt(Math.Truncate(current.id.tileID / 16)) * 16) = 15 Then
                                                                cmapx += 1
                                                                cTileX = 0
                                                            Else
                                                                cTileX += 1
                                                            End If
                                                        End If

                                                        'defaults to the current basey
                                                        cmapy = current.id.MDO.BaseY
                                                        If cnodeY = 0 Then
                                                            'if the tile is at the top then the mapy is one less
                                                            If current.id.tileID < 16 Then
                                                                cmapy -= 1
                                                                cTileY = 15
                                                            Else
                                                                cTileY -= 1
                                                            End If
                                                        ElseIf cnodeY = 17 Then
                                                            'if the tile is at the bottom then the mapy is one more
                                                            If current.id.tileID > 239 Then
                                                                cmapy += 1
                                                                cTileY = 0
                                                            Else
                                                                cTileY += 1
                                                            End If
                                                        End If

                                                        cid = cTileX + (cTileY * 16)
                                                        childmTile = myMapData.GetmTile(CStr(cmapx) & CStr(cmapy) & zStr & ".map", cid)
                                                        'if it has been flagged as discovered then 
                                                        If childmTile.Discovered Then
                                                            'return the node id of this tile
                                                            cnodeid = myMapData.GetNodeId(childmTile.id.MDO.TibMap.Name, myMapData.GetTileOffset(myY + cnodeY - 1) + (myMapData.GetTileOffset(myX + cnodeX - 1) * 256), 2)
                                                            'if it has not been flagged as mapped
                                                            '(mapped tiles are assumed to be connected already)
                                                            '(any errors there are caught by the dataminer)
                                                            If Not childmTile.Mapped Then
                                                                'build the connection
                                                                childmTile.ConnectTo(curNodeID, current, curNodeID)
                                                                current.ConnectTo(curNodeID, childmTile, cnodeid)
                                                            End If
                                                        Else
                                                            dontAdd = False
                                                            For Each c In current.Children
                                                                If c.mTileNumber = childmTile.id.mTileNumber Then dontAdd = True
                                                            Next
                                                            If Not dontAdd = True Then
                                                                'add it to this mTiles list of children
                                                                current.Children.Push(childmTile.id)
                                                            End If
                                                        End If
                                                    Else
                                                        'if its inside this mtile
                                                        mNodeMap(cnodeX, cnodeY) = 1
                                                        mHeap.AddItem(((cnodeY - 1) * 16) + (cnodeX - 1), 0)
                                                    End If
                                                End If

                                            End If
                                        End If
                                    End If
                                    'resets the debug flag if it was disabled before
                                    DebugMap = tempDebugVal
                                End If

                            Next


                        Loop
                    End If
                End If
            Next
        Next
        'flag this mTile as discovered
        current.Discovered = True
        RecentDiscovered += 1
    End Sub
    Public Sub MapMTile(ByVal current As mTile)
        If current.Mapped = True Then Exit Sub
        '	if the mTile is not flagged as discovered then discover it
        If current.Discovered = False Then DiscoverMTile(current)

        'if there are children
        If Not current.Children.Count = 0 Then
            Dim childmTile As mTile
            Dim id As mTile.tileID
            'for each child mTile
            Do While current.Children.Count > 0
                'get a childs tileid
                id = current.Children.Pop
                'use the id to get the mtile
                childmTile = myMapData.GetmTile(id.MDO.TibMap.Name, id.tileID) 'gets the next child
                'discover the child mTile
                DiscoverMTile(childmTile) 'discovers the child
            Loop

        End If
        'flag this mTile as mapped
        current.Mapped = True
        RecentMapped += 1
    End Sub
    Public Sub MineMacroPaths()
        'continually performs datamining operations to get macro path data
        'intended for use on a seperate thread
        Dim start As Date = Now()
        Dim current As mTile
        Dim id As mTile.tileID
        Dim statusmsg As String
        Do
            'generates an informational status message and sends it to the client
            If RecentDiscovered + RecentMapped = 0 Then start = Now()
            If Now.Subtract(start).Ticks > 30000 * System.TimeSpan.TicksPerMillisecond Then
                If RecentDiscovered + RecentMapped > 0 Then
                    statusmsg = CStr(RecentDiscovered) & " nodes discovered and " & CStr(RecentMapped) & " nodes mapped in " & CStr(Now.Subtract(start).Ticks / System.TimeSpan.TicksPerMillisecond / 1000) & " seconds : "
                    If Mappable.Count = 0 Then
                        statusmsg &= "Dataminer Queue is Empty"
                    Else
                        statusmsg &= CStr(Mappable.Count) & " left in the queue"
                    End If
                    RecentDiscovered = 0
                    RecentMapped = 0
                    Tibia.Packets.Incoming.TextMessagePacket.Send(myClient, Tibia.Packets.StatusMessage.ConsoleBlue, statusmsg)
                    start = Now()
                End If
            End If

            'If the queue is not empty
            If Mappable.Count > 0 Then
                'get an mtile id
                id = Mappable.Dequeue
                'use the id to get the tile object
                current = myMapData.GetmTile(id.MDO.TibMap.Name, id.tileID) 'pulls discovered mTiles first
                'map the mTile
                MapMTile(current)
            Else
                'if the queue is empty then wait half a second
                Threading.Thread.Sleep(500)
            End If
            'exit the sub if the stop flag is raised
            If StopMining = True Then Exit Sub
            'pause the miner while the active mining flag is turned off
            Do While ActiveMining = False
                Threading.Thread.Sleep(500) 'thread sleeps for .5 seconds
                Application.DoEvents()
            Loop

        Loop
    End Sub


    Public Sub RotateDebugColor()
        DebugColor = CByte(255 * Rnd())
    End Sub
End Class
