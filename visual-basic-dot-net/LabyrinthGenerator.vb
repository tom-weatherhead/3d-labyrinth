#Const TRY_TO_REFACTOR_LABYRINTH = True ' "#Const" is equivalent to C#'s "#define".
#Const DEBUG_WRITELINE = True

Public Class LabyrinthGenerator
    Public ReadOnly numberOfLevels As Integer
    Public ReadOnly numberOfRoomsPerLevel As Integer
    Public ReadOnly numberOfExtraConnections As Integer
    Public numberOfExtraConnectionsAdded As Integer = 0
    Private ReadOnly extraConnections As List(Of KeyValuePair(Of RoomInfo, RoomInfo)) = New List(Of KeyValuePair(Of RoomInfo, RoomInfo))
    Public ReadOnly rooms As List(Of RoomInfo) = New List(Of RoomInfo)
    Public ReadOnly roomLabels As Dictionary(Of RoomInfo, Integer) = New Dictionary(Of RoomInfo, Integer)
    Public ReadOnly connections As Dictionary(Of RoomInfo, List(Of RoomInfo)) = New Dictionary(Of RoomInfo, List(Of RoomInfo))
    Public ReadOnly openList As List(Of RoomInfo) = New List(Of RoomInfo)
    Private ReadOnly random As Random = New Random
    Private numberOfDifferentLabels As Integer = 0
    Private roomGoal As RoomInfo
    Private ReadOnly booksInRooms As Dictionary(Of RoomInfo, String) = New Dictionary(Of RoomInfo, String)
    Private numberOfAttemptsToRefactor As Integer = 0
    Private Const maximumNumberOfAttemptsToRefactor As Integer = 100

    Public Sub New(ByVal l As Integer, ByVal r As Integer, ByVal xc As Integer)

        If l < 2 Or r < 4 Then
            Throw New ArgumentException("Invalid parameter(s).")
        End If

        numberOfLevels = l
        numberOfRoomsPerLevel = r
        numberOfExtraConnections = xc
    End Sub

    Private Function FindConflictingConnections(ByVal room1 As RoomInfo, ByVal room2 As RoomInfo) As Boolean
        ' Test 0: Room labels ("blob numbers").

        'if (roomLabels[room1] == roomLabels[room2])
        '{
        '    return true;    // There is a conflict.
        '}

        ' Test 1: Room 3 must not be connected to room 4.

        ' 4  2
        '  \/
        '  /\
        ' 1  3

        Dim room3 As RoomInfo = New RoomInfo(room2.levelNumber, room1.roomNumber)
        Dim room4 As RoomInfo = New RoomInfo(room1.levelNumber, room2.roomNumber)

        If connections(room3).Contains(room4) Then
            Return True
        End If

        ' Test 2: Room 3 must not be connected to room 1.

        ' 3
        '  \
        '   1
        '  /
        ' 2

        room3 = New RoomInfo(2 * room1.levelNumber - room2.levelNumber, room2.roomNumber)

        If connections.ContainsKey(room3) AndAlso connections(room3).Contains(room1) Then   ' "AndAlso" is VB.Net's short-circuiting "And" operator.
            Return True
        End If

        ' Test 3: Room 3 must not be connected to room 2.

        ' 3
        '  \
        '   2
        '  /
        ' 1

        room3 = New RoomInfo(2 * room2.levelNumber - room1.levelNumber, room1.roomNumber)

        If connections.ContainsKey(room3) AndAlso connections(room3).Contains(room2) Then
            Return True
        End If

        Return False    ' There is no conflict.
    End Function

#If TRY_TO_REFACTOR_LABYRINTH Then
    Private Function FindUnusedLabel() As Integer
        Dim result As Integer = 0

        While roomLabels.Values.Contains(result)
            result = result + 1
        End While

#If DEAD_CODE Then 'DEBUG_WRITELINE
        var setOfLabels = new HashSet<int>(roomLabels.Values);

        Console.WriteLine("Labels in use: {0}; Unused label: {1}.", string.Join(", ", setOfLabels), result);
