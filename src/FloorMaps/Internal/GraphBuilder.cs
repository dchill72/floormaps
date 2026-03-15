using System;
using System.Collections.Generic;

namespace FloorMaps.Internal
{
    /// <summary>
    /// Builds a connectivity graph over rooms:
    ///   1. Bowyer–Watson Delaunay triangulation on room centres.
    ///   2. Kruskal MST for guaranteed connectivity.
    ///   3. Re-add a random fraction (LoopFactor) of the non-MST edges to create loops.
    /// Returns an edge list as pairs of room indices into the supplied list.
    /// </summary>
    internal static class GraphBuilder
    {
        internal readonly struct Edge
        {
            public readonly int A;   // index into rooms list
            public readonly int B;
            public readonly float DistSq;
            public Edge(int a, int b, float distSq) { A = a; B = b; DistSq = distSq; }
        }

        internal static List<Edge> Build(List<Room> rooms, float loopFactor, Random rng)
        {
            if (rooms.Count <= 1) return new List<Edge>();
            if (rooms.Count == 2) return new List<Edge> { MakeEdge(rooms, 0, 1) };

            var delaunay = Triangulate(rooms);

            // Sort by distance for Kruskal.
            delaunay.Sort((a, b) => a.DistSq.CompareTo(b.DistSq));

            var mstEdges   = new List<Edge>();
            var extraEdges = new List<Edge>();
            var uf         = new UnionFind(rooms.Count);

            foreach (var edge in delaunay)
            {
                if (uf.Union(edge.A, edge.B))
                    mstEdges.Add(edge);
                else
                    extraEdges.Add(edge);
            }

            // Shuffle extra edges and add back a fraction.
            Shuffle(extraEdges, rng);
            int loopCount = (int)Math.Round(extraEdges.Count * loopFactor);
            for (int i = 0; i < loopCount; i++)
                mstEdges.Add(extraEdges[i]);

            return mstEdges;
        }

        // ── Bowyer–Watson Delaunay triangulation ────────────────────────────────

        private readonly struct Triangle
        {
            public readonly int A, B, C;
            public Triangle(int a, int b, int c) { A = a; B = b; C = c; }
        }

        private static List<Edge> Triangulate(List<Room> rooms)
        {
            // Use float coords for the geometry.
            int n = rooms.Count;
            var pts = new (float x, float y)[n];
            for (int i = 0; i < n; i++)
                pts[i] = (rooms[i].Bounds.CenterX, rooms[i].Bounds.CenterY);

            // Super-triangle large enough to contain all points.
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in pts)
            {
                if (p.x < minX) minX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.x > maxX) maxX = p.x;
                if (p.y > maxY) maxY = p.y;
            }
            float dx = maxX - minX, dy = maxY - minY;
            float delta = Math.Max(dx, dy) * 10f;

            // Super-triangle vertices are appended at indices n, n+1, n+2.
            var allPts = new (float x, float y)[n + 3];
            for (int i = 0; i < n; i++) allPts[i] = pts[i];
            allPts[n]     = (minX - delta,     minY - delta);
            allPts[n + 1] = (minX + 2 * delta, minY - delta);
            allPts[n + 2] = (minX,              minY + 2 * delta);

            var triangles = new List<Triangle> { new Triangle(n, n + 1, n + 2) };

            for (int pi = 0; pi < n; pi++)
            {
                var (px, py) = allPts[pi];
                var badEdges = new List<(int, int)>();
                var bad      = new List<Triangle>();

                foreach (var tri in triangles)
                {
                    if (InCircumcircle(allPts, tri, px, py))
                    {
                        bad.Add(tri);
                        badEdges.Add((tri.A, tri.B));
                        badEdges.Add((tri.B, tri.C));
                        badEdges.Add((tri.C, tri.A));
                    }
                }

                // Find boundary (edges not shared by two bad triangles).
                var boundary = new List<(int, int)>();
                foreach (var e in badEdges)
                {
                    bool shared = false;
                    foreach (var e2 in badEdges)
                    {
                        if (e.Equals(e2)) continue;
                        if ((e.Item1 == e2.Item2 && e.Item2 == e2.Item1))
                        { shared = true; break; }
                    }
                    if (!shared) boundary.Add(e);
                }

                foreach (var tri in bad) triangles.Remove(tri);
                foreach (var (ea, eb) in boundary)
                    triangles.Add(new Triangle(ea, eb, pi));
            }

            // Collect edges that don't touch super-triangle vertices.
            var edgeSet = new HashSet<(int, int)>();
            var result  = new List<Edge>();

            foreach (var tri in triangles)
            {
                AddEdge(tri.A, tri.B, n, edgeSet, result, rooms);
                AddEdge(tri.B, tri.C, n, edgeSet, result, rooms);
                AddEdge(tri.C, tri.A, n, edgeSet, result, rooms);
            }

            return result;
        }

        private static void AddEdge(int a, int b, int superStart,
            HashSet<(int, int)> seen, List<Edge> edges, List<Room> rooms)
        {
            if (a >= superStart || b >= superStart) return;
            var key = a < b ? (a, b) : (b, a);
            if (!seen.Add(key)) return;
            edges.Add(MakeEdge(rooms, a, b));
        }

        private static bool InCircumcircle((float x, float y)[] pts, Triangle tri, float px, float py)
        {
            var (ax, ay) = pts[tri.A];
            var (bx, by) = pts[tri.B];
            var (cx, cy) = pts[tri.C];

            float ax_ = ax - px, ay_ = ay - py;
            float bx_ = bx - px, by_ = by - py;
            float cx_ = cx - px, cy_ = cy - py;

            float det = ax_ * (by_ * (cx_ * cx_ + cy_ * cy_) - cy_ * (bx_ * bx_ + by_ * by_))
                      - ay_ * (bx_ * (cx_ * cx_ + cy_ * cy_) - cx_ * (bx_ * bx_ + by_ * by_))
                      + (ax_ * ax_ + ay_ * ay_) * (bx_ * cy_ - by_ * cx_);

            return det > 0;
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private static Edge MakeEdge(List<Room> rooms, int a, int b)
        {
            float dx = rooms[a].Bounds.CenterX - rooms[b].Bounds.CenterX;
            float dy = rooms[a].Bounds.CenterY - rooms[b].Bounds.CenterY;
            return new Edge(a, b, dx * dx + dy * dy);
        }

        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ── Union-Find ───────────────────────────────────────────────────────────

        private class UnionFind
        {
            private readonly int[] _parent, _rank;
            public UnionFind(int n) {
                _parent = new int[n]; _rank = new int[n];
                for (int i = 0; i < n; i++) _parent[i] = i;
            }
            public int Find(int x) => _parent[x] == x ? x : _parent[x] = Find(_parent[x]);
            public bool Union(int a, int b) {
                int ra = Find(a), rb = Find(b);
                if (ra == rb) return false;
                if (_rank[ra] < _rank[rb]) (ra, rb) = (rb, ra);
                _parent[rb] = ra;
                if (_rank[ra] == _rank[rb]) _rank[ra]++;
                return true;
            }
        }
    }
}
