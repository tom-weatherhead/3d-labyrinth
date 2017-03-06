// LabyrinthCassandra - September 19, 2013
// Adapted from Labyrinth - Started on April 8, 2013

// This program is intended to generate a labyrinth similar to the abbey library in the movie "The Name of the Rose".
// According to director Jean-Jacques Annaud, author Umberto Eco envisioned the library as having "about a hundred" rooms.

// This program stores the labyrinth information in a Cassandra database.

#define TRY_TO_REFACTOR_LABYRINTH
//#define USE_ULTIMATE_ROOMS_LIST
#define DEBUG_WRITELINE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;
//using Cassandra.Data;
using Cassandra.Data.Linq;

namespace LabyrinthCassandra
{
    #region RoomInfo

    public class RoomInfo
    {
        public readonly int levelNumber;
        public readonly int roomNumber;

        public RoomInfo(int l, int r)
        {
            levelNumber = l;
            roomNumber = r;
        }

        public override bool Equals(object obj)
        {

            if (object.ReferenceEquals(this, obj))
            {
                return true;
            }

            var otherRoomInfo = obj as RoomInfo;

            return otherRoomInfo != null && roomNumber == otherRoomInfo.roomNumber && levelNumber == otherRoomInfo.levelNumber;
        }

        public override int GetHashCode()
        {
            return roomNumber + 1024 * levelNumber;
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", levelNumber, roomNumber);
        }

        private List<RoomInfo> GeneratePossibleNeighboursOnLevel(LabyrinthGenerator generator, int newLevel)
        {
            var result = new List<RoomInfo>();

            if (roomNumber == generator.numberOfRoomsPerLevel - 1)
            {
                // Rooms with this room number form the central core of the tower.

                for (int i = 0; i < generator.numberOfRoomsPerLevel - 1; ++i)
                {
                    result.Add(new RoomInfo(newLevel, i));
                }
            }
            else
            {
                result.Add(new RoomInfo(newLevel, (roomNumber + 1) % (generator.numberOfRoomsPerLevel - 1)));
                result.Add(new RoomInfo(newLevel, (roomNumber + generator.numberOfRoomsPerLevel - 2) % (generator.numberOfRoomsPerLevel - 1)));
                result.Add(new RoomInfo(newLevel, generator.numberOfRoomsPerLevel - 1));
            }

            return result;
        }

        public List<RoomInfo> GeneratePossibleNeighbours(LabyrinthGenerator generator)
        {
            var result = new List<RoomInfo>();

            if (levelNumber > 0)
            {
                result.AddRange(GeneratePossibleNeighboursOnLevel(generator, levelNumber - 1));
            }

            if (levelNumber < generator.numberOfLevels - 1)
            {
                result.AddRange(GeneratePossibleNeighboursOnLevel(generator, levelNumber + 1));
            }

            return result;
        }
    }

    #endregion

    #region CassandraClient

    public class CassandraClient : IDisposable
    {
        private Cluster cluster;    // Reference types are initialized to null.
        private Session session;
        private PreparedStatement statementQueryConnections;

        public CassandraClient()
        {
        }

        public void Dispose()
        {

            if (cluster != null)
            {
                Console.WriteLine("Disconnecting from the Cassandra cluster.");
                cluster.Shutdown();
            }
        }

        public void Connect(string node)
        {
            cluster = Cluster.Builder().AddContactPoint(node).Build();
            session = cluster.Connect();

            Metadata metadata = cluster.Metadata;

            Console.WriteLine("Connected to the Cassandra cluster {0} via the node {1}.", metadata.ClusterName, node);
        }

