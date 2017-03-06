// LabyrinthMultiDB - September 20, 2013
// Adapted from Labyrinth and LabyrinthCassandra.

// This program is intended to generate a labyrinth similar to the abbey library in the movie "The Name of the Rose".
// According to director Jean-Jacques Annaud, author Umberto Eco envisioned the library as having "about a hundred" rooms.

// This program stores the labyrinth information in RAM, in a Cassandra database, in a MySQL database, or in a SQL Server database.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LabyrinthMultiDB.Engine;

namespace LabyrinthMultiDB
{
    public class Program
    {
        static private int GetStorageType()
        {
            int storageType;
            string storageTypeStr;

            do
            {
                Console.WriteLine();
                Console.WriteLine("Where do you wish to store the labyrinth information?");
                Console.WriteLine("  0) RAM");
                Console.WriteLine("  1) Cassandra");
                Console.WriteLine("  2) Cassandra via Linq");
                Console.WriteLine("  3) MySQL");
                Console.WriteLine("  4) Oracle");
                Console.WriteLine("  5) SQL Server");
                Console.Write("Enter 0, 1, 2, 3, 4, or 5: ");

                storageTypeStr = Console.ReadLine();
            }
            while (!int.TryParse(storageTypeStr, out storageType) || storageType < 0 || storageType > 5);

            return storageType;
        }

        static private ILabyrinthClient CreateClient(int storageType)
        {

            switch (storageType)
            {
                case 0:
                    return new LabyrinthMultiDB.Client.RAM.RAMClient();

                case 1:
                    return new LabyrinthMultiDB.Client.Cassandra.CassandraClient();

                case 2:
                    return new LabyrinthMultiDB.Client.CassandraLinq.CassandraLinqClient();

                case 3:
                    return new LabyrinthMultiDB.Client.MySQL.MySQLClient();

                case 4:
                    return new LabyrinthMultiDB.Client.Oracle.OracleClient();

                case 5:
                    return new LabyrinthMultiDB.Client.SQLServer.SQLServerClient();

                default:
                    throw new Exception("CreateClient(): Invalid storageType.");
            }
        }

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

                var storageType = GetStorageType();
                var generator = new LabyrinthGenerator(int.Parse(args[0]), int.Parse(args[1]), numberOfExtraConnections);

                generator.Generate();

                using (var client = CreateClient(storageType))
                {
                    generator.NavigateLabyrinth(client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Caught {0}: {1}", ex.GetType().FullName, ex.Message);
            }
        }
    }
}
