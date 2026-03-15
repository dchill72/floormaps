using System;
using System.Collections.Generic;

namespace FloorMaps.Internal
{
    /// <summary>
    /// Builds Hallway objects (segments + portals) between connected room pairs.
    ///
    /// Segments are computed by BuildHV / BuildVH.
    /// Portals are derived from the *actual* segment geometry by scanning every
    /// segment for adjacency to each room face — not from pre-computed flags.
    /// This guarantees that a portal is generated only where a segment truly
    /// meets a room wall, and on the correct face regardless of route orientation.
    /// </summary>
    internal class HallwayRouter
    {
        private readonly Random         _rng;
        private readonly FloorMapConfig _config;

        internal HallwayRouter(Random rng, FloorMapConfig config)
        {
            _rng    = rng;
            _config = config;
        }

        // ── Public entry ─────────────────────────────────────────────────────────

        internal List<Hallway> Route(List<GraphBuilder.Edge> edges, List<Room> rooms)
        {
            var hallways = new List<Hallway>();

            foreach (var edge in edges)
            {
                var roomA = rooms[edge.A];
                var roomB = rooms[edge.B];

                int hallwayWidth = _rng.Next(_config.MinHallwayWidth, _config.MaxHallwayWidth + 1);

                int maxPW       = Math.Min(_config.MaxPortalWidth, hallwayWidth);
                int minPW       = Math.Min(_config.MinPortalWidth, maxPW);
                int portalWidth = _rng.Next(minPW, maxPW + 1);

                var segs = TryRoute(roomA, roomB, rooms, hallwayWidth);
                if (segs == null) continue;

                var hallway = new Hallway(roomA, roomB, segs);

                // Portals are derived from where segments actually touch each room.
                var pA = FindPortalForRoom(roomA.Bounds, segs, portalWidth);
                var pB = FindPortalForRoom(roomB.Bounds, segs, portalWidth);

                if (pA.HasValue)
                    hallway._portals.Add(new Portal(roomA, hallway, pA.Value.bounds, pA.Value.facing));
                if (pB.HasValue)
                    hallway._portals.Add(new Portal(roomB, hallway, pB.Value.bounds, pB.Value.facing));

                hallways.Add(hallway);
                roomA._connections.Add(hallway);
                roomB._connections.Add(hallway);
            }

            return hallways;
        }

        // ── Route selection ──────────────────────────────────────────────────────

        private List<TileRect>? TryRoute(Room a, Room b, List<Room> allRooms, int hw)
        {
            var hv = BuildHV(a, b, hw);
            var vh = BuildVH(a, b, hw);

            bool hvOk = hv != null && !AnySegmentOverlapsRoom(hv, allRooms, a, b);
            bool vhOk = vh != null && !AnySegmentOverlapsRoom(vh, allRooms, a, b);

            if (hvOk && vhOk) return _rng.Next(2) == 0 ? hv : vh;
            if (hvOk) return hv;
            if (vhOk) return vh;
            return null;
        }

        // ── L-shape builders (segments only) ─────────────────────────────────────

        /// <summary>Horizontal-first L: exit A's left/right face, enter B's top/bottom face.</summary>
        private static List<TileRect>? BuildHV(Room a, Room b, int hw)
        {
            int half     = hw / 2;
            int ceilHalf = hw - half;

            int ayMin = a.Bounds.Y + half,      ayMax = a.Bounds.Bottom - ceilHalf;
            int bxMin = b.Bounds.X + half,      bxMax = b.Bounds.Right  - ceilHalf;
            if (ayMin > ayMax || bxMin > bxMax) return null;

            int exitY = Clamp(b.Bounds.CenterY, ayMin, ayMax);
            int bendX = Clamp(b.Bounds.CenterX, bxMin, bxMax);

            bool bRight = b.Bounds.CenterX >= a.Bounds.CenterX;
            bool bBelow = b.Bounds.CenterY >= a.Bounds.CenterY;

            int aFaceX = bRight ? a.Bounds.Right : a.Bounds.X;
            int bFaceY = bBelow ? b.Bounds.Y     : b.Bounds.Bottom;

            int hLeft  = Math.Min(aFaceX, bendX - half);
            int hRight = Math.Max(aFaceX, bendX + ceilHalf);
            int hWidth = hRight - hLeft;

            int vTop    = Math.Min(exitY - half, bFaceY);
            int vBottom = Math.Max(exitY + ceilHalf, bFaceY);
            int vHeight = vBottom - vTop;

            var segs = new List<TileRect>(2);
            if (hWidth  > 0) segs.Add(new TileRect(hLeft,        exitY - half, hWidth,  hw));
            if (vHeight > 0) segs.Add(new TileRect(bendX - half, vTop,         hw,      vHeight));
            return segs.Count > 0 ? segs : null;
        }

        /// <summary>Vertical-first L: exit A's top/bottom face, enter B's left/right face.</summary>
        private static List<TileRect>? BuildVH(Room a, Room b, int hw)
        {
            int half     = hw / 2;
            int ceilHalf = hw - half;

            int axMin = a.Bounds.X + half,      axMax = a.Bounds.Right  - ceilHalf;
            int byMin = b.Bounds.Y + half,      byMax = b.Bounds.Bottom - ceilHalf;
            if (axMin > axMax || byMin > byMax) return null;

            int exitX = Clamp(b.Bounds.CenterX, axMin, axMax);
            int bendY = Clamp(b.Bounds.CenterY, byMin, byMax);

            bool bRight = b.Bounds.CenterX >= a.Bounds.CenterX;
            bool bBelow = b.Bounds.CenterY >= a.Bounds.CenterY;

            int aFaceY = bBelow ? a.Bounds.Bottom : a.Bounds.Y;
            int bFaceX = bRight ? b.Bounds.X      : b.Bounds.Right;

            int vTop    = Math.Min(aFaceY, bendY - half);
            int vBottom = Math.Max(aFaceY, bendY + ceilHalf);
            int vHeight = vBottom - vTop;

            int hLeft  = Math.Min(exitX - half, bFaceX);
            int hRight = Math.Max(exitX + ceilHalf, bFaceX);
            int hWidth = hRight - hLeft;

            var segs = new List<TileRect>(2);
            if (vHeight > 0) segs.Add(new TileRect(exitX - half, vTop,         hw,     vHeight));
            if (hWidth  > 0) segs.Add(new TileRect(hLeft,        bendY - half, hWidth, hw));
            return segs.Count > 0 ? segs : null;
        }