        public void CreateSchema()
        {
            Console.WriteLine("Cassandra: Deleting the old keyspace, if it exists.");

            session.DeleteKeyspaceIfExists("labyrinth");

            Console.WriteLine("Cassandra: Creating the keyspace.");

            session.Execute("CREATE KEYSPACE labyrinth WITH replication " +
                "= {'class':'SimpleStrategy', 'replication_factor':1};");

            Console.WriteLine("Cassandra: Changing the keyspace to 'labyrinth'.");

            session.ChangeKeyspace("labyrinth");

            Console.WriteLine("Cassandra: Creating the table(s).");

            session.Execute(
                "CREATE TABLE connections (" +
                    "level1 int, " +
                    "room1 int, " +
                    "level2 int, " +
                    "room2 int, " +
                    "PRIMARY KEY (level1, room1, level2, room2)" +
                    ");");

            Console.WriteLine("Cassandra: Schema created.");

            statementQueryConnections = session.Prepare("SELECT * FROM connections WHERE level1 = ? AND room1 = ?;");
        }

        public void InsertData(Dictionary<RoomInfo, List<RoomInfo>> connections)
        {
            PreparedStatement statement = session.Prepare("INSERT INTO connections (level1, room1, level2, room2) VALUES (?, ?, ?, ?);");

            foreach (var room1 in connections.Keys)
            {

                foreach (var room2 in connections[room1])
                {
                    /*
                    session.Execute(string.Format(
                        "INSERT INTO labyrinth.connections (level1, room1, level2, room2) VALUES ({0}, {1}, {2}, {3});",
                        room1.levelNumber, room1.roomNumber, room2.levelNumber, room2.roomNumber));
                     */
                    BoundStatement boundStatement = new BoundStatement(statement);

                    session.Execute(boundStatement.Bind(room1.levelNumber, room1.roomNumber, room2.levelNumber, room2.roomNumber));
                }
            }

            Console.WriteLine("Cassandra: Data inserted.");
        }

        public List<RoomInfo> QueryConnections(RoomInfo room)
        {
            Console.WriteLine("Cassandra: Querying the database to find the rooms connected to {0}.", room);

            //PreparedStatement statementQueryConnections = session.Prepare("SELECT * FROM labyrinth.connections WHERE level1 = ? AND room1 = ?;");
            BoundStatement boundStatement = new BoundStatement(statementQueryConnections);
            /*
            RowSet results = session.Execute(string.Format(
                "SELECT * FROM labyrinth.connections WHERE level1 = {0} AND room1 = {1};",
                room.levelNumber, room.roomNumber));
             */
            RowSet results = session.Execute(boundStatement.Bind(room.levelNumber, room.roomNumber));

            return results.GetRows().Select(row => new RoomInfo(row.GetValue<int>("level2"), row.GetValue<int>("room2"))).ToList();
        }
    }

    #endregion

    #region LabyrinthGenerator

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
#if USE_ULTIMATE_ROOMS_LIST
        // ultimateRoomsList: The rooms on the top and bottom levels.
        // These rooms must be connected first, or else they might be prevented from connecting to any other room by the surrounding connections.
        public readonly List<RoomInfo> ultimateRoomsList = new List<RoomInfo>();
#endif
        private readonly Random random = new Random();
        private int numberOfDifferentLabels = 0;
        private RoomInfo roomGoal;
        private readonly Dictionary<RoomInfo, string> booksInRooms = new Dictionary<RoomInfo, string>();
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

            if (connections.ContainsKey(room3) && connections[room3].Contains(room1))
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

            if (connections.ContainsKey(room3) && connections[room3].Contains(room2))
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

            if (connections.ContainsKey(room3) && connections[room3].Contains(room1))
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

            if (connections.ContainsKey(room3) && connections[room3].Contains(room2))
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

#if USE_ULTIMATE_ROOMS_LIST
            for (int r = 0; r < numberOfRoomsPerLevel; ++r)
            {
                ultimateRoomsList.Add(new RoomInfo(0, r));
                ultimateRoomsList.Add(new RoomInfo(numberOfLevels - 1, r));
            }
#endif

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

#if USE_ULTIMATE_ROOMS_LIST
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
#else
                var room1 = openList[random.Next(openList.Count)];
#endif

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

        private List<RoomInfo> FindShortestPathBetweenRooms(RoomInfo room, RoomInfo roomGoalLocal)
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

