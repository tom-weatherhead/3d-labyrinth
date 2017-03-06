using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;  // For ParameterDirection
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using LabyrinthMultiDB.Engine;

namespace LabyrinthMultiDB.Client.Oracle
{
    public class OracleClient : ILabyrinthClient
    {
        private OracleConnection connection;

        public OracleClient()
        {
        }

        public void Initialize(LabyrinthGenerator generator)
        {
            Connect(@"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ORCL)));User Id=c##labyrinth;Password=tomtom7;");
            //CreateSchema();
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

            if (connection != null)
            {
                Console.WriteLine("Disconnecting from the Oracle database.");
                //connection.Close(); // Or Dispose() ?
                connection.Dispose();
            }
        }

        public void Connect(string connectionString)
        {
            connection = new OracleConnection(connectionString);
            connection.Open();

            Console.WriteLine("Connected to the Oracle database.");
        }

        private void DeleteAllConnections()
        {
            Console.WriteLine("Oracle: Deleting all old connections.");

            var cmd = new OracleCommand("DELETE FROM c##labyrinth.connections", connection);

            cmd.ExecuteNonQuery();
        }

        private void DeleteAllBooks()
        {
            Console.WriteLine("Oracle: Deleting all old books.");

            var cmd = new OracleCommand("DELETE FROM c##labyrinth.books", connection);

            cmd.ExecuteNonQuery();
        }

        public void InsertData(LabyrinthGenerator generator)
        {
            DeleteAllConnections();
            DeleteAllBooks();

            Console.WriteLine("Oracle: Inserting new connections.");

            foreach (var room1 in generator.connections.Keys)
            {

                foreach (var room2 in generator.connections[room1])
                {
                    var cmd = new OracleCommand("INSERT INTO c##labyrinth.connections (level1, room1, level2, room2) VALUES (:level1, :room1, :level2, :room2)", connection);

                    cmd.BindByName = true; // See http://www.codeproject.com/Articles/208176/Gotcha-sharp1161-Using-Named-Parameters-with-Oracl

                    OracleParameter Level1In = cmd.Parameters.Add(":level1", OracleDbType.Int32);

                    Level1In.Direction = ParameterDirection.Input;
                    Level1In.Value = room1.levelNumber;

                    OracleParameter Room1In = cmd.Parameters.Add(":room1", OracleDbType.Int32);

                    Room1In.Direction = ParameterDirection.Input;
                    Room1In.Value = room1.roomNumber;

                    OracleParameter Level2In = cmd.Parameters.Add(":level2", OracleDbType.Int32);

                    Level2In.Direction = ParameterDirection.Input;
                    Level2In.Value = room2.levelNumber;

                    OracleParameter Room2In = cmd.Parameters.Add(":room2", OracleDbType.Int32);

                    Room2In.Direction = ParameterDirection.Input;
                    Room2In.Value = room2.roomNumber;

                    cmd.ExecuteNonQuery();
                }
            }

            Console.WriteLine("Oracle: Inserting new books.");

            foreach (var room in generator.booksInRooms.Keys)
            {
                // ThAW: Note to self: Use SqlParameters when using text parameters.
                var cmd = new OracleCommand("INSERT INTO c##labyrinth.books (level_, room, name_) VALUES (:level_, :room, :name_)", connection);

                cmd.BindByName = true;

                OracleParameter LevelIn = cmd.Parameters.Add(":level_", OracleDbType.Int32);

                LevelIn.Direction = ParameterDirection.Input;
                LevelIn.Value = room.levelNumber;

                OracleParameter RoomIn = cmd.Parameters.Add(":room", OracleDbType.Int32);

                RoomIn.Direction = ParameterDirection.Input;
                RoomIn.Value = room.roomNumber;

                OracleParameter NameIn = cmd.Parameters.Add(":name_", OracleDbType.Varchar2);

                NameIn.Direction = ParameterDirection.Input;
                NameIn.Value = generator.booksInRooms[room];

                cmd.ExecuteNonQuery();
            }

            Console.WriteLine("Oracle: Data inserted.");
        }

        public List<RoomInfo> QueryConnections(RoomInfo room)
        {
            var cmd = new OracleCommand("SELECT * FROM c##labyrinth.connections WHERE level1 = :level1 AND room1 = :room1", connection);

            cmd.BindByName = true;

            OracleParameter Level1In = cmd.Parameters.Add(":level1", OracleDbType.Int32);

            Level1In.Direction = ParameterDirection.Input;
            Level1In.Value = room.levelNumber;

            OracleParameter Room1In = cmd.Parameters.Add(":room1", OracleDbType.Int32);

            Room1In.Direction = ParameterDirection.Input;
            Room1In.Value = room.roomNumber;

            using (var reader = cmd.ExecuteReader())
            {
                var result = new List<RoomInfo>();

                while (reader.Read())
                {
                    var level2 = reader.GetInt32(reader.GetOrdinal("level2"));
                    var room2 = reader.GetInt32(reader.GetOrdinal("room2"));

                    result.Add(new RoomInfo(level2, room2));
                }

                return result;
            }
        }

        public List<string> QueryBooksInRoom(RoomInfo room)
        {
            var cmd = new OracleCommand("SELECT name_ FROM c##labyrinth.books WHERE level_ = :level_ AND room = :room", connection);

            cmd.BindByName = true;

            OracleParameter LevelIn = cmd.Parameters.Add(":level_", OracleDbType.Int32);

            LevelIn.Direction = ParameterDirection.Input;
            LevelIn.Value = room.levelNumber;

            OracleParameter RoomIn = cmd.Parameters.Add(":room", OracleDbType.Int32);

            RoomIn.Direction = ParameterDirection.Input;
            RoomIn.Value = room.roomNumber;

            using (var reader = cmd.ExecuteReader())
            {
                var result = new List<string>();

                while (reader.Read())
                {
                    result.Add(reader.GetString(reader.GetOrdinal("name_")));
                }

                return result;
            }
        }
    }
}
