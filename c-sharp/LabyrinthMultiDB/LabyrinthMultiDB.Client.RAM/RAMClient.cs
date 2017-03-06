using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LabyrinthMultiDB.Engine;

namespace LabyrinthMultiDB.Client.RAM
{
    public class RAMClient : ILabyrinthClient
    {
        private LabyrinthGenerator generator;

        public RAMClient()
        {
        }

        public void Initialize(LabyrinthGenerator generator_param)
        {
            generator = generator_param;
        }

        public bool IsRAMClient
        {
            get
            {
                return true;
            }
        }

        public void Dispose()
        {
        }

        public List<RoomInfo> QueryConnections(RoomInfo room)
        {
            return generator.connections[room];
        }

        public List<string> QueryBooksInRoom(RoomInfo room)
        {

            if (generator.booksInRooms.ContainsKey(room))
            {
                return new List<string>() { generator.booksInRooms[room] };
            }
            else
            {
                return new List<string>();
            }
        }
    }
}
