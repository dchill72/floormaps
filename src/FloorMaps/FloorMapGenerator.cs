using System;
using System.Collections.Generic;
using FloorMaps.Internal;

namespace FloorMaps
{
    /// <summary>
    /// Entry point for dungeon floor map generation.
    ///
    /// Usage:
    ///   var gen = new FloorMapGenerator();
    ///   var map = gen.Generate(new FloorMapConfig { Width = 80, Height = 80, Seed = 42 });
    /// </summary>
    public class FloorMapGenerator
    {
        public FloorMap Generate(FloorMapConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.Validate();

            // Resolve seed.
            int seed = config.Seed ?? Environment.TickCount;
            var rng  = new Random(seed);

            // ── 1. BSP split ─────────────────────────────────────────────────────
            var bounds   = new TileRect(0, 0, config.Width, config.Height);
            var bspTree  = new BspTree(rng, config.MinLeafSize);
            var root     = bspTree.Build(bounds);
            var leaves   = BspTree.GetLeaves(root);

            // ── 2. Room placement ────────────────────────────────────────────────
            var placer = new RoomPlacer(rng, config);
            var rooms  = placer.Place(leaves);

            // ── 3. Shape culling (circle) ────────────────────────────────────────
            // Already handled inside RoomPlacer for efficiency.

            // ── 4. Size / sliver culling ─────────────────────────────────────────
            rooms = RoomCuller.Cull(rooms, config);

            // Re-assign sequential IDs after culling.
            var finalRooms = ReIndex(rooms);

            // ── 4b. Assign room heights ──────────────────────────────────────────
            foreach (var room in finalRooms)
            {
                double t = rng.NextDouble();
                room.Height = (float)(config.MinRoomHeight + t * (config.MaxRoomHeight - config.MinRoomHeight));
            }

            // ── 5. Connectivity graph ────────────────────────────────────────────
            var edges = GraphBuilder.Build(finalRooms, config.LoopFactor, rng);

            // ── 6. Hallway routing ───────────────────────────────────────────────
            var router   = new HallwayRouter(rng, config);
            var hallways = router.Route(edges, finalRooms);

            // ── 7. Collect portals ───────────────────────────────────────────────
            var allPortals = new List<Portal>();
            foreach (var h in hallways)
                allPortals.AddRange(h.Portals);

            // ── 8. Rasterize ─────────────────────────────────────────────────────
            var tiles = TileRasterizer.Rasterize(
                config.Width, config.Height, finalRooms, hallways);

            return new FloorMap(tiles, finalRooms, hallways, allPortals, bounds, seed);
        }

        /// <summary>
        /// Creates new Room objects with sequential IDs 0..n-1 so that
        /// IDs remain valid indices into the returned list after culling.
        /// </summary>
        private static List<Room> ReIndex(List<Room> rooms)
        {
            var result = new List<Room>(rooms.Count);
            for (int i = 0; i < rooms.Count; i++)
                result.Add(new Room(i, rooms[i].Bounds, rooms[i].Type));
            return result;
        }
    }
}
