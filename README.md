# FloorMaps

A deterministic, seed-based dungeon floor map generator for 2D games. Produces a tile grid and a room graph in one call — both are available so you can drive rendering and entity spawning from the same data.

```
   ······                ·········   ·········
   ···0··                ·········+++·········
   ······    ·····+++++++····3····+++·········
   ······    ·····+++++++·········   ····6····
   ++++++++++·····+++++++·········   ·········
   ++++++++++··2··                   ·········
 ······++++++········   ·········
 ···1··++++++········+++·········              ··········
 ······++++++········+++····5····    ·······+++··········
 ······      ········   ·········++++·······+++·····8····
```

## Features

- **BSP room placement** — Binary Space Partitioning guarantees non-overlapping rooms distributed across the whole map
- **Configurable culling** — removes rooms below a median-area threshold and slivers above a max aspect ratio
- **Delaunay triangulation → MST → loops** — all rooms are connected, with a configurable fraction of extra edges added back for cycles
- **Face-inset hallway routing** — L-shaped hallways attach to room wall faces, never spilling out of corners
- **Portal metadata** — each hallway–room junction produces a `Portal` with exact tile bounds and facing direction for doorway spawning
- **Room vs hallway distinction** — the tile grid distinguishes `RoomFloor` from `HallwayFloor` for entity spawning rules
- **Both tile grid and room graph** — `FloorMap.Tiles[x,y]` for rendering, `FloorMap.Rooms` / `FloorMap.Hallways` / `FloorMap.Portals` for game logic
- **Fully deterministic** — same seed always produces the same map
- **Zero dependencies** — targets `netstandard2.1`, works in Unity, Godot, MonoGame, or any .NET project

## Getting started

```csharp
var map = new FloorMapGenerator().Generate(new FloorMapConfig
{
    Width  = 80,
    Height = 80,
    Seed   = 42,
});

// Tile grid — index by [x, y]
for (int x = 0; x < map.Bounds.Width;  x++)
for (int y = 0; y < map.Bounds.Height; y++)
{
    TileType tile = map.GetTile(x, y);
    // TileType.Empty | TileType.RoomFloor | TileType.HallwayFloor
}

// Room graph
foreach (Room room in map.Rooms)
{
    // room.Bounds   — TileRect (X, Y, Width, Height)
    // room.Type     — RoomType.Normal (extensible)
    // room.Connections — adjacent hallways
}

foreach (Hallway hallway in map.Hallways)
{
    // hallway.RoomA / hallway.RoomB
    // hallway.Segments — List<TileRect> of axis-aligned passage rects
    // hallway.Portals  — the two doorways connecting this hallway to its rooms
}

// Portals — one per hallway-room junction (always 2 per hallway)
foreach (Portal portal in map.Portals)
{
    // portal.Room    — the room this doorway belongs to
    // portal.Hallway — the hallway on the other side
    // portal.Bounds  — TileRect, 1 tile deep on the room wall face
    // portal.Facing  — CardinalDirection the hallway faces into the room
    //                  (North/South/East/West)
}
```

## Configuration

| Property | Default | Description |
|---|---|---|
| `Shape` | `Square` | `Square` or `Circle` bounding area |
| `Width` / `Height` | `80` | Map dimensions in tiles. `Height` is ignored for `Circle` |
| `MinLeafSize` | `10` | BSP stops splitting a partition below this size |
| `EmptyLeafChance` | `0.1` | Probability [0–1] a BSP leaf produces no room |
| `CullRatio` | `0.45` | Rooms with area below `CullRatio × median` are removed |
| `MaxAspectRatio` | `3.0` | Rooms longer than this multiple of their shorter side are removed as slivers |
| `MinHallwayWidth` | `2` | Minimum hallway passage width in tiles |
| `MaxHallwayWidth` | `4` | Maximum hallway passage width in tiles |
| `MinPortalWidth` | `1` | Minimum doorway width in tiles |
| `MaxPortalWidth` | `2` | Maximum doorway width in tiles (capped at hallway width) |
| `LoopFactor` | `0.15` | Fraction of non-MST Delaunay edges added back to create cycles |
| `Seed` | `null` | RNG seed. `null` picks a random seed each call |