#End If

        Return result
    End Function

    Private Sub PropagateNewLabel(ByVal room As RoomInfo, ByVal newLabel As Integer, ByVal addRoomsToOpenList As Boolean)
        Dim openListLocal As Stack(Of RoomInfo) = New Stack(Of RoomInfo)
        Dim closedList = New HashSet(Of RoomInfo)   ' Similar to C#'s "var".  "Option Infer On" (in the project settings) works.

        openListLocal.Push(room)

        While openListLocal.Count > 0
            room = openListLocal.Pop()
            roomLabels(room) = newLabel
            closedList.Add(room)

            If addRoomsToOpenList Then
                openList.Add(room)
            End If

            For Each room2 In connections(room)

                If Not openListLocal.Contains(room2) And Not closedList.Contains(room2) Then
                    openListLocal.Push(room2)
                End If
            Next
        End While
    End Sub

    Private Sub FindPossibleNeighboursWithDifferentLabels(ByRef room1 As RoomInfo, ByRef room2 As RoomInfo)
        Dim openListLocal = New List(Of RoomInfo)(rooms)

        While openListLocal.Count > 0
            room1 = openListLocal(random.Next(openListLocal.Count))
            openListLocal.Remove(room1)

            Dim possibleNeighbours = room1.GeneratePossibleNeighbours(Me)

            While possibleNeighbours.Count > 0
                room2 = possibleNeighbours(random.Next(possibleNeighbours.Count))
                possibleNeighbours.Remove(room2)

                If roomLabels(room1) <> roomLabels(room2) Then
                    Return
                End If
            End While
        End While

        Throw New Exception("Unable to find possible neighbours with different labels.")
    End Sub

    Private Sub Refactor()
        Console.WriteLine("Refactoring...")

        Dim room1 As RoomInfo = Nothing
        Dim room2 As RoomInfo = Nothing

        FindPossibleNeighboursWithDifferentLabels(room1, room2)

        ' Resolve the conflicts that are preventing a connection between room1 and room2.

        ' Test 1: Room 3 must not be connected to room 4.

        ' 4  2
        '  \/
        '  /\
        ' 1  3

        Dim room3 = New RoomInfo(room2.levelNumber, room1.roomNumber)
        Dim room4 = New RoomInfo(room1.levelNumber, room2.roomNumber)

        If connections(room3).Contains(room4) Then
            Console.WriteLine("Found a Type 1 conflict.")
            connections(room3).Remove(room4)
            connections(room4).Remove(room3)
            PropagateNewLabel(room3, FindUnusedLabel(), True)
            PropagateNewLabel(room4, FindUnusedLabel(), True)
        End If

        ' Test 2: Room 3 must not be connected to room 1.

        ' 3
        '  \
        '   1
        '  /
        ' 2

        room3 = New RoomInfo(2 * room1.levelNumber - room2.levelNumber, room2.roomNumber)

        If connections.ContainsKey(room3) AndAlso connections(room3).Contains(room1) Then
            Console.WriteLine("Found a Type 2 conflict.")
            connections(room1).Remove(room3)
            connections(room3).Remove(room1)
            PropagateNewLabel(room3, FindUnusedLabel(), True)
        End If

        ' Test 3: Room 3 must not be connected to room 2.

        ' 3
        '  \
        '   2
        '  /
        ' 1

        room3 = New RoomInfo(2 * room2.levelNumber - room1.levelNumber, room1.roomNumber)

        If connections.ContainsKey(room3) AndAlso connections(room3).Contains(room2) Then
            Console.WriteLine("Found a Type 3 conflict.")
            connections(room2).Remove(room3)
            connections(room3).Remove(room2)
            PropagateNewLabel(room3, FindUnusedLabel(), True)
        End If

        ' Connect room1 and room2.
        PropagateNewLabel(room2, roomLabels(room1), False)
        connections(room1).Add(room2)
        connections(room2).Add(room1)

        Dim setOfLabels = New HashSet(Of Integer)(roomLabels.Values)

        numberOfDifferentLabels = setOfLabels.Count
    End Sub

    Private Sub FinalValidityCheck()
        PropagateNewLabel(New RoomInfo(0, 0), FindUnusedLabel(), False)

        Dim setOfLabels = New HashSet(Of Integer)(roomLabels.Values)

        If setOfLabels.Count > 1 Then
            Throw New Exception(String.Format("The labyrinth is in at least {0} separate blobs.", setOfLabels.Count))
        End If

#If DEBUG_WRITELINE Then
        Console.WriteLine("The labyrinth is a single blob.")
