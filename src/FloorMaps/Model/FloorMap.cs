using System.Collections.Generic;

namespace FloorMaps
{
    public class FloorMap
    {
        /// <summary>
        /// 2D tile grid indexed [x, y]. Dimensions are Bounds.Width × Bounds.Height.
        /// Coordinate (0,0) corresponds to Bounds.X, Bounds.Y in world tile space.
        /// </summary>
        public TileType[,] Tiles { get; }

        public IReadOnlyList<Room> Rooms { get; }
        public IReadOnlyList<Hallway> Hallways { get; }

        /// <summary>All portals across all hallways, as a convenience collection.</summary>
        public IReadOnlyList<Portal> Portals { get; }

        /// <summary>The bounding rectangle of the entire map in tile coordinates.</summary>
        public TileRect Bounds { get; }

        /// <summary>The seed used to generate this map.</summary>
        public int Seed { get; }

        internal FloorMap(
            TileType[,] tiles,
            IReadOnlyList<Room> rooms,
            IReadOnlyList<Hallway> hallways,
            IReadOnlyList<Portal> portals,
            TileRect bounds,
            int seed)
        {
            Tiles    = tiles;
            Rooms    = rooms;
            Hallways = hallways;
            Portals  = portals;
            Bounds   = bounds;
            Seed     = seed;
        }

        /// <summary>
        /// Returns the tile type at local grid coordinates (lx, ly).
        /// Returns Empty if out of bounds.
        /// </summary>
        public TileType GetTile(int lx, int ly)
        {
            if (lx < 0 || lx >= Bounds.Width || ly < 0 || ly >= Bounds.Height)
                return TileType.Empty;
            return Tiles[lx, ly];
        }
    }
}
