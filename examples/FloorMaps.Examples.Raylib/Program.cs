using System;
using Raylib_cs;
using FloorMaps;

// ── Layout constants ──────────────────────────────────────────────────────────
const int TileSize  = 10;
const int MapWidth  = 80;
const int MapHeight = 80;
const int StatusBar = 50;

int screenW = MapWidth  * TileSize;
int screenH = MapHeight * TileSize + StatusBar;

// ── Colours ───────────────────────────────────────────────────────────────────
var ColEmpty   = new Color( 18,  18,  24, 255);   // near-black
var ColRoom    = new Color(196, 174, 127, 255);   // warm parchment
var ColHallway = new Color( 88, 130, 160, 255);   // slate blue
var ColPortal  = new Color(210, 120,  50, 255);   // burnt orange — doorway
var ColWall    = new Color( 40,  40,  52, 255);   // inset room border
var ColRoomId  = new Color(255, 220,  80, 255);   // yellow room label
var ColStatus  = new Color(180, 180, 180, 255);
var ColKey     = new Color(120, 120, 120, 255);

// ── Initial generation ────────────────────────────────────────────────────────
int seed = new Random().Next();
FloorMap map = GenerateMap(seed);

Raylib.InitWindow(screenW, screenH, "FloorMaps — dungeon generator");
Raylib.SetTargetFPS(60);

while (!Raylib.WindowShouldClose())
{
    // ── Input ─────────────────────────────────────────────────────────────────
    if (Raylib.IsKeyPressed(KeyboardKey.Space))
    {
        seed = new Random().Next();
        map  = GenerateMap(seed);
    }
    if (Raylib.IsKeyPressed(KeyboardKey.R))
        map = GenerateMap(seed);

    // ── Draw ──────────────────────────────────────────────────────────────────
    Raylib.BeginDrawing();
    Raylib.ClearBackground(ColEmpty);

    // Tile grid
    for (int x = 0; x < MapWidth;  x++)
    for (int y = 0; y < MapHeight; y++)
    {
        var tile = map.GetTile(x, y);
        if (tile == TileType.Empty) continue;

        Color col = tile == TileType.RoomFloor ? ColRoom : ColHallway;
        Raylib.DrawRectangle(x * TileSize, y * TileSize, TileSize, TileSize, col);
    }

    // Portals — drawn over room tiles on the room face, burnt-orange strip
    foreach (var portal in map.Portals)
    {
        Raylib.DrawRectangle(
            portal.Bounds.X      * TileSize,
            portal.Bounds.Y      * TileSize,
            portal.Bounds.Width  * TileSize,
            portal.Bounds.Height * TileSize,
            ColPortal);
    }

    // Room inset borders
    foreach (var room in map.Rooms)
    {
        int rx = room.Bounds.X      * TileSize;
        int ry = room.Bounds.Y      * TileSize;
        int rw = room.Bounds.Width  * TileSize;
        int rh = room.Bounds.Height * TileSize;
        Raylib.DrawRectangleLinesEx(new Rectangle(rx, ry, rw, rh), 1, ColWall);
    }

    // Room ID labels
    foreach (var room in map.Rooms)
    {
        string label    = room.Id.ToString();
        int    fontSize = TileSize - 2;
        int    tw       = Raylib.MeasureText(label, fontSize);
        int    lx       = room.Bounds.CenterX * TileSize - tw / 2;
        int    ly       = room.Bounds.CenterY * TileSize - fontSize / 2;
        Raylib.DrawText(label, lx, ly, fontSize, ColRoomId);
    }

    // Status bar
    int barY = MapHeight * TileSize + 8;
    string stats = $"seed: {map.Seed}   rooms: {map.Rooms.Count}   hallways: {map.Hallways.Count}   portals: {map.Portals.Count}";
    Raylib.DrawText(stats, 10, barY, 14, ColStatus);

    string keys = "SPACE = new map    R = regenerate same seed    ESC = quit";
    int keysX = screenW - Raylib.MeasureText(keys, 12) - 10;
    Raylib.DrawText(keys, keysX, barY + 2, 12, ColKey);

    Raylib.EndDrawing();
}

Raylib.CloseWindow();

static FloorMap GenerateMap(int seed) =>
    new FloorMapGenerator().Generate(new FloorMapConfig
    {
        Width  = 80,
        Height = 80,
        Seed   = seed,
    });