#End If
    End Sub
#End If

    Public Sub AddExtraConnections()
        ' Reuse openList.
        openList.Clear()
        openList.AddRange(rooms)

        While numberOfExtraConnectionsAdded < numberOfExtraConnections And openList.Count > 0
            Dim room1 = openList(random.Next(openList.Count))
            Dim possibleNeighbours = room1.GeneratePossibleNeighbours(Me)
            Dim room2 As RoomInfo = Nothing

            While IsNothing(room2) And possibleNeighbours.Count > 0
                room2 = possibleNeighbours(random.Next(possibleNeighbours.Count))

                If Not FindConflictingConnections(room1, room2) Then
                    Exit While  ' This is equivalent to using C#'s "break" to break out of the While loop.
                End If

                possibleNeighbours.Remove(room2)
                room2 = Nothing
            End While

            If IsNothing(room2) Then
                openList.Remove(room1)
                Continue While
            End If

            ' We have now chosen room1 and room2.
            connections(room1).Add(room2)
            connections(room2).Add(room1)
            extraConnections.Add(New KeyValuePair(Of RoomInfo, RoomInfo)(room1, room2))
            numberOfExtraConnectionsAdded = numberOfExtraConnectionsAdded + 1
        End While
    End Sub

    Public Sub Generate()
        Dim label = 0

        numberOfDifferentLabels = numberOfLevels * numberOfRoomsPerLevel

        For l = 0 To numberOfLevels - 1 ' As Integer

            For r = 0 To numberOfRoomsPerLevel - 1
                Dim room = New RoomInfo(l, r)

                rooms.Add(room)
                roomLabels(room) = label
                label = label + 1
                connections(room) = New List(Of RoomInfo)
                openList.Add(room)
            Next
        Next

#If USE_ULTIMATE_ROOMS_LIST Then
        for (int r = 0; r < numberOfRoomsPerLevel; ++r)
        {
            ultimateRoomsList.Add(new RoomInfo(0, r));
            ultimateRoomsList.Add(new RoomInfo(numberOfLevels - 1, r));
        }
#End If

        While numberOfDifferentLabels > 1

            If openList.Count = 0 Then
#If TRY_TO_REFACTOR_LABYRINTH Then

                If numberOfAttemptsToRefactor >= maximumNumberOfAttemptsToRefactor Then
                    Throw New Exception(String.Format("Attempted to refactor {0} times; all failed.", numberOfAttemptsToRefactor))
                End If

                numberOfAttemptsToRefactor = numberOfAttemptsToRefactor + 1
                Refactor()
#Else
                throw new Exception("Open list is unexpectedly empty.");
#End If
            End If

#If USE_ULTIMATE_ROOMS_LIST Then
            RoomInfo room1;

            if (ultimateRoomsList.Count > 0)
            {
                room1 = ultimateRoomsList[random.Next(ultimateRoomsList.Count)];
                ultimateRoomsList.Remove(room1);
            }
            else
            {
                room1 = openList[random.Next(openList.Count)];
            }
#Else
            Dim room1 = openList(random.Next(openList.Count))
#End If

            Dim possibleNeighbours = room1.GeneratePossibleNeighbours(Me)
            Dim room2 As RoomInfo = Nothing

            While IsNothing(room2) And possibleNeighbours.Count > 0
                room2 = possibleNeighbours(random.Next(possibleNeighbours.Count))

                If roomLabels(room1) <> roomLabels(room2) AndAlso Not FindConflictingConnections(room1, room2) Then
                    Exit While
                End If

                possibleNeighbours.Remove(room2)
                room2 = Nothing
            End While

            If IsNothing(room2) Then
                openList.Remove(room1)
                Continue While
            End If

            ' We have now chosen room1 and room2.
            connections(room1).Add(room2)
            connections(room2).Add(room1)

            ' Join the two "blobs" to which the two rooms belong, by modifying room labels.
            Dim label1 = roomLabels(room1)
            Dim label2 = roomLabels(room2)
            Dim minLabel = Math.Min(label1, label2)
            Dim maxLabel = Math.Max(label1, label2)

            For Each room In rooms

                If roomLabels(room) = maxLabel Then
                    roomLabels(room) = minLabel
                End If
            Next

            numberOfDifferentLabels = numberOfDifferentLabels - 1
        End While

        If numberOfExtraConnections > 0 Then
            AddExtraConnections()
        End If
    End Sub

    Public Sub Report()

        For Each room In rooms

            For Each otherRoom In connections(room)
                Console.WriteLine("{0} to {1}", room, otherRoom)
            Next
        Next

        If numberOfExtraConnections > 0 Then
