using System;

namespace FloorMaps
{
    public class FloorMapConfig
    {
        // ── Bounding shape ──────────────────────────────────────────────────────
        public BoundingShape Shape  { get; set; } = BoundingShape.Square;

        /// <summary>Width of the bounding area in tiles.</summary>
        public int Width  { get; set; } = 80;

        /// <summary>
        /// Height of the bounding area in tiles.
        /// Ignored when Shape is Circle — the diameter equals Width.
        /// </summary>
        public int Height { get; set; } = 80;

        // ── BSP ─────────────────────────────────────────────────────────────────
        /// <summary>
        /// A BSP partition stops splitting when either dimension drops below this.
        /// Smaller values produce more, smaller rooms.
        /// </summary>
        public int MinLeafSize { get; set; } = 10;

        /// <summary>
        /// Probability [0,1] that a BSP leaf produces no room at all.
        /// Higher values make sparser maps.
        /// </summary>
        public float EmptyLeafChance { get; set; } = 0.1f;

        // ── Room culling ─────────────────────────────────────────────────────────
        /// <summary>
        /// Rooms with area below (CullRatio × median area) are removed.
        /// Range [0,1]; 0.45 removes roughly the smallest quarter of rooms.
        /// </summary>
        public float CullRatio { get; set; } = 0.45f;

        /// <summary>
        /// Rooms whose longer side exceeds (MaxAspectRatio × shorter side) are
        /// removed as slivers. E.g. 3.0 rejects any room more than 3× as long as wide.
        /// </summary>
        public float MaxAspectRatio { get; set; } = 3.0f;

        // ── Hallways ─────────────────────────────────────────────────────────────
        /// <summary>Minimum hallway width in tiles (passage clearance).</summary>
        public int MinHallwayWidth { get; set; } = 2;

        /// <summary>Maximum hallway width in tiles.</summary>
        public int MaxHallwayWidth { get; set; } = 4;

        // ── Graph / connectivity ─────────────────────────────────────────────────
        /// <summary>
        /// Fraction of non-MST edges added back as loops.
        /// 0 = pure spanning tree (no cycles); 1 = all Delaunay edges kept.
        /// </summary>
        public float LoopFactor { get; set; } = 0.15f;

        // ── Portals ──────────────────────────────────────────────────────────────
        /// <summary>
        /// Minimum doorway width in tiles.
        /// Must be >= 1 and <= MinHallwayWidth.
        /// </summary>
        public int MinPortalWidth { get; set; } = 1;

        /// <summary>
        /// Maximum doorway width in tiles.
        /// Clamped at generation time to the actual hallway width so a narrow
        /// hallway never gets a wider portal than itself.
        /// Must be >= MinPortalWidth.
        /// </summary>
        public int MaxPortalWidth { get; set; } = 2;

        // ── Room heights ─────────────────────────────────────────────────────────
        /// <summary>Minimum ceiling height for rooms, in world units.</summary>
        public float MinRoomHeight { get; set; } = 3.0f;

        /// <summary>Maximum ceiling height for rooms, in world units.</summary>
        public float MaxRoomHeight { get; set; } = 9.0f;

        // ── Seeding ──────────────────────────────────────────────────────────────
        /// <summary>
        /// RNG seed for deterministic generation.
        /// null means a random seed is chosen each call.
        /// </summary>
        public int? Seed { get; set; } = null;

        public void Validate()
        {
            if (Width  < 1) throw new ArgumentException("Width must be >= 1",  nameof(Width));
            if (Height < 1) throw new ArgumentException("Height must be >= 1", nameof(Height));
            if (MinLeafSize < 4) throw new ArgumentException("MinLeafSize must be >= 4", nameof(MinLeafSize));
            if (EmptyLeafChance < 0f || EmptyLeafChance > 1f)
                throw new ArgumentException("EmptyLeafChance must be in [0,1]", nameof(EmptyLeafChance));
            if (CullRatio < 0f || CullRatio > 1f)
                throw new ArgumentException("CullRatio must be in [0,1]", nameof(CullRatio));
            if (MaxAspectRatio < 1f)
                throw new ArgumentException("MaxAspectRatio must be >= 1", nameof(MaxAspectRatio));
            if (MinHallwayWidth < 1)
                throw new ArgumentException("MinHallwayWidth must be >= 1", nameof(MinHallwayWidth));
            if (MaxHallwayWidth < MinHallwayWidth)
                throw new ArgumentException("MaxHallwayWidth must be >= MinHallwayWidth", nameof(MaxHallwayWidth));
            if (LoopFactor < 0f || LoopFactor > 1f)
                throw new ArgumentException("LoopFactor must be in [0,1]", nameof(LoopFactor));
            if (MinRoomHeight <= 0f)
                throw new ArgumentException("MinRoomHeight must be > 0", nameof(MinRoomHeight));
            if (MaxRoomHeight < MinRoomHeight)
                throw new ArgumentException("MaxRoomHeight must be >= MinRoomHeight", nameof(MaxRoomHeight));
            if (MinPortalWidth < 1)
                throw new ArgumentException("MinPortalWidth must be >= 1", nameof(MinPortalWidth));
            if (MaxPortalWidth < MinPortalWidth)
                throw new ArgumentException("MaxPortalWidth must be >= MinPortalWidth", nameof(MaxPortalWidth));
        }
    }
}
