using System.Collections.Generic;

namespace FloorMaps
{
    public class Room
    {
        public int Id { get; }
        public TileRect Bounds { get; }
        public RoomType Type { get; internal set; }

        /// <summary>Ceiling height of this room in world units, derived from the map seed.</summary>
        public float Height { get; internal set; }

        internal List<Hallway> _connections = new List<Hallway>();
        public IReadOnlyList<Hallway> Connections => _connections;

        internal Room(int id, TileRect bounds, RoomType type = RoomType.Normal)
        {
            Id = id;
            Bounds = bounds;
            Type = type;
        }

        public override string ToString() => $"Room#{Id} {Bounds}";
    }
}
