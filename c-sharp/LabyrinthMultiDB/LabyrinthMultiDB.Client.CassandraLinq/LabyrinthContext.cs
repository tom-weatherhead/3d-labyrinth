//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using Cassandra;
using Cassandra.Data.Linq;

namespace LabyrinthMultiDB.Client.CassandraLinq
{
    public class LabyrinthContext : Context
    {
        public LabyrinthContext(Session session)
            : base(session)
        {
            AddTable<Connection>();
            AddTable<Book>();
            CreateTablesIfNotExist();
        }
    }
}
