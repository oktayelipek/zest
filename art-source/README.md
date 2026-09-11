# Zest pixel source workspace

This folder is the editable/source side of ART-10. Nothing here is loaded by the
shipping Godot project. Runtime PNGs live under `game/art/pixel/` and are created
only by the asset pipeline.

## Contractor workflow

1. Read `docs/art-direction/art-10-pixel-production-pipeline.md`.
2. Copy the matching canvas template dimensions from `pipeline.json`.
3. Work on the 16 px source grid using `palette/zest-warm-pixel.gpl`.
4. Export an RGBA PNG to `art-source/exports/` using the manifest filename.
5. Run `dotnet run --project tools/Zest.AssetPipeline -- publish`.
6. Open Godot once so it creates or refreshes `<asset>.png.import` metadata.
7. Run `tools/validate-art-pipeline.ps1` before review.

The tool never silently rescales or recolors work. A bad canvas, off-palette
pixel, invalid frame sheet, or wrong filename fails publishing with a precise
error.
