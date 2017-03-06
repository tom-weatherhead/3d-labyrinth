// To create the "labyrinth" database in MySQL, type the following in a Command Prompt window:
//
// mysqladmin -u root -p create labyrinth
// mysql -u root -p
// mysql> CREATE USER 'username'@'localhost' IDENTIFIED BY 'password';
// mysql> GRANT ALL PRIVILEGES ON labyrinth.* TO 'username'@'localhost' WITH GRANT OPTION;
// mysql> FLUSH PRIVILEGES;
// mysql> CREATE TABLE labyrinth.connections (level1 int, room1 int, level2 int, room2 int, PRIMARY KEY (level1, room1, level2, room2));
// mysql> CREATE TABLE labyrinth.books (level int, room int, name text, PRIMARY KEY (level, room, name(100)));
// mysql> exit
//
// Note that "name(100)" above specifies how many characters from each "name" column to use in the primary key.
// See http://stackoverflow.com/questions/13710170/blob-text-column-bestilling-used-in-key-specification-without-a-key-length
// Note that the maximum key length is 1000 bytes, and MySQL assumes that each character requires 3 bytes (because UTF-8 encoding can require 3 bytes per character).

using System;
using System.Collections.Generic;   // For List<>
using System.Data;                  // For ParameterDirection
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using MySql.Data;
using MySql.Data.MySqlClient;
//using MySql.Data.Types;
using LabyrinthMultiDB.Engine;

namespace LabyrinthMultiDB.Client.MySQL
{
    public class MySQLClient : ILabyrinthClient
    {
        private MySqlConnection connection;

        public MySQLClient()
        {
        }

        public void Initialize(LabyrinthGenerator generator)
        {
            var dataSources = new List<string>() { "localhost", "192.168.2.101", "192.168.56.11", "192.168.56.13" };

            Console.WriteLine();
            Console.WriteLine("Select a MySQL database host:");

            for (var i = 0; i < dataSources.Count; ++i)
            {
                Console.WriteLine("  {0}) {1}", i, dataSources[i]);
            }

            Console.Write("Enter your selection: ");

            var dataSourceIndexAsString = Console.ReadLine();
            int dataSourceIndexAsInt;

            if (!int.TryParse(dataSourceIndexAsString, out dataSourceIndexAsInt) || dataSourceIndexAsInt < 0 || dataSourceIndexAsInt >= dataSources.Count)
            {
                throw new Exception("Unrecognized MySQL database host selection.");
            }

            Connect(string.Format("Database=labyrinth;Data Source={0};User Id=user;Password=tomtom7", dataSources[dataSourceIndexAsInt]));
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
                Console.WriteLine("Disconnecting from the MySQL database.");
                //connection.Close(); // Or Dispose() ?
                connection.Dispose();
            }
        }

        public void Connect(string connectionString)
        {
            connection = new MySqlConnection(connectionString);
            connection.Open();

            Console.WriteLine("Connected to the MySQL database.");
        }

        private void DeleteAllConnections()
        {
            Console.WriteLine("MySQL: Deleting all old connections.");

            var cmd = new MySqlCommand("DELETE FROM connections;", connection);

            cmd.ExecuteNonQuery();
        }

        private void DeleteAllBooks()
        {
            Console.WriteLine("MySQL: Deleting all old books.");

            var cmd = new MySqlCommand("DELETE FROM books;", connection);

            cmd.ExecuteNonQuery();
        }

        public void InsertData(LabyrinthGenerator generator)
        {
            DeleteAllConnections();
            DeleteAllBooks();

            Console.WriteLine("MySQL: Inserting new connections.");

            foreach (var room1 in generator.connections.Keys)
            {

                foreach (var room2 in generator.connections[room1])
                {
                    var cmd = new MySqlCommand("INSERT INTO connections (level1, room1, level2, room2) VALUES (@level1, @room1, @level2, @room2);", connection);

                    MySqlParameter Level1In = cmd.Parameters.Add("@level1", MySqlDbType.Int32);

                    Level1In.Direction = ParameterDirection.Input;
                    Level1In.Value = room1.levelNumber;

                    MySqlParameter Room1In = cmd.Parameters.Add("@room1", MySqlDbType.Int32);

                    Room1In.Direction = ParameterDirection.Input;
                    Room1In.Value = room1.roomNumber;

                    MySqlParameter Level2In = cmd.Parameters.Add("@level2", MySqlDbType.Int32);

                    Level2In.Direction = ParameterDirection.Input;
                    Level2In.Value = room2.levelNumber;

                    MySqlParameter Room2In = cmd.Parameters.Add("@room2", MySqlDbType.Int32);

                    Room2In.Direction = ParameterDirection.Input;
                    Room2In.Value = room2.roomNumber;

                    cmd.ExecuteNonQuery();
                }
            }

            Console.WriteLine("MySQL: Inserting new books.");

            foreach (var room in generator.booksInRooms.Keys)
            {
                // ThAW: Note to self: Use SqlParameters when using text parameters.
                var cmd = new MySqlCommand("INSERT INTO books (level, room, name) VALUES (@level, @room, @name);", connection);

                MySqlParameter LevelIn = cmd.Parameters.Add("@level", MySqlDbType.Int32);

                LevelIn.Direction = ParameterDirection.Input;
                LevelIn.Value = room.levelNumber;

                MySqlParameter RoomIn = cmd.Parameters.Add("@room", MySqlDbType.Int32);

                RoomIn.Direction = ParameterDirection.Input;
                RoomIn.Value = room.roomNumber;

                MySqlParameter NameIn = cmd.Parameters.Add("@name", MySqlDbType.Text);

                NameIn.Direction = ParameterDirection.Input;
                NameIn.Value = generator.booksInRooms[room];

                cmd.ExecuteNonQuery();
            }

            Console.WriteLine("MySQL: Data inserted.");
        }

        public List<RoomInfo> QueryConnections(RoomInfo room)
        {
            var cmd = new MySqlCommand("SELECT * FROM connections WHERE level1 = @level1 AND room1 = @room1;", connection);

            MySqlParameter Level1In = cmd.Parameters.Add("@level1", MySqlDbType.Int32);

            Level1In.Direction = ParameterDirection.Input;
            Level1In.Value = room.levelNumber;

            MySqlParameter Room1In = cmd.Parameters.Add("@room1", MySqlDbType.Int32);

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
            var cmd = new MySqlCommand("SELECT name FROM books WHERE level = @level AND room = @room;", connection);

            MySqlParameter LevelIn = cmd.Parameters.Add("@level", MySqlDbType.Int32);

            LevelIn.Direction = ParameterDirection.Input;
            LevelIn.Value = room.levelNumber;

            MySqlParameter RoomIn = cmd.Parameters.Add("@room", MySqlDbType.Int32);

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
