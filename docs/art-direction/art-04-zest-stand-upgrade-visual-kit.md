# ART-04 — Zest Stand & Upgrade Visual Kit

> Runtime-source and approval claims are superseded by
> [Current runtime visual contract](current-runtime-visual-contract.md).

## Result

The Zest stand is now an operational world object rather than a fixed backdrop.
Its 170×136 final-grid footprint remains stable while the active texture changes
to one of four physical configurations:

| Visual state | World-readable change |
| --- | --- |
| Base | Original jar, counter, cups, and lemon crate |
| Better Counter | Wider polished honey-oak counter, brass caps, organized trays |
| Electric Juicer | Brass-and-cream powered citrus press, orange half, green power lamp, pitcher |
| Bigger Cooler | Mint-and-cream insulated chest, bottled stock, ice sparkle, compact lemon crate |

`ZestStandVisual.SetPresentation` is the presentation boundary. It accepts the
active upgrade, operating state, and prepared-batch count without making Godot
nodes authoritative domain objects. Switching an upgrade changes the sprite;
it never exists only as a stat or label.

## Operating-state kit

- `SOLD OUT` is a rust hanging plaque driven by zero/disabled Classic stock.
- `RUSH MENU` is a citrus hanging plaque driven by the authoritative rush-menu set.
- `PREP +1` through `PREP +3` replace the ambiguous tiny-jar shorthand and report
  live prepared-batch count explicitly.
- The plaque sways by whole world pixels and the prepared stock cue twinkles on a
  stepped cadence, preserving nearest-neighbor motion and the animation-first rule.

World labels use the native `PixelWorldText` 5×7 bitmap alphabet. Each letter is
drawn directly on the 640×360 grid with a one-pixel shadow and an opaque contrast
plate where needed. No anti-aliased six-point UI font is rasterized into the world,
and fixed-width status plaques are sized from the longest supported message so
`SOLD OUT` and `RUSH MENU` cannot be clipped.

The existing product stock label remains a secondary exact-number readout. The
stand prop carries the glanceable state in the world.

## Source and runtime assets

AI-generated originals are retained only as concept references under
`art-source/generative/art04/` and `art-source/concepts/art04/`. They are not
published, scaled, sampled, or loaded by Godot.

The deterministic `NativeStandKit` generator in `tools/Zest.AssetPipeline/`
authors every shipping pixel directly on a transparent 170×136 canvas. It uses
only the checked-in 24-color Zest palette, hard 1–2 pixel outlines, deliberate
rectangular clusters, and no resampling stage. It publishes four source/runtime pairs:

- `prop_zest_stand_base_idle_v03.png`
- `prop_zest_stand_better_counter_idle_v02.png`
- `prop_zest_stand_electric_juicer_idle_v02.png`
- `prop_zest_stand_bigger_cooler_idle_v02.png`

Source masters live under `art-source/production/final-grid-v03/` and byte-identical
runtime copies under `game/art/production/final-grid-v03/`. All variants
use the base stand's 170×136 frame, foot pivot, warm citrus/cream/wood/leaf color
family, and scale 1 in the 640×360 pixel world.

Godot loads `prop_zest_stand_upgrades_4x1_v01.png`, a 680×136 horizontal sprite
atlas containing Base, Better Counter, Electric Juicer, and Bigger Cooler in that
order. Upgrade state selects a 170×136 atlas region; runtime drawing code does not
assemble the shop from rectangles or swap unrelated texture resources.

## Visual evidence

- `docs/artifacts/art-04-better-counter.png`
- `docs/artifacts/art-04-electric-juicer-rush-menu.png`
- `docs/artifacts/art-04-bigger-cooler-sold-out.png`

Each capture is a 1920×1080 native desktop frame with the 640×360 world rendered
at integer 3× scale.

## Production record

Mode: deterministic code-authored RGBA pixel raster via `Zest.AssetPipeline stand-kit`.

The former AI edits remain useful composition references only. They were rejected
as shipping art because reducing a 1402×1122 illustration to 170×136 retained
anti-aliased micro-detail as noisy clusters. No image-generation prompt contributes
pixels to the v03 runtime kit.
