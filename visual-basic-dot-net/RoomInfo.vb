Public Class RoomInfo
    Public ReadOnly levelNumber As Integer
    Public ReadOnly roomNumber As Integer

    Public Sub New(ByVal l As Integer, ByVal r As Integer)  ' "New" is the name of the constructor method.
        levelNumber = l
        roomNumber = r
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean

        If Object.ReferenceEquals(Me, obj) Then ' "Me" is equivalent to C#'s "this".
            Return True
        End If

        Dim otherRoomInfo As RoomInfo = TryCast(obj, RoomInfo)  ' "TryCast" is equivalent to C#'s "as" operator.

        ' "Nothing" is equivalent to C#'s "null".
        Return Not IsNothing(otherRoomInfo) AndAlso roomNumber = otherRoomInfo.roomNumber AndAlso levelNumber = otherRoomInfo.levelNumber
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return roomNumber + 1024 * levelNumber
    End Function

    Public Overrides Function ToString() As String
        Return String.Format("({0}, {1})", levelNumber, roomNumber)
    End Function

    Private Function GeneratePossibleNeighboursOnLevel(ByVal generator As LabyrinthGenerator, ByVal newLevel As Integer) As List(Of RoomInfo)
        Dim result As List(Of RoomInfo) = New List(Of RoomInfo)

        If roomNumber = generator.numberOfRoomsPerLevel - 1 Then
            ' Rooms with this room number form the central core of the tower.

            For i As Integer = 0 To generator.numberOfRoomsPerLevel - 2
                result.Add(New RoomInfo(newLevel, i))
            Next
        Else
            result.Add(New RoomInfo(newLevel, (roomNumber + 1) Mod (generator.numberOfRoomsPerLevel - 1)))
            result.Add(New RoomInfo(newLevel, (roomNumber + generator.numberOfRoomsPerLevel - 2) Mod (generator.numberOfRoomsPerLevel - 1)))
            result.Add(New RoomInfo(newLevel, generator.numberOfRoomsPerLevel - 1))
        End If

        Return result
    End Function

    Public Function GeneratePossibleNeighbours(ByVal generator As LabyrinthGenerator) As List(Of RoomInfo)
        Dim result As List(Of RoomInfo) = New List(Of RoomInfo)

        If levelNumber > 0 Then
            result.AddRange(GeneratePossibleNeighboursOnLevel(generator, levelNumber - 1))
        End If

        If levelNumber < generator.numberOfLevels - 1 Then
            result.AddRange(GeneratePossibleNeighboursOnLevel(generator, levelNumber + 1))
        End If

        Return result
    End Function
End Class
