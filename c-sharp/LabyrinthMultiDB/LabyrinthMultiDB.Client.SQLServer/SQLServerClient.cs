// Note that the type of the column "name" in the table "dbo.books" is "varchar(100)" rather than the arbitrarily long "text",
// since we use "name" in "dbo.books"'s primary key.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LabyrinthMultiDB.Engine;

namespace LabyrinthMultiDB.Client.SQLServer
{
    public class SQLServerClient : ILabyrinthClient
    {
        private SqlConnection connection;

        public SQLServerClient()
        {
        }

        public void Initialize(LabyrinthGenerator generator)
        {
            Connect(@"Persist Security Info=False;Integrated Security=SSPI;database=labyrinth;server=(local)\INSTANCENAME;Network Library=DBMSSOCN");   // TCP/IP
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
                Console.WriteLine("Disconnecting from the SQL Server database.");
                //connection.Close(); // Or Dispose() ?
                connection.Dispose();
            }
        }

        public void Connect(string connectionString)
        {
            connection = new SqlConnection(connectionString);
            connection.Open();

            Console.WriteLine("Connected to the SQL Server database.");
        }

        private void DeleteAllConnections()
        {
            Console.WriteLine("SQL Server: Deleting all old connections.");

            var cmd = new SqlCommand("DELETE FROM dbo.connections;", connection);

            cmd.ExecuteNonQuery();
        }

        private void DeleteAllBooks()
        {
            Console.WriteLine("SQL Server: Deleting all old books.");

            var cmd = new SqlCommand("DELETE FROM dbo.books;", connection);

            cmd.ExecuteNonQuery();
        }

        public void InsertData(LabyrinthGenerator generator)
        {
            DeleteAllConnections();
            DeleteAllBooks();

            Console.WriteLine("SQL Server: Inserting new connections.");

            foreach (var room1 in generator.connections.Keys)
            {

                foreach (var room2 in generator.connections[room1])
                {
                    var cmd = new SqlCommand("INSERT INTO dbo.connections (level1, room1, level2, room2) VALUES (@level1, @room1, @level2, @room2);", connection);

                    SqlParameter Level1In = cmd.Parameters.Add("@level1", SqlDbType.Int);

                    Level1In.Direction = ParameterDirection.Input;
                    Level1In.Value = room1.levelNumber;

                    SqlParameter Room1In = cmd.Parameters.Add("@room1", SqlDbType.Int);

                    Room1In.Direction = ParameterDirection.Input;
                    Room1In.Value = room1.roomNumber;

                    SqlParameter Level2In = cmd.Parameters.Add("@level2", SqlDbType.Int);

                    Level2In.Direction = ParameterDirection.Input;
                    Level2In.Value = room2.levelNumber;

                    SqlParameter Room2In = cmd.Parameters.Add("@room2", SqlDbType.Int);

                    Room2In.Direction = ParameterDirection.Input;
                    Room2In.Value = room2.roomNumber;

                    cmd.ExecuteNonQuery();
                }
            }

            Console.WriteLine("SQL Server: Inserting new books.");

            foreach (var room in generator.booksInRooms.Keys)
            {
                // ThAW: Note to self: Use SqlParameters when using text parameters.
                var cmd = new SqlCommand("INSERT INTO dbo.books (level_, room, name) VALUES (@level_, @room, @name);", connection);

                SqlParameter LevelIn = cmd.Parameters.Add("@level_", SqlDbType.Int);

                LevelIn.Direction = ParameterDirection.Input;
                LevelIn.Value = room.levelNumber;

                SqlParameter RoomIn = cmd.Parameters.Add("@room", SqlDbType.Int);

                RoomIn.Direction = ParameterDirection.Input;
                RoomIn.Value = room.roomNumber;

                SqlParameter NameIn = cmd.Parameters.Add("@name", SqlDbType.Text);

                NameIn.Direction = ParameterDirection.Input;
                NameIn.Value = generator.booksInRooms[room];

                cmd.ExecuteNonQuery();
            }

            Console.WriteLine("SQL Server: Data inserted.");
        }

        public List<RoomInfo> QueryConnections(RoomInfo room)
        {
            var cmd = new SqlCommand("SELECT * FROM dbo.connections WHERE level1 = @level1 AND room1 = @room1;", connection);

            SqlParameter Level1In = cmd.Parameters.Add("@level1", SqlDbType.Int);

            Level1In.Direction = ParameterDirection.Input;
            Level1In.Value = room.levelNumber;

            SqlParameter Room1In = cmd.Parameters.Add("@room1", SqlDbType.Int);

            Room1In.Direction = ParameterDirection.Input;
            Room1In.Value = room.roomNumber;

            using (var reader = cmd.ExecuteReader())
            {
                var result = new List<RoomInfo>();

                while (reader.Read())
                {
                    result.Add(new RoomInfo((int)reader["level2"], (int)reader["room2"]));
                }

                return result;
            }
        }

        public List<string> QueryBooksInRoom(RoomInfo room)
        {
            var cmd = new SqlCommand("SELECT name FROM dbo.books WHERE level_ = @level_ AND room = @room;", connection);

            SqlParameter LevelIn = cmd.Parameters.Add("@level_", SqlDbType.Int);

            LevelIn.Direction = ParameterDirection.Input;
            LevelIn.Value = room.levelNumber;

            SqlParameter RoomIn = cmd.Parameters.Add("@room", SqlDbType.Int);

            RoomIn.Direction = ParameterDirection.Input;
            RoomIn.Value = room.roomNumber;

            using (var reader = cmd.ExecuteReader())
            {
                var result = new List<string>();

                while (reader.Read())
                {
                    result.Add((string)reader["name"]);
                }

                return result;
            }
        }
    }
}
