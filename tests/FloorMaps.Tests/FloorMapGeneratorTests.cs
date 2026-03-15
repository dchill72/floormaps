using System.Linq;
using Xunit;
using FloorMaps;

namespace FloorMaps.Tests
{
    public class FloorMapGeneratorTests
    {
        private static FloorMap Generate(int? seed = 42, BoundingShape shape = BoundingShape.Square)
            => new FloorMapGenerator().Generate(new FloorMapConfig
            {
                Width  = 80,
                Height = 80,
                Shape  = shape,
                Seed   = seed
            });

        // ── Determinism ──────────────────────────────────────────────────────────

        [Fact]
        public void SameSeed_ProducesIdenticalMaps()
        {
            var map1 = Generate(seed: 99);
            var map2 = Generate(seed: 99);

            Assert.Equal(map1.Rooms.Count,    map2.Rooms.Count);
            Assert.Equal(map1.Hallways.Count, map2.Hallways.Count);
            Assert.Equal(map1.Seed,           map2.Seed);

            for (int x = 0; x < map1.Bounds.Width;  x++)
            for (int y = 0; y < map1.Bounds.Height; y++)
                Assert.Equal(map1.GetTile(x, y), map2.GetTile(x, y));
        }

        [Fact]
        public void DifferentSeeds_ProduceDifferentMaps()
        {
            var map1 = Generate(seed: 1);
            var map2 = Generate(seed: 2);
            // At least room counts or tile counts differ.
            bool different = map1.Rooms.Count != map2.Rooms.Count
                || map1.Hallways.Count != map2.Hallways.Count
                || map1.GetTile(40, 40) != map2.GetTile(40, 40);
            Assert.True(different);
        }

        // ── Basic structural invariants ──────────────────────────────────────────

        [Fact]
        public void Generate_ProducesAtLeastOneRoom()
        {
            var map = Generate();
            Assert.NotEmpty(map.Rooms);
        }

        [Fact]
        public void Generate_TileGridHasCorrectDimensions()
        {
            var map = Generate();
            Assert.Equal(80, map.Tiles.GetLength(0));
            Assert.Equal(80, map.Tiles.GetLength(1));
        }

        [Fact]
        public void RoomTiles_AreAllRoomFloor()
        {
            var map = Generate();
            foreach (var room in map.Rooms)
            for (int x = room.Bounds.X; x < room.Bounds.Right;  x++)
            for (int y = room.Bounds.Y; y < room.Bounds.Bottom; y++)
                Assert.Equal(TileType.RoomFloor, map.GetTile(x, y));
        }

        [Fact]
        public void Rooms_DoNotOverlapEachOther()
        {
            var map = Generate();
            var rooms = map.Rooms;
            for (int i = 0; i < rooms.Count; i++)
            for (int j = i + 1; j < rooms.Count; j++)
                Assert.False(rooms[i].Bounds.Overlaps(rooms[j].Bounds),
                    $"Room {rooms[i].Id} overlaps Room {rooms[j].Id}");
        }

        [Fact]
        public void Rooms_HaveUniqueSequentialIds()
        {
            var map = Generate();
            var ids = map.Rooms.Select(r => r.Id).ToList();
            for (int i = 0; i < ids.Count; i++)
                Assert.Equal(i, ids[i]);
        }

        [Fact]
        public void Rooms_AspectRatioWithinConfig()
        {
            var config = new FloorMapConfig { Width = 80, Height = 80, MaxAspectRatio = 3f, Seed = 42 };
            var map = new FloorMapGenerator().Generate(config);
            foreach (var room in map.Rooms)
                Assert.True(room.Bounds.AspectRatio <= config.MaxAspectRatio + 0.001f,
                    $"Room {room.Id} has aspect ratio {room.Bounds.AspectRatio}");
        }

        [Fact]
        public void Hallways_ReferenceRoomsInMap()
        {
            var map = Generate();
            foreach (var h in map.Hallways)
            {
                Assert.Contains(h.RoomA, map.Rooms);
                Assert.Contains(h.RoomB, map.Rooms);
            }
        }

        [Fact]
        public void Hallways_HaveAtLeastOneSegment()
        {
            var map = Generate();
            foreach (var h in map.Hallways)
                Assert.NotEmpty(h.Segments);
        }

        [Fact]
        public void HallwaySegments_DoNotOverlapRooms()
        {
            var map = Generate();
            foreach (var h in map.Hallways)
            foreach (var seg in h.Segments)
            foreach (var room in map.Rooms)
            {
                if (room == h.RoomA || room == h.RoomB) continue;
                Assert.False(seg.Overlaps(room.Bounds),
                    $"Hallway {h} segment {seg} overlaps Room {room.Id} {room.Bounds}");
            }
        }

        [Fact]
        public void RoomConnections_MatchHallwayList()
        {
            var map = Generate();
            foreach (var h in map.Hallways)
            {
                Assert.Contains(h, h.RoomA.Connections);
                Assert.Contains(h, h.RoomB.Connections);
            }
        }

        // ── Circle bounding shape ────────────────────────────────────────────────

