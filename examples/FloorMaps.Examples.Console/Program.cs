using System;
using System.Collections.Generic;
using FloorMaps;

// ── Config from args or defaults ──────────────────────────────────────────────
int    seed      = args.Length > 0 && int.TryParse(args[0], out int s) ? s : 42;
int    width     = args.Length > 1 && int.TryParse(args[1], out int w) ? w : 80;
int    height    = args.Length > 2 && int.TryParse(args[2], out int h) ? h : 40;
string shapeArg  = args.Length > 3 ? args[3].ToLowerInvariant() : "square";
var    shape     = shapeArg == "circle" ? BoundingShape.Circle : BoundingShape.Square;

var config = new FloorMapConfig
{
    Seed   = seed,
    Width  = width,
    Height = height,
    Shape  = shape,
};

// ── Generate ──────────────────────────────────────────────────────────────────
var map = new FloorMapGenerator().Generate(config);

// ── Build overlays ────────────────────────────────────────────────────────────
// Room-centre labels.
var labels = new string[width, height];
foreach (var room in map.Rooms)
{
    string id = room.Id.ToString();
    int    cx = room.Bounds.CenterX;
    int    cy = room.Bounds.CenterY;
    for (int i = 0; i < id.Length; i++)
    {
        int x = cx - id.Length / 2 + i;
        if (x >= room.Bounds.X && x < room.Bounds.Right)
            labels[x, cy] = id[i].ToString();
    }
}

// Portal tile positions  →  '|' (vertical face) or '─' (horizontal face).
var portalGlyphs = new Dictionary<(int x, int y), char>();
foreach (var portal in map.Portals)
{
    // Vertical-face portals (East/West facing): tall, 1 col wide → '|'
    // Horizontal-face portals (North/South facing): wide, 1 row tall → '─'
    char glyph = (portal.Facing == CardinalDirection.East ||
                  portal.Facing == CardinalDirection.West) ? '|' : '\u2500'; // '─'

    for (int px = portal.Bounds.X; px < portal.Bounds.Right;  px++)
    for (int py = portal.Bounds.Y; py < portal.Bounds.Bottom; py++)
        portalGlyphs[(px, py)] = glyph;
}

// ── Render ────────────────────────────────────────────────────────────────────
// Glyph key:
//   ' '  empty
//   '·'  room floor (white)    label = room ID (yellow)
//   '+'  hallway floor (cyan)
//   '|'  portal on left/right wall (magenta)
//   '─'  portal on top/bottom wall (magenta)

Console.OutputEncoding = System.Text.Encoding.UTF8;

for (int y = 0; y < height; y++)
{
    for (int x = 0; x < width; x++)
    {
        var tile = map.GetTile(x, y);
        string lbl = labels[x, y];

        if (portalGlyphs.TryGetValue((x, y), out char pg))
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write(pg);
            continue;
        }

        switch (tile)
        {
            case TileType.RoomFloor:
                Console.ForegroundColor = lbl != null ? ConsoleColor.Yellow : ConsoleColor.White;
                Console.Write(lbl ?? "·");
                break;

            case TileType.HallwayFloor:
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write('+');
                break;

            default:
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(' ');
                break;
        }
    }
    Console.WriteLine();
}

Console.ResetColor();

// ── Stats ─────────────────────────────────────────────────────────────────────
Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine();
Console.WriteLine($"  seed={map.Seed}  shape={shape}  size={width}x{height}");
Console.WriteLine($"  rooms={map.Rooms.Count}  hallways={map.Hallways.Count}  portals={map.Portals.Count}");
Console.ResetColor();
