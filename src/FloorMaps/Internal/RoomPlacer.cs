using System;
using System.Collections.Generic;

namespace FloorMaps.Internal
{
    /// <summary>
    /// Places one room rect inside each BSP leaf, leaving a 1-tile padding
    /// on every side so rooms never touch the partition boundary directly.
    /// Leaves may be skipped based on EmptyLeafChance.
    /// For circle-bounded maps, rooms with any corner outside the circle are discarded.
    /// </summary>
    internal class RoomPlacer
    {
        private readonly Random _rng;
        private readonly FloorMapConfig _config;

        internal RoomPlacer(Random rng, FloorMapConfig config)
        {
            _rng    = rng;
            _config = config;
        }

        internal List<Room> Place(List<BspTree.Node> leaves)
        {
            var rooms = new List<Room>();
            int nextId = 0;

            foreach (var leaf in leaves)
            {
                if (_rng.NextDouble() < _config.EmptyLeafChance)
                    continue;

                var rect = leaf.Rect;

                // Inner usable area after padding.
                int innerX = rect.X + 1;
                int innerY = rect.Y + 1;
                int maxW   = rect.Width  - 2;
                int maxH   = rect.Height - 2;

                if (maxW < 2 || maxH < 2) continue;

                // Randomise the room dimensions within the available space.
                int w = _rng.Next(maxW / 2, maxW + 1);
                int h = _rng.Next(maxH / 2, maxH + 1);

                // Random position inside the padded area.
                int x = innerX + _rng.Next(0, maxW - w + 1);
                int y = innerY + _rng.Next(0, maxH - h + 1);

                var roomRect = new TileRect(x, y, w, h);

                if (_config.Shape == BoundingShape.Circle && !FitsInCircle(roomRect))
                    continue;

                rooms.Add(new Room(nextId++, roomRect));
            }

            return rooms;
        }

        /// <summary>
        /// Returns false if any corner of the rect falls outside the circle
        /// whose diameter equals config.Width and which is centred in the map.
        /// </summary>
        private bool FitsInCircle(TileRect rect)
        {
            float cx = _config.Width  / 2f;
            float cy = _config.Width  / 2f; // diameter = Width for circles
            float r  = _config.Width  / 2f;

            // All four corners must be inside (or on) the circle.
            int[] xs = { rect.X, rect.Right - 1 };
            int[] ys = { rect.Y, rect.Bottom - 1 };

            foreach (int x in xs)
            foreach (int y in ys)
            {
                float dx = x - cx;
                float dy = y - cy;
                if (dx * dx + dy * dy > r * r) return false;
            }
            return true;
        }
    }
}