                foreach (var room2 in connections[room])
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
            return FindShortestPathBetweenRooms(room, null);
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

            var pathFromOriginToGoal = FindShortestPathBetweenRooms(new RoomInfo(0, 0), roomGoal);

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
                "The Aeneid by Virgil"
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

        private void ReportProximityToJorge(RoomInfo room, RoomInfo JorgesRoom)
        {
            var path = FindShortestPathBetweenRooms(room, JorgesRoom);
            var distance = path.Count - 1;

            if (distance == 0)
            {
                Console.WriteLine("* You and the Venerable Jorge are in the same room! *");
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

        private List<RoomInfo> ConstructJorgesPath(RoomInfo JorgesRoom)
        {
            RoomInfo JorgesGoal;

            do
            {
                JorgesGoal = rooms[random.Next(rooms.Count)];
            }
            while (JorgesGoal.Equals(JorgesRoom));

            return FindShortestPathBetweenRooms(JorgesRoom, JorgesGoal);
        }

        public void NavigateLabyrinth()
        {
            var roomsVisited = new HashSet<RoomInfo>();
            var room = new RoomInfo(0, 0);
            var JorgesRoom = rooms[random.Next(rooms.Count)];
            var JorgesPath = ConstructJorgesPath(JorgesRoom);
            var JorgesPathIndex = 0;

            PlaceBooksInRooms();

            using (var cassandraClient = new CassandraClient())
            {
                cassandraClient.Connect("192.168.56.10");
                cassandraClient.CreateSchema();
                cassandraClient.InsertData(connections);

                for (; ; )
                {
                    roomsVisited.Add(room);

                    Console.WriteLine();
                    Console.WriteLine("You are now in room {0}.", room);
                    //Console.WriteLine("The Venerable Jorge is now in room {0}.", JorgesRoom);
                    //Console.WriteLine("Jorge's destination is room {0}", JorgesPath[JorgesPath.Count - 1]);

                    ReportProximityToJorge(room, JorgesRoom);

                    if (booksInRooms.ContainsKey(room))
                    {
                        Console.WriteLine("You have found the book '{0}'.", booksInRooms[room]);
                    }

                    if (room.Equals(roomGoal))
                    {
                        Console.WriteLine("**** Congratulations!  You have reached the goal! ****");
                    }

                    //var neighbouringRooms = connections[room];
                    var neighbouringRooms = cassandraClient.QueryConnections(room);

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
                            ReportProximityToJorge(room, JorgesRoom);
                        }
                    }
                    else if (input[0] == 'h')
                    {
                        var pathToGoal = FindShortestPathBetweenRooms(room, roomGoal);

                        Console.WriteLine("Path to goal: {0}", string.Join(" to ", pathToGoal));
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

                    if (JorgesPathIndex >= JorgesPath.Count)
                    {
                        JorgesPath = ConstructJorgesPath(JorgesRoom);
                        JorgesPathIndex = 1;
                    }

                    JorgesRoom = JorgesPath[JorgesPathIndex];
                }
            }
        }
    }

    #endregion

    #region Program

    public class Program
    {
        static void Main(string[] args)
        {

            try
            {
                // The third command-line parameter indicates how many extra connections should be added to the labyrinth
                // once it is a single blob.  This will introduce cycles, which should make the labyrinth more challenging to navigate.
                var numberOfExtraConnections = 0;

                if (args.Length < 2 || args.Length > 3)
                {
                    Console.WriteLine("Usage: Labyrinth.exe (NumberOfLevels) (NumberOfRoomsPerLevel) [NumberOfExtraConnections]");
                    return;
                }

                if (args.Length > 2)
                {
                    numberOfExtraConnections = int.Parse(args[2]);
                }

                var generator = new LabyrinthGenerator(int.Parse(args[0]), int.Parse(args[1]), numberOfExtraConnections);

                generator.Generate();
                generator.Report();
                generator.PrintLongestPath();
                generator.NavigateLabyrinth();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Caught {0}: {1}", ex.GetType().FullName, ex.Message);
            }
        }
    }

    #endregion
}
