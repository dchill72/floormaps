using System;
using System.Collections.Generic;

namespace FloorMaps.Internal
{
    /// <summary>
    /// Removes rooms that are too small or too sliver-like.
    ///
    /// Two passes:
    ///   1. Aspect ratio cull  — drops rooms where longer/shorter side > MaxAspectRatio.
    ///   2. Area cull          — drops rooms with area below (CullRatio × median area).
    /// </summary>
    internal static class RoomCuller
    {
        internal static List<Room> Cull(List<Room> rooms, FloorMapConfig config)
        {
            if (rooms.Count == 0) return rooms;

            // Pass 1: aspect ratio.
            var afterAspect = new List<Room>(rooms.Count);
            foreach (var room in rooms)
            {
                if (room.Bounds.AspectRatio <= config.MaxAspectRatio)
                    afterAspect.Add(room);
            }

            if (afterAspect.Count == 0) return afterAspect;

            // Pass 2: area relative to median.
            float median = ComputeMedianArea(afterAspect);
            float threshold = median * config.CullRatio;

            var result = new List<Room>(afterAspect.Count);
            foreach (var room in afterAspect)
            {
                if (room.Bounds.Area >= threshold)
                    result.Add(room);
            }

            return result;
        }

        private static float ComputeMedianArea(List<Room> rooms)
        {
            var areas = new int[rooms.Count];
            for (int i = 0; i < rooms.Count; i++)
                areas[i] = rooms[i].Bounds.Area;
            Array.Sort(areas);

            int n = areas.Length;
            return n % 2 == 1
                ? areas[n / 2]
                : (areas[n / 2 - 1] + areas[n / 2]) / 2f;
        }
    }
}