## Coordinate system

All coordinates are **integer tile units**. `(0, 0)` is the top-left corner of the map. Multiply by your tile size (e.g. `32`) to get world-space positions in Unity (`Vector3Int`) or Godot (`Vector2I`).

## Generation pipeline

```
BSP split
  └─ Partition the bounding area recursively, alternating H/V cuts
     until leaves fall below MinLeafSize

Room placement
  └─ Place one random-sized rect in each leaf (1-tile padding),
     skip leaves with probability EmptyLeafChance

Shape culling  [Circle only]
  └─ Drop rooms with any corner outside the circular boundary

Size / sliver culling
  └─ Drop rooms below (CullRatio × median area)
     Drop rooms with AspectRatio > MaxAspectRatio

Graph
  └─ Delaunay triangulation on room centres
     → Kruskal MST  (guarantees full connectivity)
     → Re-add LoopFactor fraction of discarded edges (creates cycles)

Hallway routing
  └─ Per edge, try H-then-V and V-then-H L-shapes
     Connection points are clamped so hallway width fits within the room face
     Accept the first orientation that doesn't overlap a third room
     Skip the edge if both overlap (MST keeps the graph connected)

Portal derivation
  └─ Scan each hallway's actual segment geometry for adjacency to / crossing
     through each connected room's four faces
     Place a 1-tile-deep Portal rect centred on the overlap, with facing
     direction pointing from hallway into room

Rasterize
  └─ Stamp hallway segments as HallwayFloor, then rooms as RoomFloor
     (room tiles are dominant at junctions)
```

## Project structure

```
FloorMaps.sln
├── src/FloorMaps/                   # Library (netstandard2.1)
│   ├── Types/                       # BoundingShape, TileType, RoomType, TileRect,
│   │                                #   CardinalDirection
│   ├── Model/                       # Room, Hallway, Portal, FloorMap
│   ├── Config/                      # FloorMapConfig
│   ├── Internal/                    # BspTree, RoomPlacer, RoomCuller,
│   │                                #   GraphBuilder, HallwayRouter, TileRasterizer
│   └── FloorMapGenerator.cs         # Public entry point
├── tests/FloorMaps.Tests/           # xUnit test suite
└── examples/
    ├── FloorMaps.Examples.Console/  # ASCII renderer (zero extra deps)
    └── FloorMaps.Examples.Raylib/   # Windowed tile renderer (Raylib-cs)
```

## Running the examples

**Console** — renders the map as ASCII art in the terminal:

```bash
dotnet run --project examples/FloorMaps.Examples.Console
# optional args: <seed> <width> <height> <square|circle>
dotnet run --project examples/FloorMaps.Examples.Console -- 42 80 40 circle
```

Glyph key: `·` room floor &nbsp;|&nbsp; `+` hallway &nbsp;|&nbsp; `|` / `─` portal &nbsp;|&nbsp; ` ` empty &nbsp;|&nbsp; yellow numbers = room IDs

**Raylib** — opens an interactive window:

```bash
dotnet run --project examples/FloorMaps.Examples.Raylib
```

| Key | Action |
|---|---|
| `SPACE` | Generate a new map with a fresh random seed |
| `R` | Regenerate with the same seed (verify determinism) |
| `ESC` | Quit |

Colour key: parchment = room floor &nbsp;|&nbsp; slate blue = hallway &nbsp;|&nbsp; burnt orange = portal &nbsp;|&nbsp; near-black = empty

## Running the tests

```bash
dotnet test
```

26 tests covering: determinism, tile grid dimensions, room non-overlap, aspect ratio culling, hallway-room connection integrity, hallway segments not overlapping third rooms, circle boundary containment, config validation, and portal correctness (count, bounds, facing direction, width range).
