using System.Collections.Generic;

namespace FloorMaps.Internal
{
    /// <summary>
    /// Paints rooms and hallway segments into a TileType[width, height] grid.
    ///
    /// Paint order:
    ///   1. HallwayFloor for all hallway segments.
    ///   2. RoomFloor for all rooms (rooms overwrite hallway tiles where they overlap,
    ///      which keeps the room identity dominant at junctions).
    /// </summary>
    internal static class TileRasterizer
    {
        internal static TileType[,] Rasterize(
            int width, int height,
            List<Room> rooms,
            List<Hallway> hallways)
        {
            var tiles = new TileType[width, height];
            // All entries default to TileType.Empty (0).

            // Pass 1 — hallway segments.
            foreach (var hallway in hallways)
            foreach (var seg in hallway.Segments)
                PaintRect(tiles, width, height, seg, TileType.HallwayFloor);

            // Pass 2 — room floors (dominant).
            foreach (var room in rooms)
                PaintRect(tiles, width, height, room.Bounds, TileType.RoomFloor);

            return tiles;
        }

        private static void PaintRect(
            TileType[,] tiles, int width, int height,
            TileRect rect, TileType type)
        {
            int x0 = Clamp(rect.X,      0, width  - 1);
            int x1 = Clamp(rect.Right,  0, width);
            int y0 = Clamp(rect.Y,      0, height - 1);
            int y1 = Clamp(rect.Bottom, 0, height);

            for (int x = x0; x < x1; x++)
            for (int y = y0; y < y1; y++)
                tiles[x, y] = type;
        }

        private static int Clamp(int v, int lo, int hi) =>
            v < lo ? lo : v > hi ? hi : v;
    }
}
