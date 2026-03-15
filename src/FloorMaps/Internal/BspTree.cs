using System;
using System.Collections.Generic;

namespace FloorMaps.Internal
{
    /// <summary>
    /// Binary Space Partitioning tree. Recursively splits a rectangle into
    /// two children by alternating horizontal/vertical cuts, with a small
    /// random offset around the midpoint to avoid uniform-looking splits.
    /// Leaf nodes are the regions available for room placement.
    /// </summary>
    internal class BspTree
    {
        internal class Node
        {
            public TileRect Rect;
            public Node? Left;
            public Node? Right;
            public bool IsLeaf => Left == null && Right == null;
        }

        private readonly Random _rng;
        private readonly int    _minLeafSize;

        internal BspTree(Random rng, int minLeafSize)
        {
            _rng         = rng;
            _minLeafSize = minLeafSize;
        }

        internal Node Build(TileRect bounds)
        {
            var root = new Node { Rect = bounds };
            Split(root);
            return root;
        }

        private void Split(Node node)
        {
            // Stop if either dimension can't be split into two viable halves.
            bool canSplitH = node.Rect.Height >= _minLeafSize * 2;
            bool canSplitV = node.Rect.Width  >= _minLeafSize * 2;

            if (!canSplitH && !canSplitV) return;

            // Prefer the longer axis; when equal, pick randomly.
            bool splitHorizontal;
            if (canSplitH && canSplitV)
                splitHorizontal = node.Rect.Height > node.Rect.Width
                    ? true
                    : node.Rect.Width > node.Rect.Height
                        ? false
                        : _rng.Next(2) == 0;
            else
                splitHorizontal = canSplitH;

            if (splitHorizontal)
            {
                // Cut along Y. The split line sits somewhere in the middle third.
                int min = node.Rect.Y + _minLeafSize;
                int max = node.Rect.Bottom - _minLeafSize;
                if (min >= max) return;
                int cut = _rng.Next(min, max);

                node.Left  = new Node { Rect = new TileRect(node.Rect.X, node.Rect.Y, node.Rect.Width, cut - node.Rect.Y) };
                node.Right = new Node { Rect = new TileRect(node.Rect.X, cut, node.Rect.Width, node.Rect.Bottom - cut) };
            }
            else
            {
                // Cut along X.
                int min = node.Rect.X + _minLeafSize;
                int max = node.Rect.Right - _minLeafSize;
                if (min >= max) return;
                int cut = _rng.Next(min, max);

                node.Left  = new Node { Rect = new TileRect(node.Rect.X, node.Rect.Y, cut - node.Rect.X, node.Rect.Height) };
                node.Right = new Node { Rect = new TileRect(cut, node.Rect.Y, node.Rect.Right - cut, node.Rect.Height) };
            }

            Split(node.Left);
            Split(node.Right);
        }

        /// <summary>Collects all leaf nodes from the tree.</summary>
        internal static List<Node> GetLeaves(Node root)
        {
            var leaves = new List<Node>();
            Collect(root, leaves);
            return leaves;
        }

        private static void Collect(Node? node, List<Node> leaves)
        {
            if (node == null) return;
            if (node.IsLeaf) { leaves.Add(node); return; }
            Collect(node.Left,  leaves);
            Collect(node.Right, leaves);
        }
    }
}
