# Zest pixel asset import contract

- Author world tiles and props on a 16 px source grid.
- Use PNG with transparency; do not pre-scale source files.
- Publish through `tools/Zest.AssetPipeline`; do not copy exports into runtime
  folders by hand.
- Import with nearest filtering, mipmaps off, and lossless compression.
- Put world sprites and tiles under `art/pixel/`; keep native-resolution UI
  illustration under `ui/` so the render boundary is obvious.
- Character pivots sit at the feet. Tile origins and collision shapes use whole
  source pixels.
- Palette additions require an ART-02 token decision; near-duplicate colors are
  not added ad hoc.

Project defaults already choose nearest canvas sampling. Commit every generated
`.import` sidecar; `tools/validate-art-pipeline.ps1` enforces the runtime preset.
The full contractor-facing contract lives in
`docs/art-direction/art-10-pixel-production-pipeline.md`.
