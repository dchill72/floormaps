using System.Collections.Generic;

namespace FloorMaps
{
    public class Hallway
    {
        public Room RoomA { get; }
        public Room RoomB { get; }

        /// <summary>
        /// Ordered list of axis-aligned rectangles forming a continuous passage
        /// between RoomA and RoomB. Segments do not overlap any Room.
        /// </summary>
        public IReadOnlyList<TileRect> Segments { get; }

        /// <summary>
        /// The two doorway portals for this hallway — one on RoomA's face,
        /// one on RoomB's face. Always contains exactly 2 entries.
        /// </summary>
        public IReadOnlyList<Portal> Portals => _portals;
        internal List<Portal> _portals = new List<Portal>();

        internal Hallway(Room roomA, Room roomB, IReadOnlyList<TileRect> segments)
        {
            RoomA    = roomA;
            RoomB    = roomB;
            Segments = segments;
        }

        public Room Other(Room room) => room == RoomA ? RoomB : RoomA;

        public override string ToString() =>
            $"Hallway({RoomA.Id} <-> {RoomB.Id}, {Segments.Count} segments)";
    }
}
