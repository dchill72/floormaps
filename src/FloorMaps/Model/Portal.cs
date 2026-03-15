namespace FloorMaps
{
    /// <summary>
    /// A doorway between a room and a hallway.
    ///
    /// Portals are metadata — they are not reflected in the tile grid.
    /// Use Portal.Bounds to place door entities, trigger zones, or lighting
    /// in your game engine.
    ///
    /// The portal rect is 1 tile deep, sitting on the room face that the
    /// hallway connects to, and PortalWidth tiles wide/tall.
    /// Its tiles are a subset of RoomFloor tiles in the grid.
    /// </summary>
    public class Portal
    {
        /// <summary>The room this portal leads into from the hallway.</summary>
        public Room Room { get; }

        /// <summary>The hallway this portal belongs to.</summary>
        public Hallway Hallway { get; }

        /// <summary>
        /// Tile rect of the doorway opening (1 tile deep on the room face).
        /// Width == 1 for left/right-face portals; Height == 1 for top/bottom-face portals.
        /// </summary>
        public TileRect Bounds { get; }

        /// <summary>
        /// Direction from the hallway into the room through this portal.
        /// E.g. West means the hallway is to the right and you walk west to enter.
        /// </summary>
        public CardinalDirection Facing { get; }

        internal Portal(Room room, Hallway hallway, TileRect bounds, CardinalDirection facing)
        {
            Room    = room;
            Hallway = hallway;
            Bounds  = bounds;
            Facing  = facing;
        }

        public override string ToString() =>
            $"Portal(Room#{Room.Id} ← {Facing} {Bounds})";
    }
}