#If DEBUG_WRITELINE Then

            For Each extraConnection In extraConnections
                Console.WriteLine("Extra connection added: {0} to {1}.", extraConnection.Key, extraConnection.Value)
            Next
#End If

            Console.WriteLine("{0} extra connection(s) requested; {1} added.", numberOfExtraConnections, numberOfExtraConnectionsAdded)
        End If

#If TRY_TO_REFACTOR_LABYRINTH Then
        If numberOfAttemptsToRefactor > 0 Then
            Console.WriteLine("The labyrinth was refactored {0} time(s).", numberOfAttemptsToRefactor)
        End If

        FinalValidityCheck()
#End If
    End Sub

    Private Function FindShortestPathBetweenRooms(ByVal room As RoomInfo, ByVal roomGoalLocal As RoomInfo) As List(Of RoomInfo)
        Dim openListLocal = New Queue(Of RoomInfo)
        Dim paths = New Dictionary(Of RoomInfo, List(Of RoomInfo))

        openListLocal.Enqueue(room)
        paths(room) = New List(Of RoomInfo) From {room}

        If room.Equals(roomGoalLocal) Then
            Return paths(room)
        End If

        While openListLocal.Count > 0
            room = openListLocal.Dequeue()

            For Each room2 In connections(room)

                If Not paths.Keys.Contains(room2) Then      ' paths.Keys is essentially the union of openListLocal and closedList.
                    openListLocal.Enqueue(room2)
                    paths(room2) = New List(Of RoomInfo)(paths(room))
                    paths(room2).Add(room2)

                    If room2.Equals(roomGoalLocal) Then
                        Return paths(room2)
                    End If
                End If
            Next
        End While

        ' Here, room is the last room to be dequeued (and thus the last room to be enqueued).
        Return paths(room)
    End Function

    Private Function FindLongestPathFromRoom(ByVal room As RoomInfo) As List(Of RoomInfo)
        Return FindShortestPathBetweenRooms(room, Nothing)
    End Function

    Public Sub PrintLongestPath()
        Dim path1 = FindLongestPathFromRoom(New RoomInfo(numberOfLevels - 1, numberOfRoomsPerLevel - 1))
        Dim longestPath = FindLongestPathFromRoom(path1(path1.Count - 1))

        Console.WriteLine()
#If DEBUG_ONLY Then
        Console.WriteLine("The longest path contains {0} rooms:", longestPath.Count);
        Console.WriteLine(string.Join(" to ", longestPath));
#Else
        Console.WriteLine("The longest path contains {0} rooms.", longestPath.Count)
#End If

        roomGoal = longestPath(longestPath.Count - 1)

        Dim pathFromOriginToGoal = FindShortestPathBetweenRooms(New RoomInfo(0, 0), roomGoal)

        Console.WriteLine()
#If DEBUG_ONLY Then
        Console.WriteLine("Aristotle's Second Book of the Poetics is in Room {0}.", roomGoal);
        Console.WriteLine();
        Console.WriteLine("The path from Room (0, 0) to Room {0} contains {1} rooms:", roomGoal, pathFromOriginToGoal.Count);
        Console.WriteLine(string.Join(" to ", pathFromOriginToGoal));
#Else
        Console.WriteLine("The path from Room (0, 0) to the goal contains {0} rooms.", pathFromOriginToGoal.Count)
