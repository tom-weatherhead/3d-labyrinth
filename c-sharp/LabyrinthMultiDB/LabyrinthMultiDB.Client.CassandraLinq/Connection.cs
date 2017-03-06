using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using Cassandra.Data.Linq;

namespace LabyrinthMultiDB.Client.CassandraLinq
{
    [AllowFiltering]    // This allows our SELECT query to work.
    public class Connection
    {
        [PartitionKey]
        public Guid id;

        [SecondaryIndex]
        public int level1;

        [SecondaryIndex]
        public int room1;

        public int level2;

        public int room2;
    }
}