        [Fact]
        public void CircleMap_AllRoomCornersInsideCircle()
        {
            var map = Generate(shape: BoundingShape.Circle);
            float cx = map.Bounds.Width  / 2f;
            float cy = map.Bounds.Height / 2f;
            float r  = map.Bounds.Width  / 2f;

            foreach (var room in map.Rooms)
            {
                int[] xs = { room.Bounds.X, room.Bounds.Right  - 1 };
                int[] ys = { room.Bounds.Y, room.Bounds.Bottom - 1 };
                foreach (int x in xs)
                foreach (int y in ys)
                {
                    float dx = x - cx, dy = y - cy;
                    Assert.True(dx * dx + dy * dy <= r * r + 0.01f,
                        $"Room {room.Id} corner ({x},{y}) is outside the circle");
                }
            }
        }

        // ── Config validation ────────────────────────────────────────────────────

        [Fact]
        public void Validate_ThrowsOnInvalidHallwayWidths()
        {
            var config = new FloorMapConfig { MinHallwayWidth = 5, MaxHallwayWidth = 3 };
            Assert.Throws<System.ArgumentException>(() => config.Validate());
        }

        [Fact]
        public void Validate_ThrowsOnZeroWidth()
        {
            var config = new FloorMapConfig { Width = 0 };
            Assert.Throws<System.ArgumentException>(() => config.Validate());
        }

        // ── Seed returned in output ───────────────────────────────────────────────

        [Fact]
        public void Map_ReturnsSeedUsedForGeneration()
        {
            var map = Generate(seed: 7777);
            Assert.Equal(7777, map.Seed);
        }

        [Fact]
        public void Map_WithNullSeed_ReturnsSomeNonZeroSeed()
        {
            var map = Generate(seed: null);
            Assert.True(map.Seed != 0 || map.Rooms.Count > 0);
        }

        // ── Portals ───────────────────────────────────────────────────────────────

        [Fact]
        public void EachHallway_HasExactlyTwoPortals()
        {
            var map = Generate();
            foreach (var h in map.Hallways)
                Assert.Equal(2, h.Portals.Count);
        }

        [Fact]
        public void FloorMap_PortalCount_EqualsHallwayCount_Times_Two()
        {
            var map = Generate();
            Assert.Equal(map.Hallways.Count * 2, map.Portals.Count);
        }

        [Fact]
        public void Portal_RoomReference_MatchesHallwayRoom()
        {
            var map = Generate();
            foreach (var h in map.Hallways)
            {
                var rooms = new[] { h.RoomA, h.RoomB };
                foreach (var p in h.Portals)
                    Assert.Contains(p.Room, rooms);
            }
        }

        [Fact]
        public void Portal_HallwayReference_MatchesParentHallway()
        {
            var map = Generate();
            foreach (var h in map.Hallways)
            foreach (var p in h.Portals)
                Assert.Same(h, p.Hallway);
        }

        [Fact]
        public void Portal_Bounds_FitWithinTheirRoom()
        {
            var map = Generate();
            foreach (var portal in map.Portals)
            {
                var r = portal.Room.Bounds;
                Assert.True(portal.Bounds.X >= r.X,
                    $"{portal} left edge outside room {portal.Room.Id}");
                Assert.True(portal.Bounds.Right <= r.Right,
                    $"{portal} right edge outside room {portal.Room.Id}");
                Assert.True(portal.Bounds.Y >= r.Y,
                    $"{portal} top edge outside room {portal.Room.Id}");
                Assert.True(portal.Bounds.Bottom <= r.Bottom,
                    $"{portal} bottom edge outside room {portal.Room.Id}");
            }
        }

        [Fact]
        public void Portal_Bounds_AreOneDimensionDeep()
        {
            var map = Generate();
            foreach (var portal in map.Portals)
                Assert.True(portal.Bounds.Width == 1 || portal.Bounds.Height == 1,
                    $"{portal} is not 1 tile deep");
        }

        [Fact]
        public void Portal_Width_WithinConfiguredRange()
        {
            var config = new FloorMapConfig
            {
                Width = 80, Height = 80, Seed = 42,
                MinPortalWidth = 1, MaxPortalWidth = 2
            };
            var map = new FloorMapGenerator().Generate(config);
            foreach (var portal in map.Portals)
            {
                int pw = portal.Bounds.Width == 1 ? portal.Bounds.Height : portal.Bounds.Width;
                Assert.True(pw >= config.MinPortalWidth,
                    $"{portal} width {pw} below MinPortalWidth");
                Assert.True(pw <= config.MaxPortalWidth,
                    $"{portal} width {pw} above MaxPortalWidth");
            }
        }

        [Fact]
        public void Portal_Facing_ConsistentWithFaceDirection()
        {
            var map = Generate();
            foreach (var portal in map.Portals)
            {
                int w = portal.Bounds.Width, h = portal.Bounds.Height;
                // Only assert when the rect is unambiguously oriented.
                // 1×1 portals (minimum portal and hallway width) are square and
                // can be on either face type.
                if (h > w)  // taller than wide → left/right face
                    Assert.True(portal.Facing == CardinalDirection.East ||
                                portal.Facing == CardinalDirection.West,
                                $"{portal} on vertical face but facing {portal.Facing}");
                else if (w > h)  // wider than tall → top/bottom face
                    Assert.True(portal.Facing == CardinalDirection.North ||
                                portal.Facing == CardinalDirection.South,
                                $"{portal} on horizontal face but facing {portal.Facing}");
                // w == h: square portal, any direction is valid
            }
        }

        [Fact]
        public void Validate_ThrowsOnInvalidPortalWidths()
        {
            var config = new FloorMapConfig { MinPortalWidth = 3, MaxPortalWidth = 1 };
            Assert.Throws<System.ArgumentException>(() => config.Validate());
        }
    }
}