#End If
    End Sub

    Private Sub PlaceBooksInRooms()
        Dim books = New List(Of String) From
        {
            "The First Book of the Poetics of Aristotle",
            "The Iliad by Homer",
            "The Odyssey by Homer",
            "The Republic by Plato",
            "Categories by Aristotle",
            "Physics by Aristotle",
            "Nicomachean Ethics by Aristotle",
            "The Aeneid by Virgil"
        }
        Dim openListLocal = New List(Of RoomInfo)(rooms)
        Dim numBooksPlaced = 1

        booksInRooms(roomGoal) = "The Second Book of the Poetics of Aristotle"
        openListLocal.Remove(roomGoal)

        While numBooksPlaced * 3 < rooms.Count And books.Count > 0
            Dim room = openListLocal(random.Next(openListLocal.Count))
            Dim book = books(random.Next(books.Count))

            openListLocal.Remove(room)
            books.Remove(book)
            booksInRooms(room) = book
            numBooksPlaced = numBooksPlaced + 1
        End While
    End Sub

    Private Sub ReportProximityToJorge(ByVal room As RoomInfo, ByVal JorgesRoom As RoomInfo)
        Dim path = FindShortestPathBetweenRooms(room, JorgesRoom)
        Dim distance = path.Count - 1

        If distance = 0 Then
            Console.WriteLine("* You and the Venerable Jorge are in the same room! *")
        ElseIf (distance <= 2) Then
            Console.WriteLine("The Venerable Jorge is very near.")
        ElseIf (distance <= 4) Then
            Console.WriteLine("The Venerable Jorge is near.")
        End If
    End Sub

    Private Function ConstructJorgesPath(ByVal JorgesRoom As RoomInfo) As List(Of RoomInfo)
        Dim JorgesGoal As RoomInfo

        Do
            JorgesGoal = rooms(random.Next(rooms.Count))
        Loop While (JorgesGoal.Equals(JorgesRoom))

        Return FindShortestPathBetweenRooms(JorgesRoom, JorgesGoal)
    End Function

    Public Sub NavigateLabyrinth()
        Dim roomsVisited = New HashSet(Of RoomInfo)
        Dim room = New RoomInfo(0, 0)
        Dim JorgesRoom = rooms(random.Next(rooms.Count))
        Dim JorgesPath = ConstructJorgesPath(JorgesRoom)
        Dim JorgesPathIndex = 0

        PlaceBooksInRooms()

        ' for (; ; )
        While True
            roomsVisited.Add(room)

            Console.WriteLine()
            Console.WriteLine("You are now in room {0}.", room)
            'Console.WriteLine("The Venerable Jorge is now in room {0}.", JorgesRoom);
            'Console.WriteLine("Jorge's destination is room {0}", JorgesPath[JorgesPath.Count - 1]);

            ReportProximityToJorge(room, JorgesRoom)

            If booksInRooms.ContainsKey(room) Then
                Console.WriteLine("You have found the book '{0}'.", booksInRooms(room))
            End If

            If room.Equals(roomGoal) Then
                Console.WriteLine("**** Congratulations!  You have reached the goal! ****")
            End If

            Dim neighbouringRooms = connections(room)

            Console.WriteLine("Possible moves:")

            For i = 0 To neighbouringRooms.Count - 1
                Dim neighbouringRoom = neighbouringRooms(i)
                Dim str = String.Format("  {0}. {1}", i, neighbouringRoom)

                If roomsVisited.Contains(neighbouringRoom) Then
                    str = str + " Visited"
                End If

                Console.WriteLine(str)
            Next

            Console.Write("Your move (or (h)elp or (q)uit): ")

            Dim input = Console.ReadLine()
            Dim inputInt As Integer
            Dim inputIsInt = Integer.TryParse(input, inputInt)

            If String.IsNullOrEmpty(input) Then
                Console.WriteLine("The input is empty.")
            ElseIf inputIsInt Then

                If inputInt < 0 Or inputInt >= neighbouringRooms.Count Then
                    Console.WriteLine("The input is out of range.")
                Else
                    room = neighbouringRooms(inputInt)
                    ReportProximityToJorge(room, JorgesRoom)
                End If
            ElseIf input(0) = "h" Then
                Dim pathToGoal = FindShortestPathBetweenRooms(room, roomGoal)

                Console.WriteLine("Path to goal: {0}", String.Join(" to ", pathToGoal))
            ElseIf input(0) = "q" Then
                Exit While
            Else
                Console.WriteLine("The input was not recognized.")
            End If

            ' Jorge's move.
            JorgesPathIndex = JorgesPathIndex + 1

            If JorgesPathIndex >= JorgesPath.Count Then
                JorgesPath = ConstructJorgesPath(JorgesRoom)
                JorgesPathIndex = 1
            End If

            JorgesRoom = JorgesPath(JorgesPathIndex)
        End While
    End Sub
End Class
