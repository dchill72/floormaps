using System;

namespace FloorMaps
{
    /// <summary>
    /// An axis-aligned rectangle in integer tile coordinates.
    /// X, Y are the top-left corner. Width and Height are in tiles.
    /// </summary>
    public readonly struct TileRect : IEquatable<TileRect>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Width;
        public readonly int Height;

        public int Right  => X + Width;
        public int Bottom => Y + Height;
        public int Area   => Width * Height;
        public float AspectRatio => Width >= Height
            ? (float)Width / Height
            : (float)Height / Width;

        public int CenterX => X + Width  / 2;
        public int CenterY => Y + Height / 2;

        public TileRect(int x, int y, int width, int height)
        {
            if (width  < 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));
            X = x; Y = y; Width = width; Height = height;
        }

        public bool Overlaps(TileRect other) =>
            X < other.Right && Right > other.X &&
            Y < other.Bottom && Bottom > other.Y;

        public bool Contains(int x, int y) =>
            x >= X && x < Right && y >= Y && y < Bottom;

        public bool Equals(TileRect other) =>
            X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;

        public override bool Equals(object obj) => obj is TileRect r && Equals(r);
        public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);
        public static bool operator ==(TileRect a, TileRect b) => a.Equals(b);
        public static bool operator !=(TileRect a, TileRect b) => !a.Equals(b);
        public override string ToString() => $"TileRect({X},{Y} {Width}x{Height})";
    }
}
