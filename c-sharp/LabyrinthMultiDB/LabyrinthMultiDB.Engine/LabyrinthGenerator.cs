#define TRY_TO_REFACTOR_LABYRINTH
#define DEBUG_WRITELINE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabyrinthMultiDB.Engine
{
    public class LabyrinthGenerator
    {
        public readonly int numberOfLevels;
        public readonly int numberOfRoomsPerLevel;
        public readonly int numberOfExtraConnections;
        public int numberOfExtraConnectionsAdded = 0;
        private readonly List<KeyValuePair<RoomInfo, RoomInfo>> extraConnections = new List<KeyValuePair<RoomInfo, RoomInfo>>();
        public readonly List<RoomInfo> rooms = new List<RoomInfo>();
        public readonly Dictionary<RoomInfo, int> roomLabels = new Dictionary<RoomInfo, int>();
        public readonly Dictionary<RoomInfo, List<RoomInfo>> connections = new Dictionary<RoomInfo, List<RoomInfo>>();
        public readonly List<RoomInfo> openList = new List<RoomInfo>();
        private readonly Random random = new Random();
        private int numberOfDifferentLabels = 0;
        private RoomInfo roomGoal;
        public readonly Dictionary<RoomInfo, string> booksInRooms = new Dictionary<RoomInfo, string>();
#if TRY_TO_REFACTOR_LABYRINTH
        private int numberOfAttemptsToRefactor = 0;
        private const int maximumNumberOfAttemptsToRefactor = 100;
#endif

        public LabyrinthGenerator(int l, int r, int xc)
        {

            if (l < 2 || r < 4)
            {
                throw new ArgumentException("Invalid parameter(s).");
            }

            numberOfLevels = l;
            numberOfRoomsPerLevel = r;
            numberOfExtraConnections = xc;
        }

        private bool FindConflictingConnections(RoomInfo room1, RoomInfo room2)
        {
            // Test 0: Room labels ("blob numbers").

            /*
            if (roomLabels[room1] == roomLabels[room2])
            {
                return true;    // There is a conflict.
            }
             */

            // Test 1: Room 3 must not be connected to room 4.

            // 4  2
            //  \/
            //  /\
            // 1  3

            var room3 = new RoomInfo(room2.levelNumber, room1.roomNumber);
            var room4 = new RoomInfo(room1.levelNumber, room2.roomNumber);

            if (connections[room3].Contains(room4))
            {
                return true;
            }

            // Test 2: Room 3 must not be connected to room 1.

            // 3
            //  \
            //   1
            //  /
            // 2

            room3 = new RoomInfo(2 * room1.levelNumber - room2.levelNumber, room2.roomNumber);

            //if (connections.ContainsKey(room3) && connections[room3].Contains(room1))
            if (connections[room1].Contains(room3))
            {
                return true;
            }

            // Test 3: Room 3 must not be connected to room 2.

            // 3
            //  \
            //   2
            //  /
            // 1

            room3 = new RoomInfo(2 * room2.levelNumber - room1.levelNumber, room1.roomNumber);

            //if (connections.ContainsKey(room3) && connections[room3].Contains(room2))
            if (connections[room2].Contains(room3))
            {
                return true;
            }

            return false;   // There is no conflict.
        }

#if TRY_TO_REFACTOR_LABYRINTH
        private int FindUnusedLabel()
        {
            var result = 0;

            while (roomLabels.Values.Contains(result))
            {
                ++result;
            }

#if DEAD_CODE //DEBUG_WRITELINE
            var setOfLabels = new HashSet<int>(roomLabels.Values);

            Console.WriteLine("Labels in use: {0}; Unused label: {1}.", string.Join(", ", setOfLabels), result);
#endif

            return result;
        }

        private void PropagateNewLabel(RoomInfo room, int newLabel, bool addRoomsToOpenList)
        {
            var openListLocal = new Stack<RoomInfo>();
            var closedList = new HashSet<RoomInfo>();

            openListLocal.Push(room);

            while (openListLocal.Count > 0)
            {
                room = openListLocal.Pop();
                roomLabels[room] = newLabel;
                closedList.Add(room);

                if (addRoomsToOpenList)
                {
                    openList.Add(room);
                }

                foreach (var room2 in connections[room])
                {

                    if (!openListLocal.Contains(room2) && !closedList.Contains(room2))
                    {
                        openListLocal.Push(room2);
                    }
                }
            }
        }

        private void FindPossibleNeighboursWithDifferentLabels(out RoomInfo room1, out RoomInfo room2)
        {
            var openListLocal = new List<RoomInfo>(rooms);

            while (openListLocal.Count > 0)
            {
                room1 = openListLocal[random.Next(openListLocal.Count)];
                openListLocal.Remove(room1);

                var possibleNeighbours = room1.GeneratePossibleNeighbours(this);

                while (possibleNeighbours.Count > 0)
                {
                    room2 = possibleNeighbours[random.Next(possibleNeighbours.Count)];
                    possibleNeighbours.Remove(room2);

                    if (roomLabels[room1] != roomLabels[room2])
                    {
                        return;
                    }
                }
            }

            throw new Exception("Unable to find possible neighbours with different labels.");
        }

        private void Refactor()
        {
            Console.WriteLine("Refactoring...");

            RoomInfo room1;
            RoomInfo room2;

            FindPossibleNeighboursWithDifferentLabels(out room1, out room2);

            // Resolve the conflicts that are preventing a connection between room1 and room2.

            // Test 1: Room 3 must not be connected to room 4.

            // 4  2
            //  \/
            //  /\
            // 1  3

            var room3 = new RoomInfo(room2.levelNumber, room1.roomNumber);
            var room4 = new RoomInfo(room1.levelNumber, room2.roomNumber);

            if (connections[room3].Contains(room4))
            {
                Console.WriteLine("Found a Type 1 conflict.");
                connections[room3].Remove(room4);
                connections[room4].Remove(room3);
                PropagateNewLabel(room3, FindUnusedLabel(), true);
                PropagateNewLabel(room4, FindUnusedLabel(), true);
            }

            // Test 2: Room 3 must not be connected to room 1.

            // 3
            //  \
            //   1
            //  /
            // 2

            room3 = new RoomInfo(2 * room1.levelNumber - room2.levelNumber, room2.roomNumber);

            //if (connections.ContainsKey(room3) && connections[room3].Contains(room1))
            if (connections[room1].Contains(room3))
            {
                Console.WriteLine("Found a Type 2 conflict.");
                connections[room1].Remove(room3);
                connections[room3].Remove(room1);
                PropagateNewLabel(room3, FindUnusedLabel(), true);
            }

            // Test 3: Room 3 must not be connected to room 2.

            // 3
            //  \
            //   2
            //  /
            // 1

            room3 = new RoomInfo(2 * room2.levelNumber - room1.levelNumber, room1.roomNumber);

            //if (connections.ContainsKey(room3) && connections[room3].Contains(room2))
            if (connections[room2].Contains(room3))
            {
                Console.WriteLine("Found a Type 3 conflict.");
                connections[room2].Remove(room3);
                connections[room3].Remove(room2);
                PropagateNewLabel(room3, FindUnusedLabel(), true);
            }

            // Connect room1 and room2.
            PropagateNewLabel(room2, roomLabels[room1], false);
            connections[room1].Add(room2);
            connections[room2].Add(room1);

            var setOfLabels = new HashSet<int>(roomLabels.Values);

            numberOfDifferentLabels = setOfLabels.Count;
        }

        private void FinalValidityCheck()
        {
            PropagateNewLabel(new RoomInfo(0, 0), FindUnusedLabel(), false);

            var setOfLabels = new HashSet<int>(roomLabels.Values);

            if (setOfLabels.Count > 1)
            {
                throw new Exception(string.Format("The labyrinth is in at least {0} separate blobs.", setOfLabels.Count));
            }

#if DEBUG_WRITELINE
            Console.WriteLine("The labyrinth is a single blob.");
#endif
        }
#endif

        public void AddExtraConnections()
        {
            // Reuse openList.
            openList.Clear();
            openList.AddRange(rooms);

            while (numberOfExtraConnectionsAdded < numberOfExtraConnections && openList.Count > 0)
            {
                var room1 = openList[random.Next(openList.Count)];
                var possibleNeighbours = room1.GeneratePossibleNeighbours(this);
                RoomInfo room2 = null;

                while (room2 == null && possibleNeighbours.Count > 0)
                {
                    room2 = possibleNeighbours[random.Next(possibleNeighbours.Count)];

                    if (!FindConflictingConnections(room1, room2))
                    {
                        break;
                    }

                    possibleNeighbours.Remove(room2);
                    room2 = null;
                }

                if (room2 == null)
                {
                    openList.Remove(room1);
                    continue;
                }

                // We have now chosen room1 and room2.
                connections[room1].Add(room2);
                connections[room2].Add(room1);
                extraConnections.Add(new KeyValuePair<RoomInfo, RoomInfo>(room1, room2));
                ++numberOfExtraConnectionsAdded;
            }
        }

        public void Generate()
        {
            var label = 0;

            numberOfDifferentLabels = numberOfLevels * numberOfRoomsPerLevel;

            for (int l = 0; l < numberOfLevels; ++l)
            {

                for (int r = 0; r < numberOfRoomsPerLevel; ++r)
                {
                    var room = new RoomInfo(l, r);

                    rooms.Add(room);
                    roomLabels[room] = label++;
                    connections[room] = new List<RoomInfo>();
                    openList.Add(room);
                }
            }

            while (numberOfDifferentLabels > 1)
            {

                if (openList.Count == 0)
                {
#if TRY_TO_REFACTOR_LABYRINTH

                    if (numberOfAttemptsToRefactor >= maximumNumberOfAttemptsToRefactor)
                    {
                        throw new Exception(string.Format("Attempted to refactor {0} times; all failed.", numberOfAttemptsToRefactor));
                    }

                    ++numberOfAttemptsToRefactor;
                    Refactor();
#else
                    throw new Exception("Open list is unexpectedly empty.");
#endif
                }

                var room1 = openList[random.Next(openList.Count)];
                var possibleNeighbours = room1.GeneratePossibleNeighbours(this);
                RoomInfo room2 = null;

                while (room2 == null && possibleNeighbours.Count > 0)
                {
                    room2 = possibleNeighbours[random.Next(possibleNeighbours.Count)];

                    if (roomLabels[room1] != roomLabels[room2] && !FindConflictingConnections(room1, room2))
                    {
                        break;
                    }

                    possibleNeighbours.Remove(room2);
                    room2 = null;
                }

                if (room2 == null)
                {
                    openList.Remove(room1);
                    continue;
                }

                // We have now chosen room1 and room2.
                connections[room1].Add(room2);
                connections[room2].Add(room1);

                // Join the two "blobs" to which the two rooms belong, by modifying room labels.
                var label1 = roomLabels[room1];
                var label2 = roomLabels[room2];
                var minLabel = Math.Min(label1, label2);
                var maxLabel = Math.Max(label1, label2);

                foreach (var room in rooms)
                {

                    if (roomLabels[room] == maxLabel)
                    {
                        roomLabels[room] = minLabel;
                    }
                }

                --numberOfDifferentLabels;
            }

            if (numberOfExtraConnections > 0)
            {
                AddExtraConnections();
            }

            Report();
            PrintLongestPath();     // This sets roomGoal.
            PlaceBooksInRooms();    // This uses roomGoal.
        }

        public void Report()
        {

            foreach (var room in rooms)
            {

                foreach (var otherRoom in connections[room])
                {
                    Console.WriteLine("{0} to {1}", room, otherRoom);
                }
            }

            if (numberOfExtraConnections > 0)
            {
#if DEBUG_WRITELINE

                foreach (var extraConnection in extraConnections)
                {
                    Console.WriteLine("Extra connection added: {0} to {1}.", extraConnection.Key, extraConnection.Value);
                }
#endif

                Console.WriteLine("{0} extra connection(s) requested; {1} added.", numberOfExtraConnections, numberOfExtraConnectionsAdded);
            }

#if TRY_TO_REFACTOR_LABYRINTH
            if (numberOfAttemptsToRefactor > 0)
            {
                Console.WriteLine("The labyrinth was refactored {0} time(s).", numberOfAttemptsToRefactor);
            }

            FinalValidityCheck();
#endif
        }

        private List<RoomInfo> FindShortestPathBetweenRooms(ILabyrinthClient client, RoomInfo room, RoomInfo roomGoalLocal)
        {
            var openListLocal = new Queue<RoomInfo>();
            var paths = new Dictionary<RoomInfo, List<RoomInfo>>();

            openListLocal.Enqueue(room);
            paths[room] = new List<RoomInfo>() { room };

            if (room.Equals(roomGoalLocal))
            {
                return paths[room];
            }

            while (openListLocal.Count > 0)
            {
                room = openListLocal.Dequeue();

                var connectionsToRoom = (client != null) ? client.QueryConnections(room) : connections[room];

                foreach (var room2 in connectionsToRoom)
                {

                    if (!paths.Keys.Contains(room2))    // paths.Keys is essentially the union of openListLocal and closedList.
                    {
                        openListLocal.Enqueue(room2);
                        paths[room2] = new List<RoomInfo>(paths[room]);
                        paths[room2].Add(room2);

                        if (room2.Equals(roomGoalLocal))
                        {
                            return paths[room2];
                        }
                    }
                }
            }

            // Here, room is the last room to be dequeued (and thus the last room to be enqueued).
            return paths[room];
        }

        private List<RoomInfo> FindLongestPathFromRoom(RoomInfo room)
        {
            return FindShortestPathBetweenRooms(null, room, null);
        }

        public void PrintLongestPath()
        {
            var path1 = FindLongestPathFromRoom(new RoomInfo(numberOfLevels - 1, numberOfRoomsPerLevel - 1));
            var longestPath = FindLongestPathFromRoom(path1[path1.Count - 1]);

            Console.WriteLine();
#if DEBUG_ONLY
            Console.WriteLine("The longest path contains {0} rooms:", longestPath.Count);
            Console.WriteLine(string.Join(" to ", longestPath));
#else
            Console.WriteLine("The longest path contains {0} rooms.", longestPath.Count);
#endif

            roomGoal = longestPath[longestPath.Count - 1];

            var pathFromOriginToGoal = FindShortestPathBetweenRooms(null, new RoomInfo(0, 0), roomGoal);

            Console.WriteLine();
#if DEBUG_ONLY
            Console.WriteLine("Aristotle's Second Book of the Poetics is in Room {0}.", roomGoal);
            Console.WriteLine();
            Console.WriteLine("The path from Room (0, 0) to Room {0} contains {1} rooms:", roomGoal, pathFromOriginToGoal.Count);
            Console.WriteLine(string.Join(" to ", pathFromOriginToGoal));
#else
            Console.WriteLine("The path from Room (0, 0) to the goal contains {0} rooms.", pathFromOriginToGoal.Count);
#endif
        }

        private void PlaceBooksInRooms()
        {
            var books = new List<string>()
            {
                "The First Book of the Poetics of Aristotle",
                "The Iliad by Homer",
                "The Odyssey by Homer",
                "The Republic by Plato",
                "Categories by Aristotle",
                "Physics by Aristotle",
                "Nicomachean Ethics by Aristotle",
                "The Aeneid by Virgil",
                "The Old Testament in Hebrew",
                "The New Testament in Greek",
                "Strong's Hebrew Dictionary",
                "Strong's Greek Dictionary"
            };
            var openListLocal = new List<RoomInfo>(rooms);
            var numBooksPlaced = 1;

            booksInRooms[roomGoal] = "The Second Book of the Poetics of Aristotle";
            openListLocal.Remove(roomGoal);

            while (numBooksPlaced * 3 < rooms.Count && books.Count > 0)
            {
                var room = openListLocal[random.Next(openListLocal.Count)];
                var book = books[random.Next(books.Count)];

                openListLocal.Remove(room);
                books.Remove(book);
                booksInRooms[room] = book;
                ++numBooksPlaced;
            }
        }

        private void ReportProximityToJorge(ILabyrinthClient client, RoomInfo room, RoomInfo JorgesRoom)
        {
            var path = FindShortestPathBetweenRooms(client, room, JorgesRoom);
            var distance = path.Count - 1;

            if (distance == 0)
            {
                Console.WriteLine("* You and the Venerable Jorge are in the same room! *");
                Console.WriteLine("'Good evening, Venerable Jorge.'");
            }
            else if (distance <= 2)
            {
                Console.WriteLine("The Venerable Jorge is very near.");
            }
            else if (distance <= 4)
            {
                Console.WriteLine("The Venerable Jorge is near.");
            }
        }

        private List<RoomInfo> ConstructJorgesPath(ILabyrinthClient client, RoomInfo JorgesRoom)
        {
            RoomInfo JorgesGoal;

            do
            {
                JorgesGoal = rooms[random.Next(rooms.Count)];
            }
            while (JorgesGoal.Equals(JorgesRoom));

            return FindShortestPathBetweenRooms(client, JorgesRoom, JorgesGoal);
        }

        public void NavigateLabyrinth(ILabyrinthClient client)
        {
            client.Initialize(this);

            if (!client.IsRAMClient)
            {
                // This ensures that we are querying the database, not RAM, for the room connections.
                connections.Clear();
                booksInRooms.Clear();
            }

            // ****
            var roomsVisited = new HashSet<RoomInfo>();
            var room = new RoomInfo(0, 0);

            //Console.WriteLine("Selecting a room for Jorge out of {0} rooms.", rooms.Count);

            var JorgesRoom = rooms[random.Next(rooms.Count)];
            var JorgesPath = ConstructJorgesPath(client, JorgesRoom);
            var JorgesPathIndex = 0;

            for (; ; )
            {
                roomsVisited.Add(room);

                Console.WriteLine();
                Console.WriteLine("You are now in room {0}.", room);
                //Console.WriteLine("The Venerable Jorge is now in room {0}.", JorgesRoom);
                //Console.WriteLine("Jorge's destination is room {0}", JorgesPath[JorgesPath.Count - 1]);

                ReportProximityToJorge(client, room, JorgesRoom);

#if DEAD_CODE
                if (booksInRooms.ContainsKey(room))
                {
                    Console.WriteLine("You have found the book '{0}'.", booksInRooms[room]);
                }
#else
                foreach (var book in client.QueryBooksInRoom(room))
                {
                    Console.WriteLine("You have found the book '{0}'.", book);
                }
#endif


                if (room.Equals(roomGoal))
                {
                    Console.WriteLine("**** Congratulations!  You have reached the goal! ****");
                }

                //var neighbouringRooms = connections[room];
                var neighbouringRooms = client.QueryConnections(room);

                Console.WriteLine("Possible moves:");

                for (var i = 0; i < neighbouringRooms.Count; ++i)
                {
                    var neighbouringRoom = neighbouringRooms[i];
                    var str = string.Format("  {0}. {1}", i, neighbouringRoom);

                    if (roomsVisited.Contains(neighbouringRoom))
                    {
                        str = str + " Visited";
                    }

                    Console.WriteLine(str);
                }

                Console.Write("Your move (or (h)elp or (q)uit): ");

                var input = Console.ReadLine();
                int inputInt;
                var inputIsInt = int.TryParse(input, out inputInt);

                if (string.IsNullOrEmpty(input))
                {
                    Console.WriteLine("The input is empty.");
                }
                else if (inputIsInt)
                {

                    if (inputInt < 0 || inputInt >= neighbouringRooms.Count)
                    {
                        Console.WriteLine("The input is out of range.");
                    }
                    else
                    {
                        room = neighbouringRooms[inputInt];
                        ReportProximityToJorge(client, room, JorgesRoom);
                    }
                }
                else if (input[0] == 'h')
                {
                    var pathToGoal = FindShortestPathBetweenRooms(client, room, roomGoal);

                    Console.WriteLine("Path to goal: {0}.", string.Join(" to ", pathToGoal));
                }
                else if (input[0] == 'q')
                {
                    break;
                }
                else
                {
                    Console.WriteLine("The input was not recognized.");
                }

                // Jorge's move.
                ++JorgesPathIndex;

                while (JorgesPathIndex >= JorgesPath.Count) // ThAW 2013/09/23 : This "while" used to be an "if", but it crashed once.
                {
                    JorgesPath = ConstructJorgesPath(client, JorgesRoom);
                    JorgesPathIndex = 1;
                }

                JorgesRoom = JorgesPath[JorgesPathIndex];
            }
        }
    }
}
