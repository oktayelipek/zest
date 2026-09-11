# ART-10 — Pixel Tile & Sprite Production Pipeline

> This remains the native-source pipeline. Active runtime-source and scaling claims
> are superseded by [Current runtime visual contract](current-runtime-visual-contract.md).

## Production contract

All shipping world pixels use the ART-02 palette and TECHART-01 renderer. The
pipeline is manifest-driven: `art-source/pipeline.json` owns canvas, frame grid,
pivot, source path, and runtime path. The publisher rejects invalid work rather
than silently resizing, recoloring, or moving pivots.

## Canvas and density

The 16 px value is the **world placement grid**, not a permanent cap on source
detail. ART-12's approved high-density tier uses 2× source art for the playable
world while retaining integer placement and nearest sampling.

| Asset | Canvas/frame | Sheet | Typical footprint |
| --- | --- | --- | --- |
| Prototype tile | 16×16 | 1×1 or documented variants | 1 world tile |
| Production tile | 32×32 | 1×1 or documented variants | 1 world tile |
| Small prop | 32×32 or 64×64 | state frames horizontally | 1–2 tiles |
| Production character | 32×48 | 4 columns × 4 rows | one standing actor |
| Hero stand | about 256×192 | one composition per PNG | 8–12 tiles |

Do not upscale exports. Godot/TECHART-01 owns integer presentation scaling.
Pixel clusters should describe silhouette first; isolated single-pixel noise is
reserved for texture accents and never used along the primary silhouette.

## Palette

The canonical machine-readable colors are in `art-source/pipeline.json`; the
artist palette is `art-source/palette/zest-warm-pixel.gpl`. Full-alpha pixels
must match one of those colors exactly. Transparent pixels may carry any RGB,
although exporters should clear them to zero. New colors require an ART-02
palette decision before they enter the manifest.

## Naming and versions

Runtime filenames are lowercase ASCII:

`<kind>_<subject>_<variant>_<action>_vNN.png`

- Kind: `tile`, `prop`, or `chr`.
- Subject/variant/action: stable semantic words, never artist initials or dates.
- Version: two digits. Increment only for an intentional incompatible visual or
  frame-layout change; source iterations remain in the art tool's history.

Examples: `tile_grass_base_idle_v01.png`,
`prop_lemon_crate_idle_v01.png`, `chr_guest_base_walk_v01.png`.

## Pivots, origins, and depth

- Every pivot is specified in source pixels in the manifest.
- Tiles use bottom-center `(width / 2, height)`.
- Props and characters use a bottom-center foot/contact pivot within the final
  four rows of the frame.
- Empty transparent padding is allowed only when required to keep a stable
  pivot across states.
- Godot actors sort from that foot pivot. Sprite center is never used for
  Y-sort; shadows sit directly below the same contact point.

## Animation sheets

Characters use 16×24 frames in a 4×4 sheet. Rows are fixed:

1. south
2. west
3. east
4. north

Columns are walk frames in chronological order at 8 fps and loop. Mirroring is
not used to synthesize east/west because Zest props and carried items may be
asymmetric. Idle or behavioral sets use separate files and preserve the same
row order, frame size, and pivot.

## Source and runtime boundary

- `art-source/`: editable exports, palettes, and manifest; never loaded by game.
- `game/art/pixel/`: published PNGs and runtime catalog; only pipeline output.
- `game/.godot/imported/`: disposable Godot cache; never source-controlled.
- `<asset>.png.import`: committed Godot import metadata generated on editor scan.

The publisher copies bytes only after validation and emits
`game/art/pixel/art10-catalog.json` for runtime tooling.

## Commands

```text
dotnet run --project tools/Zest.AssetPipeline -- samples
dotnet run --project tools/Zest.AssetPipeline -- publish
dotnet run --project tools/Zest.AssetPipeline -- validate
```

`samples --force` regenerates the three pipeline proof assets. Do not use
`--force` on contractor exports unless replacement is intentional. The normal
handoff is export → publish → open Godot once → run
`tools/validate-art-pipeline.ps1`.

## Godot import preset

- Texture importer
- lossless compression (`compress/mode=0`)
- mipmaps disabled (`mipmaps/generate=false`)
- no 3D-detection conversion for 2D sprite/tile sources
- project/CanvasItem nearest filtering from TECHART-01

This follows Godot 4's separation: compression/mipmap decisions are import
metadata, while filtering belongs to the CanvasItem/material or project
default. Generated `.import` files are validated after editor scan.

## Definition of done for a new asset

The source file passes dimensions, sheet, palette, naming, and pivot validation;
publishing creates the expected runtime path and catalog entry; Godot imports it
without changing lossless/no-mipmap rules; nearest-render visual QA shows no
blur, edge fringe, pivot hop, or Y-sort foot drift.
