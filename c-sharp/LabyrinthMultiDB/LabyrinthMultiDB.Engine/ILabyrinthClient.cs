using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabyrinthMultiDB.Engine
{
    public interface ILabyrinthClient : IDisposable
    {
        void Initialize(LabyrinthGenerator generator);
        bool IsRAMClient { get; }
        List<RoomInfo> QueryConnections(RoomInfo room);
        List<string> QueryBooksInRoom(RoomInfo room);
    }
}
