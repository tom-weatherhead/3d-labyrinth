// TODO: Add Cassandra logging, based on the logging in the DataStax C# driver Playground app.

using System;
using System.Collections.Generic;
//using System.Diagnostics;
//using System.Globalization;
//using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Data.Linq;
using LabyrinthMultiDB.Engine;

namespace LabyrinthMultiDB.Client.CassandraLinq
{
    public class CassandraLinqClient : ILabyrinthClient
    {
        private Cluster cluster;    // Reference types are initialized to null.
        private Session session;
        private LabyrinthContext labyrinthContext;
        private ContextTable<Connection> connectionTable;
        private ContextTable<Book> bookTable;

        public CassandraLinqClient()
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
            Console.WriteLine("Cassandra Linq: Changing the keyspace.");

            const string keyspaceName = "labyrinth_linq";

            session.DeleteKeyspaceIfExists(keyspaceName);
            session.CreateKeyspaceIfNotExists(keyspaceName);
            session.ChangeKeyspace(keyspaceName);

            Console.WriteLine("Cassandra Linq: Creating the context and the tables.");

            labyrinthContext = new LabyrinthContext(session);
            connectionTable = labyrinthContext.GetTable<Connection>();
            bookTable = labyrinthContext.GetTable<Book>();

            Console.WriteLine("Cassandra Linq: Schema created.");
        }

        public void InsertData(LabyrinthGenerator generator)
        {
            //var batch = session.CreateBatch();

            foreach (var room1x in generator.connections.Keys)
            {

                foreach (var room2x in generator.connections[room1x])
                {
                    var connectionEnt = new Connection() { id = Guid.NewGuid(),
                        level1 = room1x.levelNumber, room1 = room1x.roomNumber,
                        level2 = room2x.levelNumber, room2 = room2x.roomNumber };

                    //Console.WriteLine("InsertData: Adding the connection from ({0}, {1}) to ({2}, {3}).",
                    //    room1x.levelNumber, room1x.roomNumber, room2x.levelNumber, room2x.roomNumber);
                    connectionTable.AddNew(connectionEnt);
                    //batch.Append(connectionTable.Insert(connectionEnt));
                    //connectionTable.EnableQueryTracing(connectionEnt);
                }
            }

            labyrinthContext.SaveChanges(SaveChangesMode.Batch);
            //batch.Execute();

#if DEAD_CODE
            RowSet results = session.Execute("SELECT * FROM labyrinth_linq.\"Connection\";");
            var verifiedRowCount = 0;

            foreach (var row in results.GetRows())
            {
                Console.WriteLine("Verified insert: Connection from ({0}, {1}) to ({2}, {3}).",
                    row.GetValue<int>("level1"), row.GetValue<int>("room1"), row.GetValue<int>("level2"), row.GetValue<int>("room2"));
                verifiedRowCount++;
            }

            Console.WriteLine("{0} rows verified as inserted.", verifiedRowCount);
#endif

            foreach (var roomx in generator.booksInRooms.Keys)
            {
                var bookEnt = new Book() { id = Guid.NewGuid(),
                    level = roomx.levelNumber, room = roomx.roomNumber,
                    name = generator.booksInRooms[roomx] };

                bookTable.AddNew(bookEnt);
                //bookTable.EnableQueryTracing(bookEnt);
            }

            labyrinthContext.SaveChanges(SaveChangesMode.Batch);

            Console.WriteLine("Cassandra Linq: Data inserted.");
        }

        public List<RoomInfo> QueryConnections(RoomInfo room)
        {
            return
                (from c in connectionTable
                 where c.level1 == room.levelNumber && c.room1 == room.roomNumber
                 select new RoomInfo() { levelNumber = c.level2, roomNumber = c.room2 }).Execute().ToList();
        }

        public List<string> QueryBooksInRoom(RoomInfo roomx)
        {
            return
                (from b in bookTable
                 where b.level == roomx.levelNumber && b.room == roomx.roomNumber
                 select b.name).Execute().ToList();
        }
    }
}
