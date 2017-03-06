using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabyrinthMultiDB.Engine
{
    public class RoomInfo
    {
        public /* readonly */ int levelNumber;
        public /* readonly */ int roomNumber;

        public RoomInfo()   // This constructor might help fix my Cassandra Linq bug.
        {
            levelNumber = 0;
            roomNumber = 0;
        }

        public RoomInfo(int l, int r)
        {
            levelNumber = l;
            roomNumber = r;
        }

        public override bool Equals(object obj)
        {

            if (object.ReferenceEquals(this, obj))
            {
                return true;
            }

            var otherRoomInfo = obj as RoomInfo;

            return otherRoomInfo != null && roomNumber == otherRoomInfo.roomNumber && levelNumber == otherRoomInfo.levelNumber;
        }

        public override int GetHashCode()
        {
            return roomNumber + 1024 * levelNumber;
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", levelNumber, roomNumber);
        }

        private List<RoomInfo> GeneratePossibleNeighboursOnLevel(LabyrinthGenerator generator, int newLevel)
        {
            var result = new List<RoomInfo>();

            if (roomNumber == generator.numberOfRoomsPerLevel - 1)
            {
                // Rooms with this room number form the central core of the tower.

                for (int i = 0; i < generator.numberOfRoomsPerLevel - 1; ++i)
                {
                    result.Add(new RoomInfo(newLevel, i));
                }
            }
            else
            {
                result.Add(new RoomInfo(newLevel, (roomNumber + 1) % (generator.numberOfRoomsPerLevel - 1)));
                result.Add(new RoomInfo(newLevel, (roomNumber + generator.numberOfRoomsPerLevel - 2) % (generator.numberOfRoomsPerLevel - 1)));
                result.Add(new RoomInfo(newLevel, generator.numberOfRoomsPerLevel - 1));
            }

            return result;
        }

        public List<RoomInfo> GeneratePossibleNeighbours(LabyrinthGenerator generator)
        {
            var result = new List<RoomInfo>();

            if (levelNumber > 0)
            {
                result.AddRange(GeneratePossibleNeighboursOnLevel(generator, levelNumber - 1));
            }

            if (levelNumber < generator.numberOfLevels - 1)
            {
                result.AddRange(GeneratePossibleNeighboursOnLevel(generator, levelNumber + 1));
            }

            return result;
        }
    }
}
