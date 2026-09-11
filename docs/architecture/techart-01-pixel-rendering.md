# TECHART-01 — Pixel Rendering Foundation ADR

> Runtime-source and scaling claims are superseded by
> [Current runtime visual contract](../art-direction/current-runtime-visual-contract.md).

Status: Accepted  
Target: Godot 4.7 .NET, Windows/Steam  
Decision date: 2026-09-09

## Decision

The application and UI design baseline is **1920×1080**. Window stretch uses
`canvas_items` and keeps aspect ratio so native UI can adapt to desktop output.
The pixel world is isolated in `ParkWorldView`'s fixed **640×360**
`SubViewport`; `StretchShrink = 3` presents it at an exact 3× scale at the
1080p baseline. The surrounding Godot Control tree remains at application
resolution for readable text and controls.

Canvas textures default to nearest filtering and nearest-mipmap filtering is
disabled. World presentation explicitly inherits nearest sampling. The UI root
explicitly selects linear filtering, so future illustrations can remain smooth
without softening world sprites or tiles. Imported pixel sources must also use
lossless compression and no generated mipmaps; see `game/art/README.md`.

Godot's 2D transform and vertex pixel snapping are enabled. Authored camera
targets and pedestrian movement are additionally quantized to 1/16 world unit.
Camera behavior is limited to authored ART-01 framings. Overview/business use
1× logical zoom and close observation uses exact 2×; there is no continuous
fractional zoom or rotation that could produce unstable resampling.

## Sorting and depth policy

1. Static ground and paths occupy the lowest layer.
2. Props and buildings use real world depth; opaque geometry never relies on
   manual draw order.
3. Future 2D character/prop layers live under a `Node2D` with Y-sort enabled;
   feet/pivot points define ordering, never sprite centers.
4. Shadows render immediately below their owning actor. Overhead markers and
   thought feedback occupy a dedicated world-overlay layer.
5. Native UI remains outside the world SubViewport and can never participate in
   world Y-sort or depth.

## Resize contract

- 1920×1080 is the authored UI/output baseline; 1280×720 remains supported.
- The 640×360 world maps to 2× at 720p, 3× at 1080p, 4× at 1440p, and 6× at 4K.
- Root UI scaling may be fractional because fonts and Controls render at the
  target canvas; the world itself must land on an integer multiple at the
  supported 16:9 presets.
- Non-standard windows preserve aspect. A later settings pass will letterbox
  the world rather than apply an uneven nearest-neighbor scale.
- UI anchors and containers reflow inside the baseline canvas. World and UI are
  never baked into the same texture.
- World sprites are authored at their final 640×360-grid size and use runtime
  `Scale = 1`; fractional sprite scale is forbidden.

## Animation contract

- Characters use equal-cell directional sheets: south, west, east, north.
- Props are instantiated through animation-ready `AnimatedSprite2D` actors,
  including one-frame idle assets, so new frames never require scene rewrites.
- The first environmental proof is a four-frame wind bush with a fixed base.
- All motion is frame-based or integer-snapped. Sub-pixel swaying and transform
  tweening are rejected because they destabilize pixel edges.

## Rejected alternatives

- A single low-resolution viewport for both world and UI: rejected because body
  text and dense operational controls become coarse.
- Fractional stretch to fill every window: rejected because it introduces blur,
  shimmer, and inconsistent sprite density.
- Free camera zoom/rotation: rejected because it defeats authored pixel scale
  and queue readability.

## Verification

`tools/validate-pixel-foundation.ps1` checks the committed project settings and
the two explicit world/UI filter boundaries. Release build and Godot headless
startup remain required after rendering changes. Visual QA should cover
1280×720, 1920×1080, 2560×1440, and 3840×2160, slow camera pan, all three framing presets, and an
eight-person queue.
