Public Class BinaryHeap
    'The BinaryHeap class is used in the pathfinder processes of the cartographer class
    'for sorting open nodes.

    Public MaxSize As Integer
    Public ItemArr() As Integer
    Public ValueArr() As Integer
    Public length As Integer = 0
    Public Sub New(ByVal maxvalues As Integer)
        MaxSize = maxvalues
        ReDim ItemArr(MaxSize)
        ReDim ValueArr(MaxSize)
    End Sub
    Public Function GetFromTop() As Integer
        Dim returnVar As Integer = ItemArr(1)
        If length = 0 Then
            Return 0
            Exit Function
        End If
        'delete the first item
        ItemArr(1) = 0
        ValueArr(1) = 0
        'Swap first and last
        Swap(length, 1)
        length -= 1
        Dim parent As Integer = 1
        Dim swapitem As Integer = 1

        Do
            parent = swapitem
            'id's the item to be swapped
            If ((parent * 2) + 1) <= length Then 'both children exist
                'picks the smaller of the two children
                If ValueArr(parent) >= ValueArr(parent * 2) Then swapitem = parent * 2
                If ValueArr(swapitem) >= ValueArr((parent * 2) + 1) Then swapitem = (parent * 2) + 1
            ElseIf (2 * parent) <= length Then 'one child exists
                If ValueArr(parent) >= ValueArr(parent * 2) Then swapitem = parent * 2
            End If
            'swaps if there is something to swap and if not it exits the loop
            If Not parent = swapitem Then Swap(parent, swapitem) Else Exit Do
        Loop

        Return returnVar
    End Function
    Private Sub Swap(ByVal pos1 As Integer, ByVal pos2 As Integer)
        Dim temp As Integer
        'swaps the item entry
        temp = ItemArr(pos2)
        ItemArr(pos2) = ItemArr(pos1)
        ItemArr(pos1) = temp
        'swaps the value entry
        temp = ValueArr(pos2)
        ValueArr(pos2) = ValueArr(pos1)
        ValueArr(pos1) = temp
    End Sub
    Public Sub AddItem(ByVal Item As Integer, ByVal Value As Integer)
        length += 1
        'the last element in the 2 arrays becomes the new entry
        ItemArr(length) = Item
        ValueArr(length) = Value
        Dim pos As Integer = length
        Dim parent As Integer
        Do While Not pos = 1 'as long as we are not at the first position
            parent = CInt(Math.Truncate(Int(pos / 2))) 'identify the parent
            If ValueArr(pos) <= ValueArr(parent) Then 'if the parent is smaller
                Swap(parent, pos) 'swap the parent with the new item
                pos = parent 'old parent position becomes the new current position
            Else
                Exit Do
            End If

        Loop
    End Sub
    Public Sub Reset()
        'clears the heap without having to reinitialize it
        ReDim ItemArr(MaxSize)
        ReDim ValueArr(MaxSize)
        length = 0
    End Sub
    Public Sub ReSortItem(ByVal node As Integer, ByVal value As Integer)
        Dim place As Integer
        'finds the location of our node in the heap
        For i As Integer = 1 To length
            If ItemArr(i) = node Then place = i
        Next
        'changes the nodes value in the ValueArr array
        ValueArr(place) = value
        Dim pos As Integer = place
        Dim parent As Integer
        'Starts trying to bubble it upwards by comparing it against its parent
        Do While Not pos = 1
            parent = CInt(Math.Truncate(Int(pos / 2)))
            If ValueArr(pos) <= ValueArr(parent) Then
                Swap(parent, pos)
                pos = parent
            Else
                Exit Do
            End If

        Loop
    End Sub
End Class
