using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;
using LabyrinthMultiDB.Engine;

namespace LabyrinthMultiDB.Client.Cassandra
{
    public class CassandraClient : ILabyrinthClient
    {
        private Cluster cluster;    // Reference types are initialized to null.
        private Session session;
        private PreparedStatement statementQueryConnections;
        private PreparedStatement statementQueryBooksInRoom;

        public CassandraClient()
        {
        }

        public void Initialize(LabyrinthGenerator generator)
        {
            Connect("192.168.56.10");
            CreateSchema();
            InsertData(generator);
        }

        public bool IsRAMClient
        {
            get
            {
                return false;
            }
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

            Console.WriteLine("Cassandra: Creating the tables.");

            session.Execute(
                "CREATE TABLE connections (" +
                    "level1 int, " +
                    "room1 int, " +
                    "level2 int, " +
                    "room2 int, " +
                    "PRIMARY KEY (level1, room1, level2, room2)" +
                    ");");

            // Note that the entire value of the column "name" can be used in the primary key; in SQL databases, we cannot use an unlimited string in a primary key.
            session.Execute(
                "CREATE TABLE books (" +
                    "level int, " +
                    "room int, " +
                    "name text, " +
                    "PRIMARY KEY (level, room, name)" +   // ThAW 2013/09/21: Added "name" to primary key.  Each row in a table must have a unique primary key.  See http://www.w3schools.com/sql/sql_primarykey.asp
                    ");");

            Console.WriteLine("Cassandra: Schema created.");

            statementQueryConnections = session.Prepare("SELECT * FROM connections WHERE level1 = ? AND room1 = ?;");
            statementQueryBooksInRoom = session.Prepare("SELECT name FROM books WHERE level = ? AND room = ?;");
        }

        public void InsertData(LabyrinthGenerator generator)
        {
            PreparedStatement statement = session.Prepare("INSERT INTO connections (level1, room1, level2, room2) VALUES (?, ?, ?, ?);");

            foreach (var room1 in generator.connections.Keys)
            {

                foreach (var room2 in generator.connections[room1])
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

            statement = session.Prepare("INSERT INTO books (level, room, name) VALUES (?, ?, ?);");

            foreach (var room in generator.booksInRooms.Keys)
            {
                BoundStatement boundStatement = new BoundStatement(statement);

                session.Execute(boundStatement.Bind(room.levelNumber, room.roomNumber, generator.booksInRooms[room]));
            }

            Console.WriteLine("Cassandra: Data inserted.");
        }

        public List<RoomInfo> QueryConnections(RoomInfo room)
        {
            //Console.WriteLine("Cassandra: Querying the database to find the rooms connected to {0}.", room);

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

        public List<string> QueryBooksInRoom(RoomInfo room)
        {
            BoundStatement boundStatement = new BoundStatement(statementQueryBooksInRoom);
            RowSet results = session.Execute(boundStatement.Bind(room.levelNumber, room.roomNumber));

            return results.GetRows().Select(row => row.GetValue<string>("name")).ToList();
        }
    }
}