        // ── Portal derivation ────────────────────────────────────────────────────

        /// <summary>
        /// Scans <paramref name="segs"/> for a segment that exits through one of
        /// <paramref name="room"/>'s four faces — either by being flush against the
        /// face (adjacent) or by crossing through it from the inside.
        ///
        /// Face rules (exclusive rect convention: Right = X + Width):
        ///   Right  face: seg.Right &gt; room.Right  &amp;&amp; seg.X     &lt;= room.Right  → facing West
        ///   Left   face: seg.X     &lt; room.X      &amp;&amp; seg.Right &gt;= room.X      → facing East
        ///   Bottom face: seg.Bottom &gt; room.Bottom &amp;&amp; seg.Y    &lt;= room.Bottom → facing North
        ///   Top    face: seg.Y     &lt; room.Y      &amp;&amp; seg.Bottom &gt;= room.Y     → facing South
        /// Each check also requires the segment to overlap the room in the
        /// perpendicular axis so that the face is actually reached.
        /// </summary>
        private static (TileRect bounds, CardinalDirection facing)? FindPortalForRoom(
            TileRect room, List<TileRect> segs, int pw)
        {
            int half     = pw / 2;
            int ceilHalf = pw - half;

            foreach (var seg in segs)
            {
                bool yOverlap = seg.Y < room.Bottom && seg.Bottom > room.Y;
                bool xOverlap = seg.X < room.Right  && seg.Right  > room.X;

                // Right face
                if (seg.Right > room.Right && seg.X <= room.Right && yOverlap)
                {
                    var p = VFacePortal(room.Right - 1, seg, room.Y, room.Bottom,
                                        half, ceilHalf, pw, CardinalDirection.West);
                    if (p.HasValue) return p;
                }

                // Left face
                if (seg.X < room.X && seg.Right >= room.X && yOverlap)
                {
                    var p = VFacePortal(room.X, seg, room.Y, room.Bottom,
                                        half, ceilHalf, pw, CardinalDirection.East);
                    if (p.HasValue) return p;
                }

                // Bottom face
                if (seg.Bottom > room.Bottom && seg.Y <= room.Bottom && xOverlap)
                {
                    var p = HFacePortal(room.Bottom - 1, seg, room.X, room.Right,
                                        half, ceilHalf, pw, CardinalDirection.North);
                    if (p.HasValue) return p;
                }

                // Top face
                if (seg.Y < room.Y && seg.Bottom >= room.Y && xOverlap)
                {
                    var p = HFacePortal(room.Y, seg, room.X, room.Right,
                                        half, ceilHalf, pw, CardinalDirection.South);
                    if (p.HasValue) return p;
                }
            }

            return null;
        }

        /// <summary>
        /// Portal on a vertical face (left or right wall).
        /// col is the innermost room column on that face.
        /// The Y overlap between the segment and the room face determines the centre.
        /// </summary>
        private static (TileRect bounds, CardinalDirection facing)? VFacePortal(
            int col, TileRect seg,
            int roomYMin, int roomYMax,
            int half, int ceilHalf, int pw, CardinalDirection facing)
        {
            int y0 = Math.Max(seg.Y,      roomYMin);
            int y1 = Math.Min(seg.Bottom, roomYMax);
            if (y0 >= y1) return null;

            int cy   = (y0 + y1) / 2;
            int pMin = roomYMin + half;
            int pMax = roomYMax - ceilHalf;
            if (pMin > pMax) return null;

            cy = Clamp(cy, pMin, pMax);
            return (new TileRect(col, cy - half, 1, pw), facing);
        }

        /// <summary>
        /// Portal on a horizontal face (top or bottom wall).
        /// row is the innermost room row on that face.
        /// The X overlap between the segment and the room face determines the centre.
        /// </summary>
        private static (TileRect bounds, CardinalDirection facing)? HFacePortal(
            int row, TileRect seg,
            int roomXMin, int roomXMax,
            int half, int ceilHalf, int pw, CardinalDirection facing)
        {
            int x0 = Math.Max(seg.X,     roomXMin);
            int x1 = Math.Min(seg.Right, roomXMax);
            if (x0 >= x1) return null;

            int cx   = (x0 + x1) / 2;
            int pMin = roomXMin + half;
            int pMax = roomXMax - ceilHalf;
            if (pMin > pMax) return null;

            cx = Clamp(cx, pMin, pMax);
            return (new TileRect(cx - half, row, pw, 1), facing);
        }

        // ── Overlap check ────────────────────────────────────────────────────────

        private static bool AnySegmentOverlapsRoom(
            List<TileRect> segs, List<Room> rooms, Room skipA, Room skipB)
        {
            foreach (var seg in segs)
            foreach (var room in rooms)
            {
                if (room == skipA || room == skipB) continue;
                if (seg.Overlaps(room.Bounds)) return true;
            }
            return false;
        }

        private static int Clamp(int v, int lo, int hi) =>
            v < lo ? lo : v > hi ? hi : v;
    }
}
