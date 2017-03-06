using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using Cassandra.Data.Linq;

namespace LabyrinthMultiDB.Client.CassandraLinq
{
    [AllowFiltering]    // This allows our SELECT query to work.
    public class Book
    {
        [PartitionKey]
        public Guid id;

        [SecondaryIndex]
        public int level;

        [SecondaryIndex]
        public int room;

        public string name;
    }
}
